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

/// <summary>
/// Contains unit tests for the <see cref="DeadLetterService"/> class.
/// Tests various scenarios including moving messages to dead-letter queue, reviewing dead letters,
/// requeuing failed messages, health checks, and repository error handling.
/// </summary>
public sealed class DeadLetterServiceTests
{
/// <summary>
/// Mock repository for dead letter queue operations.
/// </summary>
private readonly Mock<IDeadLetterRepository> _dlRepoMock;

/// <summary>
/// Mock repository for outbox message persistence.
/// </summary>
private readonly Mock<IOutboxRepository> _outboxRepoMock;

/// <summary>
/// Mock service for outbox message operations.
/// </summary>
private readonly Mock<IOutboxService> _outboxServiceMock;

/// <summary>
/// Mock logger for dead letter service operations.
/// </summary>
private readonly Mock<ILogger<DeadLetterService>> _loggerMock;

/// <summary>
/// System under test - the dead letter service being tested.
/// </summary>
private readonly DeadLetterService _sut;

/// <summary>
/// Initializes a new instance of the <see cref="DeadLetterServiceTests"/> class.
/// Sets up mock repositories and service dependencies for testing dead letter queue functionality.
/// </summary>
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
/// <summary>
/// Tests that the constructor throws ArgumentNullException when dead letter repository is null.
/// </summary>
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
/// <summary>
/// Tests that the constructor throws ArgumentNullException when outbox repository is null.
/// </summary>
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
/// <summary>
/// Tests that the constructor throws ArgumentNullException when logger is null.
/// </summary>
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
/// <summary>
/// Tests that MoveToDlqAsync throws DeadLetterException when message is null.
/// </summary>
public async Task MoveToDlqAsync_WithNullMessage_ThrowsDeadLetterException()
{
var act = async () => await _sut.MoveToDlqAsync(null!);

await act.Should().ThrowAsync<DeadLetterException>();
}

[Fact]
/// <summary>
/// Tests that MoveToDlqAsync successfully moves a valid message to dead-letter queue.
/// Verifies that the dead letter is created with correct properties and persisted.
/// </summary>
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
/// <summary>
/// Tests that ReviewAsync throws DeadLetterException when dead letter is not found.
/// </summary>
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
/// <summary>
/// Tests that ReviewAsync successfully marks a dead letter as reviewed.
/// Verifies that the dead letter is updated with review notes and marked as reviewed.
/// </summary>
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
/// <summary>
/// Tests that GetHealthAsync returns healthy status when there are no unreviewed messages.
/// </summary>
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
/// <summary>
/// Tests that GetHealthAsync returns unhealthy status when there are unreviewed messages.
/// Verifies that the error message contains the unreviewed count.
/// </summary>
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
/// <summary>
/// Tests that RequeueAsync throws DeadLetterException when dead letter is not found.
/// </summary>
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
/// <summary>
/// Tests that RequeueAsync successfully requeues a message when original message exists.
/// Verifies that the original message state is reset to Pending and publish attempts are cleared.
/// </summary>
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

[Fact]
/// <summary>
/// Tests that GetUnreviewedAsync delegates to repository and returns unreviewed dead letters.
/// </summary>
public async Task GetUnreviewedAsync_DelegatesToRepository()
{
var deadLetters = new List<DeadLetter> { new DeadLetter(), new DeadLetter() };
_dlRepoMock
.Setup(r => r.GetUnreviewedAsync(50, It.IsAny<CancellationToken>()))
.ReturnsAsync(deadLetters);

var result = await _sut.GetUnreviewedAsync(50);

result.Should().BeSameAs(deadLetters);
_dlRepoMock.Verify(r => r.GetUnreviewedAsync(50, It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
/// <summary>
/// Tests that GetByTopicAsync delegates to repository and returns dead letters filtered by topic.
/// </summary>
public async Task GetByTopicAsync_DelegatesToRepository()
{
var topic = "orders.failed";
var deadLetters = new List<DeadLetter> { new DeadLetter() };
_dlRepoMock
.Setup(r => r.GetByTopicAsync(topic, It.IsAny<CancellationToken>()))
.ReturnsAsync(deadLetters);

var result = await _sut.GetByTopicAsync(topic);

result.Should().BeSameAs(deadLetters);
_dlRepoMock.Verify(r => r.GetByTopicAsync(topic, It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
/// <summary>
/// Tests that DeleteAsync delegates to repository and removes the dead letter.
/// </summary>
public async Task DeleteAsync_DelegatesToRepository()
{
var id = Guid.NewGuid();
_dlRepoMock
.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
.Returns(Task.CompletedTask);

await _sut.DeleteAsync(id);

_dlRepoMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
/// <summary>
/// Tests that RequeueAsync creates a new message when original message does not exist.
/// Verifies that a new outbox message is created with the same aggregate ID.
/// </summary>
public async Task RequeueAsync_WhenOriginalMessageDoesNotExist_CreatesNewMessage()
{
var message = BuildFailedMessage();
var deadLetter = DeadLetter.FromOutboxMessage(message);

_dlRepoMock
.Setup(r => r.GetByIdAsync(deadLetter.Id, It.IsAny<CancellationToken>()))
.ReturnsAsync(deadLetter);
_outboxRepoMock
.Setup(r => r.GetByIdAsync(deadLetter.OutboxMessageId, It.IsAny<CancellationToken>()))
.ReturnsAsync((OutboxMessage?)null);
_outboxRepoMock
.Setup(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
.ReturnsAsync((OutboxMessage msg, CancellationToken _) => msg);
_dlRepoMock
.Setup(r => r.UpdateAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
.Returns(Task.CompletedTask);

await _sut.RequeueAsync(deadLetter.Id, "new message created");

_outboxRepoMock.Verify(
r => r.AddAsync(It.Is<OutboxMessage>(m => m.AggregateId == message.AggregateId), It.IsAny<CancellationToken>()),
Times.Once);
}

[Fact]
/// <summary>
/// Tests that GetUnreviewedCountAsync delegates to repository and returns the count of unreviewed dead letters.
/// </summary>
public async Task GetUnreviewedCountAsync_DelegatesToRepository()
{
_dlRepoMock
.Setup(r => r.GetUnreviewedCountAsync(It.IsAny<CancellationToken>()))
.ReturnsAsync(7);

var result = await _sut.GetUnreviewedCountAsync();

result.Should().Be(7);
_dlRepoMock.Verify(r => r.GetUnreviewedCountAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
/// <summary>
/// Tests that MoveToDlqAsync throws DeadLetterException when repository throws an exception.
/// </summary>
public async Task MoveToDlqAsync_WhenRepositoryThrows_ThrowsDeadLetterException()
{
var message = BuildFailedMessage();
_dlRepoMock
.Setup(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
.ThrowsAsync(new InvalidOperationException("db error"));

var act = async () => await _sut.MoveToDlqAsync(message);

await act.Should().ThrowAsync<DeadLetterException>();
}

[Fact]
/// <summary>
/// Tests that ReviewAsync throws DeadLetterException when repository throws an exception.
/// </summary>
public async Task ReviewAsync_WhenRepositoryThrows_ThrowsDeadLetterException()
{
var id = Guid.NewGuid();
_dlRepoMock
.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
.ThrowsAsync(new InvalidOperationException("db error"));

var act = async () => await _sut.ReviewAsync(id, "notes");

await act.Should().ThrowAsync<DeadLetterException>();
}

[Fact]
/// <summary>
/// Tests that RequeueAsync throws DeadLetterException when repository throws an exception.
/// </summary>
public async Task RequeueAsync_WhenRepositoryThrows_ThrowsDeadLetterException()
{
var deadLetter = new DeadLetter { Id = Guid.NewGuid() };
_dlRepoMock
.Setup(r => r.GetByIdAsync(deadLetter.Id, It.IsAny<CancellationToken>()))
.ReturnsAsync(deadLetter);
_dlRepoMock
.Setup(r => r.UpdateAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
.ThrowsAsync(new InvalidOperationException("db error"));

var act = async () => await _sut.RequeueAsync(deadLetter.Id, "reason");

await act.Should().ThrowAsync<DeadLetterException>();
}

[Fact]
/// <summary>
/// Tests that GetHealthAsync throws DeadLetterException when repository throws an exception.
/// </summary>
public async Task GetHealthAsync_WhenRepositoryThrows_ThrowsDeadLetterException()
{
_dlRepoMock
.Setup(r => r.GetUnreviewedCountAsync(It.IsAny<CancellationToken>()))
.ThrowsAsync(new InvalidOperationException("db error"));

var act = async () => await _sut.GetHealthAsync();

await act.Should().ThrowAsync<DeadLetterException>();
}

[Fact]
/// <summary>
/// Tests that GetByTopicAsync throws DeadLetterException when repository throws an exception.
/// </summary>
public async Task GetByTopicAsync_WhenRepositoryThrows_ThrowsDeadLetterException()
{
var topic = "orders.failed";
_dlRepoMock
.Setup(r => r.GetByTopicAsync(topic, It.IsAny<CancellationToken>()))
.ThrowsAsync(new InvalidOperationException("db error"));

var act = async () => await _sut.GetByTopicAsync(topic);

await act.Should().ThrowAsync<DeadLetterException>();
}

[Fact]
/// <summary>
/// Tests that DeleteAsync throws DeadLetterException when repository throws an exception.
/// </summary>
public async Task DeleteAsync_WhenRepositoryThrows_ThrowsDeadLetterException()
{
var id = Guid.NewGuid();
_dlRepoMock
.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
.ThrowsAsync(new InvalidOperationException("db error"));

var act = async () => await _sut.DeleteAsync(id);

await act.Should().ThrowAsync<DeadLetterException>();
}

/// <summary>
/// Creates a test outbox message in a failed state for testing dead letter functionality.
/// </summary>
/// <returns>A failed outbox message with error details.</returns>
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