#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Constants used throughout the outbox pattern implementation
/// </summary>
public static class OutboxConstants
{
    /// <summary>
    /// Default topic name for domain events
    /// </summary>
    public const string DefaultTopic = "domain-events";

    /// <summary>
    /// Topic name for dead letter messages
    /// </summary>
    public const string DeadLetterTopic = "dead-letters";

    /// <summary>
    /// Maximum length for aggregate IDs
    /// </summary>
    public const int MaxAggregateIdLength = 256;

    /// <summary>
    /// Maximum length for topic names
    /// </summary>
    public const int MaxTopicLength = 128;

    /// <summary>
    /// Maximum length for event type names
    /// </summary>
    public const int MaxEventTypeNameLength = 256;

    /// <summary>
    /// Maximum length for error messages
    /// </summary>
    public const int MaxErrorMessageLength = 2000;

    /// <summary>
    /// Maximum length for correlation IDs
    /// </summary>
    public const int MaxCorrelationIdLength = 256;

    /// <summary>
    /// Default maximum number of publishing attempts
    /// </summary>
    public const int DefaultMaxPublishAttempts = 5;

    /// <summary>
    /// Default batch size for processing messages
    /// </summary>
    public const int DefaultBatchSize = 100;

    /// <summary>
    /// Default delay between processing batches (milliseconds)
    /// </summary>
    public const int DefaultDelayBetweenBatches = 5000;

    /// <summary>
    /// Default lock duration for processing (seconds)
    /// </summary>
    public const int DefaultLockDurationSeconds = 300;

    /// <summary>
    /// Default publish timeout (seconds)
    /// </summary>
    public const int DefaultPublishTimeoutSeconds = 30;

    /// <summary>
    /// Default interval for checking expired locks (milliseconds)
    /// </summary>
    public const int DefaultCheckExpiredLocksInterval = 60000;

    /// <summary>
    /// Minimum batch size validation
    /// </summary>
    public const int MinBatchSize = 1;

    /// <summary>
    /// Maximum batch size validation
    /// </summary>
    public const int MaxBatchSize = 10000;

    /// <summary>
    /// Default degree of parallelism for processing
    /// </summary>
    public const int DefaultDegreeOfParallelism = 4;

    /// <summary>
    /// Time range for archiving old messages (days)
    /// </summary>
    public const int DefaultArchiveDaysOld = 30;
}

/// <summary>
/// Standard outbox message topics/channels
/// </summary>
public static class StandardTopics
{
    /// <summary>
    /// Topic for order-related domain events
    /// </summary>
    public const string Orders = "orders.events";

    /// <summary>
    /// Topic for customer-related domain events
    /// </summary>
    public const string Customers = "customers.events";

    /// <summary>
    /// Topic for payment-related domain events
    /// </summary>
    public const string Payments = "payments.events";

    /// <summary>
    /// Topic for inventory-related domain events
    /// </summary>
    public const string Inventory = "inventory.events";

    /// <summary>
    /// Topic for notification events
    /// </summary>
    public const string Notifications = "notifications.events";

    /// <summary>
    /// Topic for system/infrastructure events
    /// </summary>
    public const string System = "system.events";
}

/// <summary>
/// Structured log property names for correlation and tracing
/// </summary>
public static class LogProperties
{
    /// <summary>
    /// Identifies the structured log property containing the outbox message identifier.
    /// </summary>
    public const string MessageId = "MessageId";

    /// <summary>
    /// Identifies the structured log property containing the correlation identifier.
    /// </summary>
    public const string CorrelationId = "CorrelationId";

    /// <summary>
    /// Identifies the structured log property containing the causation identifier.
    /// </summary>
    public const string CausationId = "CausationId";

    /// <summary>
    /// Identifies the structured log property containing the aggregate identifier.
    /// </summary>
    public const string AggregateId = "AggregateId";

    /// <summary>
    /// Identifies the structured log property containing the message topic.
    /// </summary>
    public const string Topic = "Topic";

    /// <summary>
    /// Identifies the structured log property containing the message state.
    /// </summary>
    public const string State = "State";

