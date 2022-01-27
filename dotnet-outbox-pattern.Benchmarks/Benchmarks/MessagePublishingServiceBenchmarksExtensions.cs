using System;
using System.Threading.Tasks;

namespace DotnetOutboxPattern.Benchmarks
{
    /// <summary>
    /// Provides extension methods for <see cref="MessagePublishingServiceBenchmarks"/> to support benchmarking scenarios.
    /// These methods implement common operations like warm-up, partition processing, and batch processing.
    /// </summary>
    public static class MessagePublishingServiceBenchmarksExtensions
    {
        /// <summary>
        /// Warms up the benchmark by running a single publish operation to eliminate JIT warm-up effects.
        /// This ensures the benchmark measures steady-state performance rather than initial execution.
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
        /// Each iteration calls <see cref="MessagePublishingServiceBenchmarks.ProcessPartition_Batch100"/>
        /// to simulate processing of partitioned workloads.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance.</param>
        /// <param name="partitionCount">Number of partitions to process sequentially.</param>
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
        /// Each iteration calls <see cref="MessagePublishingServiceBenchmarks.ProcessPendingMessages_Batch100"/>
        /// to simulate processing of large message volumes.
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
