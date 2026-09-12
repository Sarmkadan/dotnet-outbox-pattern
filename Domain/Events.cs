#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Serialization;

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Represents the base class for all domain events.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "eventKind", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(EntityCreatedEvent), "entityCreated")]
[JsonDerivedType(typeof(EntityUpdatedEvent), "entityUpdated")]
[JsonDerivedType(typeof(EntityDeletedEvent), "entityDeleted")]
[JsonDerivedType(typeof(CustomDomainEvent), "custom")]
[JsonDerivedType(typeof(NotificationEvent), "notification")]
public abstract class DomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the date and time in UTC when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the correlation identifier used to trace related operations.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the identifier of the command or event that caused this event.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who triggered this event.
    /// </summary>
    public string? UserId { get; init; }
}

/// <summary>
/// Represents an event raised when an entity is created.
/// </summary>
public sealed class EntityCreatedEvent : DomainEvent
{
    /// <summary>
    /// Gets the identifier of the created entity.
    /// </summary>
    public string EntityId { get; init; } = null!;

    /// <summary>
    /// Gets the type of the created entity.
    /// </summary>
    public string EntityType { get; init; } = null!;

    /// <summary>
    /// Gets the data associated with the created entity.
    /// </summary>
    public Dictionary<string, object> EntityData { get; init; } = new();
}

/// <summary>
/// Represents an event raised when an entity is updated.
/// </summary>
public sealed class EntityUpdatedEvent : DomainEvent
{
    /// <summary>
    /// Gets the identifier of the updated entity.
    /// </summary>
    public string EntityId { get; init; } = null!;

    /// <summary>
    /// Gets the type of the updated entity.
    /// </summary>
    public string EntityType { get; init; } = null!;

    /// <summary>
    /// Gets the state of the entity before the update.
    /// </summary>
    public Dictionary<string, object> OldData { get; init; } = new();

    /// <summary>
    /// Gets the state of the entity after the update.
    /// </summary>
    public Dictionary<string, object> NewData { get; init; } = new();

    /// <summary>
    /// Gets the names of the properties that changed.
    /// </summary>
    public List<string> ChangedProperties { get; init; } = new();
}

/// <summary>
/// Represents an event raised when an entity is deleted.
/// </summary>
public sealed class EntityDeletedEvent : DomainEvent
{
    /// <summary>
    /// Gets the identifier of the deleted entity.
    /// </summary>
    public string EntityId { get; init; } = null!;

    /// <summary>
    /// Gets the type of the deleted entity.
    /// </summary>
    public string EntityType { get; init; } = null!;

    /// <summary>
    /// Gets the state of the entity before it was deleted.
    /// </summary>
    public Dictionary<string, object> DeletedData { get; init; } = new();
}

/// <summary>
/// Represents a custom business domain event.
/// </summary>
public sealed class CustomDomainEvent : DomainEvent
{
    /// <summary>
    /// Gets the name or type of the custom event.
    /// </summary>
    public string EventName { get; init; } = null!;

    /// <summary>
    /// Gets the identifier of the aggregate or entity to which this event applies.
    /// </summary>
    public string AggregateId { get; init; } = null!;

    /// <summary>
    /// Gets the type of the aggregate.
    /// </summary>
    public string AggregateType { get; init; } = null!;

    /// <summary>
    /// Gets the custom event payload.
    /// </summary>
    public Dictionary<string, object> Payload { get; init; } = new();
}

/// <summary>
/// Represents a notification event used to alert subscribers.
/// </summary>
public sealed class NotificationEvent : DomainEvent
{
    /// <summary>
    /// Gets the type of the notification.
    /// </summary>
    public string NotificationType { get; init; } = null!;

    /// <summary>
    /// Gets the identifier of the notification recipient.
    /// </summary>
    public string RecipientId { get; init; } = null!;

    /// <summary>
    /// Gets the subject of the notification.
    /// </summary>
    public string Subject { get; init; } = null!;

    /// <summary>
    /// Gets the body or content of the notification.
    /// </summary>
    public string Body { get; init; } = null!;

    /// <summary>
    /// Gets a value indicating whether the notification is critical or urgent.
    /// </summary>
    public bool IsCritical { get; init; }

    /// <summary>
    /// Gets the optional action URL for the notification.
    /// </summary>
    public string? ActionUrl { get; init; }
}

/// <summary>
/// Represents a domain event together with its publishing metadata.
/// </summary>
public sealed class PublishableEvent
{
    /// <summary>
    /// Gets the domain event to publish.
    /// </summary>
    public DomainEvent Event { get; init; } = null!;

    /// <summary>
    /// Gets the topic to which the event should be published.
    /// </summary>
    public string Topic { get; init; } = null!;

    /// <summary>
    /// Gets the partition key used to maintain event ordering.
    /// </summary>
    public string? PartitionKey { get; init; }

    /// <summary>
    /// Gets the maximum number of publication attempts.
    /// </summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>
    /// Gets the required delivery guarantee.
    /// </summary>
    public DeliveryGuarantee DeliveryGuarantee { get; init; } = DeliveryGuarantee.AtLeastOnce;

    /// <summary>
    /// Gets the optional explicit idempotency key. When not set, the event identifier is used instead.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Gets the optional future time at which the message becomes eligible for publishing.
    /// </summary>
    public DateTime? ScheduledTime { get; init; }
}
