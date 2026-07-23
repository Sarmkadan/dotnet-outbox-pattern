#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Utilities;

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Configures the retry-with-backoff policy that an outbox message must exhaust before it is
/// routed to the dead letter queue. The dispatch loop consults this policy on every publish
/// failure to decide how long to wait before the next attempt, using the message's own
/// persisted <see cref="OutboxMessage.PublishAttempts"/> counter so the schedule survives
/// process restarts.
/// </summary>
public sealed class OutboxRetryOptions
{
    /// <summary>
    /// Maximum number of publish attempts before a message is dead-lettered.
    /// </summary>
    public int MaxAttempts { get; set; } = OutboxConstants.DefaultMaxPublishAttempts;

    /// <summary>
    /// Backoff strategy applied between attempts, reusing <see cref="RetryHelper"/>'s strategy set.
    /// </summary>
    public RetryStrategy BackoffStrategy { get; set; } = RetryStrategy.ExponentialBackoff;

    /// <summary>
    /// Delay used for the first retry (and the fixed interval when <see cref="BackoffStrategy"/>
    /// is <see cref="RetryStrategy.FixedDelay"/>).
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Hard ceiling applied to every computed delay, regardless of strategy.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Growth factor used by the exponential and jittered strategies.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Fixed increment added per attempt by the linear strategy.
    /// </summary>
    public TimeSpan LinearIncrement { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Random number source used to compute jitter for <see cref="RetryStrategy.JitteredBackoff"/>.
    /// Kept as an instance field (rather than a fresh <see cref="Random"/> per call) to avoid
    /// low-entropy jitter when many delays are computed in quick succession.
    /// </summary>
    private readonly Random _jitterSource = new();

    /// <summary>
    /// Computes the delay to wait before the next publish attempt.
    /// </summary>
    /// <param name="attemptNumber">
    /// The 1-based attempt number that just failed (i.e. <see cref="OutboxMessage.PublishAttempts"/>
    /// after recording the failure).
    /// </param>
    /// <returns>The delay to wait, never exceeding <see cref="MaxDelay"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="attemptNumber"/> is negative.</exception>
    public TimeSpan ComputeNextDelay(int attemptNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attemptNumber);

        var maxDelayMs = (int)Math.Min(MaxDelay.TotalMilliseconds, int.MaxValue);
        var initialDelayMs = (int)Math.Min(InitialDelay.TotalMilliseconds, int.MaxValue);

        var delayMs = BackoffStrategy switch
        {
            RetryStrategy.NoRetry => 0,
            RetryStrategy.FixedDelay => initialDelayMs,
            RetryStrategy.LinearBackoff => initialDelayMs + (int)Math.Min(
                LinearIncrement.TotalMilliseconds * Math.Max(0, attemptNumber - 1), int.MaxValue),
            RetryStrategy.ExponentialBackoff => BackoffMath.ComputeExponentialDelay(
                baseDelayMs: initialDelayMs,
                maxDelayMs: maxDelayMs,
                multiplier: BackoffMultiplier,
                attempt: attemptNumber),
            RetryStrategy.JitteredBackoff => ComputeJitteredDelay(initialDelayMs, attemptNumber),
            _ => initialDelayMs
        };

        return TimeSpan.FromMilliseconds(Math.Min(delayMs, MaxDelay.TotalMilliseconds));
    }

    /// <summary>
    /// Computes an exponential delay with added random jitter to avoid synchronized retries
    /// across multiple outbox processor instances.
    /// </summary>
    private double ComputeJitteredDelay(int initialDelayMs, int attemptNumber)
    {
        var baseDelay = BackoffMath.ComputeExponentialDelay(
            baseDelayMs: initialDelayMs,
            maxDelayMs: (int)Math.Min(MaxDelay.TotalMilliseconds, int.MaxValue),
            multiplier: BackoffMultiplier,
            attempt: attemptNumber);

        var jitter = _jitterSource.NextDouble() * baseDelay;
        return baseDelay + jitter;
    }
}
