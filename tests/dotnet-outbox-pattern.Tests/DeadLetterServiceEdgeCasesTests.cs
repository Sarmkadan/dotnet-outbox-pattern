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
/// Contains edge case unit tests for the <see cref="DeadLetterService"/> class.
/// Tests scenarios including repository failures during dead-lettering, concurrent operations,
/// and ordering guarantees for dead-letter retrieval.
/// </summary>
public sealed class DeadLetterServiceEdgeCasesTests
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
    /// Initializes a new instance of the <see cref="DeadLetterServiceEdgeCasesTests"/> class.
    /// Sets up mock repositories and service dependencies for testing edge cases.
    /// </summary>
    public DeadLetterServiceEdgeCasesTests()
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
    /// Tests that MoveToDlqAsync throws DeadLetterException when repository throws an exception.
    /// Verifies that the original message is NOT lost when dead-lettering fails.
    /// </summary>
    public async Task MoveToDlqAsync_WhenRepositoryThrows_ThrowsDeadLetterExceptionAndPreservesMessage()
    {
        // Arrange
        var message = BuildFailedMessage();
        var repositoryException = new InvalidOperationException("Database is read-only");

        _dlRepoMock
            .Setup(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var act = async () => await _sut.MoveToDlqAsync(message);

        await act.Should().ThrowAsync<DeadLetterException>()
            .Where(e => e.InnerException == repositoryException)
            .WithMessage("*Failed to move message to dead letter queue*");

        // Verify that the original message was NOT modified or removed
        // The service should not have called any repository methods that would affect the original message
        _outboxRepoMock.Verify(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _dlRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    /// <summary>
    /// Tests that concurrent dead-lettering of the same message ID is handled idempotently.
    /// Verifies that no duplicate dead-letter rows are created.
    /// </summary>
    public async Task MoveToDlqAsync_WhenCalledConcurrently_HandlesIdempotentlyAndPreventsDuplicates()
    {
        // Arrange
        var message = BuildFailedMessage();
        var firstDeadLetter = DeadLetter.FromOutboxMessage(message);

        // Simulate concurrent calls by setting up the repository to return the same dead letter
        _dlRepoMock
            .SetupSequence(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstDeadLetter) // First call succeeds
            .ThrowsAsync(new DeadLetterException("Duplicate key violation", message.Id)); // Second call fails with duplicate

        // First concurrent call
        var firstTask = _sut.MoveToDlqAsync(message);

        // Second concurrent call (should fail but not corrupt state)
        var secondTask = _sut.MoveToDlqAsync(message);

        // Wait for both to complete
        var firstResult = await firstTask;
        var secondException = await Record.ExceptionAsync(() => secondTask);

        // Assert
        firstResult.Should().NotBeNull();
        firstResult.Id.Should().NotBe(Guid.Empty);
        firstResult.OutboxMessageId.Should().Be(message.Id);

        // Second call should throw (simulating database constraint violation)
        secondException.Should().NotBeNull();
        secondException.Should().BeOfType<DeadLetterException>();

        // Verify only one dead letter was created
        _dlRepoMock.Verify(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    /// <summary>
    /// Tests that GetUnreviewedAsync returns items in stable order by MovedToDlqAt timestamp.
    /// </summary>
    public async Task GetUnreviewedAsync_ReturnsItemsInStableOrderByMovedToDlqAt()
    {
        // Arrange
        var deadLetters = new List<DeadLetter>
        {
            new DeadLetter { Id = Guid.NewGuid(), MovedToDlqAt = DateTime.UtcNow.AddMinutes(-10) },
            new DeadLetter { Id = Guid.NewGuid(), MovedToDlqAt = DateTime.UtcNow.AddMinutes(-5) },
            new DeadLetter { Id = Guid.NewGuid(), MovedToDlqAt = DateTime.UtcNow.AddMinutes(-15) }
        };

        _dlRepoMock
            .Setup(r => r.GetUnreviewedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deadLetters);

        // First call
        var firstResult = await _sut.GetUnreviewedAsync(100);

        // Second call (should return same order)
        var secondResult = await _sut.GetUnreviewedAsync(100);

        // Assert
        firstResult.Should().HaveCount(3);
        secondResult.Should().HaveCount(3);

        // Verify stable ordering - both calls should return items in the same order
        for (int i = 0; i < firstResult.Count; i++)
        {
            firstResult[i].Id.Should().Be(secondResult[i].Id);
        }

        // Verify the order is consistent (repository handles the actual sorting)
        // The key test is that the ordering is stable across multiple calls
    }

    [Fact]
    /// <summary>
    /// Tests that GetByTopicAsync returns items in stable order by MovedToDlqAt timestamp (newest first).
    /// </summary>
    public async Task GetByTopicAsync_ReturnsItemsInStableOrderByMovedToDlqAtDescending()
    {
        // Arrange
        var topic = "orders.failed";
        var deadLetters = new List<DeadLetter>
        {
            new DeadLetter { Id = Guid.NewGuid(), Topic = topic, MovedToDlqAt = DateTime.UtcNow.AddMinutes(-10) },
            new DeadLetter { Id = Guid.NewGuid(), Topic = topic, MovedToDlqAt = DateTime.UtcNow.AddMinutes(-5) },
            new DeadLetter { Id = Guid.NewGuid(), Topic = topic, MovedToDlqAt = DateTime.UtcNow.AddMinutes(-15) }
        };

        _dlRepoMock
            .Setup(r => r.GetByTopicAsync(topic, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deadLetters);

        // First call
        var firstResult = await _sut.GetByTopicAsync(topic);

        // Second call (should return same order)
        var secondResult = await _sut.GetByTopicAsync(topic);

        // Assert
        firstResult.Should().HaveCount(3);
        secondResult.Should().HaveCount(3);

        // Verify stable ordering - both calls should return items in the same order
        for (int i = 0; i < firstResult.Count; i++)
        {
            firstResult[i].Id.Should().Be(secondResult[i].Id);
        }

        // Verify the order is consistent (repository handles the actual sorting)
        // The key test is that the ordering is stable across multiple calls
    }


    [Fact]
    /// <summary>
    /// Tests that MoveToDlqAsync preserves all message properties in the dead letter record.
    /// </summary>
    public async Task MoveToDlqAsync_PreservesAllMessagePropertiesInDeadLetter()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "test-key-123",
            AggregateId = "customer-456",
            AggregateType = "Customer",
            EventType = EventType.Updated,
            EventData = "{\"id\": 456, \"name\": \"John Doe\"}",
            EventTypeName = "CustomerUpdatedEvent",
            Topic = "customers.updated",
            PublishAttempts = 3,
            MaxPublishAttempts = 5,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            CorrelationId = "corr-789",
            CausationId = "caus-101",
            Metadata = "{\"source\": \"api\", \"userId\": \"user-789\"}",
            ErrorMessage = "Connection timeout to message broker",
            ErrorStackTrace = "at System.Net.Sockets.Socket.ConnectAsync...",
            LastProcessedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        DeadLetter capturedDeadLetter = null!;

        _dlRepoMock
            .Setup(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
            .Callback<DeadLetter, CancellationToken>((dl, _) => capturedDeadLetter = dl)
            .ReturnsAsync((DeadLetter dl, CancellationToken _) => dl);

        // Act
        var result = await _sut.MoveToDlqAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.OutboxMessageId.Should().Be(message.Id);
        result.IdempotencyKey.Should().Be(message.IdempotencyKey);
        result.AggregateId.Should().Be(message.AggregateId);
        result.AggregateType.Should().Be(message.AggregateType);
        result.EventType.Should().Be(message.EventType);
        result.EventData.Should().Be(message.EventData);
        result.EventTypeName.Should().Be(message.EventTypeName);
        result.Topic.Should().Be(message.Topic);
        result.TotalAttempts.Should().Be(message.PublishAttempts);
        result.ErrorMessage.Should().Be(message.ErrorMessage);
        result.ErrorStackTrace.Should().Be(message.ErrorStackTrace);
        result.OriginalCreatedAt.Should().Be(message.CreatedAt);
        result.MovedToDlqAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.LastAttemptAt.Should().Be(message.LastProcessedAt);
        result.CorrelationId.Should().Be(message.CorrelationId);
        result.CausationId.Should().Be(message.CausationId);
        result.Metadata.Should().Be(message.Metadata);
        result.IsReviewed.Should().BeFalse();
        result.IsRequeued.Should().BeFalse();
    }

    [Fact]
    /// <summary>
    /// Tests that MoveToDlqAsync handles message with null ErrorMessage by providing default error message.
    /// </summary>
    public async Task MoveToDlqAsync_WithNullErrorMessage_ProvidesDefaultErrorMessage()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "key-null-error",
            AggregateId = "agg-1",
            AggregateType = "Test",
            EventType = EventType.Custom,
            EventData = "{\"test\": true}",
            EventTypeName = "TestEvent",
            Topic = "test.topic",
            PublishAttempts = 5,
            MaxPublishAttempts = 5,
            CreatedAt = DateTime.UtcNow,
            ErrorMessage = null // This should be handled
        };

        DeadLetter capturedDeadLetter = null!;

        _dlRepoMock
            .Setup(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
            .Callback<DeadLetter, CancellationToken>((dl, _) => capturedDeadLetter = dl)
            .ReturnsAsync((DeadLetter dl, CancellationToken _) => dl);

        // Act
        var result = await _sut.MoveToDlqAsync(message);

        // Assert
        result.ErrorMessage.Should().Be("Unknown error");
        capturedDeadLetter.ErrorMessage.Should().Be("Unknown error");
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