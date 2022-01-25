#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Represents a message that failed to be published after maximum retry attempts.
/// Dead letters require manual intervention or investigation.
/// </summary>
public sealed class DeadLetter
{
    /// <summary>
    /// Unique identifier for the dead letter record
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the original outbox message
    /// </summary>
    public Guid OutboxMessageId { get; set; }

    /// <summary>
    /// Idempotency key from the original message
    /// </summary>
    public string IdempotencyKey { get; set; } = null!;

    /// <summary>
    /// Aggregate ID that triggered the original message
    /// </summary>
    public string AggregateId { get; set; } = null!;

    /// <summary>
    /// Type of aggregate
    /// </summary>
    public string AggregateType { get; set; } = null!;

    /// <summary>
    /// Type of event
    /// </summary>
    public EventType EventType { get; set; }

    /// <summary>
    /// Serialized event data
    /// </summary>
    public string EventData { get; set; } = null!;

    /// <summary>
    /// CLR type of the event
    /// </summary>
    public string EventTypeName { get; set; } = null!;

    /// <summary>
    /// Topic where delivery was attempted
    /// </summary>
    public string Topic { get; set; } = null!;

    /// <summary>
    /// Partition key from the original message, if any
    /// </summary>
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Total number of delivery attempts made
    /// </summary>
    public int TotalAttempts { get; set; }

    /// <summary>
    /// Final error message
    /// </summary>
    public string ErrorMessage { get; set; } = null!;

    /// <summary>
    /// Stack trace from the final error
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// When the message was created originally
    /// </summary>
    public DateTime OriginalCreatedAt { get; set; }

    /// <summary>
    /// When the message was moved to dead letter
    /// </summary>
    public DateTime MovedToDlqAt { get; set; }

    /// <summary>
    /// Last attempt time before DLQ
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Correlation ID from the original message
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Causation ID from the original message
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Metadata from the original message
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this dead letter has been reviewed by an operator
    /// </summary>
    public bool IsReviewed { get; set; }

    /// <summary>
    /// Notes added by operator during review
    /// </summary>
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// When the dead letter was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Whether the dead letter has been requeued for retry
    /// </summary>
    public bool IsRequeued { get; set; }

    /// <summary>
    /// When the dead letter was requeued
    /// </summary>
    public DateTime? RequeuedAt { get; set; }

    /// <summary>
    /// Reason for requeue if applicable
    /// </summary>
    public string? RequeueReason { get; set; }

    /// <summary>
    /// Reason why the message failed
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Suggested action to resolve the issue
    /// </summary>
    public string? SuggestedAction { get; set; }

    /// <summary>
    /// Creates a dead letter from a failed outbox message
    /// </summary>
    public static DeadLetter FromOutboxMessage(OutboxMessage message)
    {
        return new DeadLetter
        {
            Id = Guid.NewGuid(),
            OutboxMessageId = message.Id,
            IdempotencyKey = message.IdempotencyKey,
            AggregateId = message.AggregateId,
            AggregateType = message.AggregateType,
            EventType = message.EventType,
            EventData = message.EventData,
            EventTypeName = message.EventTypeName,
            Topic = message.Topic,
            TotalAttempts = message.PublishAttempts,
            ErrorMessage = message.ErrorMessage ?? "Unknown error",
            ErrorStackTrace = message.ErrorStackTrace,
            OriginalCreatedAt = message.CreatedAt,
            MovedToDlqAt = DateTime.UtcNow,
            LastAttemptAt = message.LastProcessedAt,
            CorrelationId = message.CorrelationId,
            CausationId = message.CausationId,
            Metadata = message.Metadata,
            IsReviewed = false,
            IsRequeued = false
        };
    }

    /// <summary>
    /// Marks the dead letter as reviewed
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="notes"/> is null.</exception>
    public void MarkAsReviewed(string notes)
    {
        ArgumentNullException.ThrowIfNull(notes);

        IsReviewed = true;
        ReviewNotes = notes.Trim();
        ReviewedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the dead letter as requeued for retry
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="reason"/> is null.</exception>
    public void MarkAsRequeued(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        IsRequeued = true;
        RequeuedAt = DateTime.UtcNow;
        RequeueReason = reason.Trim();
    }
}
