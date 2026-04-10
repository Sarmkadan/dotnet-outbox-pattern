#nullable enable

using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Exceptions;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

public sealed class MessagePublishingServiceTests
{
    private readonly Mock<IOutboxRepository> _outboxRepoMock;
    private readonly Mock<IDeadLetterRepository> _dlRepoMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly Mock<ILogger<MessagePublishingService>> _loggerMock;
    private readonly PublishingOptions _options;
    private readonly MessagePublishingService _sut;

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

    [Fact]
    public async Task ProcessPendingMessagesAsync_WithSingleMessage_PublishesAndMarksPublished()
    {
        var message = CreatePendingMessage();
        _outboxRepoMock
            .Setup(r => r.GetPendingMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message });
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

    [Fact]
    public async Task ProcessPendingMessagesAsync_WhenPublisherThrows_RecordsFailureAndContinues()
    {
        var message1 = CreatePendingMessage();
        var message2 = CreatePendingMessage();
        _outboxRepoMock
            .Setup(r => r.GetPendingMessagesAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { message1, message2 });
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
