// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Dtos;

/// <summary>
/// Standard API error response
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = "ERROR";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
}

/// <summary>
/// Outbox message data transfer object
/// </summary>
public class OutboxMessageDto
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int PublishAttempts { get; set; }
    public int MaxPublishAttempts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PartitionKey { get; set; }
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
public class PaginatedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
}

/// <summary>
/// Statistics about the outbox
/// </summary>
public class OutboxStatisticsDto
{
    public int PendingCount { get; set; }
    public int ProcessingCount { get; set; }
    public int PublishedCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
    public double AveragePublishAttempts { get; set; }
    public DateTime? OldestPendingMessage { get; set; }
    public double SuccessRate { get; set; }

    public OutboxStatisticsDto() { }

    public OutboxStatisticsDto(OutboxMessageStatistics stats)
    {
        PendingCount = stats.PendingCount;
        ProcessingCount = stats.ProcessingCount;
        PublishedCount = stats.PublishedCount;
        FailedCount = stats.FailedCount;
        DeadLetterCount = stats.DeadLetterCount;
        AveragePublishAttempts = stats.AveragePublishAttempts;
        SuccessRate = stats.SuccessRate;
    }
}

/// <summary>
/// Archive operation result
/// </summary>
public class ArchiveResult
{
    public int ArchivedCount { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Success";
}

/// <summary>
/// Webhook subscription data transfer object
/// </summary>
public class WebhookSubscriptionDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastDeliveryAt { get; set; }
    public int SuccessfulDeliveries { get; set; }
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
public class WebhookDeliveryDto
{
    public Guid Id { get; set; }
    public int HttpStatusCode { get; set; }
    public bool IsSuccessful { get; set; }
    public DateTime DeliveredAt { get; set; }
    public int DurationMs { get; set; }
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
public class WebhookTestResult
{
    public bool IsSuccessful { get; set; }
    public int HttpStatusCode { get; set; }
    public int DurationMs { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// System health metrics
/// </summary>
public class SystemHealthDto
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
public class PerformanceMetricsDto
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
public class ErrorAnalyticsDto
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
public class ThroughputMetricsDto
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

public class ThroughputDataPoint
{
    public DateTime Timestamp { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Latency percentile metrics
/// </summary>
public class LatencyMetricsDto
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
public class ResourceMetricsDto
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public int ActiveConnections { get; set; }
    public long DiskSpaceUsedBytes { get; set; }
}

/// <summary>
/// Alert data
/// </summary>
public class AlertDto
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
