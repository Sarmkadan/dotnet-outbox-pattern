# OutboxMessage

The `OutboxMessage` class represents a persisted event message stored in the outbox pattern implementation. It tracks the state of event processing, delivery attempts, and metadata necessary for reliable event publishing. This type is used to persist events in a durable storage before they are published to a message broker, ensuring at-least-once delivery semantics.

## API

### `Id`
A unique identifier for the outbox message. This value is immutable and set at creation time.

### `IdempotencyKey`
A string used to ensure idempotent processing of the message. Messages with the same `IdempotencyKey` should not be processed more than once.

### `AggregateId`
The identifier of the aggregate root that originated the event. Used for correlation and debugging.

### `AggregateType`
The type name of the aggregate root that originated the event. Provides context about the source of the event.

### `EventType`
An enum value representing the type of the event. Used to determine how to deserialize and process the event data.

### `EventData`
A JSON-serialized representation of the event payload. Contains the actual event data to be published.

### `EventTypeName`
A string representation of the event type, typically used for routing or filtering by consumers.

### `Topic`
The target topic or queue where the event should be published. Determines the destination for the message.

### `State`
An enum value indicating the current processing state of the message (e.g., `Pending`, `Processing`, `Failed`, `Published`).

### `PublishAttempts`
The number of times publishing has been attempted for this message. Used to enforce retry limits.

### `MaxPublishAttempts`
The maximum number of publishing attempts allowed before the message is considered permanently failed.

### `CreatedAt`
The timestamp when the message was first created and stored in the outbox.

### `LastProcessedAt`
The timestamp of the last processing attempt. Updated on each attempt, whether successful or not.

### `PublishedAt`
The timestamp when the message was successfully published. `null` if not yet published.

### `ScheduledFor`
The optional timestamp indicating when the message should be published. Used for delayed delivery.

### `ErrorMessage`
A string containing the error message if the last publishing attempt failed. `null` if no error occurred.

### `ErrorStackTrace`
The stack trace associated with the last publishing failure. `null` if no error occurred.

### `PartitionKey`
An optional string used to determine the partition for the message in the message broker. Useful for ordering guarantees.

### `DeliveryGuarantee`
An enum value specifying the delivery guarantee required for this message (e.g., `AtLeastOnce`, `ExactlyOnce`).

### `CorrelationId`
An optional identifier used to correlate this message with other messages or operations in a distributed system.

## Usage

### Storing an event in the outbox
