using System;
using System.Threading.Tasks;

namespace DotnetOutboxPattern.Benchmarks
{
    /// <summary>
    /// Extension methods that add convenient helper functionality to <see cref="MessagePublishingServiceBenchmarks"/>.
    /// </summary>
    public static class MessagePublishingServiceBenchmarksExtensions
    {
        /// <summary>
        /// Warms up the benchmark by running a single publish operation to eliminate JIT warm-up effects.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance to warm up.</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        public static async Task WarmUpAsync(this MessagePublishingServiceBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            await benchmarks.PublishAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Processes messages from multiple partitions sequentially.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <param name="partitionCount">Number of partitions to process.</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="partitionCount"/> is less than 1</exception>
        public static async Task ProcessMultiplePartitionsAsync(this MessagePublishingServiceBenchmarks benchmarks, int partitionCount)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);

            for (int i = 0; i < partitionCount; i++)
            {
                await benchmarks.ProcessPartition_Batch100().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Processes a large batch of messages by running the pending messages processor multiple times.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <param name="messageCount">Number of batches to process.</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="messageCount"/> is less than 1</exception>
        public static async Task ProcessLargeBatchAsync(this MessagePublishingServiceBenchmarks benchmarks, int messageCount)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentOutOfRangeException.ThrowIfLessThan(messageCount, 1);

            for (int i = 0; i < messageCount; i++)
            {
                await benchmarks.ProcessPendingMessages_Batch100().ConfigureAwait(false);
            }
        }
    }
}