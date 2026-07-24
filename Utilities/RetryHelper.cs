#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DotnetOutboxPattern.Infrastructure;

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Helper utilities for retry policies and exponential backoff
/// Provides reusable retry logic for transient failures
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Executes an action with exponential backoff retry
    /// </summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="maxRetries">Maximum number of retries (default 5). Must be between 0 and 20 inclusive.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds (default 100). Must be between 0 and 30000 inclusive.</param>
    /// <param name="backoffMultiplier">Multiplier applied on each retry (default 2.0). Must be between 1.0 and 10.0 inclusive.</param>
    /// <returns>The result of the action.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialDelayMs"/> is negative or exceeds 30000.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="backoffMultiplier"/> is less than 1.0 or greater than 10.0.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative or exceeds 20.</exception>
    public static async Task<T> ExecuteWithExponentialBackoffAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 5,
        int initialDelayMs = 100,
        double backoffMultiplier = 2.0)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialDelayMs);
        ArgumentOutOfRangeException.ThrowIfLessThan(backoffMultiplier, 1.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(backoffMultiplier, 10.0);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetries);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxRetries, 20);

        // Cap initial delay to prevent excessive delays even with valid inputs
        const int MaxInitialDelayMs = 30000; // 30 seconds
        initialDelayMs = Math.Min(initialDelayMs, MaxInitialDelayMs);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                // Compute the delay using the shared exponential backoff logic.
                // The first retry corresponds to attempt = 1.
                var delayMs = (int)BackoffMath.ComputeExponentialDelay(
                    baseDelayMs: initialDelayMs,
                    maxDelayMs: int.MaxValue,
                    multiplier: backoffMultiplier,
                    attempt: attempt + 1);

                await Task.Delay(delayMs);
            }
        }

        // Final attempt without catching
        return await action();
    }

    /// <summary>
    /// Executes an action with fixed interval retry
    /// </summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="maxRetries">Maximum number of retries (default 5). Must be between 0 and 20 inclusive.</param>
    /// <param name="delayMs">Fixed delay in milliseconds between attempts (default 1000). Must be between 0 and 30000 inclusive.</param>
    /// <returns>The result of the action.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayMs"/> is negative or exceeds 30000.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative or exceeds 20.</exception>
    public static async Task<T> ExecuteWithFixedDelayAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 5,
        int delayMs = 1000)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delayMs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetries);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxRetries, 20);

        // Cap delay to prevent excessive delays
        const int MaxDelayMs = 30000; // 30 seconds
        delayMs = Math.Min(delayMs, MaxDelayMs);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                await Task.Delay(delayMs);
            }
        }

        return await action();
    }

    /// <summary>
    /// Executes an action with linear backoff retry
    /// </summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="maxRetries">Maximum number of retries (default 5). Must be between 0 and 20 inclusive.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds (default 100). Must be between 0 and 30000 inclusive.</param>
    /// <param name="delayIncrementMs">Increment added to delay on each attempt (default 100). Must be between 0 and 30000 inclusive.</param>
    /// <returns>The result of the action.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialDelayMs"/>, <paramref name="delayIncrementMs"/> are negative or exceed 30000.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative or exceeds 20.</exception>
    public static async Task<T> ExecuteWithLinearBackoffAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 5,
        int initialDelayMs = 100,
        int delayIncrementMs = 100)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialDelayMs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(delayIncrementMs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetries);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxRetries, 20);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialDelayMs, 30000);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(delayIncrementMs, 30000);

        // Cap initial delay and increment to prevent excessive delays
        const int MaxDelayMs = 30000; // 30 seconds
        initialDelayMs = Math.Min(initialDelayMs, MaxDelayMs);
        delayIncrementMs = Math.Min(delayIncrementMs, MaxDelayMs);

        int delay = initialDelayMs;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                await Task.Delay(delay);
                delay += delayIncrementMs;
                // Cap the delay to prevent overflow
                if (delay > MaxDelayMs)
                {
                    delay = MaxDelayMs;
                }
            }
        }

        return await action();
    }

    /// <summary>
    /// Executes an action with jittered backoff (exponential + random)
    /// Prevents thundering herd problem in distributed systems
    /// </summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="maxRetries">Maximum number of retries (default 5). Must be between 0 and 20 inclusive.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds (default 100). Must be between 0 and 30000 inclusive.</param>
    /// <returns>The result of the action.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialDelayMs"/> is negative or exceeds 30000.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative or exceeds 20.</exception>
    public static async Task<T> ExecuteWithJitteredBackoffAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 5,
        int initialDelayMs = 100)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialDelayMs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetries);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxRetries, 20);

        // Cap initial delay to prevent excessive delays
        const int MaxInitialDelayMs = 30000; // 30 seconds
        initialDelayMs = Math.Min(initialDelayMs, MaxInitialDelayMs);

        var random = new Random();
        int delay = initialDelayMs;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                // Add random jitter to delay
                var jitter = random.Next(0, delay);
                var actualDelay = delay + jitter;
                await Task.Delay(actualDelay);
                delay *= 2; // Exponential backoff
                // Cap the delay to prevent overflow
                if (delay > MaxInitialDelayMs)
                {
                    delay = MaxInitialDelayMs;
                }
            }
        }

        return await action();
    }

    /// <summary>
    /// Determines if an exception is transient and warrants retry
    /// </summary>
    public static bool IsTransientError(Exception ex)
    {
        // Network/timeout errors
        if (ex is TimeoutException or HttpRequestException or IOException)
            return true;

        // SQL Server transient errors
        if (ex is InvalidOperationException &&
            (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
             ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)))
            return true;

        // Inner exceptions
        if (ex.InnerException is not null)
            return IsTransientError(ex.InnerException);

        return false;
    }

    /// <summary>
    /// Creates a retry policy with specified configuration
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries (default 5). Must be between 0 and 20 inclusive.</param>
    /// <param name="strategy">The retry strategy to use (default ExponentialBackoff).</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds (default 100). Must be between 0 and 30000 inclusive.</param>
    /// <returns>A configured retry policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative or exceeds 20.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialDelayMs"/> is negative or exceeds 30000.</exception>
    public static RetryPolicy CreatePolicy(
        int maxRetries = 5,
        RetryStrategy strategy = RetryStrategy.ExponentialBackoff,
        int initialDelayMs = 100)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetries);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxRetries, 20);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialDelayMs);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialDelayMs, 30000);

        return new RetryPolicy
        {
            MaxRetries = maxRetries,
            Strategy = strategy,
            InitialDelayMs = initialDelayMs
        };
    }
}

