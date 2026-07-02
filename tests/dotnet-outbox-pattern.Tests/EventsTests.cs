#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for domain events and event-related models
/// </summary>
public sealed class EventsTests
{
    [Fact]
    public void DomainEvent_DefaultConstructor_InitializesProperties()
    {
        var domainEvent = new TestDomainEvent();

        domainEvent.EventId.Should().NotBe(Guid.Empty);
        domainEvent.OccurredAt.Should().BeOnOrBefore(DateTime.UtcNow);
        domainEvent.CorrelationId.Should().BeNull();
        domainEvent.CausationId.Should().BeNull();
        domainEvent.UserId.Should().BeNull();
    }

    [Fact]
    public void DomainEvent_WithOptionalProperties_SetCorrectly()
    {
        var correlationId = "corr-123";
        var causationId = "caus-456";
        var userId = "user-789";

        var domainEvent = new TestDomainEvent
        {
            CorrelationId = correlationId,
            CausationId = causationId,
            UserId = userId
        };

        domainEvent.EventId.Should().NotBe(Guid.Empty);
        domainEvent.CorrelationId.Should().Be(correlationId);
        domainEvent.CausationId.Should().Be(causationId);
        domainEvent.UserId.Should().Be(userId);
    }

    [Fact]
    public void EntityCreatedEvent_InitializesWithCorrectProperties()
    {
        var entityEvent = new EntityCreatedEvent
        {
            EntityId = "order-123",
            EntityType = "Order",
            EntityData = new Dictionary<string, object>
            {
                { "id", "order-123" },
                { "amount", 100.50 },
                { "customerId", "cust-456" }
            }
        };

        entityEvent.EventId.Should().NotBe(Guid.Empty);
        entityEvent.EntityId.Should().Be("order-123");
        entityEvent.EntityType.Should().Be("Order");
        entityEvent.EntityData.Should().HaveCount(3);
        entityEvent.EntityData["amount"].Should().Be(100.50);
    }

    [Fact]
    public void EntityUpdatedEvent_InitializesWithCorrectProperties()
    {
        var entityEvent = new EntityUpdatedEvent
        {
            EntityId = "order-123",
            EntityType = "Order",
            OldData = new Dictionary<string, object>
            {
                { "status", "pending" },
                { "amount", 50.00 }
            },
            NewData = new Dictionary<string, object>
            {
                { "status", "completed" },
                { "amount", 100.50 }
            },
            ChangedProperties = new List<string> { "status", "amount" }
        };

        entityEvent.EventId.Should().NotBe(Guid.Empty);
        entityEvent.EntityId.Should().Be("order-123");
        entityEvent.EntityType.Should().Be("Order");
        entityEvent.OldData.Should().HaveCount(2);
        entityEvent.NewData.Should().HaveCount(2);
        entityEvent.ChangedProperties.Should().HaveCount(2);
        entityEvent.ChangedProperties.Should().Contain("status");
        entityEvent.ChangedProperties.Should().Contain("amount");
    }

    [Fact]
    public void EntityDeletedEvent_InitializesWithCorrectProperties()
    {
        var entityEvent = new EntityDeletedEvent
        {
            EntityId = "order-123",
            EntityType = "Order",
            DeletedData = new Dictionary<string, object>
            {
                { "id", "order-123" },
                { "amount", 100.50 },
                { "customerId", "cust-456" },
                { "status", "completed" }
            }
        };

        entityEvent.EventId.Should().NotBe(Guid.Empty);
        entityEvent.EntityId.Should().Be("order-123");
        entityEvent.EntityType.Should().Be("Order");
        entityEvent.DeletedData.Should().HaveCount(4);
    }

    [Fact]
    public void CustomDomainEvent_InitializesWithCorrectProperties()
    {
        var customEvent = new CustomDomainEvent
        {
            EventName = "OrderStatusChanged",
            AggregateId = "order-123",
            AggregateType = "Order",
            Payload = new Dictionary<string, object>
            {
                { "oldStatus", "pending" },
                { "newStatus", "completed" },
                { "orderId", "order-123" }
            }
        };

        customEvent.EventId.Should().NotBe(Guid.Empty);
        customEvent.EventName.Should().Be("OrderStatusChanged");
        customEvent.AggregateId.Should().Be("order-123");
        customEvent.AggregateType.Should().Be("Order");
        customEvent.Payload.Should().HaveCount(3);
    }

    [Fact]
    public void NotificationEvent_InitializesWithCorrectProperties()
    {
        var notificationEvent = new NotificationEvent
        {
            NotificationType = "Email",
            RecipientId = "user-789",
            Subject = "Order Confirmation",
            Body = "Your order #123 has been confirmed",
            IsCritical = true,
            ActionUrl = "https://example.com/orders/123"
        };

        notificationEvent.EventId.Should().NotBe(Guid.Empty);
        notificationEvent.NotificationType.Should().Be("Email");
        notificationEvent.RecipientId.Should().Be("user-789");
        notificationEvent.Subject.Should().Be("Order Confirmation");
        notificationEvent.Body.Should().Be("Your order #123 has been confirmed");
        notificationEvent.IsCritical.Should().BeTrue();
        notificationEvent.ActionUrl.Should().Be("https://example.com/orders/123");
    }

    [Fact]
    public void PublishableEvent_InitializesWithCorrectProperties()
    {
        var domainEvent = new TestDomainEvent();
        var publishableEvent = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "orders.created",
            PartitionKey = "order-123",
            MaxAttempts = 3,
            DeliveryGuarantee = DeliveryGuarantee.ExactlyOnce
        };

        publishableEvent.Event.Should().Be(domainEvent);
        publishableEvent.Topic.Should().Be("orders.created");
        publishableEvent.PartitionKey.Should().Be("order-123");
        publishableEvent.MaxAttempts.Should().Be(3);
        publishableEvent.DeliveryGuarantee.Should().Be(DeliveryGuarantee.ExactlyOnce);
    }

    [Fact]
    public void PublishableEvent_DefaultConstructor_SetsDefaults()
    {
        var publishableEvent = new PublishableEvent();

        publishableEvent.Event.Should().NotBeNull();
        publishableEvent.Topic.Should().BeNull();
        publishableEvent.PartitionKey.Should().BeNull();
        publishableEvent.MaxAttempts.Should().Be(5);
        publishableEvent.DeliveryGuarantee.Should().Be(DeliveryGuarantee.AtLeastOnce);
    }

    private sealed class TestDomainEvent : DomainEvent;
}
