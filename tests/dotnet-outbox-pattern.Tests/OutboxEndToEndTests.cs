#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// End-to-end tests that exercise the outbox against a real (in-memory) SQLite database
/// through the actual repositories and <see cref="MessagePublishingService"/> - no mocks
/// on the persistence path. These verify the guarantees the pattern actually exists to
/// provide: a write committed inside a business transaction survives a process crash that
/// happens before the message is dispatched, and is then delivered at-least-once with the
/// consumer able to deduplicate via the idempotency key.
///
/// A single SQLite connection is kept open for the fixture's lifetime so the in-memory
/// database is not torn down between "process restarts". Each simulated process gets its
/// own <see cref="OutboxDbContext"/>, repositories and publishing service, exactly as a
/// freshly started host would.
/// </summary>
public sealed class OutboxEndToEndTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<OutboxDbContext> _dbOptions;

    public OutboxEndToEndTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new OutboxDbContext(_dbOptions);
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// Represents one "process": a fresh DbContext plus the repositories and publishing
    /// service wired on top of it, sharing the fixture's persistent SQLite connection.
    /// </summary>
    private sealed class ProcessScope : IDisposable
    {
        public OutboxDbContext Context { get; }
        public OutboxRepository OutboxRepository { get; }
        public DeadLetterRepository DeadLetterRepository { get; }
        public MessagePublishingService PublishingService { get; }

        public ProcessScope(DbContextOptions<OutboxDbContext> options, IMessagePublisher publisher, PublishingOptions publishingOptions)
        {
            Context = new OutboxDbContext(options);
            OutboxRepository = new OutboxRepository(Context);
            DeadLetterRepository = new DeadLetterRepository(Context);
            PublishingService = new MessagePublishingService(
                OutboxRepository,
                DeadLetterRepository,
                publisher,
                NullLogger<MessagePublishingService>.Instance,
                publishingOptions);
        }

        public void Dispose() => Context.Dispose();
    }

    private ProcessScope NewProcess(IMessagePublisher publisher, PublishingOptions? options = null)
        => new(_dbOptions, publisher, options ?? new PublishingOptions { PublishTimeout = TimeSpan.FromSeconds(5) });

    /// <summary>
    /// A publisher that records every delivery so tests can assert at-least-once delivery
    /// and consumer-side deduplication. Optionally fails a configurable number of times to
    /// exercise the retry / dead-letter paths.
    /// </summary>
    private sealed class RecordingPublisher : IMessagePublisher
    {
        private readonly int _failFirstN;
        private int _attempts;

        public RecordingPublisher(int failFirstN = 0) => _failFirstN = failFirstN;

        /// <summary>Every idempotency key handed to the publisher, including duplicates.</summary>
        public List<string> Deliveries { get; } = new();

        /// <summary>The set of distinct idempotency keys a deduplicating consumer would keep.</summary>
        public HashSet<string> DeduplicatedKeys { get; } = new();

        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            _attempts++;
            if (_attempts <= _failFirstN)
            {
                throw new InvalidOperationException($"Simulated broker failure #{_attempts}");
            }

            Deliveries.Add(message.IdempotencyKey);
            DeduplicatedKeys.Add(message.IdempotencyKey);
            return Task.CompletedTask;
        }
    }

    private static OutboxMessage NewMessage(string idempotencyKey) => new()
    {
        IdempotencyKey = idempotencyKey,
        AggregateId = "order-42",
        AggregateType = "Order",
        EventType = EventType.Created,
        EventData = "{\"orderId\":42}",
        EventTypeName = "OrderCreated",
        Topic = "orders",
        MaxPublishAttempts = 3
    };

    /// <summary>
    /// The core outbox promise: a message written inside the same DB transaction as the
    /// business change is durable. If the process crashes before the dispatcher ever runs,
    /// a freshly restarted process picks the message up and delivers it exactly once.
    /// </summary>
    [Fact]
    public async Task WriteInTransaction_CrashBeforeDispatch_RestartDeliversAtLeastOnce()
    {
        var idempotencyKey = Guid.NewGuid().ToString("N");

        // --- Process #1: write the outbox row inside a business transaction, then "crash"
        //     before the dispatcher ever runs. The transaction commit is the only thing that
        //     happens; no publishing is attempted. ---
        var crashedPublisher = new RecordingPublisher();
        using (var proc1 = NewProcess(crashedPublisher))
        {
            await using var tx = await proc1.Context.Database.BeginTransactionAsync();
            proc1.Context.OutboxMessages.Add(NewMessage(idempotencyKey));
            await proc1.Context.SaveChangesAsync();
            await tx.CommitAsync();
            // Process dies here - dispatcher never got a turn.
        }

        crashedPublisher.Deliveries.Should().BeEmpty("the process crashed before dispatching");

        // --- Process #2: a fresh host starts on the same database and runs the dispatcher. ---
        var restartedPublisher = new RecordingPublisher();
        using (var proc2 = NewProcess(restartedPublisher))
        {
            var result = await proc2.PublishingService.ProcessPendingMessagesAsync(batchSize: 10);
            result.ProcessedCount.Should().Be(1);
        }

        restartedPublisher.Deliveries.Should().ContainSingle().Which.Should().Be(idempotencyKey);

        // The row must now be Published and durable.
        using (var verify = NewProcess(restartedPublisher))
        {
            var persisted = await verify.OutboxRepository.GetByIdempotencyKeyAsync(idempotencyKey);
            persisted.Should().NotBeNull();
            persisted!.State.Should().Be(OutboxMessageState.Published);
            persisted.PublishedAt.Should().NotBeNull();
        }
    }

    /// <summary>
    /// The at-least-once guarantee means a crash in the window between a successful broker
    /// publish and the local "mark as published" commit leads to a redelivery on restart.
    /// This reproduces that window: the message is left locked and in <c>Processing</c> state
    /// (as it would be at the moment of the crash) with an expired lock. On restart the
    /// stuck message is recovered and delivered again - a genuine duplicate - and the
    /// consumer collapses it via the idempotency key.
    /// </summary>
    [Fact]
    public async Task CrashAfterPublishBeforeMark_RedeliversButConsumerDeduplicates()
    {
        var idempotencyKey = Guid.NewGuid().ToString("N");
        var publisher = new RecordingPublisher();

        // Simulate the state at the instant of the crash: the broker already received the
        // message (first delivery recorded by hand), the row was locked and flipped to
        // Processing, but the process died before MarkAsPublished was committed. The lock is
        // already expired, mimicking the time that passed until the host restarted.
        publisher.Deliveries.Add(idempotencyKey);
        publisher.DeduplicatedKeys.Add(idempotencyKey);

        using (var seed = NewProcess(publisher))
        {
            var message = NewMessage(idempotencyKey);
            message.State = OutboxMessageState.Processing;
            message.IsLocked = true;
            message.LockExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            seed.Context.OutboxMessages.Add(message);
            await seed.Context.SaveChangesAsync();
        }

        // --- Restart: recover expired locks (what OutboxProcessor does periodically), then
        //     run the dispatcher. The recovered message is delivered a second time. ---
        using (var restart = NewProcess(publisher))
        {
            var expired = await restart.OutboxRepository.GetExpiredLocksAsync();
            expired.Should().HaveCount(1, "the mid-flight message must be reclaimable after its lock expires");

            foreach (var stuck in expired)
            {
                await restart.PublishingService.ReleaseLockAsync(stuck.Id);
            }

            await restart.PublishingService.ProcessPendingMessagesAsync(batchSize: 10);
        }

        // At-least-once: the broker saw the message twice (a real duplicate).
        publisher.Deliveries.Should().HaveCount(2);
        publisher.Deliveries.Should().OnlyContain(k => k == idempotencyKey);

        // Deduplication: a consumer keyed on IdempotencyKey collapses the duplicate to one.
        publisher.DeduplicatedKeys.Should().ContainSingle().Which.Should().Be(idempotencyKey);
    }

    /// <summary>
    /// Running the dispatcher again after everything is published must be a no-op: no message
    /// is delivered twice simply because the loop ran another pass. This is the steady-state
    /// dedup guarantee (as opposed to the crash-window one above).
    /// </summary>
    [Fact]
    public async Task ReprocessingAfterPublish_DoesNotRedeliver()
    {
        var idempotencyKey = Guid.NewGuid().ToString("N");
        var publisher = new RecordingPublisher();

        using (var seed = NewProcess(publisher))
        {
            seed.Context.OutboxMessages.Add(NewMessage(idempotencyKey));
            await seed.Context.SaveChangesAsync();
        }

        using (var proc = NewProcess(publisher))
        {
            await proc.PublishingService.ProcessPendingMessagesAsync(batchSize: 10);
            // Second and third passes over the same database.
            await proc.PublishingService.ProcessPendingMessagesAsync(batchSize: 10);
            await proc.PublishingService.ProcessPendingMessagesAsync(batchSize: 10);
        }

        publisher.Deliveries.Should().ContainSingle();
    }
}
