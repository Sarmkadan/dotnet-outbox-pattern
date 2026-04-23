#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Exceptions;

/// <summary>
/// Base exception for outbox pattern related errors
/// </summary>
public sealed class OutboxException : Exception
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
/// Exception thrown when message publishing fails
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
/// Exception thrown when a dead letter operation fails
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
/// Exception thrown when message validation fails
/// </summary>
public sealed class InvalidMessageException : OutboxException
{
    public InvalidMessageException(string message, Exception? innerException = null)
        : base(message, innerException ?? new Exception(message), "INVALID_MESSAGE")
    {
    }
}

/// <summary>
/// Exception thrown when database operations fail
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
/// Exception thrown when message locking fails
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
/// Exception thrown when an outbox message is not found
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
/// Exception thrown when serialization/deserialization fails
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
/// Exception thrown when processing is already in progress
/// </summary>
public sealed class ProcessingInProgressException : OutboxException
{
    public ProcessingInProgressException(string message)
        : base(message, "PROCESSING_IN_PROGRESS")
    {
    }
}

/// <summary>
/// Exception thrown when configuration is invalid
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