/// <summary>
/// Retry strategy types
/// </summary>
public enum RetryStrategy
{
    NoRetry = 0,
    FixedDelay = 1,
    LinearBackoff = 2,
    ExponentialBackoff = 3,
    JitteredBackoff = 4
}

/// <summary>
/// Encapsulates retry policy configuration
/// </summary>
public sealed class RetryPolicy
{
    /// <summary>
    /// Maximum number of retries allowed (default 5).
    /// </summary>
    /// <remarks>
    /// This value is validated when used by <see cref="RetryHelper"/> methods.
    /// Must be between 0 and 20 inclusive to prevent unbounded retry loops.
    /// </remarks>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// The retry strategy to apply (default ExponentialBackoff).
    /// </summary>
    public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;

    /// <summary>
    /// Initial delay in milliseconds (default 100).
    /// </summary>
    /// <remarks>
    /// This value is validated when used by <see cref="RetryHelper"/> methods.
    /// Must be between 0 and 30000 inclusive to prevent excessive delays.
    /// </remarks>
    public int InitialDelayMs { get; set; } = 100;

    /// <summary>
    /// Maximum delay in milliseconds (default 30000).
    /// </summary>
    /// <remarks>
    /// This hard ceiling is applied to all computed delays to prevent unbounded delays.
    /// </remarks>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Growth factor used by exponential and jittered strategies (default 2.0).
    /// </summary>
    /// <remarks>
    /// This value is validated when used by <see cref="RetryHelper"/> methods.
    /// Must be between 1.0 and 10.0 inclusive to prevent unbounded exponential growth.
    /// </remarks>
    public double BackoffMultiplier { get; set; } = 2.0;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        return Strategy switch
        {
            RetryStrategy.NoRetry => await action(),
            RetryStrategy.FixedDelay => await RetryHelper.ExecuteWithFixedDelayAsync(action, MaxRetries, InitialDelayMs),
            RetryStrategy.LinearBackoff => await RetryHelper.ExecuteWithLinearBackoffAsync(action, MaxRetries, InitialDelayMs),
            RetryStrategy.ExponentialBackoff => await RetryHelper.ExecuteWithExponentialBackoffAsync(action, MaxRetries, InitialDelayMs, BackoffMultiplier),
            RetryStrategy.JitteredBackoff => await RetryHelper.ExecuteWithJitteredBackoffAsync(action, MaxRetries, InitialDelayMs),
            _ => await action()
        };
    }
}
