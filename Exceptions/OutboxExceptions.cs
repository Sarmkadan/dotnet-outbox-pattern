#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Exceptions;

/// <summary>
/// Base exception for outbox pattern related errors.
/// </summary>
/// <remarks>
/// Thrown when an error occurs in the outbox pattern that doesn't fall into more specific categories.
/// This is the base class for all outbox-specific exceptions and should not be thrown directly.
/// </remarks>
public class OutboxException : Exception
{
    /// <summary>
    /// Error code for this exception
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Related resource identifier if applicable
    /// </summary>
    public string? ResourceId { get; }

    public OutboxException(string message, string errorCode = "OUTBOX_ERROR", string? resourceId = null)
        : base(message)
    {
        ErrorCode = errorCode;
        ResourceId = resourceId;
    }

    public OutboxException(string message, Exception innerException, string errorCode = "OUTBOX_ERROR", string? resourceId = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ResourceId = resourceId;
    }
}

/// <summary>
/// Exception thrown when message publishing fails. This occurs when a message cannot be published to the message broker after all retry attempts have been exhausted.
/// </summary>
public sealed class MessagePublishingException : OutboxException
{
    /// <summary>
    /// ID of the message that failed to publish
    /// </summary>
    public Guid MessageId { get; }

    /// <summary>
    /// Number of attempts made
    /// </summary>
    public int AttemptNumber { get; }

    public MessagePublishingException(string message, Guid messageId, int attemptNumber = 1, Exception? innerException = null)
        : base(message, innerException ?? new Exception(message), "MESSAGE_PUBLISH_FAILED", messageId.ToString())
    {
        MessageId = messageId;
        AttemptNumber = attemptNumber;
    }
}

/// <summary>
/// Exception thrown when a dead letter operation fails. This occurs when attempting to move a message to the dead letter queue encounters an error.
/// </summary>
public sealed class DeadLetterException : OutboxException
{
    /// <summary>
    /// ID of the message moved to dead letter
    /// </summary>
    public Guid MessageId { get; }

    public DeadLetterException(string message, Guid messageId, Exception? innerException = null)
        : base(message, innerException ?? new Exception(message), "DEAD_LETTER_ERROR", messageId.ToString())
    {
        MessageId = messageId;
    }
}

/// <summary>
/// Exception thrown when message validation fails. This occurs when a message does not meet the required validation criteria before processing.
/// </summary>
public sealed class InvalidMessageException : OutboxException
{
    public InvalidMessageException(string message, Exception? innerException = null)
        : base(message, innerException ?? new Exception(message), "INVALID_MESSAGE")
    {
    }
}

/// <summary>
/// Exception thrown when database operations fail. This occurs when there is an error interacting with the outbox storage (e.g., SQL errors, connection issues).
/// </summary>
public sealed class OutboxRepositoryException : OutboxException
{
    /// <summary>
    /// Operation that failed
    /// </summary>
    public string Operation { get; }

    public OutboxRepositoryException(string message, string operation, Exception? innerException = null)
        : base(message, innerException ?? new Exception(message), "REPOSITORY_ERROR")
    {
        Operation = operation;
    }
}

/// <summary>
/// Exception thrown when message locking fails. This occurs when attempting to acquire a lock on a message for processing encounters an error.
/// </summary>
public sealed class MessageLockingException : OutboxException
{
    /// <summary>
    /// ID of the message that failed to lock
    /// </summary>
    public Guid MessageId { get; }

    public MessageLockingException(string message, Guid messageId, Exception? innerException = null)
        : base(message, innerException ?? new Exception(message), "MESSAGE_LOCKING_FAILED", messageId.ToString())
    {
        MessageId = messageId;
    }
}

/// <summary>
/// Exception thrown when an outbox message is not found. This occurs when attempting to retrieve a message by its ID and no matching record exists in the outbox store.
/// </summary>
public sealed class OutboxMessageNotFoundException : OutboxException
{
    /// <summary>
    /// ID of the message that was not found
    /// </summary>
    public Guid MessageId { get; }

    public OutboxMessageNotFoundException(Guid messageId)
        : base($"Outbox message with ID '{messageId}' was not found", "MESSAGE_NOT_FOUND", messageId.ToString())
    {
        MessageId = messageId;
    }
}

/// <summary>
/// Exception thrown when serialization/deserialization fails. This occurs when converting messages to/from their stored format encounters an error.
/// </summary>
public sealed class SerializationException : OutboxException
{
    /// <summary>
    /// Type that failed to serialize/deserialize
    /// </summary>
    public string? TargetType { get; }

    public SerializationException(string message, string? targetType = null, Exception? innerException = null)
        : base(message, innerException ?? new Exception(message), "SERIALIZATION_ERROR")
    {
        TargetType = targetType;
    }
}

/// <summary>
/// Exception thrown when processing is already in progress. This occurs when attempting to process a message that is currently being handled by another process or thread.
/// </summary>
public sealed class ProcessingInProgressException : OutboxException
{
    public ProcessingInProgressException(string message)
        : base(message, "PROCESSING_IN_PROGRESS")
    {
    }
}

/// <summary>
/// Exception thrown when configuration is invalid. This occurs when the outbox configuration contains invalid values or missing required settings.
/// </summary>
public sealed class InvalidConfigurationException : OutboxException
{
    /// <summary>
    /// Configuration property that is invalid
    /// </summary>
    public string? ConfigurationProperty { get; }

    public InvalidConfigurationException(string message, string? property = null)
        : base(message, "INVALID_CONFIGURATION")
    {
        ConfigurationProperty = property;
    }
}

/// <summary>
/// Exception thrown when validation fails. This occurs when message validation encounters one or more validation errors.
/// </summary>
public sealed class ValidationException : OutboxException
{
    /// <summary>
    /// Validation errors
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message, "VALIDATION_ERROR")
    {
        Errors = errors;
    }

    public ValidationException(string message, string propertyName, string error)
        : base(message, "VALIDATION_ERROR")
    {
        Errors = new Dictionary<string, string[]> { { propertyName, new[] { error } } };
    }
}

/// <summary>
/// Exception thrown when message processing is locked. This occurs when attempting to process a message that is currently locked by another process.
/// </summary>
public sealed class MessageProcessingLockedException : OutboxException
{
    /// <summary>
    /// ID of the message that is locked
    /// </summary>
    public Guid MessageId { get; }

    public MessageProcessingLockedException(Guid messageId)
        : base($"Message {messageId} is currently locked and cannot be processed", "MESSAGE_LOCKED", messageId.ToString())
    {
        MessageId = messageId;
    }
}

/// <summary>
/// Exception thrown when a required service is not available. This occurs when a dependent service (e.g., message broker, database) is unavailable or unreachable.
/// </summary>
public sealed class ServiceUnavailableException : OutboxException
{
    /// <summary>
    /// Name of the unavailable service
    /// </summary>
    public string ServiceName { get; }

    public ServiceUnavailableException(string serviceName, string message, Exception? innerException = null)
        : base(message, "SERVICE_UNAVAILABLE", serviceName)
    {
        ServiceName = serviceName;
    }
}

/// <summary>
/// Exception thrown when a timeout occurs during processing
/// </summary>
public sealed class ProcessingTimeoutException : OutboxException
{
    /// <summary>
    /// Timeout duration
    /// </summary>
    public TimeSpan Timeout { get; }

    public ProcessingTimeoutException(string message, TimeSpan timeout, Exception? innerException = null)
        : base(message, "PROCESSING_TIMEOUT")
    {
        Timeout = timeout;
    }
}
