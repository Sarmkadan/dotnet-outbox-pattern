# OutboxServiceTests

`OutboxServiceTests` is the unit test suite for the `OutboxService` class in the `dotnet-outbox-pattern` project. It validates the service’s constructor guards, message lifecycle operations (publishing, retrying failed messages, retrieval), and delegation patterns to the underlying repository. The tests cover both happy paths and exceptional scenarios, ensuring that argument validation, state transitions, idempotency, delivery guarantees, and error wrapping behave as specified.

## API

### `OutboxServiceTests`

The default constructor for the test class. It is responsible for initializing the test context, typically setting up mocked dependencies (repository, logger) and the system under test before each test method runs. It takes no parameters and does not return a value.

---

### `public void Constructor_WithNullRepository_ThrowsArgumentNullException`

Verifies that the `OutboxService` constructor throws an `ArgumentNullException` when a null repository is supplied. This test ensures the service fails fast on a missing mandatory dependency.

- **Parameters:** None (test method).
- **Returns:** void.
- **Throws:** The test expects the constructor to throw `ArgumentNullException`.

---

### `public void Constructor_WithNullLogger_ThrowsArgumentNullException`

Verifies that the `OutboxService` constructor throws an `ArgumentNullException` when a null logger is supplied. This test ensures the service fails fast on a missing mandatory dependency.

- **Parameters:** None (test method).
- **Returns:** void.
- **Throws:** The test expects the constructor to throw `ArgumentNullException`.

---

### `public async Task RetryFailedMessageAsync_WhenMessageNotFound_ThrowsOutboxMessageNotFoundException`

Tests that calling `RetryFailedMessageAsync` with an identifier that does not correspond to any persisted message results in an `OutboxMessageNotFoundException`. This guards against retrying non-existent messages.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** The test expects `OutboxMessageNotFoundException`.

---

### `public async Task RetryFailedMessageAsync_WhenStateIsNotFailed_ReturnsFalse`

Tests that calling `RetryFailedMessageAsync` on a message whose current state is anything other than `Failed` returns `false` without modifying the message. This enforces the precondition that only failed messages can be retried.

- **Parameters:** None (test method).
- **Returns:** `Task` (the test asserts the method under test returns `false`).
- **Throws:** No exception expected.

---

### `public async Task RetryFailedMessageAsync_WhenStateFailed_ResetsToPendingAndReturnsTrue`

Tests that calling `RetryFailedMessageAsync` on a message in the `Failed` state resets its state to `Pending` and returns `true`. This validates the core retry transition logic.

- **Parameters:** None (test method).
- **Returns:** `Task` (the test asserts the method under test returns `true`).
- **Throws:** No exception expected.

---

### `public async Task GetMessageAsync_DelegatesToRepository`

Tests that `GetMessageAsync` forwards the call to the repository’s corresponding method with the same identifier, returning whatever the repository returns. This confirms the service acts as a pass-through for single-message retrieval.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task GetMessageAsync_WhenRepositoryThrows_WrapsInOutboxException`

Tests that when the underlying repository throws an exception during `GetMessageAsync`, the service catches it and rethrows it wrapped in an `OutboxException`. This ensures consistent error surface from the service layer.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** The test expects `OutboxException`.

---

### `public async Task PublishEventAsync_WithNullPublishableEvent_ThrowsArgumentNullException`

Tests that `PublishEventAsync` throws an `ArgumentNullException` when the provided publishable event is `null`. This enforces input validation at the service boundary.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** The test expects `ArgumentNullException`.

---

### `public async Task PublishEventAsync_WhenIdempotencyKeyAlreadyExists_ReturnsExistingMessage`

Tests that when `PublishEventAsync` is called with an idempotency key that already exists in the repository, the service does not create a duplicate; instead it returns the previously stored message. This validates idempotent publishing behavior.

- **Parameters:** None (test method).
- **Returns:** `Task` (the test asserts the returned message is the existing one).
- **Throws:** No exception expected.

---

### `public async Task PublishEventAsync_WhenNewEvent_AddsToRepository`

Tests that when `PublishEventAsync` is called with a new event (no matching idempotency key), the service adds the message to the repository and returns the newly persisted message. This validates the normal publishing flow.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task GetStatisticsAsync_DelegatesToRepository`

Tests that `GetStatisticsAsync` delegates directly to the repository’s statistics method and returns its result without transformation. This confirms the service is a thin pass-through for statistics queries.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task GetAllMessagesAsync_DelegatesToRepository`

Tests that `GetAllMessagesAsync` delegates to the repository’s method for retrieving all messages and returns the result unchanged.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task GetMessagesByTopicAsync_DelegatesToRepository`

Tests that `GetMessagesByTopicAsync` forwards the topic filter to the repository and returns the filtered messages as provided by the repository.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task GetMessagesByAggregateAsync_DelegatesToRepository`

Tests that `GetMessagesByAggregateAsync` forwards the aggregate identifier to the repository and returns the repository’s result without modification.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task GetMessagesByStateAsync_DelegatesToRepository`

