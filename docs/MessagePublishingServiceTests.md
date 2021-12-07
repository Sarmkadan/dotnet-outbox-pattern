# MessagePublishingServiceTests

Test suite for the `MessagePublishingService` class, validating its behavior under normal operation, edge cases, and failure scenarios. Covers constructor argument validation, batch and single-message processing, lock handling, retry logic, dead-letter queue transitions, scheduled message filtering, and lock release mechanics.

## API

### public MessagePublishingServiceTests

Default constructor for the test class. Provided by the test framework; no custom initialization logic.

---

### public void Constructor_WithNullOutboxRepository_ThrowsArgumentNullException

Verifies that constructing `MessagePublishingService` with a null `IOutboxRepository` parameter throws an `ArgumentNullException`.

- **Parameters**: None (test method).
- **Return value**: `void`.
- **Throws**: Asserts the subject under test throws `ArgumentNullException`.

---

### public void Constructor_WithNullPublisher_ThrowsArgumentNullException

Verifies that constructing `MessagePublishingService` with a null `IMessagePublisher` parameter throws an `ArgumentNullException`.

- **Parameters**: None (test method).
- **Return value**: `void`.
- **Throws**: Asserts the subject under test throws `ArgumentNullException`.

---

### public async Task ProcessPendingMessagesAsync_WithEmptyBatch_ReturnsZeroProcessed

Ensures that when the outbox repository returns an empty batch of pending messages, the method returns zero and no publish attempts occur.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessPendingMessagesAsync_WithSingleMessage_PublishesAndMarksPublished

Confirms that a single pending message is successfully published and subsequently marked as published in the repository.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessPendingMessagesAsync_WhenPublisherThrows_RecordsFailureAndContinues

Validates that when the message publisher throws an exception for one message, the failure is recorded, and processing continues for remaining messages in the batch.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessSingleMessageAsync_WhenMessageLocked_ReturnsFalse

Verifies that `ProcessSingleMessageAsync` returns `false` when the target message is already locked by another process, preventing duplicate processing.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessSingleMessageAsync_WhenMessageCanRetry_AttemptsPublish

Ensures that a message eligible for retry (retry count below maximum) triggers a publish attempt.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessSingleMessageAsync_WhenReachedMaxRetries_MovesToDlq

Confirms that a message that has exhausted its maximum retry count is moved to the dead-letter queue instead of being published again.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessScheduledMessagesAsync_WithFutureSchedule_DoesNotProcess

Validates that messages scheduled for a future time are skipped and not processed.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ReleaseLockAsync_UnlocksExpiredMessage

Verifies that `ReleaseLockAsync` successfully unlocks a message whose lock has expired.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessPendingMessagesAsync_WhenMessageIsLocked_DoesNotProcess

Ensures that locked messages are excluded from batch processing.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessPendingMessagesAsync_WhenMessageIsScheduled_DoesNotProcess

Ensures that messages with a future scheduled time are excluded from batch processing.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessPendingMessagesAsync_WhenPublisherThrowsForAllMessages_RecordsAllFailures

Validates that when every message in a batch fails to publish, all failures are recorded individually.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessSingleMessageAsync_WhenMessageIsNull_ReturnsFalse

Ensures that passing a null message to `ProcessSingleMessageAsync` returns `false` without throwing.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessSingleMessageAsync_WhenMessageIsLocked_ReturnsFalse

Duplicate coverage confirming that a locked message causes `ProcessSingleMessageAsync` to return `false`.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessSingleMessageAsync_WhenMessageIsScheduled_ReturnsFalse

Ensures that a message with a future schedule time causes `ProcessSingleMessageAsync` to return `false`.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ProcessScheduledMessagesAsync_WithPastSchedule_ProcessesMessages

Validates that messages whose scheduled time has passed are picked up and processed.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ReleaseLockAsync_WhenMessageNotFound_DoesNotThrow

Ensures that attempting to release a lock on a non-existent message completes without throwing an exception.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

---

### public async Task ReleaseLockAsync_WhenMessageNotLocked_DoesNotUpdate

Verifies that calling `ReleaseLockAsync` on a message that is not currently locked results in no state change.

- **Parameters**: None (test method).
- **Return value**: `Task`.
- **Throws**: No expected exceptions.

## Usage

```csharp
// Example 1: Batch processing succeeds for all messages
var repository = new Mock<IOutboxRepository>();
var publisher = new Mock<IMessagePublisher>();
repository.Setup(r => r.GetPendingMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new[] { CreatePendingMessage() });
publisher.Setup(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
         .Returns(Task.CompletedTask);

var service = new MessagePublishingService(repository.Object, publisher.Object);
int processed = await service.ProcessPendingMessagesAsync(batchSize: 10, CancellationToken.None);

Assert.Equal(1, processed);
repository.Verify(r => r.MarkAsPublishedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
```

```csharp
// Example 2: Message exceeds retry limit and moves to DLQ
var message = CreateMessage(retryCount: 5, maxRetries: 5);
var repository = new Mock<IOutboxRepository>();
var publisher = new Mock<IMessagePublisher>();
publisher.Setup(p => p.PublishAsync(message, It.IsAny<CancellationToken>()))
         .ThrowsAsync(new InvalidOperationException("Publish failure"));

var service = new MessagePublishingService(repository.Object, publisher.Object);
bool result = await service.ProcessSingleMessageAsync(message, CancellationToken.None);

Assert.False(result);
repository.Verify(r => r.MoveToDeadLetterQueueAsync(message.Id, It.IsAny<CancellationToken>()), Times.Once);
```

## Notes

- **Locking semantics**: Messages already locked are consistently skipped in both batch and single-message processing paths. Tests confirm this behavior across multiple scenarios to guard against accidental double-processing.
- **Scheduling**: Future-scheduled messages are excluded from standard pending-message batches; only past-schedule messages are processed via the scheduled-message path.
- **Retry and DLQ**: The retry boundary is enforced precisely—messages at the maximum retry count are moved to the dead-letter queue rather than retried. Messages below the threshold are retried, and failures are recorded.
- **Null safety**: `ProcessSingleMessageAsync` explicitly handles a null message argument by returning `false` rather than throwing, preventing cascading failures in iteration loops.
- **Lock release idempotency**: `ReleaseLockAsync` is safe to call on messages that do not exist or are not locked; it silently succeeds without side effects.
- **Thread safety**: The tests do not directly exercise concurrent access, but the locking and lock-release behaviors they validate form the foundation for safe multi-instance or concurrent processing scenarios.
