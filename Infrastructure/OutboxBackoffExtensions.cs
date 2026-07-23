#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Fluent configuration and pure delay calculation for the outbox processor's batch size and
/// idle backoff. Kept separate from <see cref="OutboxProcessorOptions"/> so the delay maths can
/// be unit-tested in isolation, without spinning up the background service.
/// </summary>
public static class OutboxBackoffExtensions
{
    /// <summary>
    /// Sets the number of messages processed per batch.
    /// </summary>
    /// <param name="options">Options to configure.</param>
    /// <param name="batchSize">Messages per batch; must be greater than zero.</param>
    /// <returns>The same options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> is not positive.</exception>
    public static OutboxProcessorOptions WithBatchSize(this OutboxProcessorOptions options, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);

        options.BatchSize = batchSize;
        return options;
    }

    /// <summary>
    /// Configures exponential idle backoff: the poll delay starts at <paramref name="baseDelayMs"/>
    /// and multiplies by <paramref name="multiplier"/> for each consecutive empty batch, never
    /// exceeding <paramref name="maxDelayMs"/>.
    /// </summary>
    /// <param name="options">Options to configure.</param>
    /// <param name="baseDelayMs">Base delay in milliseconds; must be zero or positive.</param>
    /// <param name="maxDelayMs">Delay ceiling in milliseconds; must be greater than or equal to <paramref name="baseDelayMs"/>.</param>
    /// <param name="multiplier">Growth factor per empty batch; must be greater than or equal to 1.</param>
    /// <returns>The same options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="baseDelayMs"/> is negative.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDelayMs"/> is less than <paramref name="baseDelayMs"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="multiplier"/> is less than 1.0.</exception>
    public static OutboxProcessorOptions WithExponentialBackoff(
        this OutboxProcessorOptions options,
        int baseDelayMs,
        int maxDelayMs,
        double multiplier = 2.0)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegative(baseDelayMs);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelayMs, baseDelayMs);
        ArgumentOutOfRangeException.ThrowIfLessThan(multiplier, 1.0);

        options.BackoffStrategy = BackoffStrategy.Exponential;
        options.DelayBetweenBatches = baseDelayMs;
        options.MaxDelayBetweenBatches = maxDelayMs;
        options.BackoffMultiplier = multiplier;
        return options;
    }

    /// <summary>
    /// Turns off backoff, pinning the processor to a constant <paramref name="delayMs"/> poll cadence.
    /// </summary>
    /// <param name="options">Options to configure.</param>
    /// <param name="delayMs">Fixed delay in milliseconds; must be zero or positive.</param>
    /// <returns>The same options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delayMs"/> is negative.</exception>
    public static OutboxProcessorOptions WithFixedDelay(this OutboxProcessorOptions options, int delayMs)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegative(delayMs);

        options.BackoffStrategy = BackoffStrategy.Fixed;
        options.DelayBetweenBatches = delayMs;
        return options;
    }

    /// <summary>
    /// Validates the batch-size and backoff configuration together, surfacing every problem at
    /// once rather than failing on the first. Throws <see cref="ArgumentException"/> when invalid.
    /// </summary>
    /// <param name="options">Options to validate.</param>
    /// <returns>The same options instance for chaining once validated.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">One or more values are out of range.</exception>
    public static OutboxProcessorOptions ValidateBackoff(this OutboxProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (options.BatchSize <= 0)
            errors.Add($"{nameof(options.BatchSize)} must be greater than zero (was {options.BatchSize}).");

        if (options.DelayBetweenBatches < 0)
            errors.Add($"{nameof(options.DelayBetweenBatches)} cannot be negative (was {options.DelayBetweenBatches}).");

        if (options.MaxDelayBetweenBatches < options.DelayBetweenBatches)
            errors.Add(
                $"{nameof(options.MaxDelayBetweenBatches)} ({options.MaxDelayBetweenBatches}) must be greater than or equal to " +
                $"{nameof(options.DelayBetweenBatches)} ({options.DelayBetweenBatches}).");

        if (options.BackoffStrategy == BackoffStrategy.Exponential && options.BackoffMultiplier < 1.0)
            errors.Add($"{nameof(options.BackoffMultiplier)} must be at least 1.0 for exponential backoff (was {options.BackoffMultiplier}).");

        if (errors.Count > 0)
            throw new ArgumentException("Invalid outbox backoff configuration: " + string.Join(" ", errors), nameof(options));

        return options;
    }

    /// <summary>
    /// Computes the delay before the next poll given how many consecutive batches found no work.
    /// A pure function: same inputs always produce the same output, so it is trivially testable.
    /// </summary>
    /// <param name="options">The processor options describing the strategy.</param>
    /// <param name="consecutiveEmptyBatches">
    /// How many batches in a row processed zero messages. Zero means the last batch did work, so
    /// the base delay is returned. Negative values are treated as zero.
    /// </param>
    /// <returns>The delay to wait, never exceeding <see cref="OutboxProcessorOptions.MaxDelayBetweenBatches"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static TimeSpan ComputeDelay(this OutboxProcessorOptions options, int consecutiveEmptyBatches)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Normalise the base delay (negative values are treated as zero)
        var baseDelay = Math.Max(0, options.DelayBetweenBatches);

        // Fixed backoff or no empty batches – just return the base delay.
        if (options.BackoffStrategy != BackoffStrategy.Exponential || consecutiveEmptyBatches <= 0)
        {
            return TimeSpan.FromMilliseconds(baseDelay);
        }

        // Delegate the exponential calculation to the shared BackoffMath implementation.
        var delayMs = BackoffMath.ComputeExponentialDelay(
            baseDelayMs: options.DelayBetweenBatches,
            maxDelayMs: options.MaxDelayBetweenBatches,
            multiplier: options.BackoffMultiplier,
            attempt: consecutiveEmptyBatches);

        return TimeSpan.FromMilliseconds(delayMs);
    }
}
