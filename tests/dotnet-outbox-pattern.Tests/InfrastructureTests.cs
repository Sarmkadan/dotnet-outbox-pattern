#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Events;
using DotnetOutboxPattern.Exceptions;
using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for the SerializationHelper class.
/// </summary>
public sealed class SerializationHelperTests
{
    [Fact]
    public void Serialize_ProducesCamelCasePropertyNames()
    {
        /// <summary>
        /// Verifies that the Serialize method produces camel case property names.
        /// </summary>
        var stats = new OutboxStatistics
        {
            TotalMessages = 100,
            PublishedMessages = 90,
            FailedMessages = 10
        };

        var json = SerializationHelper.Serialize(stats);

        json.Should().Contain("\"totalMessages\"");
        json.Should().Contain("\"publishedMessages\"");
        json.Should().NotContain("\"TotalMessages\"");
    }

    [Fact]
    public void Deserialize_WithValidJson_ReturnsMappedObject()
    {
        /// <summary>
        /// Verifies that the Deserialize method returns a mapped object when given valid JSON.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        var json = """{"totalMessages":50,"publishedMessages":45,"failedMessages":5}""";

        var result = SerializationHelper.Deserialize<OutboxStatistics>(json);

        result.TotalMessages.Should().Be(50);
        result.PublishedMessages.Should().Be(45);
        result.FailedMessages.Should().Be(5);
    }

    [Fact]
    public void Serialize_ThenDeserialize_PreservesHealthMetricValues()
    {
        /// <summary>
        /// Verifies that the Serialize and Deserialize methods preserve health metric values.
        /// </summary>
        var original = new HealthMetrics
        {
            IsHealthy = false,
            ConsecutiveFailures = 7,
            LockedMessagesCount = 2,
            HasExpiredLocks = true
        };

        var json = SerializationHelper.Serialize(original);
        var restored = SerializationHelper.Deserialize<HealthMetrics>(json);

        restored.IsHealthy.Should().BeFalse();
        restored.ConsecutiveFailures.Should().Be(7);
        restored.LockedMessagesCount.Should().Be(2);
        restored.HasExpiredLocks.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_WithInvalidJson_ThrowsSerializationException()
    {
        /// <summary>
        /// Verifies that the Deserialize method throws a SerializationException when given invalid JSON.
        /// </summary>
        var act = () => SerializationHelper.Deserialize<OutboxStatistics>("{{not valid json}}");

        act.Should().Throw<SerializationException>();
    }

    [Fact]
    public void IsValidJson_WithWellFormedObject_ReturnsTrue()
    {
        /// <summary>
        /// Verifies that the IsValidJson method returns true for well-formed JSON objects.
        /// </summary>
        SerializationHelper.IsValidJson("""{"key":"value","num":42}""").Should().BeTrue();
    }

    [Fact]
    public void IsValidJson_WithJsonArray_ReturnsTrue()
    {
        /// <summary>
        /// Verifies that the IsValidJson method returns true for JSON arrays.
        /// </summary>
        SerializationHelper.IsValidJson("[1,2,3]").Should().BeTrue();
    }

    [Fact]
    public void IsValidJson_WithMalformedJson_ReturnsFalse()
    {
        /// <summary>
        /// Verifies that the IsValidJson method returns false for malformed JSON.
        /// </summary>
        SerializationHelper.IsValidJson("{broken: json").Should().BeFalse();
    }

    [Fact]
    public void SerializePretty_ProducesIndentedMultilineOutput()
    {
        /// <summary>
        /// Verifies that the SerializePretty method produces indented multiline output.
        /// </summary>
        var stats = new OutboxStatistics { TotalMessages = 1 };

        var json = SerializationHelper.SerializePretty(stats);

        json.Should().Contain("\n");
        json.Length.Should().BeGreaterThan(SerializationHelper.Serialize(stats).Length);
    }

    [Fact]
    public void Serialize_OmitsNullReferenceAndNullableValueProperties()
    {
        /// <summary>
        /// Verifies that the Serialize method omits null reference and nullable value properties.
        /// </summary>
        var metrics = new HealthMetrics
        {
            ErrorMessage = null,
            LastSuccessfulPublish = null
        };

        var json = SerializationHelper.Serialize(metrics);

        json.Should().NotContain("\"errorMessage\"");
        json.Should().NotContain("\"lastSuccessfulPublish\"");
    }
}

/// <summary>
/// Tests for the EventPublisher class.
/// </summary>
public sealed class EventPublisherTests
{
    private readonly Mock<ILogger<EventPublisher>> _loggerMock;
    private readonly EventPublisher _sut;

    public EventPublisherTests()
    {
        _loggerMock = new Mock<ILogger<EventPublisher>>();
        _sut = new EventPublisher(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        /// <summary>
        /// Verifies that the EventPublisher constructor throws an ArgumentNullException when given a null logger.
        /// </summary>
        var act = () => new EventPublisher(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task Subscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        /// <summary>
        /// Verifies that the Subscribe method throws an ArgumentNullException when given a null handler.
        /// </summary>
        var act = () => _sut.Subscribe<MessagePublishedEvent>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        /// <summary>
        /// Verifies that the PublishAsync method throws an ArgumentNullException when given a null event.
        /// </summary>
        var act = async () => await _sut.PublishAsync<MessagePublishedEvent>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_CompletesWithoutThrowing()
    {
        /// <summary>
        /// Verifies that the PublishAsync method completes without throwing when there are no subscribers.
        /// </summary>
        var @event = new MessagePublishedEvent { MessageId = Guid.NewGuid(), AggregateId = "agg-1" };

        var act = async () => await _sut.PublishAsync(@event);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithSubscriber_InvokesHandlerExactlyOnce()
    {
        /// <summary>
        /// Verifies that the PublishAsync method invokes the handler exactly once when there is a subscriber.
        /// </summary>
        var captured = new List<MessagePublishedEvent>();
        using var _ = _sut.Subscribe<MessagePublishedEvent>(e =>
        {
            captured.Add(e);
            return Task.CompletedTask;
        });

        var expected = new MessagePublishedEvent { MessageId = Guid.NewGuid(), AggregateId = "agg-2" };
        await _sut.PublishAsync(expected);

        captured.Should().ContainSingle(e => e.MessageId == expected.MessageId);
    }

    [Fact]
    public async Task Dispose_Subscription_StopsDeliveryToRemovedHandler()
    {
        /// <summary>
        /// Verifies that the Dispose method stops delivery to the removed handler.
        /// </summary>
        var callCount = 0;
        var subscription = _sut.Subscribe<MessagePublishFailedEvent>(_ =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        subscription.Dispose();

        await _sut.PublishAsync(new MessagePublishFailedEvent { MessageId = Guid.NewGuid() });

        callCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerThrows_DoesNotPropagateExceptionToCaller()
    {
        /// <summary>
        /// Verifies that the PublishAsync method does not propagate the exception to the caller when the handler throws.
        /// </summary>
        using var _ = _sut.Subscribe<MessageMovedToDeadLetterEvent>(_ =>
            Task.FromException(new InvalidOperationException("handler exploded")));

        var @event = new MessageMovedToDeadLetterEvent { MessageId = Guid.NewGuid(), Reason = "max retries reached" };
        var act = async () => await _sut.PublishAsync(@event);

        await act.Should().NotThrowAsync();
    }
}
