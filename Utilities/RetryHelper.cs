#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    /// <param name="maxRetries">Maximum number of retries (default 5).</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds (default 100).</param>
    /// <param name="backoffMultiplier">Multiplier applied on each retry (default 2.0).</param>
    /// <returns>The result of the action.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialDelayMs"/> is negative.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="backoffMultiplier"/> is less than 1.0.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative.</exception>
    public static async Task<T> ExecuteWithExponentialBackoffAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 5,
        int initialDelayMs = 100,
        double backoffMultiplier = 2.0)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegative(initialDelayMs);
        ArgumentOutOfRangeException.ThrowIfLessThan(backoffMultiplier, 1.0);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

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
    public static async Task<T> ExecuteWithFixedDelayAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 5,
        int delayMs = 1000)
    {
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
    public static async Task<T> ExecuteWithLinearBackoffAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 5,
        int initialDelayMs = 100,
        int delayIncrementMs = 100)
    {
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
            }
        }

        return await action();
    }

    /// <summary>
    /// Executes an action with jittered backoff (exponential + random)
    /// Prevents thundering herd problem in distributed systems
    /// </summary>
    public static async Task<T> ExecuteWithJitteredBackoffAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 5,
        int initialDelayMs = 100)
    {
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
    public static RetryPolicy CreatePolicy(
        int maxRetries = 5,
        RetryStrategy strategy = RetryStrategy.ExponentialBackoff,
        int initialDelayMs = 100)
    {
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
    public int MaxRetries { get; set; } = 5;
    public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;
    public int InitialDelayMs { get; set; } = 100;
    public int MaxDelayMs { get; set; } = 30000;
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
