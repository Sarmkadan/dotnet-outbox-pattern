# StructuredLoggingExtensions

Provides strongly-typed, structured logging extensions for operations related to the Outbox Pattern in .NET applications. These extensions wrap common outbox scenarios (publishing, retries, dead-letter moves, health checks, and performance metrics) with consistent, machine-readable log events that include context such as message identifiers, retry counts, and timing information.

## API

### `public static void LogOutboxOperation(ILogger logger, string operationName, string messageId, int? retryCount = null, string? correlationId = null, Exception? exception = null)`

Logs a generic outbox operation with the specified name. This is a low-level entry point used by other methods in this class to emit consistent structured logs.

- **Parameters**
  - `logger`: The `ILogger` instance.
  - `operationName`: A short identifier for the operation (e.g., "Publish", "MoveToDeadLetter").
  - `messageId`: The unique identifier of the message being processed.
  - `retryCount`: Optional retry attempt number.
  - `correlationId`: Optional correlation identifier for tracing across services.
  - `exception`: Optional exception that occurred during the operation.
- **Return value**: None.
- **Exceptions**: Does not throw.

---

### `public static void LogMessagePublishing(ILogger logger, string messageId, string messageType, string destination, int? retryCount = null, string? correlationId = null)`

Logs the start of an outbox message publishing operation.

- **Parameters**
  - `logger`: The `ILogger` instance.
  - `messageId`: The unique identifier of the message being published.
  - `messageType`: The type or category of the message.
  - `destination`: The target endpoint or queue where the message will be sent.
  - `retryCount`: Optional retry attempt number.
  - `correlationId`: Optional correlation identifier for tracing.
- **Return value**: None.
- **Exceptions**: Does not throw.

---

### `public static void LogMessagePublishSuccess(ILogger logger, string messageId, string messageType, string destination, TimeSpan duration, int? retryCount = null, string? correlationId = null)`

Logs a successful message publishing operation.

- **Parameters**
  - `logger`: The `ILogger` instance.
  - `messageId`: The unique identifier of the successfully published message.
  - `messageType`: The type or category of the message.
  - `destination`: The target endpoint or queue where the message was sent.
  - `duration`: The time taken to publish the message.
  - `retryCount`: Optional retry attempt number.
  - `correlationId`: Optional correlation identifier for tracing.
- **Return value**: None.
- **Exceptions**: Does not throw.

---
### `public static void LogMessagePublishFailure(ILogger logger, string messageId, string messageType, string destination, Exception exception, int? retryCount = null, string? correlationId = null)`

Logs a failed message publishing operation.

- **Parameters**
  - `logger`: The `ILogger` instance.
  - `messageId`: The unique identifier of the message that failed to publish.
  - `messageType`: The type or category of the message.
  - `destination`: The target endpoint or queue where the publishing failed.
  - `exception`: The exception that caused the failure.
  - `retryCount`: Optional retry attempt number.
  - `correlationId`: Optional correlation identifier for tracing.
- **Return value**: None.
- **Exceptions**: Does not throw.

---
### `public static void LogMessageMovedToDeadLetter(ILogger logger, string messageId, string messageType, string reason, int? retryCount = null, string? correlationId = null)`

Logs the movement of a message to the dead-letter queue.

- **Parameters**
  - `logger`: The `ILogger` instance.
  - `messageId`: The unique identifier of the message moved to dead-letter.
  - `messageType`: The type or category of the message.
  - `reason`: The reason for moving the message to dead-letter.
  - `retryCount`: Optional retry attempt number.
  - `correlationId`: Optional correlation identifier for tracing.
- **Return value**: None.
- **Exceptions**: Does not throw.

---
### `public static void LogMessageRetry(ILogger logger, string messageId, string messageType, int retryCount, string reason, string? correlationId = null)`

Logs a retry attempt for a message.

- **Parameters**
  - `logger`: The `ILogger` instance.
  - `messageId`: The unique identifier of the message being retried.
  - `messageType`: The type or category of the message.
  - `retryCount`: The current retry attempt number.
  - `reason`: The reason for the retry.
  - `correlationId`: Optional correlation identifier for tracing.
- **Return value**: None.
- **Exceptions**: Does not throw.

---
### `public static void LogHealthStatus(ILogger logger, bool isHealthy, string component, string? detail = null)`

Logs the health status of an outbox-related component.

- **Parameters**
  - `logger`: The `ILogger` instance.
  - `isHealthy`: Whether the component is healthy.
  - `component`: The name or identifier of the component being checked.
  - `detail`: Optional additional detail about the health status.
- **Return value**: None.
- **Exceptions**: Does not throw.

---
### `public static void LogPerformanceMetric(ILogger logger, string metricName, double value, string? unit = null, string? context = null)`

Logs a performance metric related to outbox operations.

- **Parameters**
  - `logger`: The `ILogger` instance.
  - `metricName`: The name of the metric being recorded.
  - `value`: The measured value.
  - `unit`: Optional unit of measurement (e.g., "ms", "ops/s").
  - `context`: Optional context describing the source or scope of the metric.
- **Return value**: None.
- **Exceptions**: Does not throw.

## Usage

### Example 1: Publishing a message with retry tracking
