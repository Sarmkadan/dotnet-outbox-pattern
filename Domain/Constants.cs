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
    public const string MessageId = "MessageId";
    public const string CorrelationId = "CorrelationId";
    public const string CausationId = "CausationId";
    public const string AggregateId = "AggregateId";
    public const string Topic = "Topic";
    public const string State = "State";
    public const string Attempts = "Attempts";
    public const string Duration = "Duration";
    public const string Success = "Success";
}

/// <summary>
/// Error codes for different failure scenarios
/// </summary>
public static class ErrorCodes
{
    public const string MessageNotFound = "MSG_NOT_FOUND";
    public const string PublishingFailed = "PUBLISH_FAILED";
    public const string SerializationError = "SERIALIZATION_ERROR";
    public const string DeserializationError = "DESERIALIZATION_ERROR";
    public const string DatabaseError = "DATABASE_ERROR";
    public const string InvalidMessage = "INVALID_MESSAGE";
    public const string InvalidConfiguration = "INVALID_CONFIG";
    public const string OperationTimeout = "OPERATION_TIMEOUT";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string DeadLetterQueueError = "DLQ_ERROR";
}

/// <summary>
/// HTTP header names for outbox integration
/// </summary>
public static class HttpHeaders
{
    public const string CorrelationId = "X-Correlation-Id";
    public const string CausationId = "X-Causation-Id";
    public const string IdempotencyKey = "X-Idempotency-Key";
    public const string RequestId = "X-Request-Id";
}
