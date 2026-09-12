# Outbox exceptions

`Exceptions/OutboxExceptions.cs` defines the outbox-specific exception hierarchy in the
`DotnetOutboxPattern.Exceptions` namespace. All specialized exceptions are sealed and
derive from `OutboxException`.

This document distinguishes the source file's stated purpose for a type from the places
where the repository currently throws it. Types described as having no throw site are
still public and can be constructed by consumers.

## Common base: `OutboxException`

`OutboxException` derives from `Exception` and is the only non-sealed type in this file.
It represents an outbox error that does not fit a more specific category and is the base
of every other exception below.

The application currently throws it directly from `OutboxService` when publishing an
event, retrieving a message or statistics, retrying a message, querying messages, or
archiving messages fails.

It carries:

- `ErrorCode` (`string`), supplied by the constructor and defaulting to `OUTBOX_ERROR`.
- `ResourceId` (`string?`), supplied by the constructor and defaulting to `null`.
- The inherited `Message`.
- The inherited `InnerException` only when the overload accepting an exception is used.

Constructors accept `(message, errorCode, resourceId)` or
`(message, innerException, errorCode, resourceId)`.

## Specialized exceptions

### `MessagePublishingException`

Represents failure to publish a message to the message broker after retries are
exhausted. There is no production throw site in the repository.

It carries `MessageId` (`Guid`) and `AttemptNumber` (`int`, default `1`). Its inherited
`ErrorCode` is `MESSAGE_PUBLISH_FAILED`, and `ResourceId` is the string form of
`MessageId`. If the optional `innerException` is `null`, the constructor creates a new
`Exception` containing the same message, so `InnerException` is always non-null.

### `DeadLetterException`

Represents a failed dead-letter operation. `DeadLetterRepository` throws it when its
add, lookup, query, update, delete, or count operations fail. `DeadLetterService` throws
it when dead-letter operations fail and when review or requeue cannot find a dead
letter.

It carries `MessageId` (`Guid`). Its inherited `ErrorCode` is `DEAD_LETTER_ERROR`, and
`ResourceId` is the string form of `MessageId`. Several operations use `Guid.Empty`
when no message ID is available. If `innerException` is `null`, the constructor creates
a new `Exception` containing the same message.

### `InvalidMessageException`

Represents a message that fails validation before processing. `OutboxRepository.AddAsync`
throws it when an `ArgumentException` occurs while adding a message.

It adds no properties. Its inherited `ErrorCode` is `INVALID_MESSAGE`, `ResourceId` is
`null`, and a null optional `innerException` is replaced with a new `Exception`
containing the same message.

### `OutboxRepositoryException`

Represents failure while interacting with outbox storage. `OutboxRepository` throws it
for persistence and query failures, including add, retrieval, update, delete, count,
statistics, archive, and batch-claim operations. It is also used for update concurrency
conflicts.

It carries `Operation` (`string`), which the repository populates with the relevant
method name. Its inherited `ErrorCode` is `REPOSITORY_ERROR` and `ResourceId` is `null`.
A null optional `innerException` is replaced with a new `Exception` containing the same
message.

### `MessageLockingException`

Represents failure to acquire a message lock for processing. There is no production
throw site in the repository.

It carries `MessageId` (`Guid`). Its inherited `ErrorCode` is
`MESSAGE_LOCKING_FAILED`, and `ResourceId` is the string form of `MessageId`. A null
optional `innerException` is replaced with a new `Exception` containing the same
message.

### `OutboxMessageNotFoundException`

Represents failure to find an outbox message by ID. `OutboxService.RetryFailedMessageAsync`
throws it when the requested message does not exist.

It carries `MessageId` (`Guid`). Its message is generated as
`Outbox message with ID '<id>' was not found`, its inherited `ErrorCode` is
`MESSAGE_NOT_FOUND`, and `ResourceId` is the string form of `MessageId`. It does not set
an inner exception.

### `SerializationException`

Represents serialization or deserialization failure. `SerializationHelper` throws it
when serialization fails, JSON is invalid, deserialization fails, or generic
deserialization returns `null`.

It carries `TargetType` (`string?`, default `null`). Its inherited `ErrorCode` is
`SERIALIZATION_ERROR` and `ResourceId` is `null`. A null optional `innerException` is
replaced with a new `Exception` containing the same message.

### `ProcessingInProgressException`

Represents an attempt to process a message already being handled by another process or
thread. There is no production throw site in the repository.

It adds no properties. Its inherited `ErrorCode` is `PROCESSING_IN_PROGRESS`,
`ResourceId` is `null`, and it does not set an inner exception.

### `InvalidConfigurationException`

Represents invalid values or missing required settings in outbox configuration. There
is no production throw site in the repository.

It carries `ConfigurationProperty` (`string?`, default `null`). Its inherited
`ErrorCode` is `INVALID_CONFIGURATION`, `ResourceId` is `null`, and it does not set an
inner exception.

### `ValidationException`

Represents one or more validation errors. There is no production throw site in the
repository.

It carries `Errors` (`IReadOnlyDictionary<string, string[]>`). One constructor stores a
provided dictionary directly; the other creates a one-entry dictionary from a property
name and one error string. Its inherited `ErrorCode` is `VALIDATION_ERROR`,
`ResourceId` is `null`, and it does not set an inner exception.

### `MessageProcessingLockedException`

Represents an attempt to process a message currently locked by another process. There
is no production throw site in the repository.

It carries `MessageId` (`Guid`). Its message is generated as
`Message <id> is currently locked and cannot be processed`, its inherited `ErrorCode`
is `MESSAGE_LOCKED`, and `ResourceId` is the string form of `MessageId`. It does not set
an inner exception.

### `ServiceUnavailableException`

Represents an unavailable or unreachable dependency such as a message broker or
database. There is no production throw site in the repository.

It carries `ServiceName` (`string`). Its inherited `ErrorCode` is
`SERVICE_UNAVAILABLE`, and `ResourceId` is the service name. Although its constructor
accepts an optional `innerException`, the implementation does not pass that value to
the base class, so `InnerException` remains `null`.

### `ProcessingTimeoutException`

Represents a timeout during processing. There is no production throw site in the
repository.

It carries `Timeout` (`TimeSpan`). Its inherited `ErrorCode` is `PROCESSING_TIMEOUT`
and `ResourceId` is `null`. Although its constructor accepts an optional
`innerException`, the implementation does not pass that value to the base class, so
`InnerException` remains `null`.
