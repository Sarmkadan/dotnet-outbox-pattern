# ErrorResponse

The `ErrorResponse` type serves as a standardized data transfer object (DTO) within the `dotnet-outbox-pattern` project, designed to encapsulate detailed diagnostic information regarding failed outbox message processing or publishing attempts. It aggregates temporal data, identification keys, aggregate context, and specific error payloads to facilitate robust error handling, retry logic analysis, and distributed tracing across the system.

## API

The following members constitute the public interface of the `ErrorResponse` type.

### Properties

#### `Message`
*   **Type:** `string`
*   **Purpose:** Contains a human-readable description of the error or failure state.
*   **Returns:** The error message text.
*   **Throws:** Never.

#### `Code`
*   **Type:** `string`
*   **Purpose:** Provides a machine-readable error code used for programmatic error classification and handling.
*   **Returns:** The error code string.
*   **Throws:** Never.

#### `Timestamp`
*   **Type:** `DateTime`
*   **Purpose:** Records the exact date and time when the error response was generated or the failure occurred.
*   **Returns:** The timestamp of the event.
*   **Throws:** Never.

#### `TraceId`
*   **Type:** `string?`
*   **Purpose:** Holds the distributed tracing identifier associated with the request flow, allowing correlation across services. This value may be null if tracing is not enabled or available.
*   **Returns:** The trace identifier or `null`.
*   **Throws:** Never.

#### `Id`
*   **Type:** `Guid`
*   **Purpose:** Represents the unique identifier for this specific error record or outbox entry.
*   **Returns:** The unique GUID.
*   **Throws:** Never.

#### `IdempotencyKey`
*   **Type:** `string`
*   **Purpose:** Stores the key used to ensure idempotency for the original operation, preventing duplicate processing.
*   **Returns:** The idempotency key string.
*   **Throws:** Never.

#### `AggregateId`
*   **Type:** `string`
*   **Purpose:** Identifies the specific aggregate instance involved in the failed operation.
*   **Returns:** The aggregate identifier.
*   **Throws:** Never.

#### `AggregateType`
*   **Type:** `string`
*   **Purpose:** Specifies the fully qualified name or type identifier of the aggregate associated with the event.
*   **Returns:** The aggregate type name.
*   **Throws:** Never.

#### `EventType`
*   **Type:** `string`
*   **Purpose:** Indicates the specific type of domain event that failed to publish or process.
*   **Returns:** The event type name.
*   **Throws:** Never.

#### `Topic`
*   **Type:** `string`
*   **Purpose:** Defines the message broker topic or channel where the event was intended to be published.
*   **Returns:** The topic name.
*   **Throws:** Never.

#### `State`
*   **Type:** `string`
*   **Purpose:** Represents the current lifecycle state of the outbox message (e.g., "Failed", "Pending", "Processed").
*   **Returns:** The state string.
*   **Throws:** Never.

#### `PublishAttempts`
*   **Type:** `int`
*   **Purpose:** Tracks the number of times the system has attempted to publish this specific message.
*   **Returns:** The current count of attempts.
*   **Throws:** Never.

#### `MaxPublishAttempts`
*   **Type:** `int`
*   **Purpose:** Defines the configured threshold for maximum retry attempts before the message is considered permanently failed.
*   **Returns:** The maximum allowed attempts.
*   **Throws:** Never.

#### `CreatedAt`
*   **Type:** `DateTime`
*   **Purpose:** Records the date and time when the outbox message was originally created.
*   **Returns:** The creation timestamp.
*   **Throws:** Never.

#### `PublishedAt`
*   **Type:** `DateTime?`
*   **Purpose:** Stores the timestamp of the last successful publication. This value is null if the message has never been successfully published.
*   **Returns:** The publication timestamp or `null`.
*   **Throws:** Never.

#### `ErrorMessage`
*   **Type:** `string?`
*   **Purpose:** Contains the specific exception message or stack trace details resulting from the failure. This value may be null if no specific exception was caught.
*   **Returns:** The detailed error message or `null`.
*   **Throws:** Never.

#### `PartitionKey`
*   **Type:** `string?`
*   **Purpose:** Specifies the partition key used for ordering messages within the message broker. This value may be null if partitioning is not utilized.
*   **Returns:** The partition key or `null`.
*   **Throws:** Never.

