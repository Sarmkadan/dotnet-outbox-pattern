// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Enum representing the processing state of an outbox message.
/// </summary>
public enum OutboxMessageState
{
    /// <summary>
    /// Message is pending processing
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message is currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Message has been successfully published
    /// </summary>
    Published = 2,

    /// <summary>
    /// Message processing failed and moved to dead letter queue
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Message has been manually archived
    /// </summary>
    Archived = 4
}

/// <summary>
/// Enum representing the type of event contained in the outbox message.
/// </summary>
public enum EventType
{
    /// <summary>
    /// Entity was created
    /// </summary>
    Created = 1,

    /// <summary>
    /// Entity was updated
    /// </summary>
    Updated = 2,

    /// <summary>
    /// Entity was deleted
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// Custom business event
    /// </summary>
    Custom = 4,

    /// <summary>
    /// Notification event
    /// </summary>
    Notification = 5
}

/// <summary>
/// Enum for message delivery guarantee levels.
/// </summary>
public enum DeliveryGuarantee
{
    /// <summary>
    /// At least once delivery with potential duplicates
    /// </summary>
    AtLeastOnce = 1,

    /// <summary>
    /// Exactly once delivery (best effort)
    /// </summary>
    ExactlyOnce = 2
}

/// <summary>
/// Enum for retry policies.
/// </summary>
public enum RetryPolicyType
{
    /// <summary>
    /// No retry
    /// </summary>
    NoRetry = 0,

    /// <summary>
    /// Fixed interval retry
    /// </summary>
    FixedInterval = 1,

    /// <summary>
    /// Exponential backoff retry
    /// </summary>
    ExponentialBackoff = 2,

    /// <summary>
    /// Linear backoff retry
    /// </summary>
    LinearBackoff = 3
}
