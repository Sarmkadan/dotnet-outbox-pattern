using System;

namespace DotnetOutboxPattern.Infrastructure
{
    /// <summary>
    /// Extension methods that make configuring <see cref="OutboxProcessorOptions"/> more fluent and safe.
    /// </summary>
    public static class OutboxProcessorOptionsExtensions
    {
        /// <summary>
        /// Enables the outbox processor.
        /// </summary>
        public static OutboxProcessorOptions Enable(this OutboxProcessorOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.Enabled = true;
            return options;
        }

        /// <summary>
        /// Disables the outbox processor.
        /// </summary>
        public static OutboxProcessorOptions Disable(this OutboxProcessorOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.Enabled = false;
            return options;
        }

        /// <summary>
        /// Configures batch processing parameters.
        /// </summary>
        /// <param name="batchSize">Number of messages to process per batch. Must be greater than zero.</param>
        /// <param name="delayBetweenBatches">Delay in milliseconds between batches. Must be zero or positive.</param>
        public static OutboxProcessorOptions ConfigureBatch(this OutboxProcessorOptions options, int batchSize, int delayBetweenBatches)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "BatchSize must be greater than zero.");
            if (delayBetweenBatches < 0) throw new ArgumentOutOfRangeException(nameof(delayBetweenBatches), "DelayBetweenBatches cannot be negative.");

            options.BatchSize = batchSize;
            options.DelayBetweenBatches = delayBetweenBatches;
            return options;
        }

        /// <summary>
        /// Validates the current configuration and throws if any value is out of the expected range.
        /// </summary>
        public static OutboxProcessorOptions Validate(this OutboxProcessorOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.BatchSize <= 0)
                throw new ArgumentException("BatchSize must be greater than zero.", nameof(options.BatchSize));

            if (options.DelayBetweenBatches < 0)
                throw new ArgumentException("DelayBetweenBatches cannot be negative.", nameof(options.DelayBetweenBatches));

            if (options.CheckExpiredLocksInterval <= 0)
                throw new ArgumentException("CheckExpiredLocksInterval must be greater than zero.", nameof(options.CheckExpiredLocksInterval));

            if (options.LockDurationSeconds <= 0)
                throw new ArgumentException("LockDurationSeconds must be greater than zero.", nameof(options.LockDurationSeconds));

            if (options.OldestMessageAgeThresholdMinutes < 0)
                throw new ArgumentException("OldestMessageAgeThresholdMinutes cannot be negative.", nameof(options.OldestMessageAgeThresholdMinutes));

            return options;
        }
    }
}
