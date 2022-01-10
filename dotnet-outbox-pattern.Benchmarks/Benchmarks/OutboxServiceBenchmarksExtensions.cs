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
	/// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1</exception>
	public static async Task PublishMultipleEvents_Parallel(this OutboxServiceBenchmarks benchmarks, int count)
	{
		ArgumentNullException.ThrowIfNull(benchmarks);
		ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

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
	/// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> or <paramref name="events"/> is <see langword="null"/></exception>
	public static async Task PublishEventBatch(this OutboxServiceBenchmarks benchmarks, IEnumerable<EntityCreatedEvent> events)
	{
		ArgumentNullException.ThrowIfNull(benchmarks);
		ArgumentNullException.ThrowIfNull(events);

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
	/// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
	public static async Task<Dictionary<string, object>> GetDetailedStatistics(this OutboxServiceBenchmarks benchmarks)
	{
		ArgumentNullException.ThrowIfNull(benchmarks);

		var stats = await benchmarks.GetStatistics();

		var result = new Dictionary<string, object>
		{
			{ "TotalMessages", stats.TotalMessages },
			{ "PendingMessages", stats.PendingMessages },
			{ "ProcessingMessages", stats.ProcessingMessages },
			{ "PublishedMessages", stats.PublishedMessages },
			{ "FailedMessages", stats.FailedMessages },
			{ "ArchivedMessages", stats.ArchivedMessages },
			{ "DeadLetterCount", stats.DeadLetterCount },
			{ "AveragePublishTimeMs", Math.Round(stats.AveragePublishTime.TotalMilliseconds, 2) },
			{ "OldestPendingAgeMs", stats.OldestPendingAge.HasValue ? Math.Round(stats.OldestPendingAge.Value.TotalMilliseconds, 2) : 0 },
			{ "SuccessRate", stats.SuccessRate }
		};

		return result;
	}

	/// <summary>
	/// Waits for all pending messages to be processed
	/// </summary>
	/// <param name="benchmarks">The benchmark instance</param>
	/// <param name="timeoutSeconds">Maximum wait time in seconds</param>
	/// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="timeoutSeconds"/> is less than 1</exception>
	public static async Task WaitForProcessingCompletion(this OutboxServiceBenchmarks benchmarks, int timeoutSeconds = 30)
	{
		ArgumentNullException.ThrowIfNull(benchmarks);
		ArgumentOutOfRangeException.ThrowIfLessThan(timeoutSeconds, 1);

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
