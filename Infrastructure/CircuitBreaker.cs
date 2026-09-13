#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Configuration options for the circuit breaker
/// </summary>
public sealed class CircuitBreakerOptions
{
    private const bool DefaultEnabled = true;
    private const int DefaultFailureThreshold = 5;
    private const int DefaultHalfOpenTestRequests = 2;
    private const int DefaultOpenDurationMinutes = 1;
    private const int DefaultHalfOpenSuccessDurationSeconds = 30;
    private const int DefaultHalfOpenFailureDurationSeconds = 10;
    private static readonly TimeSpan DefaultOpenDuration = TimeSpan.FromMinutes(DefaultOpenDurationMinutes);
    private static readonly TimeSpan DefaultHalfOpenSuccessDuration =
        TimeSpan.FromSeconds(DefaultHalfOpenSuccessDurationSeconds);
    private static readonly TimeSpan DefaultHalfOpenFailureDuration =
        TimeSpan.FromSeconds(DefaultHalfOpenFailureDurationSeconds);

    /// <summary>
    /// Whether the circuit breaker is enabled
    /// </summary>
    public bool Enabled { get; set; } = DefaultEnabled;

    /// <summary>
    /// Number of consecutive failures before opening the circuit
    /// </summary>
    public int FailureThreshold { get; set; } = DefaultFailureThreshold;

    /// <summary>
    /// Duration to keep the circuit open before transitioning to half-open state
    /// </summary>
    public TimeSpan OpenDuration { get; set; } = DefaultOpenDuration;

    /// <summary>
    /// Number of test requests to allow in half-open state
    /// </summary>
    public int HalfOpenTestRequests { get; set; } = DefaultHalfOpenTestRequests;

    /// <summary>
    /// Duration to wait before transitioning from half-open to closed state after success
    /// </summary>
    public TimeSpan HalfOpenSuccessDuration { get; set; } = DefaultHalfOpenSuccessDuration;

    /// <summary>
    /// Duration to wait before transitioning from half-open to open state after failure
    /// </summary>
    public TimeSpan HalfOpenFailureDuration { get; set; } = DefaultHalfOpenFailureDuration;
}

/// <summary>
/// Circuit breaker for protecting downstream message brokers from overload
/// Implements the Closed/Open/Half-Open pattern to prevent hammering a downed broker
/// </summary>
public sealed class CircuitBreaker : IDisposable
{
    private const int InitialCount = 0;
    private const string CircuitBlockedLogMessage = "Circuit breaker blocked operation - circuit is open";
    private const string FailureRecordedLogMessage = "Circuit breaker recorded failure";
    private const string CircuitOpenedLogMessage = "Circuit breaker opened after {FailureCount} failures";
    private const string CircuitClosedLogMessage = "Circuit breaker closed - downstream service recovered";
    private const string HalfOpenTransitionLogMessage = "Circuit breaker transitioning to half-open state for testing";
    private const string HalfOpenSuccessLogMessage = "Circuit breaker test request succeeded in half-open state";
    private const string HalfOpenFailureLogMessage = "Circuit breaker test request failed in half-open state - reopening circuit";
    private const string ForcedHalfOpenLogMessage = "Circuit breaker forced into half-open state for testing";
    private static readonly DateTime UninitializedTimestamp = DateTime.MinValue;

    private readonly CircuitBreakerOptions _options;
    private readonly ILogger? _logger;
    private readonly object _lock = new();
    private CircuitState _state;
    private int _failureCount;
    private DateTime _openedAt;
    private DateTime _lastTestAt;
    private int _testSuccessCount;
    private bool _disposed;

