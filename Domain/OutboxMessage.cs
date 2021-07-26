// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Represents a message in the transactional outbox pattern.
/// Guarantees reliable message delivery with ordering and deduplication.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier for the outbox message
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Idempotency key for deduplication across retries
    /// </summary>
    public string IdempotencyKey { get; set; } = null!;

    /// <summary>
    /// The aggregate/entity that triggered this message
    /// </summary>
    public string AggregateId { get; set; } = null!;

    /// <summary>
    /// Type of aggregate (entity type name)
    /// </summary>
    public string AggregateType { get; set; } = null!;

    /// <summary>
    /// Type of event that occurred
    /// </summary>
    public EventType EventType { get; set; }

    /// <summary>
    /// The event data serialized as JSON
    /// </summary>
    public string EventData { get; set; } = null!;

    /// <summary>
    /// The CLR type name of the event
    /// </summary>
    public string EventTypeName { get; set; } = null!;

    /// <summary>
    /// Topic or channel where the message should be published
    /// </summary>
    public string Topic { get; set; } = null!;

    /// <summary>
    /// Current processing state of the message
    /// </summary>
    public OutboxMessageState State { get; set; } = OutboxMessageState.Pending;

    /// <summary>
    /// Number of publishing attempts made
    /// </summary>
    public int PublishAttempts { get; set; }

    /// <summary>
    /// Maximum number of attempts before moving to dead letter
    /// </summary>
    public int MaxPublishAttempts { get; set; } = 5;

    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the message was last processed
    /// </summary>
    public DateTime? LastProcessedAt { get; set; }

    /// <summary>
    /// When the message was successfully published
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Scheduled time for delayed processing
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// Error message if publishing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace of the last error
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Partition key for ordering guarantees (same partition = same order)
    /// </summary>
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Whether this message requires exactly-once delivery semantics
    /// </summary>
    public DeliveryGuarantee DeliveryGuarantee { get; set; } = DeliveryGuarantee.AtLeastOnce;

    /// <summary>
    /// Correlation ID for tracing related messages
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Causation ID linking to the event that caused this message
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Optional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Priority level for processing (higher number = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether the message has been locked for processing
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// When the processing lock expires (for distributed locking)
    /// </summary>
    public DateTime? LockExpiresAt { get; set; }

    /// <summary>
    /// Validates the outbox message for correctness
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(IdempotencyKey))
            throw new ArgumentException("IdempotencyKey cannot be empty");

        if (string.IsNullOrWhiteSpace(AggregateId))
            throw new ArgumentException("AggregateId cannot be empty");

        if (string.IsNullOrWhiteSpace(AggregateType))
            throw new ArgumentException("AggregateType cannot be empty");

        if (string.IsNullOrWhiteSpace(EventData))
            throw new ArgumentException("EventData cannot be empty");

        if (string.IsNullOrWhiteSpace(EventTypeName))
            throw new ArgumentException("EventTypeName cannot be empty");

        if (string.IsNullOrWhiteSpace(Topic))
            throw new ArgumentException("Topic cannot be empty");

        if (MaxPublishAttempts <= 0)
            throw new ArgumentException("MaxPublishAttempts must be greater than 0");
    }

    /// <summary>
    /// Marks the message as published
    /// </summary>
    public void MarkAsPublished()
    {
        State = OutboxMessageState.Published;
        PublishedAt = DateTime.UtcNow;
        IsLocked = false;
        ErrorMessage = null;
        ErrorStackTrace = null;
    }

    /// <summary>
    /// Records a publishing failure
    /// </summary>
    public void RecordFailure(string errorMessage, string? stackTrace = null)
    {
        PublishAttempts++;
        LastProcessedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        ErrorStackTrace = stackTrace;
        IsLocked = false;

        if (PublishAttempts >= MaxPublishAttempts)
        {
            State = OutboxMessageState.Failed;
        }
    }

    /// <summary>
    /// Locks the message for processing with expiration
    /// </summary>
    public void Lock(TimeSpan lockDuration)
    {
        IsLocked = true;
        LockExpiresAt = DateTime.UtcNow.Add(lockDuration);
        State = OutboxMessageState.Processing;
    }

    /// <summary>
    /// Unlocks the message if the lock has expired
    /// </summary>
    public bool UnlockIfExpired()
    {
        if (IsLocked && LockExpiresAt.HasValue && DateTime.UtcNow >= LockExpiresAt)
        {
            IsLocked = false;
            State = OutboxMessageState.Pending;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Determines if the message should be retried
    /// </summary>
    public bool CanRetry() => PublishAttempts < MaxPublishAttempts && State != OutboxMessageState.Published;
}