    /// <summary>
    /// Identifies the structured log property containing the number of processing attempts.
    /// </summary>
    public const string Attempts = "Attempts";

    /// <summary>
    /// Identifies the structured log property containing an operation's duration.
    /// </summary>
    public const string Duration = "Duration";

    /// <summary>
    /// Identifies the structured log property indicating whether an operation succeeded.
    /// </summary>
    public const string Success = "Success";
}

/// <summary>
/// Error codes for different failure scenarios
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Indicates that the requested outbox message could not be found.
    /// </summary>
    public const string MessageNotFound = "MSG_NOT_FOUND";

    /// <summary>
    /// Indicates that publishing an outbox message failed.
    /// </summary>
    public const string PublishingFailed = "PUBLISH_FAILED";

    /// <summary>
    /// Indicates that message serialization failed.
    /// </summary>
    public const string SerializationError = "SERIALIZATION_ERROR";

    /// <summary>
    /// Indicates that message deserialization failed.
    /// </summary>
    public const string DeserializationError = "DESERIALIZATION_ERROR";

    /// <summary>
    /// Indicates that a database operation failed.
    /// </summary>
    public const string DatabaseError = "DATABASE_ERROR";

    /// <summary>
    /// Indicates that an outbox message is invalid.
    /// </summary>
    public const string InvalidMessage = "INVALID_MESSAGE";

    /// <summary>
    /// Indicates that the outbox configuration is invalid.
    /// </summary>
    public const string InvalidConfiguration = "INVALID_CONFIG";

    /// <summary>
    /// Indicates that an operation exceeded its allowed execution time.
    /// </summary>
    public const string OperationTimeout = "OPERATION_TIMEOUT";

    /// <summary>
    /// Indicates that an operation encountered a concurrency conflict.
    /// </summary>
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

    /// <summary>
    /// Indicates that an operation involving the dead-letter queue failed.
    /// </summary>
    public const string DeadLetterQueueError = "DLQ_ERROR";
}

/// <summary>
/// HTTP header names for outbox integration
/// </summary>
public static class HttpHeaders
{
    /// <summary>
    /// Names the HTTP header used to propagate a correlation identifier.
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// Names the HTTP header used to propagate a causation identifier.
    /// </summary>
    public const string CausationId = "X-Causation-Id";

    /// <summary>
    /// Names the HTTP header used to provide an idempotency key.
    /// </summary>
    public const string IdempotencyKey = "X-Idempotency-Key";

    /// <summary>
    /// Names the HTTP header used to propagate a request identifier.
    /// </summary>
    public const string RequestId = "X-Request-Id";
}

/// <summary>
/// Database schema constants for outbox tables and columns
/// </summary>
public static class DatabaseSchema
{
    /// <summary>
    /// Name of the outbox messages table
    /// </summary>
    public const string OutboxMessages = "OutboxMessages";

    /// <summary>
    /// Name of the outbox message ID column
    /// </summary>
    public const string Id = "Id";

    /// <summary>
    /// Name of the outbox message state column
    /// </summary>
    public const string State = "State";

    /// <summary>
    /// Name of the outbox message lock flag column
    /// </summary>
    public const string IsLocked = "IsLocked";

    /// <summary>
    /// Name of the outbox message lock expiration column
    /// </summary>
    public const string LockExpiresAt = "LockExpiresAt";

    /// <summary>
    /// Name of the outbox message priority column
    /// </summary>
    public const string Priority = "Priority";

    /// <summary>
    /// Name of the outbox message creation timestamp column
    /// </summary>
    public const string CreatedAt = "CreatedAt";

    /// <summary>
    /// Name of the outbox message scheduled timestamp column
    /// </summary>
    public const string ScheduledFor = "ScheduledFor";

    /// <summary>
    /// Name of the outbox message last processed timestamp column
    /// </summary>
    public const string LastProcessedAt = "LastProcessedAt";

    /// <summary>
    /// Name of the outbox message partition key column
    /// </summary>
    public const string PartitionKey = "PartitionKey";

    /// <summary>
    /// Name of the outbox message topic column
    /// </summary>
    public const string Topic = "Topic";
}
