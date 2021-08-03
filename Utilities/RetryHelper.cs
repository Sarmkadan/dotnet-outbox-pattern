// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static async Task<T> ExecuteWithExponentialBackoffAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 5,
        int initialDelayMs = 100,
        double backoffMultiplier = 2.0)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

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
                delay = (int)(delay * backoffMultiplier);
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
        if (ex.InnerException != null)
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
public class RetryPolicy
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
