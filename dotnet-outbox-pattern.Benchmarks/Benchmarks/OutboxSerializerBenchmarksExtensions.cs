// dotnet-outbox-pattern.Benchmarks/Benchmarks/OutboxSerializerBenchmarksExtensions.cs
using System;
using System.Diagnostics;

namespace DotnetOutboxPattern.Benchmarks
{
    /// <summary>
    /// Extension methods that provide convenient benchmarking utilities for <see cref="OutboxSerializerBenchmarks"/>
    /// to measure serialization and deserialization performance.
    /// </summary>
    public static class OutboxSerializerBenchmarksExtensions
    {
        /// <summary>
        /// Measures the time taken to serialize events for the specified number of iterations.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance to measure.</param>
        /// <param name="iterations">The number of iterations to perform. Must be positive.</param>
        /// <returns>The total elapsed time for all serialization iterations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="iterations"/> is not positive.</exception>
        public static TimeSpan MeasureSerializationTime(this OutboxSerializerBenchmarks benchmarks, int iterations = 1)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    benchmarks.SerializeEvent();
                }
            }
            finally
            {
                stopwatch.Stop();
            }

            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Measures the time taken to deserialize events for the specified number of iterations.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance to measure.</param>
        /// <param name="iterations">The number of iterations to perform. Must be positive.</param>
        /// <returns>The total elapsed time for all deserialization iterations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="iterations"/> is not positive.</exception>
        public static TimeSpan MeasureDeserializationTime(this OutboxSerializerBenchmarks benchmarks, int iterations = 1)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    benchmarks.DeserializeEvent();
                }
            }
            finally
            {
                stopwatch.Stop();
            }

            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Measures the time taken to serialize large events for the specified number of iterations.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance to measure.</param>
        /// <param name="iterations">The number of iterations to perform. Must be positive.</param>
        /// <returns>The total elapsed time for all large event serialization iterations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="iterations"/> is not positive.</exception>
        public static TimeSpan MeasureLargeEventSerializationTime(this OutboxSerializerBenchmarks benchmarks, int iterations = 1)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    benchmarks.SerializeLargeEvent();
                }
            }
            finally
            {
                stopwatch.Stop();
            }

            return stopwatch.Elapsed;
        }
    }
}
