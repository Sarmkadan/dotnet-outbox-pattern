#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Exceptions;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

public sealed class OutboxServiceTests
{
    private readonly Mock<IOutboxRepository> _repositoryMock;
    private readonly Mock<ILogger<OutboxService>> _loggerMock;
    private readonly OutboxService _sut;

    public OutboxServiceTests()
    {
        _repositoryMock = new Mock<IOutboxRepository>();
        _loggerMock = new Mock<ILogger<OutboxService>>();
        _sut = new OutboxService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var act = () => new OutboxService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new OutboxService(_repositoryMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task RetryFailedMessageAsync_WhenMessageNotFound_ThrowsOutboxMessageNotFoundException()
    {
        var messageId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage?)null);

        var act = async () => await _sut.RetryFailedMessageAsync(messageId);

        await act.Should().ThrowAsync<OutboxMessageNotFoundException>();
    }

    [Fact]
    public async Task RetryFailedMessageAsync_WhenStateIsNotFailed_ReturnsFalse()
    {
        var messageId = Guid.NewGuid();
        var message = BuildMessage(messageId, OutboxMessageState.Published);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _sut.RetryFailedMessageAsync(messageId);

        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RetryFailedMessageAsync_WhenStateFailed_ResetsToPendingAndReturnsTrue()
    {
        var messageId = Guid.NewGuid();
        var message = BuildMessage(messageId, OutboxMessageState.Failed);
        message.PublishAttempts = 5;
        message.ErrorMessage = "some error";
        message.ErrorStackTrace = "stack trace";

        _repositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RetryFailedMessageAsync(messageId);

        result.Should().BeTrue();
        message.State.Should().Be(OutboxMessageState.Pending);
        message.PublishAttempts.Should().Be(0);
        message.ErrorMessage.Should().BeNull();
        message.ErrorStackTrace.Should().BeNull();
        message.LastProcessedAt.Should().BeNull();

        _repositoryMock.Verify(r => r.UpdateAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessageAsync_DelegatesToRepository()
    {
        var messageId = Guid.NewGuid();
        var expected = BuildMessage(messageId, OutboxMessageState.Pending);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetMessageAsync(messageId);

        result.Should().BeSameAs(expected);
        _repositoryMock.Verify(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessageAsync_WhenRepositoryThrows_WrapsInOutboxException()
    {
        var messageId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db error"));

        var act = async () => await _sut.GetMessageAsync(messageId);

        await act.Should().ThrowAsync<OutboxException>();
    }

    [Fact]
    public async Task PublishEventAsync_WithNullPublishableEvent_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.PublishEventAsync((PublishableEvent)null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishEventAsync_WhenIdempotencyKeyAlreadyExists_ReturnsExistingMessage()
    {
        var existingEvent = new EntityCreatedEvent { EntityId = "e-1", EntityType = "Order" };
        var publishable = new PublishableEvent
        {
            Event = existingEvent,
            Topic = "orders.created"
        };

        var existingMessage = BuildMessage(Guid.NewGuid(), OutboxMessageState.Published);
        existingMessage.IdempotencyKey = existingEvent.EventId.ToString();

        _repositoryMock
            .Setup(r => r.GetByIdempotencyKeyAsync(existingEvent.EventId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMessage);

        var result = await _sut.PublishEventAsync(publishable);

        result.Should().BeSameAs(existingMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishEventAsync_WhenNewEvent_AddsToRepository()
    {
        var domainEvent = new EntityCreatedEvent { EntityId = "e-2", EntityType = "Product" };
        var publishable = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "products.created",
            MaxAttempts = 3,
            DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce
        };

        _repositoryMock
            .Setup(r => r.GetByIdempotencyKeyAsync(domainEvent.EventId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage?)null);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage msg, CancellationToken _) => msg);

        var result = await _sut.PublishEventAsync(publishable);

        result.Should().NotBeNull();
        result.Topic.Should().Be("products.created");
        result.State.Should().Be(OutboxMessageState.Pending);
        result.MaxPublishAttempts.Should().Be(3);
        result.AggregateId.Should().Be("e-2");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_DelegatesToRepository()
    {
        var stats = new OutboxStatistics { TotalMessages = 42, PublishedMessages = 40, FailedMessages = 2 };

        _repositoryMock
            .Setup(r => r.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await _sut.GetStatisticsAsync();

        result.Should().BeSameAs(stats);
        result.TotalMessages.Should().Be(42);
    }

    private static OutboxMessage BuildMessage(Guid id, OutboxMessageState state) => new()
    {
        Id = id,
        IdempotencyKey = $"key-{id}",
        AggregateId = "agg-1",
        AggregateType = "Order",
        EventType = EventType.Created,
        EventData = "{\"id\":1}",
        EventTypeName = "OrderCreatedEvent",
        Topic = "orders.created",
        State = state,
        MaxPublishAttempts = 5,
        CreatedAt = DateTime.UtcNow
    };
}
