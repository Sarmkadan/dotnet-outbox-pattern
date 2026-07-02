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
    private readonly Mock<IOutboxSerializer> _serializerMock;
    private readonly OutboxService _sut;

    public OutboxServiceTests()
    {
        _repositoryMock = new Mock<IOutboxRepository>();
        _loggerMock = new Mock<ILogger<OutboxService>>();
        _serializerMock = new Mock<IOutboxSerializer>();
        _sut = new OutboxService(_repositoryMock.Object, _loggerMock.Object, _serializerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var act = () => new OutboxService(null!, _loggerMock.Object, _serializerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new OutboxService(_repositoryMock.Object, null!, _serializerMock.Object);
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

    [Fact]
    public async Task GetAllMessagesAsync_DelegatesToRepository()
    {
        var messages = new List<OutboxMessage> { BuildMessage(Guid.NewGuid(), OutboxMessageState.Pending) };
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var result = await _sut.GetAllMessagesAsync();

        result.Should().BeSameAs(messages);
        _repositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessagesByTopicAsync_DelegatesToRepository()
    {
        var topic = "orders.created";
        var messages = new List<OutboxMessage> { BuildMessage(Guid.NewGuid(), OutboxMessageState.Pending) };
        _repositoryMock
            .Setup(r => r.GetByTopicAsync(topic, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var result = await _sut.GetMessagesByTopicAsync(topic);

        result.Should().BeSameAs(messages);
        _repositoryMock.Verify(r => r.GetByTopicAsync(topic, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessagesByAggregateAsync_DelegatesToRepository()
    {
        var aggregateId = "order-123";
        var messages = new List<OutboxMessage> { BuildMessage(Guid.NewGuid(), OutboxMessageState.Pending) };
        _repositoryMock
            .Setup(r => r.GetByAggregateIdAsync(aggregateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var result = await _sut.GetMessagesByAggregateAsync(aggregateId);

        result.Should().BeSameAs(messages);
        _repositoryMock.Verify(r => r.GetByAggregateIdAsync(aggregateId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessagesByStateAsync_DelegatesToRepository()
    {
        var state = OutboxMessageState.Published;
        var messages = new List<OutboxMessage> { BuildMessage(Guid.NewGuid(), state) };
        _repositoryMock
            .Setup(r => r.GetByStateAsync(state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var result = await _sut.GetMessagesByStateAsync(state);

        result.Should().BeSameAs(messages);
        _repositoryMock.Verify(r => r.GetByStateAsync(state, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessagesByDateRangeAsync_DelegatesToRepository()
    {
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;
        var messages = new List<OutboxMessage> { BuildMessage(Guid.NewGuid(), OutboxMessageState.Pending) };
        _repositoryMock
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var result = await _sut.GetMessagesByDateRangeAsync(startDate, endDate);

        result.Should().BeSameAs(messages);
        _repositoryMock.Verify(r => r.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishEventAsync_WithIdempotencyKey_UsesCorrectKey()
    {
        var domainEvent = new EntityCreatedEvent { EntityId = "e-3", EntityType = "Customer" };
        var publishable = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "customers.created",
            IdempotencyKey = "custom-idempotency-key"
        };

        _repositoryMock
            .Setup(r => r.GetByIdempotencyKeyAsync("custom-idempotency-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage?)null);
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage msg, CancellationToken _) => msg);

        var result = await _sut.PublishEventAsync(publishable);

        result.IdempotencyKey.Should().Be("custom-idempotency-key");
    }

    [Fact]
    public async Task PublishEventAsync_WithDeliveryGuaranteeAtMostOnce_SetsMaxAttemptsToOne()
    {
        var domainEvent = new EntityCreatedEvent { EntityId = "e-4", EntityType = "Product" };
        var publishable = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "products.created",
            DeliveryGuarantee = DeliveryGuarantee.AtMostOnce
        };

        _repositoryMock
            .Setup(r => r.GetByIdempotencyKeyAsync(domainEvent.EventId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage?)null);
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage msg, CancellationToken _) => msg);

        var result = await _sut.PublishEventAsync(publishable);

        result.MaxPublishAttempts.Should().Be(1);
    }

    [Fact]
    public async Task PublishEventAsync_WithDeliveryGuaranteeAtLeastOnce_SetsMaxAttemptsToDefault()
    {
        var domainEvent = new EntityCreatedEvent { EntityId = "e-5", EntityType = "Product" };
        var publishable = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "products.created",
            DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce,
            MaxAttempts = 3
        };

        _repositoryMock
            .Setup(r => r.GetByIdempotencyKeyAsync(domainEvent.EventId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage?)null);
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage msg, CancellationToken _) => msg);

        var result = await _sut.PublishEventAsync(publishable);

        result.MaxPublishAttempts.Should().Be(3);
    }

    [Fact]
    public async Task PublishEventAsync_WithScheduledTime_SetsScheduledFor()
    {
        var scheduledTime = DateTime.UtcNow.AddHours(1);
        var domainEvent = new EntityCreatedEvent { EntityId = "e-6", EntityType = "Product" };
        var publishable = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "products.created",
            ScheduledTime = scheduledTime
        };

        _repositoryMock
            .Setup(r => r.GetByIdempotencyKeyAsync(domainEvent.EventId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage?)null);
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OutboxMessage msg, CancellationToken _) => msg);

        var result = await _sut.PublishEventAsync(publishable);

        result.ScheduledFor.Should().BeCloseTo(scheduledTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RetryFailedMessageAsync_WhenStateIsProcessing_ReturnsFalse()
    {
        var messageId = Guid.NewGuid();
        var message = BuildMessage(messageId, OutboxMessageState.Processing);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RetryFailedMessageAsync(messageId);

        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RetryFailedMessageAsync_WhenStateIsCompleted_ReturnsFalse()
    {
        var messageId = Guid.NewGuid();
        var message = BuildMessage(messageId, OutboxMessageState.Completed);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var result = await _sut.RetryFailedMessageAsync(messageId);

        result.Should().BeFalse();
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
