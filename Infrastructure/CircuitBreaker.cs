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
    /// <summary>
    /// Whether the circuit breaker is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Number of consecutive failures before opening the circuit
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration to keep the circuit open before transitioning to half-open state
    /// </summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Number of test requests to allow in half-open state
    /// </summary>
    public int HalfOpenTestRequests { get; set; } = 2;

    /// <summary>
    /// Duration to wait before transitioning from half-open to closed state after success
    /// </summary>
    public TimeSpan HalfOpenSuccessDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Duration to wait before transitioning from half-open to open state after failure
    /// </summary>
    public TimeSpan HalfOpenFailureDuration { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Circuit breaker for protecting downstream message brokers from overload
/// Implements the Closed/Open/Half-Open pattern to prevent hammering a downed broker
/// </summary>
public sealed class CircuitBreaker : IDisposable
{
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
        _failureCount = 0;
        _openedAt = DateTime.MinValue;
        _lastTestAt = DateTime.MinValue;
        _testSuccessCount = 0;
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
                _failureCount = 0;
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
            _failureCount = 0;
            _openedAt = DateTime.MinValue;
            _testSuccessCount = 0;
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
                _testSuccessCount = 0;
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
        _logger?.LogDebug("Circuit breaker blocked operation - circuit is open");
    }

    private void LogFailure(Exception ex)
    {
        _logger?.LogDebug(ex, "Circuit breaker recorded failure");
    }

    private void LogCircuitOpened(Exception ex)
    {
        _logger?.LogWarning(ex, "Circuit breaker opened after {FailureCount} failures", _failureCount);
    }

    private void LogCircuitClosed()
    {
        _logger?.LogInformation("Circuit breaker closed - downstream service recovered");
    }

    private void LogCircuitTransitionToHalfOpen()
    {
        _logger?.LogInformation("Circuit breaker transitioning to half-open state for testing");
    }

    private void LogHalfOpenSuccess()
    {
        _logger?.LogInformation("Circuit breaker test request succeeded in half-open state");
    }

    private void LogHalfOpenFailure()
    {
        _logger?.LogWarning("Circuit breaker test request failed in half-open state - reopening circuit");
    }

    private void LogCircuitForcedHalfOpen()
    {
        _logger?.LogInformation("Circuit breaker forced into half-open state for testing");
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
}
