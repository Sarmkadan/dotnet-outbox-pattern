#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Dtos;

/// <summary>
/// Standard API error response
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string Code { get; set; } = "ERROR";
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the trace identifier.
    /// </summary>
    public string? TraceId { get; set; }
}

/// <summary>
/// Outbox message data transfer object
/// </summary>
public sealed class OutboxMessageDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the idempotency key.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the aggregate identifier.
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the aggregate type.
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the topic.
    /// </summary>
    public string Topic { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    public string State { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the number of publish attempts.
    /// </summary>
    public int PublishAttempts { get; set; }
    /// <summary>
    /// Gets or sets the maximum publish attempts.
    /// </summary>
    public int MaxPublishAttempts { get; set; }
    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the published timestamp.
    /// </summary>
    public DateTime? PublishedAt { get; set; }
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Gets or sets the partition key.
    /// </summary>
    public string? PartitionKey { get; set; }
    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    public string? CorrelationId { get; set; }

    public OutboxMessageDto() { }

    public OutboxMessageDto(OutboxMessage message)
    {
        Id = message.Id;
        IdempotencyKey = message.IdempotencyKey;
        AggregateId = message.AggregateId;
        AggregateType = message.AggregateType;
        EventType = message.EventTypeName;
        Topic = message.Topic;
        State = message.State.ToString();
        PublishAttempts = message.PublishAttempts;
        MaxPublishAttempts = message.MaxPublishAttempts;
        CreatedAt = message.CreatedAt;
        PublishedAt = message.PublishedAt;
        ErrorMessage = message.ErrorMessage;
        PartitionKey = message.PartitionKey;
        CorrelationId = message.CorrelationId;
    }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
public sealed class PaginatedResponse<T>
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int Page { get; set; }
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }
    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();
    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    public int TotalItems { get; set; }
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
}

/// <summary>
/// Statistics about the outbox
/// </summary>
public sealed class OutboxStatisticsDto
{
    /// <summary>
    /// Gets or sets the total number of messages.
    /// </summary>
    public int TotalMessages { get; set; }
    /// <summary>
    /// Gets or sets the pending message count.
    /// </summary>
    public int PendingCount { get; set; }
    /// <summary>
    /// Gets or sets the processing message count.
    /// </summary>
    public int ProcessingCount { get; set; }
    /// <summary>
    /// Gets or sets the published message count.
    /// </summary>
    public int PublishedCount { get; set; }
    /// <summary>
    /// Gets or sets the failed message count.
    /// </summary>
    public int FailedCount { get; set; }
    /// <summary>
    /// Gets or sets the dead letter count.
    /// </summary>
    public int DeadLetterCount { get; set; }
    /// <summary>
    /// Gets or sets the average publish attempts.
    /// </summary>
    public double AveragePublishAttempts { get; set; }
    /// <summary>
    /// Gets or sets the oldest pending message timestamp.
    /// </summary>
    public DateTime? OldestPendingMessage { get; set; }
    /// <summary>
    /// Gets or sets the success rate.
    /// </summary>
    public double SuccessRate { get; set; }

    public OutboxStatisticsDto() { }

    public OutboxStatisticsDto(OutboxStatistics stats)
    {
        TotalMessages = (int)stats.TotalMessages;
        PendingCount = (int)stats.PendingMessages;
        ProcessingCount = (int)stats.ProcessingMessages;
        PublishedCount = (int)stats.PublishedMessages;
        FailedCount = (int)stats.FailedMessages;
        DeadLetterCount = (int)stats.DeadLetterCount;
        AveragePublishAttempts = stats.AveragePublishTime.TotalMilliseconds;
        SuccessRate = stats.SuccessRate;
    }
}

/// <summary>
/// Archive operation result
/// </summary>
public sealed class ArchiveResult
{
    /// <summary>
    /// Gets or sets the number of archived items.
    /// </summary>
    public int ArchivedCount { get; set; }
    /// <summary>
    /// Gets or sets the execution timestamp.
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the operation status.
    /// </summary>
    public string Status { get; set; } = "Success";
}

/// <summary>
/// Webhook subscription data transfer object
/// </summary>
public sealed class WebhookSubscriptionDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the webhook URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the list of events to subscribe to.
    /// </summary>
    public List<string> Events { get; set; } = new();
    /// <summary>
    /// Gets or sets whether the subscription is active.
    /// </summary>
    public bool IsActive { get; set; }
    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the last delivery timestamp.
    /// </summary>
    public DateTime? LastDeliveryAt { get; set; }
    /// <summary>
    /// Gets or sets the number of successful deliveries.
    /// </summary>
    public int SuccessfulDeliveries { get; set; }
    /// <summary>
    /// Gets or sets the number of failed deliveries.
    /// </summary>
    public int FailedDeliveries { get; set; }

    public WebhookSubscriptionDto() { }

    public WebhookSubscriptionDto(dynamic subscription)
    {
        Id = subscription.Id;
        Url = subscription.Url;
        Events = subscription.Events;
        IsActive = subscription.IsActive;
        CreatedAt = subscription.CreatedAt;
        LastDeliveryAt = subscription.LastDeliveryAt;
        SuccessfulDeliveries = subscription.SuccessfulDeliveries;
        FailedDeliveries = subscription.FailedDeliveries;
    }
}

