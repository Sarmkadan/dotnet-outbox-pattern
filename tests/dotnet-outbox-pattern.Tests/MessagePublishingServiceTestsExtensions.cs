#nullable enable

using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

public static class MessagePublishingServiceTestsExtensions
{
    /// <summary>
    /// Creates a MessagePublishingService instance with mocked dependencies for testing.
    /// </summary>
    /// <param name="outboxRepoMock">Mock of IOutboxRepository</param>
    /// <param name="dlRepoMock">Mock of IDeadLetterRepository</param>
    /// <param name="publisherMock">Mock of IMessagePublisher</param>
    /// <param name="loggerMock">Mock of ILogger</param>
    /// <param name="options">PublishingOptions (optional, defaults to MaxRetries=5)</param>
    /// <returns>Configured MessagePublishingService instance</returns>
    public static MessagePublishingService CreateService(
        this Mock<IOutboxRepository> outboxRepoMock,
        Mock<IDeadLetterRepository>? dlRepoMock = null,
        Mock<IMessagePublisher>? publisherMock = null,
        Mock<ILogger<MessagePublishingService>>? loggerMock = null,
        PublishingOptions? options = null)
    {
        dlRepoMock ??= new Mock<IDeadLetterRepository>();
        publisherMock ??= new Mock<IMessagePublisher>();
        loggerMock ??= new Mock<ILogger<MessagePublishingService>>();
        options ??= new PublishingOptions { MaxRetries = 5 };

        return new MessagePublishingService(
            outboxRepoMock.Object,
            dlRepoMock.Object,
            publisherMock.Object,
            loggerMock.Object,
            options);
    }

    /// <summary>
    /// Creates a test message with default pending state.
    /// </summary>
    /// <param name="id">Optional message ID</param>
    /// <returns>Configured OutboxMessage</returns>
    public static OutboxMessage CreateTestMessage(this MessagePublishingServiceTests _, Guid? id = null)
    {
        return new OutboxMessage
        {
            Id = id ?? Guid.NewGuid(),
            IdempotencyKey = $"key-{Guid.NewGuid()}",
            AggregateId = "test-agg-1",
            AggregateType = "TestAggregate",
            EventType = EventType.Created,
            EventData = "{\"test\":true}",
            EventTypeName = "TestEvent",
            Topic = "tests.created",
            State = OutboxMessageState.Pending,
            MaxPublishAttempts = 5,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Verifies that the publisher was called exactly once with the specified message.
    /// </summary>
    /// <param name="publisherMock">Publisher mock</param>
    /// <param name="message">Expected message</param>
    public static void VerifyPublishCalledOnceWith(
        this Mock<IMessagePublisher> publisherMock,
        OutboxMessage message)
    {
        publisherMock.Verify(
            p => p.PublishAsync(
                It.Is<OutboxMessage>(m => m.Id == message.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Sets up the outbox repository to return a specific message by ID.
    /// </summary>
    /// <param name="outboxRepoMock">Outbox repository mock</param>
    /// <param name="message">Message to return</param>
    /// <returns>Setup chain for fluent API</returns>
    public static Mock<IOutboxRepository> SetupGetByIdAsync(
        this Mock<IOutboxRepository> outboxRepoMock,
        OutboxMessage message)
    {
        outboxRepoMock
            .Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        return outboxRepoMock;
    }

    /// <summary>
    /// Sets up the outbox repository to return a list of messages.
    /// </summary>
    /// <param name="outboxRepoMock">Outbox repository mock</param>
    /// <param name="messages">Messages to return</param>
    /// <returns>Setup chain for fluent API</returns>
    public static Mock<IOutboxRepository> SetupGetPendingMessagesAsync(
        this Mock<IOutboxRepository> outboxRepoMock,
        IEnumerable<OutboxMessage> messages)
    {
        outboxRepoMock
            .Setup(r => r.GetPendingMessagesAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages.ToList());
        return outboxRepoMock;
    }

    /// <summary>
    /// Sets up the publisher to complete successfully for a message.
    /// </summary>
    /// <param name="publisherMock">Publisher mock</param>
    /// <param name="message">Message to setup</param>
    /// <returns>Setup chain for fluent API</returns>
    public static Mock<IMessagePublisher> SetupPublishSuccess(
        this Mock<IMessagePublisher> publisherMock,
        OutboxMessage message)
    {
        publisherMock
            .Setup(p => p.PublishAsync(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return publisherMock;
    }

    /// <summary>
    /// Sets up the outbox repository to update successfully.
    /// </summary>
    /// <param name="outboxRepoMock">Outbox repository mock</param>
    /// <returns>Setup chain for fluent API</returns>
    public static Mock<IOutboxRepository> SetupUpdateSuccess(this Mock<IOutboxRepository> outboxRepoMock)
    {
        outboxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return outboxRepoMock;
    }
}