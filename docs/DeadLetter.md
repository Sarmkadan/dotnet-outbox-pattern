# DeadLetter

The `DeadLetter` type represents a message that could not be successfully processed from an outbox and has been moved to a dead-letter queue (DLQ) for further inspection or manual reprocessing. It captures the original event data, processing context, and failure details to aid in diagnostics and recovery.

## API

### `Id`
- **Purpose**: Unique identifier for the dead-letter entry.
- **Type**: `Guid`
- **Notes**: Assigned at creation; immutable.

### `OutboxMessageId`
- **Purpose**: References the original outbox message that failed processing.
- **Type**: `Guid`
- **Notes**: Links to the outbox message that triggered this dead-letter.

### `IdempotencyKey`
- **Purpose**: Ensures idempotent processing of the dead-letter.
- **Type**: `string`
- **Notes**: Used to prevent duplicate reprocessing attempts.

### `AggregateId`
- **Purpose**: Identifies the aggregate root associated with the failed event.
- **Type**: `string`
- **Notes**: Part of the event metadata; may be `null` if not applicable.

### `AggregateType`
- **Purpose**: Describes the type of the aggregate root.
- **Type**: `string`
- **Notes**: Used for routing or type-specific handling; may be `null`.

### `EventType`
- **Purpose**: Enumeration indicating the type of the failed event.
- **Type**: `EventType`
- **Notes**: Defines the category or schema of the event.

### `EventData`
- **Purpose**: Serialized payload of the failed event.
- **Type**: `string`
- **Notes**: Contains the raw event data as a string; may be `null` if not captured.

### `EventTypeName`
- **Purpose**: Human-readable name of the event type.
- **Type**: `string`
- **Notes**: Useful for logging or UI display.

### `Topic`
- **Purpose**: Identifies the message topic or channel the event was published to.
- **Type**: `string`
- **Notes**: May be `null` if not applicable.

### `PartitionKey`
- **Purpose**: Optional partition key used for message routing.
- **Type**: `string?`
- **Notes**: Used in systems that support partitioning; may be `null`.

### `TotalAttempts`
- **Purpose**: Counts the number of processing attempts made for this event.
- **Type**: `int`
- **Notes**: Includes all prior attempts, including those before DLQ routing.

### `ErrorMessage`
- **Purpose**: Describes the failure reason.
- **Type**: `string`
- **Notes**: Captures the exception message or processing error.

### `ErrorStackTrace`
- **Purpose**: Provides the stack trace of the failure.
- **Type**: `string?`
- **Notes**: May be `null` if not captured or not applicable.

### `OriginalCreatedAt`
- **Purpose**: Timestamp when the original outbox message was created.
- **Type**: `DateTime`
- **Notes**: Immutable; used for aging analysis.

### `MovedToDlqAt`
- **Purpose**: Timestamp when the message was moved to the DLQ.
- **Type**: `DateTime`
- **Notes**: Set at the time of DLQ insertion.

### `LastAttemptAt`
- **Purpose**: Timestamp of the most recent processing attempt.
- **Type**: `DateTime?`
- **Notes**: May be `null` if no attempts were recorded.

### `CorrelationId`
- **Purpose**: Tracks the correlation context across services.
- **Type**: `string?`
- **Notes**: Used for tracing; may be `null`.

### `CausationId`
- **Purpose**: Identifies the immediate cause of the event.
- **Type**: `string?`
- **Notes**: Used for causality tracking; may be `null`.

### `Metadata`
- **Purpose**: Additional contextual information as a JSON string.
- **Type**: `string?`
- **Notes**: May be `null` if no metadata was provided.

### `IsReviewed`
- **Purpose**: Indicates whether the dead-letter has been reviewed.
- **Type**: `bool`
- **Notes**: Defaults to `false`; updated manually or via tooling.

## Usage

### Example 1: Creating and Saving a DeadLetter
