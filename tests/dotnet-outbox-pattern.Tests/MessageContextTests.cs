#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for MessageContext tracing and activity management
/// </summary>
public sealed class MessageContextTests
{
    [Fact]
    public void GetOrCreateCorrelationId_ReturnsValidGuidString()
    {
        var correlationId = MessageContext.GetOrCreateCorrelationId();

        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
        correlationId.Should().NotBeEmptyCorrelationId();
    }

    [Fact]
    public void GetOrCreateCorrelationId_ReturnsDifferentIds()
    {
        var id1 = MessageContext.GetOrCreateCorrelationId();
        var id2 = MessageContext.GetOrCreateCorrelationId();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void GetOrCreateCausationId_WithActivity_ReturnsCurrentActivityId()
    {
        // This test verifies the behavior when there's an active activity
        using var activity = new ActivitySource("Test").StartActivity("TestActivity");

        var causationId = MessageContext.GetOrCreateCausationId();

        causationId.Should().NotBeNullOrWhiteSpace();
        // When there's an active activity, it should return the activity ID
        if (activity is not null)
        {
            causationId.Should().Be(activity.Id);
        }
    }

    [Fact]
    public void GetOrCreateCausationId_WithoutActivity_ReturnsValidGuidString()
    {
        // Ensure no active activity
        Activity.Current = null;

        var causationId = MessageContext.GetOrCreateCausationId();

        causationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(causationId, out _).Should().BeTrue();
    }

    [Fact]
    public void StartActivity_WithMessage_SetsCorrectTags()
    {
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

        using var activity = MessageContext.StartActivity(message, "PublishMessage");

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
    public void StartActivity_WithoutPartitionKey_DoesNotSetPartitionKeyTag()
    {
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

        using var activity = MessageContext.StartActivity(message, "PublishMessage");

        activity.Should().NotBeNull();
        activity?.Tags.Should().NotContain(t => t.Key == "outbox.partition_key");
    }

    [Fact]
    public void StartServiceActivity_SetsCorrectTags()
    {
        using var activity = MessageContext.StartServiceActivity("OutboxProcessor", "ProcessPendingMessages");

        activity.Should().NotBeNull();
        activity?.Tags.Should().Contain(t => t.Key == "service" && t.Value == "OutboxProcessor");
        activity?.Tags.Should().Contain(t => t.Key == "operation" && t.Value == "ProcessPendingMessages");
    }

    [Fact]
    public void RecordEvent_AddsEventToActivity()
    {
        using var activity = new ActivitySource("Test").StartActivity("TestActivity");
        var attributes = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 123 } };

        MessageContext.RecordEvent("TestEvent", attributes);

        activity.Should().NotBeNull();
        // Activity events are accessible via the activity's events collection
        activity?.Events.Should().Contain(e => e.Name == "TestEvent");
    }

    [Fact]
    public void RecordEvent_WithNullAttributes_DoesNotThrow()
    {
        using var activity = new ActivitySource("Test").StartActivity("TestActivity");

        var act = () => MessageContext.RecordEvent("TestEvent", null);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_SetsExceptionTags()
    {
        using var activity = new ActivitySource("Test").StartActivity("TestActivity");
        var exception = new InvalidOperationException("Test exception message");

        MessageContext.RecordException(exception);

        activity.Should().NotBeNull();
        activity?.Tags.Should().Contain(t => t.Key == "exception.type" && t.Value == "InvalidOperationException");
        activity?.Tags.Should().Contain(t => t.Key == "exception.message" && t.Value == "Test exception message");
        activity?.Tags.Should().Contain(t => t.Key == "otel.status_code" && t.Value == "ERROR");
    }

    [Fact]
    public void ActivityScope_DisposesActivity()
    {
        var activity = new ActivitySource("Test").StartActivity("TestActivity");
        var scope = new ActivityScope(activity);

        scope.Dispose();

        // After disposal, the activity should be stopped
        activity?.IsStopped.Should().BeTrue();
    }

    [Fact]
    public void UseScope_ExtensionMethod_CreatesDisposableScope()
    {
        using var activity = new ActivitySource("Test").StartActivity("TestActivity");

        using var scope = activity.UseScope();

        scope.Should().NotBeNull();
        scope.Should().BeOfType<ActivityScope>();
    }

    [Fact]
    public void ActivityExtensions_UseScope_DisposesCorrectly()
    {
        var activity = new ActivitySource("Test").StartActivity("TestActivity");

        using (activity.UseScope())
        {
            // Activity is active within scope
            activity?.IsStopped.Should().BeFalse();
        }

        // Activity is stopped after scope disposal
        activity?.IsStopped.Should().BeTrue();
    }
}

file static class StringExtensions
{
    public static AndConstraint<StringAssertions> NotBeEmptyCorrelationId(this StringAssertions assertions)
    {
        return assertions.NotBeEmpty().And.NotBe("00000000-0000-0000-0000-000000000000");
    }
}
