#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for DeadLetter domain model
/// </summary>
public sealed class DeadLetterTests
{
    private static OutboxMessage CreateFailedMessage() => new()
    {
        Id = Guid.NewGuid(),
        IdempotencyKey = "key-001",
        AggregateId = "order-123",
        AggregateType = "Order",
        EventType = EventType.Created,
        EventData = "{\"orderId\":\"123\"}",
        EventTypeName = "OrderCreatedEvent",
        Topic = "orders.created",
        PublishAttempts = 5,
        MaxPublishAttempts = 5,
        ErrorMessage = "Failed to connect to message broker",
        ErrorStackTrace = "at MessageBroker.Publish() in line 42",
        CreatedAt = DateTime.UtcNow.AddMinutes(-30),
        LastProcessedAt = DateTime.UtcNow.AddMinutes(-5),
        CorrelationId = "corr-123",
        CausationId = "caus-456",
        Metadata = "{\"userId\":\"user-789\"}",
        State = OutboxMessageState.Failed
    };

    [Fact]
    public void FromOutboxMessage_CreatesDeadLetterWithCorrectProperties()
    {
        var message = CreateFailedMessage();
        var deadLetter = DeadLetter.FromOutboxMessage(message);

        deadLetter.Id.Should().NotBe(Guid.Empty);
        deadLetter.OutboxMessageId.Should().Be(message.Id);
        deadLetter.IdempotencyKey.Should().Be(message.IdempotencyKey);
        deadLetter.AggregateId.Should().Be(message.AggregateId);
        deadLetter.AggregateType.Should().Be(message.AggregateType);
        deadLetter.EventType.Should().Be(message.EventType);
        deadLetter.EventData.Should().Be(message.EventData);
        deadLetter.EventTypeName.Should().Be(message.EventTypeName);
        deadLetter.Topic.Should().Be(message.Topic);
        deadLetter.TotalAttempts.Should().Be(message.PublishAttempts);
        deadLetter.ErrorMessage.Should().Be(message.ErrorMessage);
        deadLetter.ErrorStackTrace.Should().Be(message.ErrorStackTrace);
        deadLetter.OriginalCreatedAt.Should().Be(message.CreatedAt);
        deadLetter.MovedToDlqAt.Should().BeOnOrAfter(message.CreatedAt);
        deadLetter.LastAttemptAt.Should().Be(message.LastProcessedAt);
        deadLetter.CorrelationId.Should().Be(message.CorrelationId);
        deadLetter.CausationId.Should().Be(message.CausationId);
        deadLetter.Metadata.Should().Be(message.Metadata);
        deadLetter.IsReviewed.Should().BeFalse();
        deadLetter.IsRequeued.Should().BeFalse();
    }

    [Fact]
    public void MarkAsReviewed_SetsReviewProperties()
    {
        var deadLetter = DeadLetter.FromOutboxMessage(CreateFailedMessage());
        var reviewNotes = "Investigated - network issue resolved";

        deadLetter.MarkAsReviewed(reviewNotes);

        deadLetter.IsReviewed.Should().BeTrue();
        deadLetter.ReviewNotes.Should().Be(reviewNotes);
        deadLetter.ReviewedAt.Should().BeOnOrAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void MarkAsRequeued_SetsRequeueProperties()
    {
        var deadLetter = DeadLetter.FromOutboxMessage(CreateFailedMessage());
        var reason = "Network issue resolved, retrying";

        deadLetter.MarkAsRequeued(reason);

        deadLetter.IsRequeued.Should().BeTrue();
        deadLetter.RequeuedAt.Should().BeOnOrAfter(DateTime.UtcNow.AddMinutes(-1));
        deadLetter.RequeueReason.Should().Be(reason);
    }

    [Fact]
    public void DeadLetter_DefaultConstructor_InitializesProperties()
    {
        var deadLetter = new DeadLetter();

        deadLetter.Id.Should().NotBe(Guid.Empty);
        deadLetter.IsReviewed.Should().BeFalse();
        deadLetter.IsRequeued.Should().BeFalse();
        deadLetter.ReviewNotes.Should().BeNull();
        deadLetter.ReviewedAt.Should().BeNull();
        deadLetter.RequeuedAt.Should().BeNull();
        deadLetter.RequeueReason.Should().BeNull();
    }

    [Fact]
    public void DeadLetter_WithFailureReasonAndSuggestedAction_SetsProperties()
    {
        var deadLetter = new DeadLetter
        {
            Id = Guid.NewGuid(),
            OutboxMessageId = Guid.NewGuid(),
            IdempotencyKey = "key-123",
            AggregateId = "order-123",
            AggregateType = "Order",
            EventType = EventType.Updated,
            EventData = "{\"orderId\":\"123\"}",
            EventTypeName = "OrderUpdatedEvent",
            Topic = "orders.updated",
            TotalAttempts = 5,
            ErrorMessage = "Serialization failed",
            FailureReason = "Invalid event data format",
            SuggestedAction = "Fix event serialization logic"
        };

        deadLetter.FailureReason.Should().Be("Invalid event data format");
        deadLetter.SuggestedAction.Should().Be("Fix event serialization logic");
    }
}
