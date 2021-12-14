# IdempotencyKeyGenerator

Utility class for generating and managing idempotency keys in distributed systems. It provides static methods to create deterministic keys for common operations and helper classes to enforce idempotency at the application level.

## API

### Static Methods

#### `public static string ForEntityCreation(object entity)`
Generates a deterministic idempotency key for entity creation operations. The key is derived from the entity type and its unique identifier.
- **Parameters**: `entity` – The entity being created.
- **Returns**: A string key formatted as `{EntityType}:{EntityId}`.
- **Throws**: `ArgumentNullException` if `entity` is null.

#### `public static string ForStateTransition(string stateName, object entity)`
Generates a deterministic idempotency key for state transition operations. The key combines the state name and the entity identifier.
- **Parameters**:
  - `stateName` – The name of the target state.
  - `entity` – The entity undergoing the transition.
- **Returns**: A string key formatted as `{EntityType}:{EntityId}:{StateName}`.
- **Throws**: `ArgumentNullException` if `stateName` or `entity` is null.

#### `public static string ForWebhookAttempt(string webhookUrl, object payload)`
Generates a deterministic idempotency key for webhook delivery attempts. The key includes the target URL and a hash of the payload.
- **Parameters**:
  - `webhookUrl` – The destination URL for the webhook.
  - `payload` – The payload being sent.
- **Returns**: A string key formatted as `webhook:{UrlHash}:{PayloadHash}`.
- **Throws**: `ArgumentNullException` if `webhookUrl` or `payload` is null.

#### `public static string ForTimestampedEvent(DateTime timestamp, string eventType)`
Generates a deterministic idempotency key for timestamped events. The key combines the event type and the timestamp in UTC.
- **Parameters**:
  - `timestamp` – The event occurrence time.
  - `eventType` – The type of event.
- **Returns**: A string key formatted as `{EventType}:{Timestamp:yyyyMMddHHmmss}`.
- **Throws**: `ArgumentNullException` if `eventType` is null.

#### `public static string ForExternalApiCall(string apiName, object request)`
Generates a deterministic idempotency key for external API calls. The key includes the API name and a hash of the request.
- **Parameters**:
  - `apiName` – The name of the external API.
  - `request` – The request payload.
- **Returns**: A string key formatted as `api:{ApiName}:{RequestHash}`.
- **Throws**: `ArgumentNullException` if `apiName` or `request` is null.

### `IdempotentOrderEventHandler`

Handler class that enforces idempotency for order-related events. Uses a `MessageDeduplicator` to track processed messages.

#### `public async Task HandleOrderCreatedAsync(OrderCreatedEvent orderEvent)`
Processes an order creation event idempotently. Checks for duplicates before proceeding.
- **Parameters**: `orderEvent` – The order creation event.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if `orderEvent` is null.

#### `public async Task<bool> TryProcessIdempotentlyAsync(string key, Func<Task> action)`
Attempts to process an operation idempotently using the provided key. Executes the action only if the key has not been processed before.
- **Parameters**:
  - `key` – The idempotency key.
  - `action` – The operation to execute.
- **Returns**: `true` if the action was executed; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `key` or `action` is null.

### `MessageDeduplicator`

Helper class for tracking processed messages and preventing duplicate processing.

#### `public bool IsDuplicate(string key)`
Checks whether a message with the given key has already been processed.
- **Parameters**: `key` – The idempotency key.
- **Returns**: `true` if the key exists in the deduplication store; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `key` is null.

#### `public void MarkAsProcessed(string key)`
Marks a message as processed by storing its idempotency key.
- **Parameters**: `key` – The idempotency key.
- **Throws**: `ArgumentNullException` if `key` is null.

#### `public void CleanupExpired(TimeSpan retentionPeriod)`
Removes expired entries from the deduplication store based on the provided retention period.
- **Parameters**: `retentionPeriod` – The maximum age of entries to retain.
- **Throws**: `ArgumentNullException` if `retentionPeriod` is null.

### `IdempotentPublisher`

Helper class for publishing messages idempotently.

#### `public string Data`
Gets or sets the payload data associated with the publisher.

#### `public async Task<Guid> PublishOrderCreatedAsync(OrderCreatedEvent orderEvent)`
Publishes an order creation event idempotently. Generates a key using `ForEntityCreation` and ensures the event is processed only once.
- **Parameters**: `orderEvent` – The order creation event.
- **Returns**: A `Task<Guid>` representing the asynchronous operation, yielding the generated event ID.
- **Throws**: `ArgumentNullException` if `orderEvent` is null.

#### `public async Task PublishOrderFlowAsync(OrderFlowEvent orderFlowEvent)`
Publishes an order flow event idempotently. Ensures the event is processed only once using a key derived from the event.
- **Parameters**: `orderFlowEvent` – The order flow event.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if `orderFlowEvent` is null.

### Entry Point

#### `public static async Task Main(string[] args)`
Demonstrates usage of the `IdempotentPublisher` and `MessageDeduplicator` in a console application.
- **Parameters**: `args` – Command-line arguments.
- **Returns**: A `Task` representing the asynchronous operation.

## Usage

### Example 1: Publishing an Order Creation Event Idempotently
