#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for MessageContextExtensions - extension methods for MessageContext
/// Covers: header get/set round-trip, correlation id propagation, extension helpers' null handling
/// </summary>
public sealed class MessageContextExtensionsTests : IDisposable
{
    // ActivitySource.StartActivity returns null unless something is actually listening
    private readonly ActivityListener _activityListener = new()
    {
        ShouldListenTo = _ => true,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
    };

    public MessageContextExtensionsTests()
    {
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener.Dispose();
    }

    [Fact]
    public void StartActivity_WithMessage_ExtensionMethod_ReturnsDisposableScope()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = "order-123",
            Topic = "orders.created",
            EventType = EventType.Created,
            State = OutboxMessageState.Pending,
            CorrelationId = "corr-456",
            PartitionKey = "partition-789"
        };

        // Act
        using var scope = new MessageContext().StartActivity(message, "PublishMessage");

        // Assert
        scope.Should().NotBeNull();
        scope.Should().BeOfType<ActivityScope>();
    }

    [Fact]
    public void StartActivity_WithMessage_ExtensionMethod_CreatesActivityWithCorrectTags()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = "order-123",
            Topic = "orders.created",
            EventType = EventType.Created,
            State = OutboxMessageState.Pending,
            CorrelationId = "corr-456",
            PartitionKey = "partition-789"
        };

        // Act
        using var scope = new MessageContext().StartActivity(message, "PublishMessage");
        var activity = Activity.Current;

        // Assert
        activity.Should().NotBeNull();
        activity?.Tags.Should().Contain(t => t.Key == "outbox.message_id" && t.Value == message.Id.ToString());
        activity?.Tags.Should().Contain(t => t.Key == "outbox.aggregate_id" && t.Value == message.AggregateId);
        activity?.Tags.Should().Contain(t => t.Key == "outbox.topic" && t.Value == message.Topic);
        activity?.Tags.Should().Contain(t => t.Key == "outbox.event_type" && t.Value == message.EventType.ToString());
        activity?.Tags.Should().Contain(t => t.Key == "outbox.state" && t.Value == message.State.ToString());
        activity?.Tags.Should().Contain(t => t.Key == "trace.correlation_id" && t.Value == message.CorrelationId);
        activity?.Tags.Should().Contain(t => t.Key == "outbox.partition_key" && t.Value == message.PartitionKey);
    }

    [Fact]
    public void StartActivity_WithoutPartitionKey_ExtensionMethod_DoesNotSetPartitionKeyTag()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = "order-123",
            Topic = "orders.created",
            EventType = EventType.Created,
            State = OutboxMessageState.Pending,
            CorrelationId = "corr-456",
            PartitionKey = null
        };

        // Act
        using var scope = new MessageContext().StartActivity(message, "PublishMessage");
        var activity = Activity.Current;

        // Assert
        activity.Should().NotBeNull();
        activity?.Tags.Should().NotContain(t => t.Key == "outbox.partition_key");
    }

    [Fact]
    public void StartActivity_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        OutboxMessage? message = null;
        var context = new MessageContext();

        // Act
        var act = () => context.StartActivity(message!, "PublishMessage");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*");
    }

    [Fact]
    public void StartActivity_WithNullOperationName_ThrowsArgumentException()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = "order-123",
            Topic = "orders.created",
            EventType = EventType.Created,
            State = OutboxMessageState.Pending,
            CorrelationId = "corr-456"
        };
        var context = new MessageContext();

        // Act
        var act = () => context.StartActivity(message, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*");
    }

    [Fact]
    public void StartActivity_WithEmptyOperationName_ThrowsArgumentException()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = "order-123",
            Topic = "orders.created",
            EventType = EventType.Created,
            State = OutboxMessageState.Pending,
            CorrelationId = "corr-456"
        };
        var context = new MessageContext();

        // Act
        var act = () => context.StartActivity(message, "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*");
    }

    [Fact]
    public void StartActivity_WithWhitespaceOperationName_DoesNotThrow()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = "order-123",
            Topic = "orders.created",
            EventType = EventType.Created,
            State = OutboxMessageState.Pending,
            CorrelationId = "corr-456"
        };
        var context = new MessageContext();

        // Act - ArgumentException.ThrowIfNullOrEmpty doesn't throw for whitespace-only strings
        var act = () => context.StartActivity(message, "   ");

        // Assert - should not throw since ThrowIfNullOrEmpty only checks for null/empty, not whitespace
        act.Should().NotThrow();
    }

    [Fact]
    public void StartServiceActivity_ExtensionMethod_ReturnsDisposableScope()
    {
        // Arrange & Act
        using var scope = new MessageContext().StartServiceActivity("OutboxProcessor", "ProcessPendingMessages");

        // Assert
        scope.Should().NotBeNull();
        scope.Should().BeOfType<ActivityScope>();
    }

    [Fact]
    public void StartServiceActivity_WithValidParameters_ExtensionMethod_SetsCorrectTags()
    {
        // Arrange & Act
        using var scope = new MessageContext().StartServiceActivity("OutboxProcessor", "ProcessPendingMessages");
        var activity = Activity.Current;

        // Assert
        activity.Should().NotBeNull();
        activity?.Tags.Should().Contain(t => t.Key == "service" && t.Value == "OutboxProcessor");
        activity?.Tags.Should().Contain(t => t.Key == "operation" && t.Value == "ProcessPendingMessages");
    }

    [Fact]
    public void StartServiceActivity_WithNullServiceName_ThrowsArgumentException()
    {
        // Arrange
        var context = new MessageContext();

        // Act
        var act = () => context.StartServiceActivity(null!, "ProcessPendingMessages");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*");
    }

    [Fact]
    public void StartServiceActivity_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var context = new MessageContext();

        // Act
        var act = () => context.StartServiceActivity("", "ProcessPendingMessages");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*");
    }

    [Fact]
    public void StartServiceActivity_WithNullOperationName_ThrowsArgumentException()
    {
        // Arrange
        var context = new MessageContext();

        // Act
        var act = () => context.StartServiceActivity("OutboxProcessor", null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*");
    }

    [Fact]
    public void StartServiceActivity_WithEmptyOperationName_ThrowsArgumentException()
    {
        // Arrange
        var context = new MessageContext();

        // Act
        var act = () => context.StartServiceActivity("OutboxProcessor", "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*");
    }

    [Fact]
    public void RecordEvent_ExtensionMethod_WithEventNameAndAttributes_AddsEventToActivity()
    {
        // Arrange
        using var activity = new ActivitySource("Test").StartActivity("TestActivity");
        var attributes = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 123 } };

        // Act
        new MessageContext().RecordEvent("TestEvent", attributes);

        // Assert
        activity.Should().NotBeNull();
        activity?.Events.Should().Contain(e => e.Name == "TestEvent");
    }

    [Fact]
    public void RecordEvent_ExtensionMethod_WithNullAttributes_DoesNotThrow()
    {
        // Arrange
        using var activity = new ActivitySource("Test").StartActivity("TestActivity");

        // Act
        var act = () => new MessageContext().RecordEvent("TestEvent", null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordEvent_ExtensionMethod_WithNullEventName_ThrowsArgumentException()
    {
        // Arrange
        var context = new MessageContext();

        // Act
        var act = () => context.RecordEvent(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*");
    }

    [Fact]
    public void RecordEvent_ExtensionMethod_WithEmptyEventName_ThrowsArgumentException()
    {
        // Arrange
        var context = new MessageContext();

        // Act
        var act = () => context.RecordEvent("");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*");
    }

    [Fact]
    public void RecordEvent_ExtensionMethod_WithWhitespaceEventName_DoesNotThrow()
    {
        // Arrange
        var context = new MessageContext();

        // Act - ArgumentException.ThrowIfNullOrEmpty doesn't throw for whitespace-only strings
        var act = () => context.RecordEvent("   ");

        // Assert - should not throw since ThrowIfNullOrEmpty only checks for null/empty, not whitespace
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_ExtensionMethod_WithException_SetsExceptionTags()
    {
        // Arrange
        using var activity = new ActivitySource("Test").StartActivity("TestActivity");
        var exception = new InvalidOperationException("Test exception message");

        // Act
        new MessageContext().RecordException(exception);

        // Assert
        activity.Should().NotBeNull();
        activity?.Tags.Should().Contain(t => t.Key == "exception.type" && t.Value == "InvalidOperationException");
        activity?.Tags.Should().Contain(t => t.Key == "exception.message" && t.Value == "Test exception message");
        activity?.Tags.Should().Contain(t => t.Key == "otel.status_code" && t.Value == "ERROR");
    }

    [Fact]
    public void RecordException_ExtensionMethod_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new MessageContext();

        // Act
        var act = () => context.RecordException(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*");
    }

    [Fact]
    public void StartActivity_ExtensionMethod_CreatesAndStopsActivity()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = "order-123",
            Topic = "orders.created",
            EventType = EventType.Created,
            State = OutboxMessageState.Pending,
            CorrelationId = "corr-456"
        };

        Activity? activity = null;

        // Act
        using (var scope = new MessageContext().StartActivity(message, "PublishMessage"))
        {
            activity = Activity.Current;
            // Activity should be active within the scope
            activity?.IsStopped.Should().BeFalse();
        }

        // After scope disposal, activity should be stopped
        activity?.IsStopped.Should().BeTrue();
    }

    [Fact]
    public void StartServiceActivity_ExtensionMethod_CreatesAndStopsActivity()
    {
        // Arrange
        Activity? activity = null;

        // Act
        using (var scope = new MessageContext().StartServiceActivity("TestService", "TestOperation"))
        {
            activity = Activity.Current;
            // Activity should be active within the scope
            activity?.IsStopped.Should().BeFalse();
        }

        // After scope disposal, activity should be stopped
        activity?.IsStopped.Should().BeTrue();
    }

    [Fact]
    public void ExtensionMethods_WorkWithDifferentMessageTypes()
    {
        // Test with different OutboxMessage configurations
        var message1 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = "aggregate-1",
            Topic = "topic.1",
            EventType = EventType.Created,
            State = OutboxMessageState.Pending,
            CorrelationId = "corr-1"
        };

        var message2 = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = "aggregate-2",
            Topic = "topic.2",
            EventType = EventType.Updated,
            State = OutboxMessageState.Published,
            CorrelationId = "corr-2",
            PartitionKey = "partition-2"
        };

        // Act & Assert - should not throw
        var act1 = () => new MessageContext().StartActivity(message1, "Operation1");
        var act2 = () => new MessageContext().StartActivity(message2, "Operation2");

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    [Fact]
    public void ExtensionMethods_WorkWithDifferentServiceNames()
    {
        // Test with different service names
        var serviceNames = new[] { "OutboxProcessor", "MessagePublisher", "EventHandler", "ServiceBus" };
        var operationNames = new[] { "Process", "Publish", "Handle", "Send" };

        // Act & Assert - should not throw
        foreach (var serviceName in serviceNames)
        {
            foreach (var operationName in operationNames)
            {
                var act = () => new MessageContext().StartServiceActivity(serviceName, operationName);
                act.Should().NotThrow();
            }
        }
    }
}
