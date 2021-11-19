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
        public static BatchProcessingBenchmarks ConfigureBatchSize(this BatchProcessingBenchmarks benchmark, int batchSize)
        {
            if (benchmark == null) throw new ArgumentNullException(nameof(benchmark));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");

            benchmark.BatchSize = batchSize;
            return benchmark;
        }

        /// <summary>
        /// Runs a warm‑up iteration: sets up the benchmark, processes a single batch of pending messages,
        /// then cleans up. Useful to eliminate JIT warm‑up effects before measuring.
        /// </summary>
        public static async Task WarmUpAsync(this BatchProcessingBenchmarks benchmark)
        {
            if (benchmark == null) throw new ArgumentNullException(nameof(benchmark));

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
        public static async Task<TimeSpan> RunAllAsync(this BatchProcessingBenchmarks benchmark, int iterations = 1)
        {
            if (benchmark == null) throw new ArgumentNullException(nameof(benchmark));
            if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be positive.");

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
        /// Returns the elapsed <see cref="TimeSpan"/>.
        /// </summary>
        public static async Task<TimeSpan> MeasurePendingProcessingAsync(this BatchProcessingBenchmarks benchmark)
        {
            if (benchmark == null) throw new ArgumentNullException(nameof(benchmark));

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
