// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Dtos;

/// <summary>
/// Request to publish an event to the outbox
/// </summary>
public class PublishEventRequest
{
    public string AggregateId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object>? EventData { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? PartitionKey { get; set; }
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Request to register a webhook subscription
/// </summary>
public class RegisterWebhookRequest
{
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public Dictionary<string, string>? Headers { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request to update a webhook subscription
/// </summary>
public class UpdateWebhookRequest
{
    public string? Url { get; set; }
    public List<string>? Events { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request to manually archive messages
/// </summary>
public class ArchiveMessagesRequest
{
    public int DaysOld { get; set; } = 30;
    public int? MaxMessages { get; set; }
    public bool DryRun { get; set; } = false;
}

/// <summary>
/// Request to batch process messages
/// </summary>
public class BatchProcessRequest
{
    public List<Guid> MessageIds { get; set; } = new();
    public string Action { get; set; } = string.Empty; // "retry", "archive", "delete"
}

/// <summary>
/// Request to search messages with filters
/// </summary>
public class MessageSearchRequest
{
    public string? AggregateId { get; set; }
    public string? AggregateType { get; set; }
    public string? Topic { get; set; }
    public string? State { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int? MinPublishAttempts { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// Request for dead letter review
/// </summary>
public class ReviewDeadLetterRequest
{
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Reviewed"; // Reviewed, Ignored
}

/// <summary>
/// Request to requeue a dead letter message
/// </summary>
public class RequeueDeadLetterRequest
{
    public string Reason { get; set; } = string.Empty;
    public int? MaxRetries { get; set; }
}

/// <summary>
/// Batch configuration for message export
/// </summary>
public class ExportRequest
{
    public string Format { get; set; } = "json"; // json, csv, xml
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public bool IncludeEventData { get; set; } = false;
}

/// <summary>
/// Request to create a new event type configuration
/// </summary>
public class CreateEventTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int DefaultMaxRetries { get; set; } = 5;
    public int DefaultTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Request to update event type configuration
/// </summary>
public class UpdateEventTypeRequest
{
    public string? Description { get; set; }
    public string? Topic { get; set; }
    public int? DefaultMaxRetries { get; set; }
    public int? DefaultTimeoutSeconds { get; set; }
}

/// <summary>
/// Request to create a notification
/// </summary>
public class CreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Info, Warning, Error
    public Dictionary<string, string>? Metadata { get; set; }
}