#### `CorrelationId`
*   **Type:** `string?`
*   **Purpose:** Links this error to a broader business transaction or related set of operations. This value may be null.
*   **Returns:** The correlation identifier or `null`.
*   **Throws:** Never.

### Constructors

#### `OutboxMessageDto()`
*   **Purpose:** Initializes a new instance of the data transfer object. Note: Despite the class name `ErrorResponse`, the provided signature indicates a constructor named `OutboxMessageDto`, suggesting this type may inherit from or alias `OutboxMessageDto` functionality.
*   **Parameters:** None.
*   **Returns:** A new instance of the object.
*   **Throws:** Never.

### Other Members

#### `OutboxMessageDto`
*   **Type:** `OutboxMessageDto` (Member signature implies a property, field, or type reference).
*   **Purpose:** Exposes an instance or reference to the underlying `OutboxMessageDto` structure, allowing access to base message properties not explicitly re-exposed on `ErrorResponse`.
*   **Returns:** The `OutboxMessageDto` instance.
*   **Throws:** Never.

## Usage

### Example 1: Inspecting a Failed Message for Retry Logic
This example demonstrates how to inspect an `ErrorResponse` to determine if a message should be retried based on the attempt count and maximum threshold.

```csharp
public void HandleFailedPublish(ErrorResponse error)
{
    if (error.PublishAttempts >= error.MaxPublishAttempts)
    {
        Console.WriteLine($"Message {error.Id} permanently failed after {error.PublishAttempts} attempts.");
        Console.WriteLine($"Error Code: {error.Code}, Details: {error.ErrorMessage}");
        // Trigger dead-letter queue logic or alerting
        return;
    }

    Console.WriteLine($"Retry eligible for Message {error.Id}. Attempt {error.PublishAttempts} of {error.MaxPublishAttempts}.");
    Console.WriteLine($"Target Topic: {error.Topic}, Aggregate: {error.AggregateType}");
}
```

### Example 2: Correlating Errors for Distributed Tracing
This example illustrates extracting tracing and correlation identifiers from the response to log context for debugging distributed systems.

```csharp
public void LogErrorContext(ErrorResponse error)
{
    var traceContext = string.IsNullOrEmpty(error.TraceId) 
        ? "No Trace Available" 
        : $"TraceId: {error.TraceId}";
    
    var correlationContext = string.IsNullOrEmpty(error.CorrelationId) 
        ? "No CorrelationId" 
        : $"CorrelationId: {error.CorrelationId}";

    Console.WriteLine($"[ERROR] {error.Timestamp} - {error.Message}");
    Console.WriteLine($"Context: {traceContext} | {correlationContext}");
    Console.WriteLine($"Partition: {error.PartitionKey ?? "Default"} | State: {error.State}");
}
```

## Notes

*   **Nullability:** Several properties (`TraceId`, `PublishedAt`, `ErrorMessage`, `PartitionKey`, `CorrelationId`) are nullable. Consumers must perform null checks before accessing members like `Length` or formatting these values to avoid `NullReferenceException`.
*   **Immutability vs. Mutability:** The signature provided exposes only getters (properties) and a constructor. If the underlying implementation allows modification of counters like `PublishAttempts` or state fields after instantiation, care must be taken in multi-threaded environments. However, based strictly on the provided read-only property signatures, instances appear intended to be immutable data snapshots.
*   **Constructor Naming Discrepancy:** The constructor is named `OutboxMessageDto()` while the class is `ErrorResponse`. This suggests `ErrorResponse` may be a specialized view or derived type of `OutboxMessageDto`. Instantiation relies on this specific constructor name if defined directly on the class, or it implies an inheritance structure where the base constructor is being highlighted.
*   **Time Zones:** All `DateTime` properties (`Timestamp`, `CreatedAt`, `PublishedAt`) should be assumed to be in UTC unless documented otherwise by the consuming service, to ensure consistency across distributed nodes.
*   **Thread Safety:** As a data transfer object containing primarily primitive types and strings, `ErrorResponse` is generally safe for concurrent reading. If any internal state mutation occurs (not visible in the provided public getters), external synchronization would be required during write operations.
