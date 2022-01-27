#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetOutboxPattern.Exceptions;

/// <summary>
/// Extension methods for <see cref="OutboxException"/> and its derived types
/// </summary>
public static class OutboxExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception indicates a retryable error condition.
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the error is retryable; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null</exception>
    public static bool IsRetryable(this OutboxException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            MessagePublishingException => true,
            MessageLockingException => true,
            ServiceUnavailableException => true,
            ProcessingTimeoutException => true,
            OutboxRepositoryException repoEx when repoEx.Operation.Contains("timeout", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a formatted error message that includes the error code, resource ID, and exception details.
    /// </summary>
    /// <param name="exception">The exception to format</param>
    /// <returns>A formatted error message string</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null</exception>
    public static string GetFormattedErrorMessage(this OutboxException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message = $"[{exception.ErrorCode}] {exception.Message}";

        if (!string.IsNullOrEmpty(exception.ResourceId))
        {
            message += $" | Resource: " + exception.ResourceId;
        }

        if (exception is Exception ex && ex.InnerException != null)
        {
            message += $" | Inner: " + ex.InnerException.Message;
        }

        return message;
    }

    /// <summary>
    /// Gets a dictionary of diagnostic information about the exception.
    /// </summary>
    /// <param name="exception">The exception to analyze</param>
    /// <returns>A read-only dictionary containing diagnostic information</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null</exception>
    public static IReadOnlyDictionary<string, string> GetDiagnosticInfo(this OutboxException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var diagnostics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ErrorCode"] = exception.ErrorCode,
            ["Message"] = exception.Message,
            ["ExceptionType"] = exception.GetType().FullName ?? "Unknown"
        };

        if (!string.IsNullOrEmpty(exception.ResourceId))
        {
            diagnostics["ResourceId"] = exception.ResourceId!;
        }

        switch (exception)
        {
            case MessagePublishingException msgEx:
                diagnostics["MessageId"] = msgEx.MessageId.ToString("D", CultureInfo.InvariantCulture);
                diagnostics["AttemptNumber"] = msgEx.AttemptNumber.ToString(CultureInfo.InvariantCulture);
                break;

            case DeadLetterException dlEx:
                diagnostics["MessageId"] = dlEx.MessageId.ToString("D", CultureInfo.InvariantCulture);
                break;

            case MessageLockingException mlEx:
                diagnostics["MessageId"] = mlEx.MessageId.ToString("D", CultureInfo.InvariantCulture);
                break;

            case OutboxMessageNotFoundException notFoundEx:
                diagnostics["MessageId"] = notFoundEx.MessageId.ToString("D", CultureInfo.InvariantCulture);
                break;

            case MessageProcessingLockedException lockedEx:
                diagnostics["MessageId"] = lockedEx.MessageId.ToString("D", CultureInfo.InvariantCulture);
                break;

            case OutboxRepositoryException repoEx:
                diagnostics["Operation"] = repoEx.Operation;
                break;

            case SerializationException serEx:
                if (!string.IsNullOrEmpty(serEx.TargetType))
                {
                    diagnostics["TargetType"] = serEx.TargetType!;
                }
                break;

            case InvalidConfigurationException configEx:
                if (!string.IsNullOrEmpty(configEx.ConfigurationProperty))
                {
                    diagnostics["ConfigurationProperty"] = configEx.ConfigurationProperty!;
                }
                break;

            case ServiceUnavailableException svcEx:
                diagnostics["ServiceName"] = svcEx.ServiceName;
                break;

            case ProcessingTimeoutException timeoutEx:
                diagnostics["Timeout"] = timeoutEx.Timeout.ToString();
                break;

            case ValidationException valEx:
                diagnostics["ValidationErrors"] = string.Join("; ", valEx.Errors.Count);
                break;
        }

        if (exception.InnerException != null)
        {
            diagnostics["InnerExceptionType"] = exception.InnerException.GetType().FullName ?? "Unknown";
        }

        return diagnostics.AsReadOnly();
    }

    /// <summary>
    /// Creates a new exception with additional context while preserving the original exception.
    /// </summary>
    /// <param name="exception">The original exception</param>
    /// <param name="additionalContext">Additional context to include in the new message</param>
    /// <returns>A new exception with combined context</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null</exception>
    public static OutboxException WithContext(this OutboxException exception, string additionalContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(additionalContext);

        var newMessage = $"{exception.Message} | {additionalContext}";

        return exception switch
        {
            MessagePublishingException msgEx => new MessagePublishingException(
                newMessage,
                msgEx.MessageId,
                msgEx.AttemptNumber,
                exception),

            DeadLetterException dlEx => new DeadLetterException(
                newMessage,
                dlEx.MessageId,
                exception),

            MessageLockingException mlEx => new MessageLockingException(
                newMessage,
                mlEx.MessageId,
                exception),

            OutboxMessageNotFoundException notFoundEx => new OutboxMessageNotFoundException(
                notFoundEx.MessageId)
                {
                    Source = newMessage
                },

            MessageProcessingLockedException lockedEx => new MessageProcessingLockedException(
                lockedEx.MessageId)
                {
                    Source = newMessage
                },

            OutboxRepositoryException repoEx => new OutboxRepositoryException(
                newMessage,
                repoEx.Operation,
                exception),

            SerializationException serEx => new SerializationException(
                newMessage,
                serEx.TargetType,
                exception),

            InvalidConfigurationException configEx => new InvalidConfigurationException(
                newMessage,
                configEx.ConfigurationProperty),

            ServiceUnavailableException svcEx => new ServiceUnavailableException(
                svcEx.ServiceName,
                newMessage,
                exception),

            ProcessingTimeoutException timeoutEx => new ProcessingTimeoutException(
                newMessage,
                timeoutEx.Timeout,
                exception),

            ValidationException valEx => new ValidationException(
                newMessage,
                valEx.Errors),

            _ => new OutboxException(newMessage, exception)
        };
    }

    /// <summary>
    /// Gets a value indicating whether the exception represents a critical failure that should not be retried.
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the error is critical and should not be retried; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null</exception>
    public static bool IsCritical(this OutboxException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            InvalidMessageException => true,
            SerializationException => true,
            ValidationException => true,
            ProcessingInProgressException => true,
            OutboxMessageNotFoundException => true,
            MessageProcessingLockedException => true,
            _ => false
        };
    }
}