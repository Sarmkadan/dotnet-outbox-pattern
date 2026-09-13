# Domain constants

`Domain/Constants.cs` defines compile-time constants in the `DotnetOutboxPattern.Domain` namespace. The constants are grouped into six static classes according to their role.

## `OutboxConstants`

General defaults and limits used by the outbox implementation.

| Constant | Value | Purpose |
| --- | ---: | --- |
| `DefaultTopic` | `"domain-events"` | Default topic name for domain events. |
| `DeadLetterTopic` | `"dead-letters"` | Topic name for dead-letter messages. |
| `MaxAggregateIdLength` | `256` | Maximum length for aggregate IDs. |
| `MaxTopicLength` | `128` | Maximum length for topic names. |
| `MaxEventTypeNameLength` | `256` | Maximum length for event type names. |
| `MaxErrorMessageLength` | `2000` | Maximum length for error messages. |
| `MaxCorrelationIdLength` | `256` | Maximum length for correlation IDs. |
| `DefaultMaxPublishAttempts` | `5` | Default maximum number of publishing attempts. |
| `DefaultBatchSize` | `100` | Default batch size for message processing. |
| `DefaultDelayBetweenBatches` | `5000` | Default delay between processing batches, in milliseconds. |
| `DefaultLockDurationSeconds` | `300` | Default processing lock duration, in seconds. |
| `DefaultPublishTimeoutSeconds` | `30` | Default publish timeout, in seconds. |
| `DefaultCheckExpiredLocksInterval` | `60000` | Default interval for checking expired locks, in milliseconds. |
| `MinBatchSize` | `1` | Minimum batch size accepted by validation. |
| `MaxBatchSize` | `10000` | Maximum batch size accepted by validation. |
| `DefaultDegreeOfParallelism` | `4` | Default degree of parallelism for processing. |
| `DefaultArchiveDaysOld` | `30` | Age threshold for archiving old messages, in days. |

## `StandardTopics`

Standard topic/channel names for categories of outbox messages.

| Constant | Value | Purpose |
| --- | --- | --- |
| `Orders` | `"orders.events"` | Topic for order-related domain events. |
| `Customers` | `"customers.events"` | Topic for customer-related domain events. |
| `Payments` | `"payments.events"` | Topic for payment-related domain events. |
| `Inventory` | `"inventory.events"` | Topic for inventory-related domain events. |
| `Notifications` | `"notifications.events"` | Topic for notification events. |
| `System` | `"system.events"` | Topic for system and infrastructure events. |

## `LogProperties`

Property names used for structured logging, correlation, and tracing.

| Constant | Value | Purpose |
| --- | --- | --- |
| `MessageId` | `"MessageId"` | Message identifier property name. |
| `CorrelationId` | `"CorrelationId"` | Correlation identifier property name. |
| `CausationId` | `"CausationId"` | Causation identifier property name. |
| `AggregateId` | `"AggregateId"` | Aggregate identifier property name. |
| `Topic` | `"Topic"` | Topic property name. |
| `State` | `"State"` | Message state property name. |
| `Attempts` | `"Attempts"` | Publishing-attempt count property name. |
| `Duration` | `"Duration"` | Duration property name. |
| `Success` | `"Success"` | Success-status property name. |

## `ErrorCodes`

Codes identifying different failure scenarios.

| Constant | Value | Purpose |
| --- | --- | --- |
| `MessageNotFound` | `"MSG_NOT_FOUND"` | Identifies a message-not-found failure. |
| `PublishingFailed` | `"PUBLISH_FAILED"` | Identifies a publishing failure. |
| `SerializationError` | `"SERIALIZATION_ERROR"` | Identifies a serialization failure. |
| `DeserializationError` | `"DESERIALIZATION_ERROR"` | Identifies a deserialization failure. |
| `DatabaseError` | `"DATABASE_ERROR"` | Identifies a database failure. |
| `InvalidMessage` | `"INVALID_MESSAGE"` | Identifies an invalid message. |
| `InvalidConfiguration` | `"INVALID_CONFIG"` | Identifies invalid configuration. |
| `OperationTimeout` | `"OPERATION_TIMEOUT"` | Identifies an operation timeout. |
| `ConcurrencyConflict` | `"CONCURRENCY_CONFLICT"` | Identifies a concurrency conflict. |
| `DeadLetterQueueError` | `"DLQ_ERROR"` | Identifies a dead-letter queue failure. |

## `HttpHeaders`

HTTP header names used for outbox integration.

| Constant | Value | Purpose |
| --- | --- | --- |
| `CorrelationId` | `"X-Correlation-Id"` | Carries a correlation identifier. |
| `CausationId` | `"X-Causation-Id"` | Carries a causation identifier. |
| `IdempotencyKey` | `"X-Idempotency-Key"` | Carries an idempotency key. |
| `RequestId` | `"X-Request-Id"` | Carries a request identifier. |

## `DatabaseSchema`

Database schema names for the outbox table and its columns.

| Constant | Value | Purpose |
| --- | --- | --- |
| `OutboxMessages` | `"OutboxMessages"` | Outbox messages table name. |
| `Id` | `"Id"` | Message ID column name. |
| `State` | `"State"` | Message state column name. |
| `IsLocked` | `"IsLocked"` | Message lock flag column name. |
| `LockExpiresAt` | `"LockExpiresAt"` | Lock expiration column name. |
| `Priority` | `"Priority"` | Message priority column name. |
| `CreatedAt` | `"CreatedAt"` | Message creation timestamp column name. |
| `ScheduledFor` | `"ScheduledFor"` | Message scheduled timestamp column name. |
| `LastProcessedAt` | `"LastProcessedAt"` | Message last-processed timestamp column name. |
| `PartitionKey` | `"PartitionKey"` | Message partition key column name. |
| `Topic` | `"Topic"` | Message topic column name. |
