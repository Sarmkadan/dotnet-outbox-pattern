using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetOutboxPattern.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotnetOutboxPattern.Benchmarks;

/// <summary>
/// Extension methods for <see cref="OutboxServiceBenchmarks"/> to provide additional benchmarking utilities
/// </summary>
public static class OutboxServiceBenchmarksExtensions
{
    /// <summary>
    /// Publishes multiple events in parallel and measures the throughput
    /// </summary>
    /// <param name="benchmarks">The benchmark instance</param>
    /// <param name="count">Number of events to publish</param>
    /// <returns>Task representing the operation</returns>
    public static async Task PublishMultipleEvents_Parallel(this OutboxServiceBenchmarks benchmarks, int count)
    {
        var tasks = new List<Task>();

        for (int i = 0; i < count; i++)
        {
            var domainEvent = new EntityCreatedEvent
            {
                EventId = Guid.NewGuid(),
                EntityId = Guid.NewGuid().ToString(),
                EntityType = "BenchmarkEntity",
                OccurredAt = DateTime.UtcNow
            };

            tasks.Add(benchmarks._outboxService!.PublishEventAsync(domainEvent, "benchmark.topic"));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Publishes a batch of events in a single transaction
    /// </summary>
    /// <param name="benchmarks">The benchmark instance</param>
    /// <param name="events">Collection of events to publish</param>
    /// <returns>Task representing the operation</returns>
    public static async Task PublishEventBatch(this OutboxServiceBenchmarks benchmarks, IEnumerable<EntityCreatedEvent> events)
    {
        foreach (var domainEvent in events)
        {
            await benchmarks._outboxService!.PublishEventAsync(domainEvent, "batch.topic");
        }
    }

    /// <summary>
    /// Gets detailed statistics including message counts and processing times
    /// </summary>
    /// <param name="benchmarks">The benchmark instance</param>
    /// <returns>Dictionary with statistics data</returns>
    public static async Task<Dictionary<string, object>> GetDetailedStatistics(this OutboxServiceBenchmarks benchmarks)
    {
        var stats = await benchmarks.GetStatistics();

        var result = new Dictionary<string, object>
        {
            { "TotalMessages", stats.TotalMessages },
            { "PendingMessages", stats.PendingMessages },
            { "ProcessedMessages", stats.ProcessedMessages },
            { "AverageProcessingTimeMs", Math.Round(stats.AverageProcessingTime.TotalMilliseconds, 2) },
            { "LastProcessedAt", stats.LastProcessedAt }
        };

        return result;
    }

    /// <summary>
    /// Waits for all pending messages to be processed
    /// </summary>
    /// <param name="benchmarks">The benchmark instance</param>
    /// <param name="timeoutSeconds">Maximum wait time in seconds</param>
    /// <returns>Task representing the operation</returns>
    public static async Task WaitForProcessingCompletion(this OutboxServiceBenchmarks benchmarks, int timeoutSeconds = 30)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        while (DateTime.UtcNow - startTime < timeout)
        {
            var stats = await benchmarks.GetStatistics();

            if (stats.PendingMessages == 0)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Processing did not complete within {timeoutSeconds} seconds");
    }
}