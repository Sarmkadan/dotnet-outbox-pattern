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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="outboxRepoMock"/> is null.</exception>
    public static MessagePublishingService CreateService(
        this Mock<IOutboxRepository> outboxRepoMock,
        Mock<IDeadLetterRepository>? dlRepoMock = null,
        Mock<IMessagePublisher>? publisherMock = null,
        Mock<ILogger<MessagePublishingService>>? loggerMock = null,
        PublishingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(outboxRepoMock);

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
    /// <param name="aggregateId">Aggregate ID (optional)</param>
    /// <param name="eventType">Event type (optional)</param>
    /// <param name="topic">Topic name (optional)</param>
    /// <param name="state">Message state (optional, defaults to Pending)</param>
    /// <returns>Configured OutboxMessage</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="aggregateId"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="topic"/> is null or empty.</exception>
    public static OutboxMessage CreateTestMessage(
        this MessagePublishingServiceTests _,
        Guid? id = null,
        string? aggregateId = null,
        EventType? eventType = null,
        string? topic = null,
        OutboxMessageState state = OutboxMessageState.Pending)
    {
        return new OutboxMessage
        {
            Id = id ?? Guid.NewGuid(),
            IdempotencyKey = $"key-{Guid.NewGuid()}",
            AggregateId = aggregateId ?? "test-agg-1",
            AggregateType = "TestAggregate",
            EventType = eventType ?? EventType.Created,
            EventData = "{\"test\":true}",
            EventTypeName = "TestEvent",
            Topic = topic ?? "tests.created",
            State = state,
            MaxPublishAttempts = 5,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Verifies that the publisher was called exactly once with the specified message.
    /// </summary>
    /// <param name="publisherMock">Publisher mock</param>
    /// <param name="message">Expected message</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisherMock"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
    public static void VerifyPublishCalledOnceWith(
        this Mock<IMessagePublisher> publisherMock,
        OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(publisherMock);
        ArgumentNullException.ThrowIfNull(message);

        publisherMock.Verify(
            p => p.PublishAsync(
                It.Is<OutboxMessage>(m => m.Id == message.Id && m.Topic == message.Topic),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Sets up the outbox repository to return a specific message by ID.
    /// </summary>
    /// <param name="outboxRepoMock">Outbox repository mock</param>
    /// <param name="message">Message to return</param>
    /// <returns>Setup chain for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="outboxRepoMock"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
    public static Mock<IOutboxRepository> SetupGetByIdAsync(
        this Mock<IOutboxRepository> outboxRepoMock,
        OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(outboxRepoMock);
        ArgumentNullException.ThrowIfNull(message);

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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="outboxRepoMock"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="messages"/> is null.</exception>
    public static Mock<IOutboxRepository> SetupGetPendingMessagesAsync(
        this Mock<IOutboxRepository> outboxRepoMock,
        IEnumerable<OutboxMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(outboxRepoMock);
        ArgumentNullException.ThrowIfNull(messages);

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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisherMock"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
    public static Mock<IMessagePublisher> SetupPublishSuccess(
        this Mock<IMessagePublisher> publisherMock,
        OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(publisherMock);
        ArgumentNullException.ThrowIfNull(message);

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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="outboxRepoMock"/> is null.</exception>
    public static Mock<IOutboxRepository> SetupUpdateSuccess(this Mock<IOutboxRepository> outboxRepoMock)
    {
        ArgumentNullException.ThrowIfNull(outboxRepoMock);

        outboxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return outboxRepoMock;
    }

    /// <summary>
    /// Sets up the outbox repository to throw an exception when getting a message.
    /// </summary>
    /// <param name="outboxRepoMock">Outbox repository mock</param>
    /// <param name="exception">Exception to throw</param>
    /// <returns>Setup chain for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="outboxRepoMock"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static Mock<IOutboxRepository> SetupGetByIdAsyncThrows(
        this Mock<IOutboxRepository> outboxRepoMock,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(outboxRepoMock);
        ArgumentNullException.ThrowIfNull(exception);

        outboxRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        return outboxRepoMock;
    }

    /// <summary>
    /// Sets up the publisher to throw an exception for a message.
    /// </summary>
    /// <param name="publisherMock">Publisher mock</param>
    /// <param name="exception">Exception to throw</param>
    /// <returns>Setup chain for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisherMock"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static Mock<IMessagePublisher> SetupPublishThrows(
        this Mock<IMessagePublisher> publisherMock,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(publisherMock);
        ArgumentNullException.ThrowIfNull(exception);

        publisherMock
            .Setup(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        return publisherMock;
    }

    /// <summary>
    /// Sets up the dead letter repository to add successfully.
    /// </summary>
    /// <param name="dlRepoMock">Dead letter repository mock</param>
    /// <returns>Setup chain for fluent API</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dlRepoMock"/> is null.</exception>
    public static Mock<IDeadLetterRepository> SetupAddDeadLetterSuccess(
        this Mock<IDeadLetterRepository> dlRepoMock)
    {
        ArgumentNullException.ThrowIfNull(dlRepoMock);

        dlRepoMock
            .Setup(r => r.AddAsync(It.IsAny<DeadLetter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeadLetter dl, CancellationToken _) => dl);
        return dlRepoMock;
    }
}