Tests that `GetMessagesByStateAsync` forwards the state filter to the repository and returns the repository’s result unchanged.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task GetMessagesByDateRangeAsync_DelegatesToRepository`

Tests that `GetMessagesByDateRangeAsync` forwards the date range criteria to the repository and returns the repository’s result without transformation.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task PublishEventAsync_WithIdempotencyKey_UsesCorrectKey`

Tests that when `PublishEventAsync` is invoked with a specific idempotency key, the service passes that exact key to the repository for idempotency checks and storage. This ensures the key is not altered or ignored.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task PublishEventAsync_WithDeliveryGuaranteeAtMostOnce_SetsMaxAttemptsToOne`

Tests that when a publishable event specifies `AtMostOnce` delivery guarantee, the resulting outbox message has its maximum attempts set to `1`. This enforces the “at most once” semantic at the message level.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

---

### `public async Task PublishEventAsync_WithDeliveryGuaranteeAtLeastOnce_SetsMaxAttemptsToDefault`

Tests that when a publishable event specifies `AtLeastOnce` delivery guarantee, the resulting outbox message has its maximum attempts set to the default value (typically greater than 1), allowing retries until successful delivery.

- **Parameters:** None (test method).
- **Returns:** `Task`.
- **Throws:** No exception expected.

## Usage

### Example 1: Testing the retry flow for a failed message

```csharp
[Fact]
public async Task RetryFailedMessage_ResetsState_And_ReturnsTrue()
{
    // Arrange
    var failedMessage = new OutboxMessage
    {
        Id = Guid.NewGuid(),
        State = OutboxMessageState.Failed
    };

    mockRepository.Setup(r => r.GetMessageAsync(failedMessage.Id))
                  .ReturnsAsync(failedMessage);
    mockRepository.Setup(r => r.UpdateAsync(It.IsAny<OutboxMessage>()))
                  .Returns(Task.CompletedTask);

    var service = new OutboxService(mockRepository.Object, mockLogger.Object);

    // Act
    var result = await service.RetryFailedMessageAsync(failedMessage.Id);

    // Assert
    Assert.True(result);
    Assert.Equal(OutboxMessageState.Pending, failedMessage.State);
    mockRepository.Verify(r => r.UpdateAsync(failedMessage), Times.Once);
}
```

### Example 2: Testing idempotent publishing with an existing key

```csharp
[Fact]
public async Task PublishEvent_IdempotencyKeyExists_ReturnsExistingMessage()
{
    // Arrange
    var idempotencyKey = "order-123-confirmation";
    var existingMessage = new OutboxMessage
    {
        Id = Guid.NewGuid(),
        IdempotencyKey = idempotencyKey,
        State = OutboxMessageState.Pending
    };

    var newEvent = new PublishableEvent
    {
        IdempotencyKey = idempotencyKey,
        Payload = "{\"orderId\":123}",
        Topic = "orders"
    };

    mockRepository.Setup(r => r.GetMessageByIdempotencyKeyAsync(idempotencyKey))
                  .ReturnsAsync(existingMessage);

    var service = new OutboxService(mockRepository.Object, mockLogger.Object);

    // Act
    var result = await service.PublishEventAsync(newEvent);

    // Assert
    Assert.Same(existingMessage, result);
    mockRepository.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>()), Times.Never);
}
```

## Notes

- **Constructor validation:** The tests `Constructor_WithNullRepository_ThrowsArgumentNullException` and `Constructor_WithNullLogger_ThrowsArgumentNullException` confirm that `OutboxService` applies defensive null checks on all mandatory dependencies. Instantiation with null arguments is always rejected before any operation is attempted.
- **State precondition for retry:** `RetryFailedMessageAsync` only acts on messages in the `Failed` state. Any other state (e.g., `Pending`, `Completed`, `InProgress`) results in `false` and no state mutation. The caller must check the return value to determine whether a retry was performed.
- **Idempotency enforcement:** `PublishEventAsync` uses the idempotency key to prevent duplicate messages. If a key collision is detected, the existing message is returned immediately and no new entry is added to the repository. The test `PublishEventAsync_WithIdempotencyKey_UsesCorrectKey` ensures the key is passed verbatim.
- **Delivery guarantees:** The maximum attempts field on an outbox message is derived from the delivery guarantee specified in the publishable event. `AtMostOnce` sets it to 1 (no retries); `AtLeastOnce` sets it to a default value greater than 1, enabling the retry infrastructure to attempt delivery multiple times.
- **Error wrapping:** When the repository throws during `GetMessageAsync`, the service wraps the exception in an `OutboxException`. This provides a consistent exception type for callers and prevents direct leakage of persistence-layer exceptions.
- **Delegation pattern:** The majority of query methods (`GetStatisticsAsync`, `GetAllMessagesAsync`, `GetMessagesByTopicAsync`, `GetMessagesByAggregateAsync`, `GetMessagesByStateAsync`, `GetMessagesByDateRangeAsync`) are pure pass-throughs to the repository. They contain no additional logic, filtering, or transformation. Tests for these methods verify delegation and return-value fidelity.
- **Thread safety:** The test suite does not directly address thread safety. The service methods are asynchronous and rely on the underlying repository implementation for concurrency control. Idempotency checks suggest a design that tolerates concurrent publish attempts for the same key, but actual safety depends on the repository’s transactional or locking behavior, which is outside the scope of these tests.