    /// <summary>
    /// Current state of the circuit
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                return GetCurrentState();
            }
        }
    }

    /// <summary>
    /// Whether the circuit is currently allowing operations
    /// </summary>
    public bool IsAllowed
    {
        get
        {
            lock (_lock)
            {
                var state = GetCurrentState();
                return state == CircuitState.Closed || state == CircuitState.HalfOpen;
            }
        }
    }

    /// <summary>
    /// Current exception if the circuit is open (downstream failure)
    /// </summary>
    public Exception? LastException { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class
    /// </summary>
    /// <param name="options">Circuit breaker options</param>
    /// <param name="logger">Optional logger</param>
    public CircuitBreaker(CircuitBreakerOptions options, ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _state = CircuitState.Closed;
        _failureCount = InitialCount;
        _openedAt = UninitializedTimestamp;
        _lastTestAt = UninitializedTimestamp;
        _testSuccessCount = InitialCount;
    }

    /// <summary>
    /// Attempts to execute an action if the circuit allows it
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>True if the action was executed, false if the circuit is open</returns>
    public async Task<bool> ExecuteAsync(Func<Task> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (!IsAllowed)
        {
            LogCircuitBlocked();
            return false;
        }

        try
        {
            await action();
            RecordSuccess();
            return true;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            return false;
        }
    }

    /// <summary>
    /// Attempts to execute a function if the circuit allows it
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="func">The function to execute</param>
    /// <returns>The result if successful, default if the circuit is open</returns>
    public async Task<T?> ExecuteAsync<T>(Func<Task<T>> func)
    {
        if (func is null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (!IsAllowed)
        {
            LogCircuitBlocked();
            return default;
        }

        try
        {
            var result = await func();
            RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            return default;
        }
    }

    /// <summary>
    /// Records a successful operation
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitState.HalfOpen)
            {
                _testSuccessCount++;
                LogHalfOpenSuccess();

                // If we've had enough successes in half-open state, close the circuit
                if (_testSuccessCount >= _options.HalfOpenTestRequests)
                {
                    Reset();
                    LogCircuitClosed();
                }
            }
            else
            {
                // Reset failure count on success in closed state
                _failureCount = InitialCount;
            }
        }
    }

    /// <summary>
    /// Records a failed operation
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    public void RecordFailure(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        lock (_lock)
        {
            _failureCount++;
            LastException = exception;

            LogFailure(exception);

            // Check if we've exceeded the failure threshold
            if (_state == CircuitState.Closed && _failureCount >= _options.FailureThreshold)
            {
                Open();
                LogCircuitOpened(exception);
            }
            else if (_state == CircuitState.HalfOpen)
            {
                // Failed in half-open state - go back to open
                Open();
                LogHalfOpenFailure();
            }
        }
    }

    /// <summary>
    /// Resets the circuit breaker to closed state
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitState.Closed;
            _failureCount = InitialCount;
            _openedAt = UninitializedTimestamp;
            _testSuccessCount = InitialCount;
            LastException = null;
        }
    }

    /// <summary>
    /// Forces the circuit breaker into half-open state for testing
    /// </summary>
    public void ForceHalfOpen()
    {
        lock (_lock)
        {
            if (_state == CircuitState.Open)
            {
                _state = CircuitState.HalfOpen;
                _lastTestAt = DateTime.UtcNow;
                LogCircuitForcedHalfOpen();
            }
        }
    }

    /// <summary>
    /// Gets the current state, handling state transitions
    /// </summary>
    private CircuitState GetCurrentState()
    {
        if (!_options.Enabled)
        {
            return CircuitState.Closed;
        }

        if (_state == CircuitState.Open)
        {
            // Check if it's time to transition to half-open
            var elapsed = DateTime.UtcNow - _openedAt;
            if (elapsed >= _options.OpenDuration)
            {
                _state = CircuitState.HalfOpen;
                _lastTestAt = DateTime.UtcNow;
                _testSuccessCount = InitialCount;
                LogCircuitTransitionToHalfOpen();
            }
        }
        else if (_state == CircuitState.HalfOpen)
        {
            // In half-open state, we allow a limited number of test requests
            // After successful tests, we close the circuit
            // After failures, we re-open the circuit
        }

        return _state;
    }

    /// <summary>
    /// Opens the circuit
    /// </summary>
    private void Open()
    {
        _state = CircuitState.Open;
        _openedAt = DateTime.UtcNow;
    }

    private void LogCircuitBlocked()
    {
        _logger?.LogDebug(CircuitBlockedLogMessage);
    }

    private void LogFailure(Exception ex)
    {
        _logger?.LogDebug(ex, FailureRecordedLogMessage);
    }

    private void LogCircuitOpened(Exception ex)
    {
        _logger?.LogWarning(ex, CircuitOpenedLogMessage, _failureCount);
    }

    private void LogCircuitClosed()
    {
        _logger?.LogInformation(CircuitClosedLogMessage);
    }

    private void LogCircuitTransitionToHalfOpen()
    {
        _logger?.LogInformation(HalfOpenTransitionLogMessage);
    }

    private void LogHalfOpenSuccess()
    {
        _logger?.LogInformation(HalfOpenSuccessLogMessage);
    }

    private void LogHalfOpenFailure()
    {
        _logger?.LogWarning(HalfOpenFailureLogMessage);
    }

    private void LogCircuitForcedHalfOpen()
    {
        _logger?.LogInformation(ForcedHalfOpenLogMessage);
    }

    /// <summary>
    /// Disposes the circuit breaker
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns a string representation of the circuit breaker state
    /// </summary>
    /// <returns>Current state and failure count</returns>
    public override string ToString()
    {
        lock (_lock)
        {
            var state = GetCurrentState();
            return $"State: {state}, FailureCount: {_failureCount}";
        }
    }
}
