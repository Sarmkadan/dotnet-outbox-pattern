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
		/// <param name="options">The options to configure</param>
		/// <returns>The configured options for method chaining</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
		public static OutboxProcessorOptions Enable(this OutboxProcessorOptions options)
		{
			ArgumentNullException.ThrowIfNull(options);
			options.Enabled = true;
			return options;
		}

		/// <summary>
		/// Disables the outbox processor.
		/// </summary>
		/// <param name="options">The options to configure</param>
		/// <returns>The configured options for method chaining</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
		public static OutboxProcessorOptions Disable(this OutboxProcessorOptions options)
		{
			ArgumentNullException.ThrowIfNull(options);
			options.Enabled = false;
			return options;
		}

		/// <summary>
		/// Configures batch processing parameters.
		/// </summary>
		/// <param name="batchSize">Number of messages to process per batch. Must be greater than zero.</param>
		/// <param name="delayBetweenBatches">Delay in milliseconds between batches. Must be zero or positive.</param>
		/// <returns>The configured options for method chaining</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="batchSize"/> is less than or equal to zero</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="delayBetweenBatches"/> is negative</exception>
		public static OutboxProcessorOptions ConfigureBatch(this OutboxProcessorOptions options, int batchSize, int delayBetweenBatches)
		{
			ArgumentNullException.ThrowIfNull(options);
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);
			ArgumentOutOfRangeException.ThrowIfNegative(delayBetweenBatches);

			options.BatchSize = batchSize;
			options.DelayBetweenBatches = delayBetweenBatches;
			return options;
		}

		/// <summary>
		/// Validates the current configuration and throws if any value is out of the expected range.
		/// </summary>
		/// <param name="options">The options to validate</param>
		/// <returns>The validated options for method chaining</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="options.BatchSize"/> is less than or equal to zero</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="options.DelayBetweenBatches"/> is negative</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="options.CheckExpiredLocksInterval"/> is less than or equal to zero</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="options.LockDurationSeconds"/> is less than or equal to zero</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="options.OldestMessageAgeThresholdMinutes"/> is negative</exception>
		public static OutboxProcessorOptions Validate(this OutboxProcessorOptions options)
		{
			ArgumentNullException.ThrowIfNull(options);

			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.BatchSize, 0);
			ArgumentOutOfRangeException.ThrowIfNegative(options.DelayBetweenBatches);
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.CheckExpiredLocksInterval, 0);
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.LockDurationSeconds, 0);
			ArgumentOutOfRangeException.ThrowIfNegative(options.OldestMessageAgeThresholdMinutes);

			return options;
		}
	}
}