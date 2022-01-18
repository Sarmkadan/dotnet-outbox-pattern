#nullable enable
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
    /// <param name="logger">The logger instance</param>
    /// <param name="level">The log level</param>
    /// <param name="operation">The outbox operation being performed</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="context">Additional context data</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="operation"/> is null or empty</exception>
    public static void LogOutboxOperation(
        this ILogger logger,
        LogLevel level,
        string operation,
        string? aggregateId = null,
        string? messageId = null,
        Dictionary<string, object>? context = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(operation);

        var baseContext = new Dictionary<string, object>
        {
            { "Operation", operation },
            { "Timestamp", DateTime.UtcNow }
        };

        if (!string.IsNullOrEmpty(aggregateId))
            baseContext["AggregateId"] = aggregateId;

        if (!string.IsNullOrEmpty(messageId))
            baseContext["MessageId"] = messageId;

        if (context is not null)
        {
            foreach (var kvp in context)
                baseContext[kvp.Key] = kvp.Value;
        }

        logger.Log(level, "Outbox operation: {Operation}", operation);
    }

    /// <summary>
    /// Logs message publishing with details
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="topic">The target topic</param>
    /// <param name="publishAttempt">The current attempt number</param>
    /// <param name="maxAttempts">The maximum number of attempts</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> or <paramref name="topic"/> is null or empty</exception>
    public static void LogMessagePublishing(
        this ILogger logger,
        Guid messageId,
        string aggregateId,
        string topic,
        int publishAttempt,
        int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        ArgumentException.ThrowIfNullOrEmpty(topic);

        logger.LogInformation(
            "Publishing message {MessageId} for aggregate {AggregateId} to topic {Topic} (attempt {Attempt}/{MaxAttempts})",
            messageId, aggregateId, topic, publishAttempt, maxAttempts);
    }

    /// <summary>
    /// Logs message publishing success
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="durationMs">The duration in milliseconds</param>
    /// <param name="publishAttempts">The number of attempts made</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    public static void LogMessagePublishSuccess(
        this ILogger logger,
        Guid messageId,
        long durationMs,
        int publishAttempts)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogInformation(
            "Message {MessageId} published successfully in {DurationMs}ms (after {Attempts} attempt(s))",
            messageId, durationMs, publishAttempts);
    }

    /// <summary>
    /// Logs message publishing failure
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="errorMessage">The error message</param>
    /// <param name="publishAttempt">The current attempt number</param>
    /// <param name="maxAttempts">The maximum number of attempts</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errorMessage"/> is null or empty</exception>
    public static void LogMessagePublishFailure(
        this ILogger logger,
        Guid messageId,
        string errorMessage,
        int publishAttempt,
        int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);

        var level = publishAttempt >= maxAttempts ? LogLevel.Error : LogLevel.Warning;

        logger.Log(
            level,
            "Message {MessageId} publishing failed: {Error} (attempt {Attempt}/{MaxAttempts})",
            messageId, errorMessage, publishAttempt, maxAttempts);
    }

    /// <summary>
    /// Logs message moved to dead letter queue
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="reason">The reason for moving to dead letter queue</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> or <paramref name="reason"/> is null or empty</exception>
    public static void LogMessageMovedToDeadLetter(
        this ILogger logger,
        Guid messageId,
        string aggregateId,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        ArgumentException.ThrowIfNullOrEmpty(reason);

        logger.LogError(
            "Message {MessageId} for aggregate {AggregateId} moved to dead letter queue: {Reason}",
            messageId, aggregateId, reason);
    }

    /// <summary>
    /// Logs message retry
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="retryCount">The retry attempt number</param>
    /// <param name="delayMs">The delay in milliseconds before retry</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    public static void LogMessageRetry(
        this ILogger logger,
        Guid messageId,
        int retryCount,
        long delayMs)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogWarning(
            "Retrying message {MessageId} (retry #{RetryCount}) after {DelayMs}ms",
            messageId, retryCount, delayMs);
    }

    /// <summary>
    /// Logs system health status
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="status">The health status</param>
    /// <param name="pendingMessages">Number of pending messages</param>
    /// <param name="processingMessages">Number of messages being processed</param>
    /// <param name="failedMessages">Number of failed messages</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="status"/> is null or empty</exception>
    public static void LogHealthStatus(
        this ILogger logger,
        string status,
        int pendingMessages,
        int processingMessages,
        int failedMessages)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(status);

        var level = status == "Healthy" ? LogLevel.Information : LogLevel.Warning;

        logger.Log(
            level,
            "System health: {Status} - Pending: {Pending}, Processing: {Processing}, Failed: {Failed}",
            status, pendingMessages, processingMessages, failedMessages);
    }

    /// <summary>
    /// Logs performance metric
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="metricName">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="unit">The metric unit</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="metricName"/> or <paramref name="unit"/> is null or empty</exception>
    public static void LogPerformanceMetric(
        this ILogger logger,
        string metricName,
        double value,
        string unit)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(metricName);
        ArgumentException.ThrowIfNullOrEmpty(unit);

        logger.LogInformation(
            "Performance metric {MetricName}: {Value:F2}{Unit}",
            metricName, value, unit);
    }
}
