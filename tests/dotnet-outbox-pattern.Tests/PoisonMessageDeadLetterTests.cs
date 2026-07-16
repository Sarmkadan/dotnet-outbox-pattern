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
/// End-to-end tests for the poison-message path: a message that can never be published
/// (bad payload, permanently rejecting broker, etc.) must not be retried forever. After its
/// attempts are exhausted it has to leave the hot pending set and land in the dead-letter
/// store so an operator can inspect it, and it must stop being redelivered.
///
/// These run against real repositories over an in-memory SQLite database rather than mocks,
/// so the state transitions and the dead-letter row are actually persisted and re-read.
/// </summary>
public sealed class PoisonMessageDeadLetterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<OutboxDbContext> _dbOptions;

    public PoisonMessageDeadLetterTests()
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

    /// <summary>A publisher that always throws - the message can never be delivered.</summary>
    private sealed class AlwaysFailingPublisher : IMessagePublisher
    {
        public int Attempts { get; private set; }

        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new InvalidOperationException("poison message - broker permanently rejects it");
        }
    }

    private OutboxDbContext NewContext() => new(_dbOptions);

    /// <summary>
    /// Re-arms a message that a failed attempt left in <see cref="OutboxMessageState.Processing"/>
    /// back to <see cref="OutboxMessageState.Pending"/> so the next dispatcher tick picks it up.
    /// This models the requeue that a scheduler / lock-recovery sweep performs between retry
    /// attempts; without it a message only ever gets a single publish attempt per lifetime.
    /// </summary>
    private async Task RequeueForRetryAsync(string idempotencyKey)
    {
        using var ctx = NewContext();
        var msg = await ctx.OutboxMessages.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
        if (msg is not null && msg.State == OutboxMessageState.Processing)
        {
            msg.State = OutboxMessageState.Pending;
            msg.IsLocked = false;
            msg.LockExpiresAt = null;
            await ctx.SaveChangesAsync();
        }
    }

    private (MessagePublishingService service, OutboxRepository outbox, DeadLetterRepository dlq) NewProcess(
        OutboxDbContext context, IMessagePublisher publisher)
    {
        var outbox = new OutboxRepository(context);
        var dlq = new DeadLetterRepository(context);
        var service = new MessagePublishingService(
            outbox,
            dlq,
            publisher,
            NullLogger<MessagePublishingService>.Instance,
            new PublishingOptions { PublishTimeout = TimeSpan.FromSeconds(5) });
        return (service, outbox, dlq);
    }

    private static OutboxMessage NewPoisonMessage(int maxAttempts)
        => new()
        {
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            AggregateId = "agg-1",
            AggregateType = "Widget",
            EventType = EventType.Custom,
            EventData = "{\"bad\":true}",
            EventTypeName = "WidgetBroke",
            Topic = "widgets",
            MaxPublishAttempts = maxAttempts
        };

    /// <summary>
    /// A message that never publishes is retried up to <see cref="OutboxMessage.MaxPublishAttempts"/>
    /// times and then moved to the dead-letter store, marked Failed, and no longer returned as
    /// pending. The publisher must have been called exactly MaxPublishAttempts times - not more
    /// (no infinite retry), not fewer (retries were actually attempted).
    /// </summary>
    [Fact]
    public async Task PoisonMessage_ExhaustsRetries_MovesToDeadLetter()
    {
        const int maxAttempts = 3;
        var idempotencyKey = string.Empty;

        using (var seed = NewContext())
        {
            var msg = NewPoisonMessage(maxAttempts);
            idempotencyKey = msg.IdempotencyKey;
            seed.OutboxMessages.Add(msg);
            await seed.SaveChangesAsync();
        }

        var publisher = new AlwaysFailingPublisher();

        // Each pass corresponds to one dispatcher tick. A fresh context per tick mimics the
        // scoped DbContext the background service resolves on every iteration.
        for (var tick = 0; tick < maxAttempts; tick++)
        {
            using (var ctx = NewContext())
            {
                var (service, _, _) = NewProcess(ctx, publisher);
                await service.ProcessPendingMessagesAsync(batchSize: 10);
            }

            // Between ticks the failed message is requeued for another attempt, until the
            // attempt that tips it past MaxPublishAttempts dead-letters it (after which the
            // requeue is a no-op because the state is Failed, not Processing).
            await RequeueForRetryAsync(idempotencyKey);
        }

        publisher.Attempts.Should().Be(maxAttempts, "the message must be retried exactly up to its limit");

        // One more tick after exhaustion must be a no-op: the message is no longer pending.
        using (var afterCtx = NewContext())
        {
            var (service, _, _) = NewProcess(afterCtx, publisher);
            await service.ProcessPendingMessagesAsync(batchSize: 10);
        }

        publisher.Attempts.Should().Be(maxAttempts, "an exhausted message must not be retried again");

        using (var verify = NewContext())
        {
            var (_, outbox, dlq) = NewProcess(verify, publisher);

            var persisted = await outbox.GetByIdempotencyKeyAsync(idempotencyKey);
            persisted.Should().NotBeNull();
            persisted!.State.Should().Be(OutboxMessageState.Failed);
            persisted.PublishAttempts.Should().Be(maxAttempts);
            persisted.ErrorMessage.Should().NotBeNullOrEmpty();

            var pending = await outbox.GetPendingMessagesAsync(10);
            pending.Should().BeEmpty("a dead-lettered message must leave the pending set");

            var deadLetter = await dlq.GetByOutboxMessageIdAsync(persisted.Id);
            deadLetter.Should().NotBeNull("the poison message must be captured in the dead-letter store");
            deadLetter!.IdempotencyKey.Should().Be(idempotencyKey);
            deadLetter.Topic.Should().Be("widgets");

            (await dlq.GetCountAsync()).Should().Be(1);
        }
    }

    /// <summary>
    /// With <c>MaxPublishAttempts = 1</c> a poison message is dead-lettered on its very first
    /// failure, with no retry at all. Guards the boundary of the retry accounting.
    /// </summary>
    [Fact]
    public async Task PoisonMessage_WithSingleAttempt_DeadLettersImmediately()
    {
        var idempotencyKey = string.Empty;
        using (var seed = NewContext())
        {
            var msg = NewPoisonMessage(maxAttempts: 1);
            idempotencyKey = msg.IdempotencyKey;
            seed.OutboxMessages.Add(msg);
            await seed.SaveChangesAsync();
        }

        var publisher = new AlwaysFailingPublisher();
        using (var ctx = NewContext())
        {
            var (service, _, _) = NewProcess(ctx, publisher);
            await service.ProcessPendingMessagesAsync(batchSize: 10);
        }

        publisher.Attempts.Should().Be(1);

        using (var verify = NewContext())
        {
            var (_, outbox, dlq) = NewProcess(verify, publisher);
            var persisted = await outbox.GetByIdempotencyKeyAsync(idempotencyKey);
            persisted!.State.Should().Be(OutboxMessageState.Failed);
            (await dlq.GetByOutboxMessageIdAsync(persisted.Id)).Should().NotBeNull();
        }
    }

    /// <summary>
    /// A message that fails a couple of times and then succeeds must be delivered and marked
    /// Published, and must never reach the dead-letter store. This is the counterpart to the
    /// poison case: transient failures inside the retry budget are recovered, not dead-lettered.
    /// </summary>
    [Fact]
    public async Task TransientFailure_WithinRetryBudget_EventuallyPublishesAndSkipsDeadLetter()
    {
        var idempotencyKey = string.Empty;
        using (var seed = NewContext())
        {
            var msg = NewPoisonMessage(maxAttempts: 5);
            idempotencyKey = msg.IdempotencyKey;
            seed.OutboxMessages.Add(msg);
            await seed.SaveChangesAsync();
        }

        var publisher = new FlakyPublisher(failFirstN: 2);

        // Three ticks: fail, fail, succeed. Requeue between ticks so a failed attempt gets
        // another turn; once published the requeue no-ops (state is Published, not Processing).
        for (var tick = 0; tick < 3; tick++)
        {
            using (var ctx = NewContext())
            {
                var (service, _, _) = NewProcess(ctx, publisher);
                await service.ProcessPendingMessagesAsync(batchSize: 10);
            }

            await RequeueForRetryAsync(idempotencyKey);
        }

        using (var verify = NewContext())
        {
            var (_, outbox, dlq) = NewProcess(verify, publisher);
            var persisted = await outbox.GetByIdempotencyKeyAsync(idempotencyKey);
            persisted!.State.Should().Be(OutboxMessageState.Published);
            (await dlq.GetCountAsync()).Should().Be(0, "a recovered message must never be dead-lettered");
        }
    }

    /// <summary>Fails the first N attempts, succeeds afterwards.</summary>
    private sealed class FlakyPublisher : IMessagePublisher
    {
        private readonly int _failFirstN;
        private int _attempts;
        public FlakyPublisher(int failFirstN) => _failFirstN = failFirstN;

        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            _attempts++;
            if (_attempts <= _failFirstN)
                throw new InvalidOperationException($"transient failure #{_attempts}");
            return Task.CompletedTask;
        }
    }
}
