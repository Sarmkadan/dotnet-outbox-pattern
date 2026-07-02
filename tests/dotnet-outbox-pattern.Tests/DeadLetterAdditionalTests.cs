#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Additional comprehensive tests for DeadLetter domain model
/// </summary>
public sealed class DeadLetterAdditionalTests
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
        State = OutboxMessageState.Failed,
        PartitionKey = "order-123",
        DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce,
        Priority = 10
    };

    [Fact]
    public void FromOutboxMessage_WithAllProperties_CopiesCorrectly()
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
        deadLetter.PartitionKey.Should().BeNull();
    }

    [Fact]
    public void MarkAsReviewed_WithEmptyNotes_SetsReviewedProperties()
    {
        var deadLetter = DeadLetter.FromOutboxMessage(CreateFailedMessage());

        deadLetter.MarkAsReviewed(string.Empty);

        deadLetter.IsReviewed.Should().BeTrue();
        deadLetter.ReviewNotes.Should().BeEmpty();
        deadLetter.ReviewedAt.Should().BeOnOrAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void MarkAsReviewed_WithWhitespaceNotes_TrimsNotes()
    {
        var deadLetter = DeadLetter.FromOutboxMessage(CreateFailedMessage());

        deadLetter.MarkAsReviewed("  reviewed with notes  ");

        deadLetter.ReviewNotes.Should().Be("reviewed with notes");
    }

    [Fact]
    public void MarkAsRequeued_WithEmptyReason_SetsRequeueProperties()
    {
        var deadLetter = DeadLetter.FromOutboxMessage(CreateFailedMessage());

        deadLetter.MarkAsRequeued(string.Empty);

        deadLetter.IsRequeued.Should().BeTrue();
        deadLetter.RequeueReason.Should().BeEmpty();
        deadLetter.RequeuedAt.Should().BeOnOrAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void MarkAsRequeued_WithWhitespaceReason_TrimsReason()
    {
        var deadLetter = DeadLetter.FromOutboxMessage(CreateFailedMessage());

        deadLetter.MarkAsRequeued("  network resolved  ");

        deadLetter.RequeueReason.Should().Be("network resolved");
    }

    [Fact]
    public void DeadLetter_WithAllProperties_InitializesCorrectly()
    {
        var now = DateTime.UtcNow;
        var deadLetter = new DeadLetter
        {
            Id = Guid.NewGuid(),
            OutboxMessageId = Guid.NewGuid(),
            IdempotencyKey = "test-key",
            AggregateId = "test-agg",
            AggregateType = "TestType",
            EventType = EventType.Custom,
            EventData = "{\"test\":true}",
            EventTypeName = "TestEvent",
            Topic = "test.topic",
            TotalAttempts = 3,
            ErrorMessage = "test error",
            ErrorStackTrace = "test stack",
            OriginalCreatedAt = now.AddDays(-1),
            MovedToDlqAt = now,
            LastAttemptAt = now.AddHours(-1),
            CorrelationId = "corr-1",
            CausationId = "caus-1",
            Metadata = "{\"meta\":\"data\"}",
            IsReviewed = true,
            ReviewNotes = "test notes",
            ReviewedAt = now,
            IsRequeued = true,
            RequeuedAt = now,
            RequeueReason = "test reason",
            FailureReason = "test failure",
            SuggestedAction = "test action"
        };

        deadLetter.Id.Should().NotBe(Guid.Empty);
        deadLetter.OutboxMessageId.Should().NotBe(Guid.Empty);
        deadLetter.IdempotencyKey.Should().Be("test-key");
        deadLetter.AggregateId.Should().Be("test-agg");
        deadLetter.AggregateType.Should().Be("TestType");
        deadLetter.EventType.Should().Be(EventType.Custom);
        deadLetter.EventData.Should().Be("{\"test\":true}");
        deadLetter.EventTypeName.Should().Be("TestEvent");
        deadLetter.Topic.Should().Be("test.topic");
        deadLetter.TotalAttempts.Should().Be(3);
        deadLetter.ErrorMessage.Should().Be("test error");
        deadLetter.ErrorStackTrace.Should().Be("test stack");
        deadLetter.OriginalCreatedAt.Should().Be(now.AddDays(-1));
        deadLetter.MovedToDlqAt.Should().Be(now);
        deadLetter.LastAttemptAt.Should().Be(now.AddHours(-1));
        deadLetter.CausationId.Should().Be("caus-1");
        deadLetter.Metadata.Should().Be("{\"meta\":\"data\"}");
        deadLetter.IsReviewed.Should().BeTrue();
        deadLetter.ReviewNotes.Should().Be("test notes");
        deadLetter.ReviewedAt.Should().Be(now);
        deadLetter.IsRequeued.Should().BeTrue();
        deadLetter.RequeuedAt.Should().Be(now);
        deadLetter.RequeueReason.Should().Be("test reason");
        deadLetter.FailureReason.Should().Be("test failure");
        deadLetter.SuggestedAction.Should().Be("test action");
    }

    [Fact]
    public void DeadLetter_DefaultConstructor_GeneratesNewGuid()
    {
        var deadLetter = new DeadLetter();

        deadLetter.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void FromOutboxMessage_WithNullErrorMessage_UsesDefaultErrorMessage()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "key-1",
            AggregateId = "agg-1",
            AggregateType = "Type",
            EventType = EventType.Created,
            EventData = "data",
            EventTypeName = "Event",
            Topic = "topic",
            PublishAttempts = 5,
            MaxPublishAttempts = 5,
            ErrorMessage = null,
            CreatedAt = DateTime.UtcNow
        };

        var deadLetter = DeadLetter.FromOutboxMessage(message);

        deadLetter.ErrorMessage.Should().Be("Unknown error");
    }

    [Fact]
    public void FromOutboxMessage_WithNullErrorStackTrace_CopiesNull()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "key-1",
            AggregateId = "agg-1",
            AggregateType = "Type",
            EventType = EventType.Created,
            EventData = "data",
            EventTypeName = "Event",
            Topic = "topic",
            PublishAttempts = 5,
            MaxPublishAttempts = 5,
            ErrorStackTrace = null,
            CreatedAt = DateTime.UtcNow
        };

        var deadLetter = DeadLetter.FromOutboxMessage(message);

        deadLetter.ErrorStackTrace.Should().BeNull();
    }
}