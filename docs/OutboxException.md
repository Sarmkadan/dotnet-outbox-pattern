# OutboxException

The `OutboxException` serves as the base abstraction for all errors occurring within the outbox pattern implementation, encapsulating contextual data such as the failed message identifier, operation type, and retry attempts to facilitate robust error handling, logging, and dead-letter processing strategies.

## API

### `ErrorCode`
*   **Type**: `public string`
*   **Purpose**: Provides a machine-readable identifier representing the specific category of the failure (e.g., `SERIALIZATION_FAILURE`, `LOCK_CONFLICT`).
*   **Usage**: Used by monitoring systems and switch-case logic to determine recovery strategies without parsing exception messages.

### `ResourceId`
*   **Type**: `public string?`
*   **Purpose**: Optionally identifies the specific resource entity involved in the operation that triggered the exception, if applicable to the error context.
*   **Usage**: Useful for auditing or correlating the exception with specific domain entities when the error is not strictly tied to a message ID.

### `OutboxException` (Constructor)
*   **Signature**: `public OutboxException()`
*   **Purpose**: Initializes a new instance of the `OutboxException` class with default values.
*   **Parameters**: None.
*   **Returns**: A new `OutboxException` instance.

### `OutboxException` (Constructor with Message)
*   **Signature**: `public OutboxException(string message)`
*   **Purpose**: Initializes a new instance with a specified error message.
*   **Parameters**:
    *   `message`: The human-readable description of the error.
*   **Returns**: A new `OutboxException` instance.

### `MessageId` (Base)
*   **Type**: `public Guid`
*   **Purpose**: Stores the unique identifier of the outbox message associated with the failure.
*   **Usage**: Critical for tracing specific message lifecycles and implementing idempotency checks during retries.

### `AttemptNumber`
*   **Type**: `public int`
*   **Purpose**: Indicates the current retry attempt count when the exception was thrown.
*   **Usage**: Used to enforce retry limits or implement exponential backoff strategies based on the number of previous failures.

### `MessagePublishingException`
*   **Type**: `public MessagePublishingException`
*   **Purpose**: A specialized derivative indicating failure during the transmission of a message to the external broker.
*   **Members**:
    *   `MessageId`: `public Guid` – The ID of the message that failed to publish.

### `DeadLetterException`
*   **Type**: `public DeadLetterException`
*   **Purpose**: Signals that a message has exceeded the maximum retry threshold and has been moved to the dead-letter queue.
*   **Members**:
    *   `MessageId`: `public Guid` – The ID of the message being dead-lettered.
    *   `Operation`: `public string` – The specific operation that consistently failed.

### `InvalidMessageException`
*   **Type**: `public InvalidMessageException`
*   **Purpose**: Thrown when a message payload fails validation rules before processing or publishing.
*   **Members**:
    *   `TargetType`: `public string?` – The expected .NET type of the message payload that failed validation.

### `OutboxRepositoryException`
*   **Type**: `public OutboxRepositoryException`
*   **Purpose**: Indicates infrastructure-level failures within the persistence layer (e.g., database connectivity, constraint violations).
*   **Members**:
    *   `MessageId`: `public Guid` – The ID of the message involved in the repository operation.

### `MessageLockingException`
*   **Type**: `public MessageLockingException`
*   **Purpose**: Thrown when a message cannot be locked for processing, typically due to concurrency conflicts or timeout expiration.
*   **Members**:
    *   `MessageId`: `public Guid` – The ID of the message that could not be locked.

### `OutboxMessageNotFoundException`
*   **Type**: `public OutboxMessageNotFoundException`
*   **Purpose**: Indicates that a requested message ID does not exist in the outbox storage.
*   **Usage**: Often used to handle race conditions where a message was processed and deleted between lookup and action.

### `SerializationException`
*   **Type**: `public SerializationException`
*   **Purpose**: Signals failure during the serialization or deserialization of the message payload.
*   **Usage**: Triggers alerts for schema mismatches or corrupted data in the outbox table.

### `ProcessingInProgressException`
*   **Type**: `public ProcessingInProgressException`
*   **Purpose**: Thrown when an attempt is made to process a message that is already marked as being processed by another worker.
*   **Usage**: Prevents duplicate processing in distributed environments.

### `ConfigurationProperty`
*   **Type**: `public string?`
*   **Purpose**: Identifies the specific configuration setting that caused a validation or initialization error.
*   **Usage**: Assists developers in quickly locating misconfigured settings in `appsettings.json` or environment variables.

## Usage

### Example 1: Handling Specific Publishing Failures
This example demonstrates catching a specialized `MessagePublishingException` to implement custom retry logic based on the attempt number.

```csharp
try
{
    await outboxService.PublishAsync(message);
}
catch (MessagePublishingException ex)
{
    if (ex.AttemptNumber >= 5)
    {
        // Log critical failure and move to dead letter manually if needed
        logger.LogError(ex, "Message {MessageId} failed after {Attempts} attempts", ex.MessageId, ex.AttemptNumber);
        throw new DeadLetterException(ex.MessageId, "Publish", "Max retries exceeded");
    }

    // Log warning and allow standard retry policy to handle it
    logger.LogWarning(ex, "Transient publish failure for {MessageId} (Attempt {Attempt})", ex.MessageId, ex.AttemptNumber);
    throw; 
}
```

### Example 2: Diagnosing Serialization and Configuration Issues
This example shows how to inspect `SerializationException` and `InvalidMessageException` to identify data integrity or configuration problems.

```csharp
try
{
    var message = await outboxRepository.GetByIdAsync(messageId);
    var payload = serializer.Deserialize(message.Content, message.TargetType);
}
catch (SerializationException ex)
{
    logger.Critical(ex, "Failed to deserialize message {MessageId}. Payload may be corrupted.", ex.MessageId);
    // Trigger alert for data repair team
}
catch (InvalidMessageException ex)
{
    logger.Warning("Message {MessageId} failed validation for type {Type}", ex.MessageId, ex.TargetType);
    // Optionally discard message or notify sender
}
catch (OutboxRepositoryException ex)
{
    logger.Error(ex, "Database error accessing message {MessageId}", ex.MessageId);
    // Trigger infrastructure alert
}
```

## Notes

*   **Inheritance Hierarchy**: While `OutboxException` is the base type, specific scenarios (locking, serialization, publishing) should always be caught using their derived types (`MessageLockingException`, `SerializationException`, etc.) to access context-specific properties like `TargetType` or `ConfigurationProperty`. Catching only the base type will result in loss of this granular data unless explicit casting is performed.
*   **Thread Safety**: Instances of `OutboxException` and its derivatives are immutable once constructed; properties such as `MessageId`, `AttemptNumber`, and `ErrorCode` are set at creation time. This makes exception instances safe to pass across thread boundaries, log asynchronously, or store in concurrent collections without synchronization overhead.
*   **Guid Consistency**: The `MessageId` property is guaranteed to be populated in all message-centric derived exceptions (`MessagePublishingException`, `MessageLockingException`, etc.). However, in generic `OutboxException` instances thrown during infrastructure bootstrapping, reliance on `MessageId` should be avoided unless verified.
*   **Nullability**: Properties `ResourceId`, `TargetType`, and `ConfigurationProperty` are nullable (`string?`). Consumers must perform null checks before accessing these members, as they are only populated when the error context specifically relates to a resource, a payload type, or a configuration key.
