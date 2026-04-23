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

public sealed class SerializationHelperTests
{
    [Fact]
    public void Serialize_ProducesCamelCasePropertyNames()
    {
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
        var json = """{"totalMessages":50,"publishedMessages":45,"failedMessages":5}""";

        var result = SerializationHelper.Deserialize<OutboxStatistics>(json);

        result.TotalMessages.Should().Be(50);
        result.PublishedMessages.Should().Be(45);
        result.FailedMessages.Should().Be(5);
    }

    [Fact]
    public void Serialize_ThenDeserialize_PreservesHealthMetricValues()
    {
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
        var act = () => SerializationHelper.Deserialize<OutboxStatistics>("{{not valid json}}");

        act.Should().Throw<SerializationException>();
    }

    [Fact]
    public void IsValidJson_WithWellFormedObject_ReturnsTrue()
    {
        SerializationHelper.IsValidJson("""{"key":"value","num":42}""").Should().BeTrue();
    }

    [Fact]
    public void IsValidJson_WithJsonArray_ReturnsTrue()
    {
        SerializationHelper.IsValidJson("[1,2,3]").Should().BeTrue();
    }

    [Fact]
    public void IsValidJson_WithMalformedJson_ReturnsFalse()
    {
        SerializationHelper.IsValidJson("{broken: json").Should().BeFalse();
    }

    [Fact]
    public void SerializePretty_ProducesIndentedMultilineOutput()
    {
        var stats = new OutboxStatistics { TotalMessages = 1 };

        var json = SerializationHelper.SerializePretty(stats);

        json.Should().Contain("\n");
        json.Length.Should().BeGreaterThan(SerializationHelper.Serialize(stats).Length);
    }

    [Fact]
    public void Serialize_OmitsNullReferenceAndNullableValueProperties()
    {
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
        var act = () => new EventPublisher(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task Subscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        var act = () => _sut.Subscribe<MessagePublishedEvent>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.PublishAsync<MessagePublishedEvent>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_CompletesWithoutThrowing()
    {
        var @event = new MessagePublishedEvent { MessageId = Guid.NewGuid(), AggregateId = "agg-1" };

        var act = async () => await _sut.PublishAsync(@event);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithSubscriber_InvokesHandlerExactlyOnce()
    {
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
        using var _ = _sut.Subscribe<MessageMovedToDeadLetterEvent>(_ =>
            Task.FromException(new InvalidOperationException("handler exploded")));

        var @event = new MessageMovedToDeadLetterEvent { MessageId = Guid.NewGuid(), Reason = "max retries reached" };
        var act = async () => await _sut.PublishAsync(@event);

        await act.Should().NotThrowAsync();
    }
}
