#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Tests for verifying that a requeued poison message (one that previously
// exhausted its retry attempts) cannot be retried forever. After being
// requeued, it should respect the new MaxPublishAttempts configuration
// and be dead-lettered again after exhausting the new attempts.
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
/// Tests for the poison-message path when messages are requeued from dead letter.
/// A message that was previously dead-lettered due to exhausted retries must not
/// be retried forever when requeued. It must respect MaxPublishAttempts and
/// be dead-lettered again after exhausting the configured attempts.
/// </summary>
public sealed class PoisonMessageRequeueDeadLetterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<OutboxDbContext> _dbOptions;

    public PoisonMessageRequeueDeadLetterTests()
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

    private (MessagePublishingService service, OutboxRepository outbox, DeadLetterRepository dlqRepo, DeadLetterService dlqService) NewProcess(
        OutboxDbContext context,
        IMessagePublisher publisher,
        PublishingOptions? options = null)
    {
        var outbox = new OutboxRepository(context);
        var dlqRepo = new DeadLetterRepository(context);
        var outboxService = new OutboxService(outbox, NullLogger<OutboxService>.Instance, new SystemTextJsonOutboxSerializer());
        var dlqService = new DeadLetterService(dlqRepo, outbox, outboxService, NullLogger<DeadLetterService>.Instance);
        var service = new MessagePublishingService(
            outbox,
            dlqRepo,
            publisher,
            NullLogger<MessagePublishingService>.Instance,
            options ?? new PublishingOptions { PublishTimeout = TimeSpan.FromSeconds(5) });
        return (service, outbox, dlqRepo, dlqService);
    }

    private static OutboxMessage NewPoisonMessage(int maxAttempts) => new()
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
    /// A message that was dead-lettered due to exhausted retries must not be retried
    /// forever when requeued. It must respect the configured MaxPublishAttempts and
    /// be dead-lettered again after exhausting the new attempts.
    /// </summary>
    [Fact]
    public async Task RequeuedPoisonMessage_WithNewMaxAttempts_ExhaustsRetriesAgain()
    {
        const int initialMaxAttempts = 3;
        const int newMaxAttempts = 2;
        var idempotencyKey = string.Empty;

        // Create and dead-letter a message with initial max attempts
        using (var seed = NewContext())
        {
            var msg = NewPoisonMessage(initialMaxAttempts);
            idempotencyKey = msg.IdempotencyKey;
            seed.OutboxMessages.Add(msg);
            await seed.SaveChangesAsync();
        }

        var publisher = new AlwaysFailingPublisher();

        // First pass: exhaust retries and dead-letter the message
        for (var tick = 0; tick < initialMaxAttempts; tick++)
        {
            using var ctx = NewContext();
            var (service, _, _, _) = NewProcess(ctx, publisher);
            await service.ProcessPendingMessagesAsync(batchSize: 10);
        }

        publisher.Attempts.Should().Be(initialMaxAttempts);

        // Verify message is dead-lettered
        using (var verify = NewContext())
        {
            var (_, outbox, _, _) = NewProcess(verify, publisher);
            var persisted = await outbox.GetByIdempotencyKeyAsync(idempotencyKey);
            persisted.Should().NotBeNull();
            persisted!.State.Should().Be(OutboxMessageState.Failed);
            persisted.PublishAttempts.Should().Be(initialMaxAttempts);
        }

        // Requeue the dead-lettered message with a new MaxPublishAttempts configuration
        using (var requeueCtx = NewContext())
        {
            var (_, outbox, dlqRepo, dlqService) = NewProcess(requeueCtx, publisher);
            var deadLetters = await dlqService.GetUnreviewedAsync(limit: 10);
            deadLetters.Should().HaveCount(1);

            await dlqService.RequeueAsync(deadLetters[0].Id, "re-test with different retry policy", default);
        }

        // Second pass: the requeued message should be retried with the new max attempts
        // and dead-lettered again after exhausting the new attempts
        for (var tick = 0; tick < newMaxAttempts; tick++)
        {
            using var ctx = NewContext();
            var (service, _, _, _) = NewProcess(ctx, publisher);
            await service.ProcessPendingMessagesAsync(batchSize: 10);
        }

        publisher.Attempts.Should().Be(initialMaxAttempts + newMaxAttempts,
            "the requeued message must be retried exactly up to its new limit");

        // Verify the requeued message is dead-lettered again
        using (var finalVerify = NewContext())
        {
            var (_, outbox, _, _) = NewProcess(finalVerify, publisher);
            var messages = await outbox.GetAllAsync(limit: 100);
            var requeuedMessage = messages.FirstOrDefault(m => m.IdempotencyKey.StartsWith(idempotencyKey + "_requeue_"));
            requeuedMessage.Should().NotBeNull();
            requeuedMessage!.State.Should().Be(OutboxMessageState.Failed);
            requeuedMessage.PublishAttempts.Should().Be(newMaxAttempts);
        }
    }

    /// <summary>
    /// A message that was dead-lettered due to exhausted retries must not be retried
    /// forever when requeued, even when using default MaxPublishAttempts (5).
    /// This is the regression test for the bug where MaxPublishAttempts was not
    /// reset when requeuing.
    /// </summary>
    [Fact]
    public async Task RequeuedPoisonMessage_WithDefaultAttempts_NotRetriedForever()
    {
        const int initialMaxAttempts = 3;
        var idempotencyKey = string.Empty;

        // Create and dead-letter a message with initial max attempts
        using (var seed = NewContext())
        {
            var msg = NewPoisonMessage(initialMaxAttempts);
            idempotencyKey = msg.IdempotencyKey;
            seed.OutboxMessages.Add(msg);
            await seed.SaveChangesAsync();
        }

        var publisher = new AlwaysFailingPublisher();

        // First pass: exhaust retries and dead-letter the message
        for (var tick = 0; tick < initialMaxAttempts; tick++)
        {
            using var ctx = NewContext();
            var (service, _, _, _) = NewProcess(ctx, publisher);
            await service.ProcessPendingMessagesAsync(batchSize: 10);
        }

        publisher.Attempts.Should().Be(initialMaxAttempts);

        // Requeue the dead-lettered message (will use default MaxPublishAttempts = 5)
        using (var requeueCtx = NewContext())
        {
            var (_, outbox, dlqRepo, dlqService) = NewProcess(requeueCtx, publisher);
            var deadLetters = await dlqService.GetUnreviewedAsync(limit: 10);
            deadLetters.Should().HaveCount(1);

            await dlqService.RequeueAsync(deadLetters[0].Id, "re-test with default retry policy", default);
        }

        // Second pass: the requeued message should be retried with default max attempts (5)
        // and dead-lettered again after 5 attempts (not forever!)
        for (var tick = 0; tick < 5; tick++)
        {
            using var ctx = NewContext();
            var (service, _, _, _) = NewProcess(ctx, publisher);
            await service.ProcessPendingMessagesAsync(batchSize: 10);
        }

        publisher.Attempts.Should().Be(initialMaxAttempts + 5,
            "the requeued message must be retried exactly up to the default limit of 5 attempts");

        // Verify the requeued message is dead-lettered again
        using (var finalVerify = NewContext())
        {
            var (_, outbox, _, _) = NewProcess(finalVerify, publisher);
            var messages = await outbox.GetAllAsync(limit: 100);
            var requeuedMessage = messages.FirstOrDefault(m => m.IdempotencyKey.StartsWith(idempotencyKey + "_requeue_"));
            requeuedMessage.Should().NotBeNull();
            requeuedMessage!.State.Should().Be(OutboxMessageState.Failed);
            requeuedMessage.PublishAttempts.Should().Be(5);
        }
    }
}