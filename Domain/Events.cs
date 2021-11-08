#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Causation ID linking to the command/event that caused this
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// User who triggered this event
    /// </summary>
    public string? UserId { get; init; }
}

/// <summary>
/// Event raised when an entity is created
/// </summary>
public sealed class EntityCreatedEvent : DomainEvent
{
    /// <summary>
    /// ID of the created entity
    /// </summary>
    public string EntityId { get; init; } = null!;

    /// <summary>
    /// Type of entity created
    /// </summary>
    public string EntityType { get; init; } = null!;

    /// <summary>
    /// Data of the created entity
    /// </summary>
    public Dictionary<string, object> EntityData { get; init; } = new();
}

/// <summary>
/// Event raised when an entity is updated
/// </summary>
public sealed class EntityUpdatedEvent : DomainEvent
{
    /// <summary>
    /// ID of the updated entity
    /// </summary>
    public string EntityId { get; init; } = null!;

    /// <summary>
    /// Type of entity updated
    /// </summary>
    public string EntityType { get; init; } = null!;

    /// <summary>
    /// Previous state of the entity
    /// </summary>
    public Dictionary<string, object> OldData { get; init; } = new();

    /// <summary>
    /// New state of the entity
    /// </summary>
    public Dictionary<string, object> NewData { get; init; } = new();

    /// <summary>
    /// List of changed property names
    /// </summary>
    public List<string> ChangedProperties { get; init; } = new();
}

/// <summary>
/// Event raised when an entity is deleted
/// </summary>
public sealed class EntityDeletedEvent : DomainEvent
{
    /// <summary>
    /// ID of the deleted entity
    /// </summary>
    public string EntityId { get; init; } = null!;

    /// <summary>
    /// Type of entity deleted
    /// </summary>
    public string EntityType { get; init; } = null!;

    /// <summary>
    /// Previous state of the deleted entity
    /// </summary>
    public Dictionary<string, object> DeletedData { get; init; } = new();
}

/// <summary>
/// Event for custom business domain events
/// </summary>
public sealed class CustomDomainEvent : DomainEvent
{
    /// <summary>
    /// Name/type of the custom event
    /// </summary>
    public string EventName { get; init; } = null!;

    /// <summary>
    /// Aggregate/entity this event applies to
    /// </summary>
    public string AggregateId { get; init; } = null!;

    /// <summary>
    /// Type of aggregate
    /// </summary>
    public string AggregateType { get; init; } = null!;

    /// <summary>
    /// Custom event payload
    /// </summary>
    public Dictionary<string, object> Payload { get; init; } = new();
}

/// <summary>
/// Notification event for alerting subscribers
/// </summary>
public sealed class NotificationEvent : DomainEvent
{
    /// <summary>
    /// Type of notification
    /// </summary>
    public string NotificationType { get; init; } = null!;

    /// <summary>
    /// Recipient of the notification
    /// </summary>
    public string RecipientId { get; init; } = null!;

    /// <summary>
    /// Subject of the notification
    /// </summary>
    public string Subject { get; init; } = null!;

    /// <summary>
    /// Body/content of the notification
    /// </summary>
    public string Body { get; init; } = null!;

    /// <summary>
    /// Whether the notification is critical/urgent
    /// </summary>
    public bool IsCritical { get; init; }

    /// <summary>
    /// Optional action URL for the notification
    /// </summary>
    public string? ActionUrl { get; init; }
}

/// <summary>
/// Represents an event with metadata for publishing
/// </summary>
public sealed class PublishableEvent
{
    /// <summary>
    /// The domain event to publish
    /// </summary>
    public DomainEvent Event { get; init; } = null!;

    /// <summary>
    /// Topic where the event should be published
    /// </summary>
    public string Topic { get; init; } = null!;

    /// <summary>
    /// Partition key for maintaining order
    /// </summary>
    public string? PartitionKey { get; init; }

    /// <summary>
    /// Maximum number of publication attempts
    /// </summary>
    public int MaxAttempts { get; init; } = 5;

    /// <summary>
    /// Required delivery guarantee
    /// </summary>
    public DeliveryGuarantee DeliveryGuarantee { get; init; } = DeliveryGuarantee.AtLeastOnce;

    /// <summary>
    /// Optional explicit idempotency key. When not set, the event's ID is used instead.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Optional future time at which the message should become eligible for publishing
    /// </summary>
    public DateTime? ScheduledTime { get; init; }
}
