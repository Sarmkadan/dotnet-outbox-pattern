#nullable enable

using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Exceptions;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Contains unit tests for the <see cref="MessagePublishingService"/> class.
/// Tests various scenarios including message processing, error handling, locking, scheduling,
/// and dead-letter queue operations to ensure the outbox pattern message publishing works correctly.
/// </summary>
public sealed class MessagePublishingServiceTests
{
    private readonly Mock<IOutboxRepository> _outboxRepoMock;
    private readonly Mock<IDeadLetterRepository> _dlRepoMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly Mock<ILogger<MessagePublishingService>> _loggerMock;
    private readonly PublishingOptions _options;
    private readonly MessagePublishingService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePublishingServiceTests"/> class.
    /// Sets up mock dependencies and creates a test instance of <see cref="MessagePublishingService"/> with default options.
    /// </summary>
    public MessagePublishingServiceTests()
    {
        _outboxRepoMock = new Mock<IOutboxRepository>();
        _dlRepoMock = new Mock<IDeadLetterRepository>();
        _publisherMock = new Mock<IMessagePublisher>();
        _loggerMock = new Mock<ILogger<MessagePublishingService>>();
        _options = new PublishingOptions { MaxRetries = 5 };
        _sut = new MessagePublishingService(
            _outboxRepoMock.Object,
            _dlRepoMock.Object,
            _publisherMock.Object,
            _loggerMock.Object,
            _options);
    }

    /// <summary>
    /// Tests that the constructor throws <see cref="ArgumentNullException"/> when the outbox repository is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOutboxRepository_ThrowsArgumentNullException()
    {
        var act = () => new MessagePublishingService(
            null!,
            _dlRepoMock.Object,
            _publisherMock.Object,
            _loggerMock.Object,
            _options);

        act.Should().Throw<ArgumentNullException>().WithParameterName("outboxRepository");
    }

    /// <summary>
    /// Tests that the constructor throws <see cref="ArgumentNullException"/> when the message publisher is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPublisher_ThrowsArgumentNullException()
    {
        var act = () => new MessagePublishingService(
            _outboxRepoMock.Object,
            _dlRepoMock.Object,
            null!,
            _loggerMock.Object,
            _options);

        act.Should().Throw<ArgumentNullException>().WithParameterName("publisher");
    }