/// <summary>
/// Webhook delivery history entry
/// </summary>
public sealed class WebhookDeliveryDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int HttpStatusCode { get; set; }
    /// <summary>
    /// Gets or sets whether the delivery was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }
    /// <summary>
    /// Gets or sets the delivery timestamp.
    /// </summary>
    public DateTime DeliveredAt { get; set; }
    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }
    /// <summary>
    /// Gets or sets the response body.
    /// </summary>
    public string? ResponseBody { get; set; }

    public WebhookDeliveryDto() { }

    public WebhookDeliveryDto(dynamic delivery)
    {
        Id = delivery.Id;
        HttpStatusCode = delivery.HttpStatusCode;
        IsSuccessful = delivery.IsSuccessful;
        DeliveredAt = delivery.DeliveredAt;
        DurationMs = delivery.DurationMs;
        ResponseBody = delivery.ResponseBody;
    }
}

/// <summary>
/// Webhook test result
/// </summary>
public sealed class WebhookTestResult
{
    /// <summary>
    /// Gets or sets whether the test was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int HttpStatusCode { get; set; }
    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }
    /// <summary>
    /// Gets or sets the response body.
    /// </summary>
    public string? ResponseBody { get; set; }
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Gets or sets the test timestamp.
    /// </summary>
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// System health metrics
/// </summary>
public sealed class SystemHealthDto
{
    public string Status { get; set; } = "Healthy";
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public int PendingMessages { get; set; }
    public int ProcessingMessages { get; set; }
    public int LockedMessages { get; set; }
    public bool DatabaseConnected { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public string? ErrorMessage { get; set; }

    public SystemHealthDto() { }

    public SystemHealthDto(dynamic health)
    {
        Status = health.Status;
        PendingMessages = health.PendingMessages;
        ProcessingMessages = health.ProcessingMessages;
        LockedMessages = health.LockedMessages;
        DatabaseConnected = health.DatabaseConnected;
        CpuUsagePercent = health.CpuUsagePercent;
        MemoryUsagePercent = health.MemoryUsagePercent;
    }
}

/// <summary>
/// Performance metrics
/// </summary>
public sealed class PerformanceMetricsDto
{
    public long AverageLatencyMs { get; set; }
    public long P50LatencyMs { get; set; }
    public long P95LatencyMs { get; set; }
    public long P99LatencyMs { get; set; }
    public int RequestsPerSecond { get; set; }
    public double ErrorRate { get; set; }

    public PerformanceMetricsDto() { }

    public PerformanceMetricsDto(dynamic metrics)
    {
        AverageLatencyMs = metrics.AverageLatencyMs;
        P50LatencyMs = metrics.P50LatencyMs;
        P95LatencyMs = metrics.P95LatencyMs;
        P99LatencyMs = metrics.P99LatencyMs;
        RequestsPerSecond = metrics.RequestsPerSecond;
        ErrorRate = metrics.ErrorRate;
    }
}

/// <summary>
/// Error analysis and distribution
/// </summary>
public sealed class ErrorAnalyticsDto
{
    public int TotalErrors { get; set; }
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public Dictionary<string, int> ErrorsByAggregate { get; set; } = new();
    public int DeadLetterCount { get; set; }
    public double ErrorRate { get; set; }

    public ErrorAnalyticsDto() { }

    public ErrorAnalyticsDto(dynamic analytics)
    {
        TotalErrors = analytics.TotalErrors;
        ErrorsByType = analytics.ErrorsByType;
        ErrorsByAggregate = analytics.ErrorsByAggregate;
        DeadLetterCount = analytics.DeadLetterCount;
        ErrorRate = analytics.ErrorRate;
    }
}

/// <summary>
/// Message throughput metrics
/// </summary>
public sealed class ThroughputMetricsDto
{
    public List<ThroughputDataPoint> DataPoints { get; set; } = new();
    public int TotalMessages { get; set; }
    public double AveragePerUnit { get; set; }

    public ThroughputMetricsDto() { }

    public ThroughputMetricsDto(dynamic metrics)
    {
        DataPoints = metrics.DataPoints;
        TotalMessages = metrics.TotalMessages;
        AveragePerUnit = metrics.AveragePerUnit;
    }
}

public sealed class ThroughputDataPoint
{
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Latency percentile metrics
/// </summary>
public sealed class LatencyMetricsDto
{
    public long MinLatencyMs { get; set; }
    public long MaxLatencyMs { get; set; }
    public long AverageLatencyMs { get; set; }
    public long MedianLatencyMs { get; set; }

    public LatencyMetricsDto() { }

    public LatencyMetricsDto(dynamic metrics)
    {
        MinLatencyMs = metrics.MinLatencyMs;
        MaxLatencyMs = metrics.MaxLatencyMs;
        AverageLatencyMs = metrics.AverageLatencyMs;
        MedianLatencyMs = metrics.MedianLatencyMs;
    }
}

/// <summary>
/// System resource metrics
/// </summary>
public sealed class ResourceMetricsDto
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public int ActiveConnections { get; set; }
    public long DiskSpaceUsedBytes { get; set; }
}

/// <summary>
/// Alert data
/// </summary>
public sealed class AlertDto
{
    public string Severity { get; set; } = "Warning";
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AlertDto() { }

    public AlertDto(dynamic alert)
    {
        Severity = alert.Severity;
        Message = alert.Message;
        CreatedAt = alert.CreatedAt;
    }
}
