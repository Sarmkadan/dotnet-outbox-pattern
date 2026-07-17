#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using System.Globalization;

namespace DotnetOutboxPattern.Logging;

/// <summary>
/// Validation helpers for StructuredLoggingExtensions to ensure proper logging configuration
/// </summary>
public static class StructuredLoggingExtensionsValidation
{
    /// <summary>
    /// Validates the logger parameter for StructuredLoggingExtensions methods
    /// </summary>
    /// <param name="logger">The logger instance to validate</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown if logger is null</exception>
    public static IReadOnlyList<string> Validate(this ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var problems = new List<string>();

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for LogOutboxOperation
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="operation">The operation name</param>
    /// <param name="aggregateId">Optional aggregate identifier</param>
    /// <param name="messageId">Optional message identifier</param>
    /// <param name="context">Optional context dictionary</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(
        this ILogger logger,
        string operation,
        string? aggregateId = null,
        string? messageId = null,
        Dictionary<string, object>? context = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(operation);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(operation))
            problems.Add("Operation name cannot be empty or whitespace.");

        if (!string.IsNullOrEmpty(aggregateId) && string.IsNullOrWhiteSpace(aggregateId))
            problems.Add("AggregateId cannot be whitespace.");

        if (!string.IsNullOrEmpty(messageId) && string.IsNullOrWhiteSpace(messageId))
            problems.Add("MessageId cannot be whitespace.");

        if (context is not null)
        {
            foreach (var kvp in context)
            {
                if (kvp.Key is null)
                    problems.Add("Context dictionary cannot contain null keys.");
                else if (string.IsNullOrWhiteSpace(kvp.Key))
                    problems.Add("Context dictionary keys cannot be empty or whitespace.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for LogMessagePublishing
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="topic">The topic name</param>
    /// <param name="publishAttempt">Current publish attempt number</param>
    /// <param name="maxAttempts">Maximum allowed attempts</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(
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

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(aggregateId))
            problems.Add("AggregateId cannot be whitespace.");

        if (string.IsNullOrWhiteSpace(topic))
            problems.Add("Topic name cannot be whitespace.");

        if (publishAttempt < 0)
            problems.Add("Publish attempt cannot be negative.");

        if (maxAttempts <= 0)
            problems.Add("Max attempts must be positive.");

        if (publishAttempt > maxAttempts)
            problems.Add("Publish attempt cannot exceed max attempts.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for LogMessagePublishSuccess
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="durationMs">Duration in milliseconds</param>
    /// <param name="publishAttempts">Number of publish attempts</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(
        this ILogger logger,
        Guid messageId,
        long durationMs,
        int publishAttempts)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var problems = new List<string>();

        if (durationMs < 0)
            problems.Add("Duration cannot be negative.");

        if (publishAttempts < 0)
            problems.Add("Publish attempts cannot be negative.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for LogMessagePublishFailure
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="publishAttempt">Current publish attempt number</param>
    /// <param name="maxAttempts">Maximum allowed attempts</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(
        this ILogger logger,
        Guid messageId,
        string errorMessage,
        int publishAttempt,
        int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(errorMessage))
            problems.Add("Error message cannot be whitespace.");

        if (publishAttempt < 0)
            problems.Add("Publish attempt cannot be negative.");

        if (maxAttempts <= 0)
            problems.Add("Max attempts must be positive.");

        if (publishAttempt > maxAttempts)
            problems.Add("Publish attempt cannot exceed max attempts.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for LogMessageMovedToDeadLetter
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="reason">Reason for moving to dead letter</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(
        this ILogger logger,
        Guid messageId,
        string aggregateId,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        ArgumentException.ThrowIfNullOrEmpty(reason);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(aggregateId))
            problems.Add("AggregateId cannot be whitespace.");

        if (string.IsNullOrWhiteSpace(reason))
            problems.Add("Reason cannot be whitespace.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for LogMessageRetry
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="messageId">The message identifier</param>
    /// <param name="retryCount">Retry count</param>
    /// <param name="delayMs">Delay in milliseconds</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(
        this ILogger logger,
        Guid messageId,
        int retryCount,
        long delayMs)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var problems = new List<string>();

        if (retryCount < 0)
            problems.Add("Retry count cannot be negative.");

        if (delayMs < 0)
            problems.Add("Delay cannot be negative.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for LogHealthStatus
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="status">Health status</param>
    /// <param name="pendingMessages">Pending message count</param>
    /// <param name="processingMessages">Processing message count</param>
    /// <param name="failedMessages">Failed message count</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(
        this ILogger logger,
        string status,
        int pendingMessages,
        int processingMessages,
        int failedMessages)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(status);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(status))
            problems.Add("Status cannot be whitespace.");

        if (pendingMessages < 0)
            problems.Add("Pending messages count cannot be negative.");

        if (processingMessages < 0)
            problems.Add("Processing messages count cannot be negative.");

        if (failedMessages < 0)
            problems.Add("Failed messages count cannot be negative.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates parameters for LogPerformanceMetric
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="metricName">Metric name</param>
    /// <param name="value">Metric value</param>
    /// <param name="unit">Metric unit</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(
        this ILogger logger,
        string metricName,
        double value,
        string unit)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrEmpty(metricName);
        ArgumentException.ThrowIfNullOrEmpty(unit);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(metricName))
            problems.Add("Metric name cannot be whitespace.");

        if (string.IsNullOrWhiteSpace(unit))
            problems.Add("Unit cannot be whitespace.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if the logger parameter is not null
    /// </summary>
    /// <param name="logger">The logger instance to check</param>
    /// <returns>True if logger is not null, false otherwise</returns>
    public static bool IsValid(this ILogger? logger) => logger is not null;

    /// <summary>
    /// Ensures the logger parameter is valid for StructuredLoggingExtensions
    /// </summary>
    /// <param name="logger">The logger instance to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if logger is null</exception>
    public static void EnsureValid(this ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
    }
}