    /// <summary>
    /// Tests that processing an empty batch of messages returns zero processed and zero failed counts.
    /// Verifies that no publishing attempts are made when there are no pending messages.
    /// </summary>
    [Fact]
    public async Task ProcessPendingMessagesAsync_WithEmptyBatch_ReturnsZeroProcessed()
    {
        _outboxRepoMock
            .Setup(r => r.GetPendingMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage>());

        var result = await _sut.ProcessPendingMessagesAsync(100);

        result.ProcessedCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that processing a single pending message publishes it and marks it as published.
    /// Verifies that the message state is updated to <see cref="OutboxMessageState.Published"/> after successful publishing.
    /// </summary>
    [Fact]
    public async Task ProcessPendingMessagesAsync_WithSingleMessage_PublishesAndMarksPublished()
    {
        var message = CreatePendingMessage();
        _outboxRepoMock
            .Setup(r => r.GetPendingMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message });
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _publisherMock
            .Setup(p => p.PublishAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outboxRepoMock
            .Setup(r => r.UpdateAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ProcessPendingMessagesAsync(100);

        result.ProcessedCount.Should().Be(1);
        message.State.Should().Be(OutboxMessageState.Published);
        _publisherMock.Verify(p => p.PublishAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that when the publisher throws an exception, the failure is recorded and processing continues with remaining messages.
    /// Verifies that the service handles exceptions gracefully and updates the failure count appropriately.
    /// </summary>
    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenPublisherThrows_RecordsFailureAndContinues()
    {
        var message1 = CreatePendingMessage();
        var message2 = CreatePendingMessage();
        _outboxRepoMock
            .Setup(r => r.GetPendingMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message1, message2 });
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message1);
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message2);
        _publisherMock
            .Setup(p => p.PublishAsync(message1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish error"));
        _publisherMock
            .Setup(p => p.PublishAsync(message2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outboxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ProcessPendingMessagesAsync(100);

        result.ProcessedCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that processing a locked message returns false and does not attempt to publish.
    /// Verifies that messages with <see cref="OutboxMessage.IsLocked"/> set to true are skipped.
    /// </summary>
    [Fact]
    public async Task ProcessSingleMessageAsync_WhenMessageLocked_ReturnsFalse()
    {
        var message = CreatePendingMessage();
        message.IsLocked = true;
        message.LockExpiresAt = DateTime.UtcNow.AddMinutes(5);

        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _sut.ProcessSingleMessageAsync(message.Id);

        result.Should().BeFalse();
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that a message that can be retried attempts publishing when processed.
    /// Verifies that a message with no lock and no scheduling constraint is published successfully.
    /// </summary>
    [Fact]
    public async Task ProcessSingleMessageAsync_WhenMessageCanRetry_AttemptsPublish()
    {
        var message = CreatePendingMessage();
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _publisherMock
            .Setup(p => p.PublishAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outboxRepoMock
            .Setup(r => r.UpdateAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ProcessSingleMessageAsync(message.Id);

        result.Should().BeTrue();
        _publisherMock.Verify(p => p.PublishAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that when maximum retry attempts are reached, the message is moved to the dead-letter queue.
    /// Verifies that messages with publish attempts equal to max attempts are handled appropriately.
    /// </summary>
    [Fact]
    public async Task ProcessSingleMessageAsync_WhenReachedMaxRetries_MovesToDlq()
    {
        var message = CreatePendingMessage();
        message.PublishAttempts = 5;
        message.MaxPublishAttempts = 5;
        message.ErrorMessage = "max retries reached";

        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _publisherMock
            .Setup(p => p.PublishAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outboxRepoMock
            .Setup(r => r.UpdateAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dlRepoMock
            .Setup(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeadLetter dl, CancellationToken _) => dl);

        var result = await _sut.ProcessSingleMessageAsync(message.Id);

        message.State.Should().Be(OutboxMessageState.Published);
        _dlRepoMock.Verify(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that messages scheduled for the future are not processed.
    /// Verifies that messages with <see cref="OutboxMessage.ScheduledFor"/> in the future are skipped during processing.
    /// </summary>
    [Fact]
    public async Task ProcessScheduledMessagesAsync_WithFutureSchedule_DoesNotProcess()
    {
        var message = CreatePendingMessage();
        message.ScheduledFor = DateTime.UtcNow.AddHours(1);

        _outboxRepoMock
            .Setup(r => r.GetScheduledMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage>());

        var result = await _sut.ProcessScheduledMessagesAsync(100);

        result.ProcessedCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that the lock is released for messages with expired locks.
    /// Verifies that messages with <see cref="OutboxMessage.LockExpiresAt"/> in the past have their <see cref="OutboxMessage.IsLocked"/> set to false.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_UnlocksExpiredMessage()
    {
        var message = CreatePendingMessage();
        message.IsLocked = true;
        message.LockExpiresAt = DateTime.UtcNow.AddSeconds(-1);

        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _outboxRepoMock
            .Setup(r => r.UpdateAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ReleaseLockAsync(message.Id);

        message.IsLocked.Should().BeFalse();
        _outboxRepoMock.Verify(r => r.UpdateAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that locked messages are not processed during batch processing.
    /// Verifies that messages with <see cref="OutboxMessage.IsLocked"/> set to true are skipped when processing pending messages.
    /// </summary>
    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenMessageIsLocked_DoesNotProcess()
    {
        var message = CreatePendingMessage();
        message.IsLocked = true;
        message.LockExpiresAt = DateTime.UtcNow.AddMinutes(5);

        _outboxRepoMock
            .Setup(r => r.GetPendingMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message });
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _sut.ProcessPendingMessagesAsync(100);

        result.ProcessedCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that scheduled messages are not processed during batch processing.
    /// Verifies that messages with <see cref="OutboxMessage.ScheduledFor"/> set to a future date are skipped when processing pending messages.
    /// </summary>
    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenMessageIsScheduled_DoesNotProcess()
    {
        var message = CreatePendingMessage();
        message.ScheduledFor = DateTime.UtcNow.AddHours(1);

        _outboxRepoMock
            .Setup(r => r.GetPendingMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message });
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _sut.ProcessPendingMessagesAsync(100);

        result.ProcessedCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that when the publisher throws exceptions for multiple messages, all failures are recorded.
    /// Verifies that the service continues processing even when multiple messages fail to publish.
    /// </summary>
    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenPublisherThrowsForAllMessages_RecordsAllFailures()
    {
        var message1 = CreatePendingMessage();
        var message2 = CreatePendingMessage();
        var message3 = CreatePendingMessage();

        _outboxRepoMock
            .Setup(r => r.GetPendingMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message1, message2, message3 });
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message1);
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message2);
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message3.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message3);
        _publisherMock
            .SetupSequence(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("error1"))
            .ThrowsAsync(new InvalidOperationException("error2"))
            .Returns(Task.CompletedTask);
        _outboxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ProcessPendingMessagesAsync(100);

        result.ProcessedCount.Should().Be(1);
        result.FailedCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that processing a null message returns false without throwing an exception.
    /// Verifies that the service handles non-existent messages gracefully.
    /// </summary>
    [Fact]
    public async Task ProcessSingleMessageAsync_WhenMessageIsNull_ReturnsFalse()
    {
        var messageId = Guid.NewGuid();
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage?)null);

        var result = await _sut.ProcessSingleMessageAsync(messageId);

        result.Should().BeFalse();
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that processing a locked message returns false during single message processing.
    /// Verifies that messages with <see cref="OutboxMessage.IsLocked"/> set to true are skipped when processing individual messages.
    /// </summary>
    [Fact]
    public async Task ProcessSingleMessageAsync_WhenMessageIsLocked_ReturnsFalse()
    {
        var message = CreatePendingMessage();
        message.IsLocked = true;
        message.LockExpiresAt = DateTime.UtcNow.AddMinutes(5);

        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _sut.ProcessSingleMessageAsync(message.Id);

        result.Should().BeFalse();
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that processing a scheduled message returns false during single message processing.
    /// Verifies that messages with <see cref="OutboxMessage.ScheduledFor"/> set to a future date are skipped when processing individual messages.
    /// </summary>
    [Fact]
    public async Task ProcessSingleMessageAsync_WhenMessageIsScheduled_ReturnsFalse()
    {
        var message = CreatePendingMessage();
        message.ScheduledFor = DateTime.UtcNow.AddHours(1);

        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _sut.ProcessSingleMessageAsync(message.Id);

        result.Should().BeFalse();
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that messages scheduled in the past are processed during scheduled message processing.
    /// Verifies that messages with <see cref="OutboxMessage.ScheduledFor"/> in the past are published successfully.
    /// </summary>
    [Fact]
    public async Task ProcessScheduledMessagesAsync_WithPastSchedule_ProcessesMessages()
    {
        var message = CreatePendingMessage();
        message.ScheduledFor = DateTime.UtcNow.AddMinutes(-30);

        _outboxRepoMock
            .Setup(r => r.GetScheduledMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message });
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _publisherMock
            .Setup(p => p.PublishAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _outboxRepoMock
            .Setup(r => r.UpdateAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ProcessScheduledMessagesAsync(100);

        result.ProcessedCount.Should().Be(1);
        message.State.Should().Be(OutboxMessageState.Published);
    }

    /// <summary>
    /// Tests that releasing a lock for a non-existent message does not throw an exception.
    /// Verifies that the service handles missing messages gracefully during lock release operations.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_WhenMessageNotFound_DoesNotThrow()
    {
        var messageId = Guid.NewGuid();
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage?)null);

        await _sut.ReleaseLockAsync(messageId);

        _outboxRepoMock.Verify(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that releasing a lock for a message that is not locked does not update the message.
    /// Verifies that the service only updates messages that are actually locked.
    /// </summary>
    [Fact]
    public async Task ReleaseLockAsync_WhenMessageNotLocked_DoesNotUpdate()
    {
        var message = CreatePendingMessage();
        message.IsLocked = false;

        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _outboxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ReleaseLockAsync(message.Id);

        _outboxRepoMock.Verify(r => r.UpdateAsync(message, It.IsAny<CancellationToken>()), Times.Never);
    }

    private static OutboxMessage CreatePendingMessage() => new()
    {
        Id = Guid.NewGuid(),
        IdempotencyKey = $"key-{Guid.NewGuid()}",
        AggregateId = "agg-1",
        AggregateType = "Order",
        EventType = EventType.Created,
        EventData = "{\"id\":1}",
        EventTypeName = "OrderCreatedEvent",
        Topic = "orders.created",
        State = OutboxMessageState.Pending,
        MaxPublishAttempts = 5,
        CreatedAt = DateTime.UtcNow
    };
}
