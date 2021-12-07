# OutboxMessageAdditionalTests

`OutboxMessageAdditionalTests` is a unit test class that validates the behavior of the `OutboxMessage` domain entity beyond its core persistence concerns. It focuses on state transitions, validation rules, error recording, and the enforcement of invariants such as maximum publish attempts and required fields. These tests ensure that the outbox message correctly manages its lifecycle from creation through publishing or failure.

## API

### Validate_WithValidMessage_DoesNotThrowException
Validates that constructing or validating an `OutboxMessage` with all required fields populated correctly does not produce an exception.  
**Parameters:** None (test method).  
**Returns:** `void`.  
**Throws:** No exception is expected; the test fails if any exception is thrown.

### Validate_WithValidMessage_SetsDefaultValues
Confirms that a valid `OutboxMessage` initializes optional fields to their expected defaults (e.g., publish attempt counters, timestamps, lock tokens).  
**Parameters:** None.  
**Returns:** `void`.

### Validate_WithWhitespaceIdempotencyKey_ThrowsArgumentException
Ensures that providing an idempotency key consisting only of whitespace causes an `ArgumentException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** The test expects the validation logic to throw `ArgumentException`.

### Validate_WithEmptyAggregateId_ThrowsArgumentException
Verifies that an empty or null aggregate identifier triggers an `ArgumentException` during validation.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Expects `ArgumentException`.

### Validate_WithEmptyAggregateType_ThrowsArgumentException
Verifies that an empty or null aggregate type string triggers an `ArgumentException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Expects `ArgumentException`.

### Validate_WithEmptyEventData_ThrowsArgumentException
Ensures that empty or null event data payload causes an `ArgumentException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Expects `ArgumentException`.

### Validate_WithEmptyEventTypeName_ThrowsArgumentException
Ensures that an empty or null event type name triggers an `ArgumentException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Expects `ArgumentException`.

### Validate_WithEmptyTopic_ThrowsArgumentException
Ensures that an empty or null topic string triggers an `ArgumentException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Expects `ArgumentException`.

### Validate_WithNonPositiveMaxPublishAttempts_ThrowsArgumentException
Confirms that setting the maximum publish attempts to zero or a negative value throws an `ArgumentException`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** Expects `ArgumentException`.

### MarkAsPublished_FromPendingState_SetsCorrectState
Tests that calling `MarkAsPublished` on a message in the `Pending` state transitions it to the `Published` state.  
**Parameters:** None.  
**Returns:** `void`.

### MarkAsPublished_FromProcessingState_SetsCorrectState
Tests that calling `MarkAsPublished` on a message currently in the `Processing` state correctly moves it to `Published`.  
**Parameters:** None.  
**Returns:** `void`.

### MarkAsPublished_SetsPublishedAtToCurrentTime
Verifies that `MarkAsPublished` records the publication timestamp as the current UTC time (within a reasonable tolerance).  
**Parameters:** None.  
**Returns:** `void`.

### MarkAsPublished_ClearsErrorState
Ensures that after a successful publish, any previously recorded error message and stack trace are cleared.  
**Parameters:** None.  
**Returns:** `void`.

### RecordFailure_WithErrorMessage_StoresError
Confirms that calling `RecordFailure` with an error message persists that message on the outbox message entity.  
**Parameters:** None.  
**Returns:** `void`.

### RecordFailure_WithErrorMessageAndStackTrace_StoresBoth
Confirms that `RecordFailure` stores both the error message and the associated stack trace when both are supplied.  
**Parameters:** None.  
**Returns:** `void`.

### RecordFailure_SetsLastProcessedAt
Verifies that `RecordFailure` updates the `LastProcessedAt` timestamp to the current time.  
**Parameters:** None.  
**Returns:** `void`.

### RecordFailure_ReleasesLock
Ensures that `RecordFailure` clears the lock token, effectively releasing the message for subsequent processing attempts.  
**Parameters:** None.  
**Returns:** `void`.

### RecordFailure_BelowMaxAttempts_RemainsPending
Tests that when the number of recorded failures is still below the configured maximum, the message state stays `Pending` (or equivalent retryable state).  
**Parameters:** None.  
**Returns:** `void`.

### RecordFailure_AtMaxAttempts_SetsToFailed
Confirms that when the failure count exactly reaches the maximum publish attempts, the message transitions to `Failed`.  
**Parameters:** None.  
**Returns:** `void`.

### RecordFailure_ExceedsMaxAttempts_SetsToFailed
Confirms that exceeding the maximum publish attempts (e.g., due to concurrent failures) still results in a `Failed` state and does not wrap or reset the counter.  
**Parameters:** None.  
**Returns:** `void`.

## Usage

### Example 1: Validating and marking a message as published
```csharp
var message = new OutboxMessage
{
    IdempotencyKey = "order-123-event-1",
    AggregateId = "order-123",
    AggregateType = "Order",
    EventTypeName = "OrderPlaced",
    EventData = "{ \"orderId\": \"123\" }",
    Topic = "orders",
    MaxPublishAttempts = 5
};

// Validation is expected to succeed and set defaults such as PublishAttempts = 0.
message.Validate();

// After dispatch succeeds:
message.MarkAsPublished();

Assert.Equal(OutboxMessageState.Published, message.State);
Assert.NotNull(message.PublishedAt);
Assert.Null(message.ErrorMessage);
```

### Example 2: Recording failures and reaching the failed state
```csharp
var message = new OutboxMessage
{
    IdempotencyKey = "payment-456",
    AggregateId = "payment-456",
    AggregateType = "Payment",
    EventTypeName = "PaymentCaptured",
    EventData = "{ \"amount\": 99.99 }",
    Topic = "payments",
    MaxPublishAttempts = 3
};

message.Validate();

// First failure
message.RecordFailure("Network timeout", "System.Net.Http.HttpRequestException: ...");
Assert.Equal(OutboxMessageState.Pending, message.State);
Assert.Equal(1, message.PublishAttempts);

// Second failure
message.RecordFailure("Network timeout");
Assert.Equal(2, message.PublishAttempts);

// Third failure — reaches max attempts
message.RecordFailure("Service unavailable");
Assert.Equal(OutboxMessageState.Failed, message.State);
Assert.Equal(3, message.PublishAttempts);
Assert.NotNull(message.LastProcessedAt);
Assert.Null(message.LockToken); // lock released
```

## Notes

- **Validation order:** The validation methods imply that all required string fields (aggregate id, aggregate type, event data, event type name, topic, idempotency key) must be non-null and non-empty. Whitespace-only strings are treated as invalid for the idempotency key; similar treatment for other fields should be assumed.
- **State machine integrity:** `MarkAsPublished` is expected to succeed only from `Pending` or `Processing` states. Calling it from `Published` or `Failed` is not covered by these tests and may throw or no-op depending on implementation.
- **Idempotency and concurrency:** `RecordFailure` releases the lock token, allowing another process to pick up the message. Combined with the attempt counter, this implements a retry-with-backoff pattern. The tests confirm that exceeding max attempts (even by more than one) clamps the state to `Failed`.
- **Timestamps:** Both `MarkAsPublished` and `RecordFailure` set timestamps (`PublishedAt`, `LastProcessedAt`) to the current time. Tests typically allow a small delta when asserting exact values.
- **Error clearing:** A successful publish clears error information. If a message fails, gets retried, and later succeeds, the final state carries no residual error data.
