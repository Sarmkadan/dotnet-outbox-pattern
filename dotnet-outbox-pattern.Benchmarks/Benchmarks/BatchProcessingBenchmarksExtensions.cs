using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DotnetOutboxPattern.Benchmarks
{
    /// <summary>
    /// Extension methods that add convenient helper functionality to <see cref="BatchProcessingBenchmarks"/>.
    /// </summary>
    public static class BatchProcessingBenchmarksExtensions
    {
        /// <summary>
        /// Configures the batch size and returns the benchmark instance for fluent chaining.
        /// </summary>
        /// <param name="benchmark">The benchmark instance to configure.</param>
        /// <param name="batchSize">The batch size to use for processing.</param>
        /// <returns>The configured benchmark instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmark"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> is not positive.</exception>
        public static BatchProcessingBenchmarks ConfigureBatchSize(this BatchProcessingBenchmarks benchmark, int batchSize)
        {
            ArgumentNullException.ThrowIfNull(benchmark);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

            benchmark.BatchSize = batchSize;
            return benchmark;
        }

        /// <summary>
        /// Runs a warm‑up iteration: sets up the benchmark, processes a single batch of pending messages,
        /// then cleans up. Useful to eliminate JIT warm‑up effects before measuring.
        /// </summary>
        /// <param name="benchmark">The benchmark instance to warm up.</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmark"/> is <see langword="null"/>.</exception>
        public static async Task WarmUpAsync(this BatchProcessingBenchmarks benchmark)
        {
            ArgumentNullException.ThrowIfNull(benchmark);

            benchmark.Setup();
            try
            {
                await benchmark.ProcessPendingMessages().ConfigureAwait(false);
            }
            finally
            {
                benchmark.Cleanup();
            }
        }

        /// <summary>
        /// Executes the full benchmark cycle a specified number of times, measuring the total elapsed time.
        /// The benchmark is set up once, the processing methods are called repeatedly, and then cleanup/dispose
        /// are performed.
        /// </summary>
        /// <param name="benchmark">The benchmark instance to run.</param>
        /// <param name="iterations">The number of iterations to perform. Must be positive.</param>
        /// <returns>The total elapsed time for all iterations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmark"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="iterations"/> is not positive.</exception>
        public static async Task<TimeSpan> RunAllAsync(this BatchProcessingBenchmarks benchmark, int iterations = 1)
        {
            ArgumentNullException.ThrowIfNull(benchmark);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

            benchmark.Setup();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    await benchmark.ProcessPendingMessages().ConfigureAwait(false);
                    await benchmark.ProcessPartitionMessages().ConfigureAwait(false);
                }
            }
            finally
            {
                stopwatch.Stop();
                benchmark.Cleanup();
                benchmark.Dispose();
            }

            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Measures the time taken to process pending messages once.
        /// </summary>
        /// <param name="benchmark">The benchmark instance to measure.</param>
        /// <returns>The elapsed <see cref="TimeSpan"/> for processing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmark"/> is <see langword="null"/>.</exception>
        public static async Task<TimeSpan> MeasurePendingProcessingAsync(this BatchProcessingBenchmarks benchmark)
        {
            ArgumentNullException.ThrowIfNull(benchmark);

            benchmark.Setup();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await benchmark.ProcessPendingMessages().ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();
                benchmark.Cleanup();
            }

            return stopwatch.Elapsed;
        }
    }
}
