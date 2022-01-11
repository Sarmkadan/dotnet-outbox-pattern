using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Benchmarks;

/// <summary>
/// Extension methods for <see cref="OutboxRepositoryBenchmarks"/> providing additional benchmarking scenarios
/// and utilities for testing outbox repository performance under various conditions.
/// </summary>
/// <remarks>
/// All methods in this class validate that the benchmark instance and repository are properly initialized.
/// Methods that add data will clean up any existing data before inserting new records to ensure consistent benchmarks.
/// </remarks>
public static class OutboxRepositoryBenchmarksExtensions
{
    /// <summary>
    /// Benchmark adding multiple messages (100) in a single batch operation.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task AddMultipleMessages_Batch100(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        var messages = new List<OutboxMessage>(100);
        for (int i = 0; i < 100; i++)
        {
            messages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = Guid.NewGuid().ToString(),
                AggregateId = Guid.NewGuid().ToString(),
                AggregateType = "TestAggregate",
                EventType = EventType.Created,
                EventData = $"{\"Index\":{i}}",
                EventTypeName = "TestEvent",
                Topic = "test.topic",
                PartitionKey = "test-partition",
                State = OutboxMessageState.Pending,
                CreatedAt = DateTime.UtcNow,
                MaxPublishAttempts = 5
            });
        }

        await benchmarks._repository.AddRangeAsync(messages);
    }

    /// <summary>
    /// Benchmark adding messages with different partition keys to test partition distribution.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task AddMessages_DifferentPartitions(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        var messages = new List<OutboxMessage>(100);
        for (int i = 0; i < 100; i++)
        {
            messages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = Guid.NewGuid().ToString(),
                AggregateId = Guid.NewGuid().ToString(),
                AggregateType = "TestAggregate",
                EventType = EventType.Created,
                EventData = $"{\"Index\":{i}}",
                EventTypeName = "TestEvent",
                Topic = "test.topic",
                PartitionKey = $"partition-{i % 10}", // 10 different partitions
                State = OutboxMessageState.Pending,
                CreatedAt = DateTime.UtcNow,
                MaxPublishAttempts = 5
            });
        }

        await benchmarks._repository.AddRangeAsync(messages);
    }

    /// <summary>
    /// Benchmark retrieving pending messages with a limit on the number of messages returned.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task GetPendingMessages_Limited50(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        await benchmarks._repository.GetPendingMessagesAsync(50);
    }

    /// <summary>
    /// Benchmark retrieving pending messages by specific partition with size limit.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task GetPendingMessagesByPartition_Limited50(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        await benchmarks._repository.GetPendingByPartitionAsync("test-partition", 50);
    }

    /// <summary>
    /// Benchmark retrieving pending count when database has many messages.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task GetPendingCount_LargeDataset(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        // First add a large batch to simulate real-world scenario
        await benchmarks.AddMultipleMessages_Batch100();

        await benchmarks._repository.GetPendingCountAsync();
    }

    /// <summary>
    /// Benchmark getting statistics when database has pending messages.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task GetStatistics_WithData(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        // Add some data first
        await benchmarks.AddSingleMessage();
        await benchmarks.AddMultipleMessages_Batch100();

        await benchmarks._repository.GetStatisticsAsync();
    }

    /// <summary>
    /// Benchmark retrieving pending messages by multiple partitions.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task GetPendingMessages_MultiplePartitions(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        // Add messages to multiple partitions
        await benchmarks.AddMessages_DifferentPartitions();

        // Retrieve from one partition
        await benchmarks._repository.GetPendingByPartitionAsync("partition-0", 100);
    }

    /// <summary>
    /// Utility method to add a large dataset (1000 messages) for stress testing.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task AddLargeDataset_1000Messages(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        var messages = new List<OutboxMessage>(1000);
        for (int i = 0; i < 1000; i++)
        {
            messages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = Guid.NewGuid().ToString(),
                AggregateId = Guid.NewGuid().ToString(),
                AggregateType = "TestAggregate",
                EventType = EventType.Created,
                EventData = $"{\"Index\":{i}}",
                EventTypeName = "TestEvent",
                Topic = "test.topic",
                PartitionKey = "stress-test-partition",
                State = OutboxMessageState.Pending,
                CreatedAt = DateTime.UtcNow,
                MaxPublishAttempts = 5
            });
        }

        await benchmarks._repository.AddRangeAsync(messages);
    }

    /// <summary>
    /// Benchmark retrieving pending messages after large dataset insertion.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task GetPendingMessages_AfterLargeInsert(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        await benchmarks.AddLargeDataset_1000Messages();
        await benchmarks._repository.GetPendingMessagesAsync(100);
    }

    /// <summary>
    /// Benchmark getting statistics after large dataset insertion.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing the initialized repository.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or its repository is null.</exception>
    [Benchmark]
    public static async Task GetStatistics_AfterLargeInsert(this OutboxRepositoryBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(benchmarks._repository);

        await benchmarks.AddLargeDataset_1000Messages();
        await benchmarks._repository.GetStatisticsAsync();
    }
}