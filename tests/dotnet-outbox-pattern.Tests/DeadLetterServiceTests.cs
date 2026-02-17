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

public class DeadLetterServiceTests
{
    private readonly Mock<IDeadLetterRepository> _dlRepoMock;
    private readonly Mock<IOutboxRepository> _outboxRepoMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<ILogger<DeadLetterService>> _loggerMock;
    private readonly DeadLetterService _sut;

    public DeadLetterServiceTests()
    {
        _dlRepoMock = new Mock<IDeadLetterRepository>();
        _outboxRepoMock = new Mock<IOutboxRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _loggerMock = new Mock<ILogger<DeadLetterService>>();
        _sut = new DeadLetterService(
            _dlRepoMock.Object,
            _outboxRepoMock.Object,
            _outboxServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullDlRepository_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterService(
            null!,
            _outboxRepoMock.Object,
            _outboxServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("dlRepository");
    }

    [Fact]
    public void Constructor_WithNullOutboxRepository_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterService(
            _dlRepoMock.Object,
            null!,
            _outboxServiceMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("outboxRepository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterService(
            _dlRepoMock.Object,
            _outboxRepoMock.Object,
            _outboxServiceMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task MoveToDlqAsync_WithNullMessage_ThrowsDeadLetterException()
    {
        var act = async () => await _sut.MoveToDlqAsync(null!);

        await act.Should().ThrowAsync<DeadLetterException>();
    }

    [Fact]
    public async Task MoveToDlqAsync_WithValidMessage_AddsDeadLetterAndReturnsIt()
    {
        var message = BuildFailedMessage();
        _dlRepoMock
            .Setup(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeadLetter dl, CancellationToken _) => dl);

        var result = await _sut.MoveToDlqAsync(message);

        result.OutboxMessageId.Should().Be(message.Id);
        result.Topic.Should().Be(message.Topic);
        result.ErrorMessage.Should().Be(message.ErrorMessage);
        _dlRepoMock.Verify(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewAsync_WhenDeadLetterNotFound_ThrowsDeadLetterException()
    {
        var id = Guid.NewGuid();
        _dlRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeadLetter?)null);

        var act = async () => await _sut.ReviewAsync(id, "operator notes");

        await act.Should().ThrowAsync<DeadLetterException>();
    }

    [Fact]
    public async Task ReviewAsync_WhenFound_MarksAsReviewedAndPersistsUpdate()
    {
        var deadLetter = DeadLetter.FromOutboxMessage(BuildFailedMessage());
        _dlRepoMock
            .Setup(r => r.GetByIdAsync(deadLetter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deadLetter);
        _dlRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ReviewAsync(deadLetter.Id, "confirmed broker issue");

        deadLetter.IsReviewed.Should().BeTrue();
        deadLetter.ReviewNotes.Should().Be("confirmed broker issue");
        _dlRepoMock.Verify(r => r.UpdateAsync(deadLetter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHealthAsync_WithNoUnreviewedMessages_ReturnsHealthy()
    {
        _dlRepoMock
            .Setup(r => r.GetUnreviewedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _dlRepoMock
            .Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        var result = await _sut.GetHealthAsync();

        result.IsHealthy.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetHealthAsync_WithUnreviewedMessages_ReturnsUnhealthyWithCount()
    {
        _dlRepoMock
            .Setup(r => r.GetUnreviewedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        _dlRepoMock
            .Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var result = await _sut.GetHealthAsync();

        result.IsHealthy.Should().BeFalse();
        result.ErrorMessage.Should().Contain("4");
    }

    [Fact]
    public async Task RequeueAsync_WhenDeadLetterNotFound_ThrowsDeadLetterException()
    {
        var id = Guid.NewGuid();
        _dlRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeadLetter?)null);

        var act = async () => await _sut.RequeueAsync(id, "retry after infra fix");

        await act.Should().ThrowAsync<DeadLetterException>();
    }

    [Fact]
    public async Task RequeueAsync_WhenOriginalMessageExists_ResetsToPendingAndMarksRequeued()
    {
        var message = BuildFailedMessage();
        message.State = OutboxMessageState.Failed;
        message.PublishAttempts = 5;
        message.ErrorMessage = "old error";

        var deadLetter = DeadLetter.FromOutboxMessage(message);

        _dlRepoMock
            .Setup(r => r.GetByIdAsync(deadLetter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deadLetter);
        _outboxRepoMock
            .Setup(r => r.GetByIdAsync(deadLetter.OutboxMessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _outboxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dlRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.RequeueAsync(deadLetter.Id, "infrastructure repaired");

        message.State.Should().Be(OutboxMessageState.Pending);
        message.PublishAttempts.Should().Be(0);
        message.ErrorMessage.Should().BeNull();

        deadLetter.IsRequeued.Should().BeTrue();
        deadLetter.RequeueReason.Should().Be("infrastructure repaired");
    }

    private static OutboxMessage BuildFailedMessage() => new()
    {
        Id = Guid.NewGuid(),
        IdempotencyKey = "key-fail-01",
        AggregateId = "order-99",
        AggregateType = "Order",
        EventType = EventType.Created,
        EventData = "{\"id\":99}",
        EventTypeName = "OrderCreatedEvent",
        Topic = "orders.failed",
        PublishAttempts = 5,
        MaxPublishAttempts = 5,
        CreatedAt = DateTime.UtcNow.AddHours(-1),
        ErrorMessage = "connection refused"
    };
}
