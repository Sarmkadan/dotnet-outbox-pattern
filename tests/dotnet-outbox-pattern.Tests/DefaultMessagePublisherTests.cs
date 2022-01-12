#nullable enable

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for the DefaultMessagePublisher class.
/// </summary>
public sealed class DefaultMessagePublisherTests
{
    private readonly Mock<ILogger<DefaultMessagePublisher>> _loggerMock;
    private readonly DefaultMessagePublisher _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisherTests"/> class.
    /// </summary>
    public DefaultMessagePublisherTests()
    {
        _loggerMock = new Mock<ILogger<DefaultMessagePublisher>>();
        _sut = new DefaultMessagePublisher(_loggerMock.Object);
    }

    /// <summary>
    /// Verifies that the constructor throws an ArgumentNullException when a null logger is passed.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultMessagePublisher(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that the PublishAsync method completes successfully when a valid message is passed.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithValidMessage_CompletesSuccessfully()
    {
        var message = CreateTestMessage();

        var act = async () => await _sut.PublishAsync(message);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that the PublishAsync method logs message details when a valid message is passed.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithValidMessage_LogsMessageDetails()
    {
        var message = CreateTestMessage();
        message.Topic = "orders.created";
        message.AggregateId = "order-123";

        await _sut.PublishAsync(message);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Publishing message") &&
                    v.ToString()!.Contains(message.Id.ToString()) &&
                    v.ToString()!.Contains("orders.created") &&
                    v.ToString()!.Contains("order-123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that the PublishAsync method does not throw when a null message is passed.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithNullMessage_DoesNotThrow()
    {
        var message = CreateTestMessage();

        var act = async () => await _sut.PublishAsync(message);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that the PublishAsync method respects the CancellationToken.
    /// </summary>
    [Fact]
    public async Task PublishAsync_RespectsCancellationToken()
    {
        var message = CreateTestMessage();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _sut.PublishAsync(message, cts.Token);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that the PublishAsync method publishes each message when multiple messages are passed.
    /// </summary>
    [Fact]
    public async Task PublishAsync_MultipleMessages_PublishesEach()
    {
        var message1 = CreateTestMessage();
        var message2 = CreateTestMessage();

        await _sut.PublishAsync(message1);
        await _sut.PublishAsync(message2);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that the PublishAsync method logs the event type.
    /// </summary>
    [Fact]
    public async Task PublishAsync_LogsEventType()
    {
        var message = CreateTestMessage();
        message.EventType = EventType.Updated;
        message.EventTypeName = "OrderUpdatedEvent";

        await _sut.PublishAsync(message);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Creates a test message.
    /// </summary>
    /// <returns>A test message.</returns>
    private static OutboxMessage CreateTestMessage() => new()
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

public sealed class MessagePublisherFactoryTests
{
    /// <summary>
    /// Verifies that the CreateLoggingPublisher method returns a valid publisher.
    /// </summary>
    [Fact]
    public void CreateLoggingPublisher_ReturnsValidPublisher()
    {
        var loggerMock = new Mock<ILogger>();

        var publisher = MessagePublisherFactory.CreateLoggingPublisher(loggerMock.Object);

        publisher.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the LoggingPublisher publishes a message.
    /// </summary>
    [Fact]
    public async Task LoggingPublisher_PublishesMessage()
    {
        var loggerMock = new Mock<ILogger>();
        var publisher = MessagePublisherFactory.CreateLoggingPublisher(loggerMock.Object);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "test.topic",
            AggregateId = "test-1"
        };

        var act = async () => await publisher.PublishAsync(message);

        await act.Should().NotThrowAsync();
    }
}
