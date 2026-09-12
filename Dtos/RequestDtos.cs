#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Dtos;

/// <summary>
/// Request to publish an event to the outbox
/// </summary>
public sealed class PublishEventRequest
{
    /// <summary>
    /// Identifier of the aggregate the event belongs to
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the aggregate the event belongs to
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Type of the event being published
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Optional payload associated with the event
    /// </summary>
    public Dictionary<string, object>? EventData { get; set; }

    /// <summary>
    /// Topic to which the event should be published
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Optional partition key used for ordering
    /// </summary>
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Optional correlation identifier for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Optional key used to make publishing idempotent
    /// </summary>
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Request to register a webhook subscription
/// </summary>
public sealed class RegisterWebhookRequest
{
    /// <summary>
    /// Endpoint URL that will receive webhook notifications
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Event types the webhook should be notified about
    /// </summary>
    public List<string> Events { get; set; } = new();

    /// <summary>
    /// Optional HTTP headers to include in webhook requests
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Whether the webhook subscription is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request to update a webhook subscription
/// </summary>
public sealed class UpdateWebhookRequest
{
    /// <summary>
    /// Optional new URL for the webhook
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Optional new list of events the webhook should be notified about
    /// </summary>
    public List<string>? Events { get; set; }

    /// <summary>
    /// Optional new HTTP headers to include in webhook requests
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Optional flag to activate or deactivate the webhook
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request to manually archive messages
/// </summary>
public sealed class ArchiveMessagesRequest
{
    /// <summary>
    /// Minimum age in days for messages to be archived
    /// </summary>
    public int DaysOld { get; set; } = 30;

    /// <summary>
    /// Optional maximum number of messages to archive
    /// </summary>
    public int? MaxMessages { get; set; }

    /// <summary>
    /// When true, only reports what would be archived without archiving
    /// </summary>
    public bool DryRun { get; set; } = false;
}

/// <summary>
/// Request to batch process messages
/// </summary>
public sealed class BatchProcessRequest
{
    /// <summary>
    /// Identifiers of the messages to process
    /// </summary>
    public List<Guid> MessageIds { get; set; } = new();

    /// <summary>
    /// Action to apply to the messages: "retry", "archive" or "delete"
    /// </summary>
    public string Action { get; set; } = string.Empty; // "retry", "archive", "delete"
}

/// <summary>
/// Request to search messages with filters
/// </summary>
public sealed class MessageSearchRequest
{
    /// <summary>
    /// Optional aggregate identifier to filter by
    /// </summary>
    public string? AggregateId { get; set; }

    /// <summary>
    /// Optional aggregate type to filter by
    /// </summary>
    public string? AggregateType { get; set; }

    /// <summary>
    /// Optional topic to filter by
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// Optional message state to filter by
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Optional lower bound for the created timestamp
    /// </summary>
    public DateTime? CreatedAfter { get; set; }

    /// <summary>
    /// Optional upper bound for the created timestamp
    /// </summary>
    public DateTime? CreatedBefore { get; set; }

    /// <summary>
    /// Optional minimum number of publish attempts
    /// </summary>
    public int? MinPublishAttempts { get; set; }

    /// <summary>
    /// Page number to return
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of results per page
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Property to sort results by
    /// </summary>
    public string? SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Sort direction: "asc" or "desc"
    /// </summary>
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// Request for dead letter review
/// </summary>
public sealed class ReviewDeadLetterRequest
{
    /// <summary>
    /// Notes describing the review decision
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Review status: "Reviewed" or "Ignored"
    /// </summary>
    public string Status { get; set; } = "Reviewed"; // Reviewed, Ignored
}

/// <summary>
/// Request to requeue a dead letter message
/// </summary>
public sealed class RequeueDeadLetterRequest
{
    /// <summary>
    /// Reason for requeueing the message
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Optional maximum number of retries after requeueing
    /// </summary>
    public int? MaxRetries { get; set; }
}

/// <summary>
/// Batch configuration for message export
/// </summary>
public sealed class ExportRequest
{
    /// <summary>
    /// Export format: "json", "csv" or "xml"
    /// </summary>
    public string Format { get; set; } = "json"; // json, csv, xml

    /// <summary>
    /// Optional start of the export date range
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Optional end of the export date range
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Optional message status to filter the export by
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Whether to include event payload data in the export
    /// </summary>
    public bool IncludeEventData { get; set; } = false;
}

/// <summary>
/// Request to create a new event type configuration
/// </summary>
public sealed class CreateEventTypeRequest
{
    /// <summary>
    /// Name of the event type
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the event type
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Topic to which events of this type are published
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Default maximum number of retries for this event type
    /// </summary>
    public int DefaultMaxRetries { get; set; } = 5;

    /// <summary>
    /// Default timeout in seconds for this event type
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Request to update event type configuration
/// </summary>
public sealed class UpdateEventTypeRequest
{
    /// <summary>
    /// Optional new description for the event type
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional new topic for the event type
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// Optional new default maximum number of retries
    /// </summary>
    public int? DefaultMaxRetries { get; set; }

    /// <summary>
    /// Optional new default timeout in seconds
    /// </summary>
    public int? DefaultTimeoutSeconds { get; set; }
}

/// <summary>
/// Request to create a notification
/// </summary>
public sealed class CreateNotificationRequest
{
    /// <summary>
    /// Title of the notification
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Body of the notification
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Severity of the notification: "Info", "Warning" or "Error"
    /// </summary>
    public string Severity { get; set; } = "Info"; // Info, Warning, Error

    /// <summary>
    /// Optional metadata associated with the notification
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
