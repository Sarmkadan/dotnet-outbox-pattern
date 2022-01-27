// dotnet-outbox-pattern.Benchmarks/Benchmarks/OutboxSerializerBenchmarksExtensions.cs
using System;
using System.Diagnostics;

namespace DotnetOutboxPattern.Benchmarks
{
    /// <summary>
    /// Provides extension methods for <see cref="OutboxSerializerBenchmarks"/> to measure the performance of
    /// serialization and deserialization operations across multiple iterations. These methods use a high-resolution
    /// <see cref="Stopwatch"/> to capture precise timing metrics.
    /// </summary>
    public static class OutboxSerializerBenchmarksExtensions
    {
        /// <summary>
        /// Measures the total time required to serialize events using the <see cref="OutboxSerializerBenchmarks.SerializeEvent"/>
        /// method over the specified number of iterations.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance containing the event to serialize.</param>
        /// <param name="iterations">The number of iterations to perform. Must be a positive integer.</param>
        /// <returns>
        /// A <see cref="TimeSpan"/> representing the total elapsed time across all iterations.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="benchmarks"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="iterations"/> is less than or equal to zero.
        /// </exception>
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
        /// Measures the total time required to deserialize events using the <see cref="OutboxSerializerBenchmarks.DeserializeEvent"/>
        /// method over the specified number of iterations.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance containing the event to deserialize.</param>
        /// <param name="iterations">The number of iterations to perform. Must be a positive integer.</param>
        /// <returns>
        /// A <see cref="TimeSpan"/> representing the total elapsed time across all iterations.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="benchmarks"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="iterations"/> is less than or equal to zero.
        /// </exception>
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
        /// Measures the total time required to serialize large events using the <see cref="OutboxSerializerBenchmarks.SerializeLargeEvent"/>
        /// method over the specified number of iterations.
        /// </summary>
        /// <param name="benchmarks">The benchmark instance containing the large event to serialize.</param>
        /// <param name="iterations">The number of iterations to perform. Must be a positive integer.</param>
        /// <returns>
        /// A <see cref="TimeSpan"/> representing the total elapsed time across all iterations.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="benchmarks"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="iterations"/> is less than or equal to zero.
        /// </exception>
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
