// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Logging;

/// <summary>
/// Extension methods for structured logging with custom contexts
/// Provides semantic logging for outbox operations
/// </summary>
public static class StructuredLoggingExtensions
{
    /// <summary>
    /// Logs outbox operation with context
    /// </summary>
    public static void LogOutboxOperation(
        this ILogger logger,
        LogLevel level,
        string operation,
        string? aggregateId = null,
        string? messageId = null,
        Dictionary<string, object>? context = null)
    {
        var baseContext = new Dictionary<string, object>
        {
            { "Operation", operation },
            { "Timestamp", DateTime.UtcNow }
        };

        if (!string.IsNullOrEmpty(aggregateId))
            baseContext["AggregateId"] = aggregateId;

        if (!string.IsNullOrEmpty(messageId))
            baseContext["MessageId"] = messageId;

        if (context != null)
        {
            foreach (var kvp in context)
                baseContext[kvp.Key] = kvp.Value;
        }

        var message = $"Outbox operation: {operation}";
        logger.Log(level, message);
    }

    /// <summary>
    /// Logs message publishing with details
    /// </summary>
    public static void LogMessagePublishing(
        this ILogger logger,
        Guid messageId,
        string aggregateId,
        string topic,
        int publishAttempt,
        int maxAttempts)
    {
        logger.LogInformation(
            "Publishing message {MessageId} for aggregate {AggregateId} to topic {Topic} (attempt {Attempt}/{MaxAttempts})",
            messageId, aggregateId, topic, publishAttempt, maxAttempts);
    }

    /// <summary>
    /// Logs message publishing success
    /// </summary>
    public static void LogMessagePublishSuccess(
        this ILogger logger,
        Guid messageId,
        long durationMs,
        int publishAttempts)
    {
        logger.LogInformation(
            "Message {MessageId} published successfully in {DurationMs}ms (after {Attempts} attempt(s))",
            messageId, durationMs, publishAttempts);
    }

    /// <summary>
    /// Logs message publishing failure
    /// </summary>
    public static void LogMessagePublishFailure(
        this ILogger logger,
        Guid messageId,
        string errorMessage,
        int publishAttempt,
        int maxAttempts)
    {
        var level = publishAttempt >= maxAttempts ? LogLevel.Error : LogLevel.Warning;

        logger.Log(
            level,
            "Message {MessageId} publishing failed: {Error} (attempt {Attempt}/{MaxAttempts})",
            messageId, errorMessage, publishAttempt, maxAttempts);
    }

    /// <summary>
    /// Logs message moved to dead letter queue
    /// </summary>
    public static void LogMessageMovedToDeadLetter(
        this ILogger logger,
        Guid messageId,
        string aggregateId,
        string reason)
    {
        logger.LogError(
            "Message {MessageId} for aggregate {AggregateId} moved to dead letter queue: {Reason}",
            messageId, aggregateId, reason);
    }

    /// <summary>
    /// Logs message retry
    /// </summary>
    public static void LogMessageRetry(
        this ILogger logger,
        Guid messageId,
        int retryCount,
        long delayMs)
    {
        logger.LogWarning(
            "Retrying message {MessageId} (retry #{RetryCount}) after {DelayMs}ms",
            messageId, retryCount, delayMs);
    }

    /// <summary>
    /// Logs system health status
    /// </summary>
    public static void LogHealthStatus(
        this ILogger logger,
        string status,
        int pendingMessages,
        int processingMessages,
        int failedMessages)
    {
        var level = status == "Healthy" ? LogLevel.Information : LogLevel.Warning;

        logger.Log(
            level,
            "System health: {Status} - Pending: {Pending}, Processing: {Processing}, Failed: {Failed}",
            status, pendingMessages, processingMessages, failedMessages);
    }

    /// <summary>
    /// Logs performance metric
    /// </summary>
    public static void LogPerformanceMetric(
        this ILogger logger,
        string metricName,
        double value,
        string unit)
    {
        logger.LogInformation(
            "Performance metric {MetricName}: {Value:F2}{Unit}",
            metricName, value, unit);
    }
}
