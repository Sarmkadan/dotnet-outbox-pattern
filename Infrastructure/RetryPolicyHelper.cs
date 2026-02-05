// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Helper class for calculating retry delays based on policy and attempt number
/// </summary>
public static class RetryPolicyHelper
{
    /// <summary>
    /// Calculates the delay for the next retry attempt
    /// </summary>
    public static TimeSpan CalculateDelay(
        int attemptNumber,
        PublishingOptions options)
    {
        if (attemptNumber <= 0)
            throw new ArgumentException("Attempt number must be greater than 0", nameof(attemptNumber));

        var baseDelay = options.InitialRetryDelay.TotalSeconds;
        var maxDelay = options.MaxRetryDelay.TotalSeconds;

        var delaySeconds = options.RetryPolicy switch
        {
            RetryPolicyType.NoRetry => 0,

            RetryPolicyType.FixedInterval =>
                baseDelay,

            RetryPolicyType.LinearBackoff =>
                Math.Min(baseDelay * attemptNumber, maxDelay),

            RetryPolicyType.ExponentialBackoff =>
                Math.Min(
                    baseDelay * Math.Pow(options.BackoffMultiplier, attemptNumber - 1),
                    maxDelay),

            _ => baseDelay
        };

        // Add jitter if enabled
        if (options.UseJitter)
        {
            var jitter = Random.Shared.NextDouble() * delaySeconds * 0.1; // 10% jitter
            delaySeconds += jitter;
        }

        return TimeSpan.FromSeconds(Math.Max(delaySeconds, 1)); // Minimum 1 second
    }

    /// <summary>
    /// Calculates retry statistics for diagnostic purposes
    /// </summary>
    public static RetryStatistics CalculateStatistics(PublishingOptions options, int maxAttempts)
    {
        var stats = new RetryStatistics
        {
            RetryPolicy = options.RetryPolicy,
            MaxAttempts = maxAttempts,
            TotalRetries = maxAttempts - 1
        };

        var totalDelay = TimeSpan.Zero;

        for (int i = 1; i < maxAttempts; i++)
        {
            var delay = CalculateDelay(i, options);
            totalDelay = totalDelay.Add(delay);
        }

        stats.TotalDelayTime = totalDelay;
        stats.AverageRetryDelay = TimeSpan.FromSeconds(totalDelay.TotalSeconds / stats.TotalRetries);

        return stats;
    }
}

/// <summary>
/// Statistics about retry behavior
/// </summary>
public class RetryStatistics
{
    /// <summary>
    /// The retry policy being used
    /// </summary>
    public RetryPolicyType RetryPolicy { get; set; }

    /// <summary>
    /// Maximum number of attempts
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// Total number of retry attempts (excluding first attempt)
    /// </summary>
    public int TotalRetries { get; set; }

    /// <summary>
    /// Total time spent waiting for retries
    /// </summary>
    public TimeSpan TotalDelayTime { get; set; }

    /// <summary>
    /// Average delay between retries
    /// </summary>
    public TimeSpan AverageRetryDelay { get; set; }

    /// <summary>
    /// Maximum delay between any two retries
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; }
}
