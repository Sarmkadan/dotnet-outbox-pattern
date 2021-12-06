# DeadLetterServiceTests

Unit test suite for the `DeadLetterService` class, validating its behavior in handling dead-letter queue (DLQ) operations such as moving messages to DLQ, reviewing dead-lettered messages, requeuing them, and health monitoring. The tests ensure proper exception handling, repository interactions, and edge case coverage for DLQ workflows.

## API

### `DeadLetterServiceTests`
Constructor for the test class. Initializes test dependencies and mocks required for validating `DeadLetterService` behavior.

### `Constructor_WithNullDlRepository_ThrowsArgumentNullException()`
Validates that the `DeadLetterService` constructor throws an `ArgumentNullException` when a null dead-letter repository is provided.

### `Constructor_WithNullOutboxRepository_ThrowsArgumentNullException()`
Validates that the `DeadLetterService` constructor throws an `ArgumentNullException` when a null outbox repository is provided.

### `Constructor_WithNullLogger_ThrowsArgumentNullException()`
Validates that the `DeadLetterService` constructor throws an `ArgumentNullException` when a null logger is provided.

### `MoveToDlqAsync_WithNullMessage_ThrowsDeadLetterException()`
Ensures that calling `MoveToDlqAsync` with a null message results in a `DeadLetterException` being thrown.

### `MoveToDlqAsync_WithValidMessage_AddsDeadLetterAndReturnsIt()`
Verifies that providing a valid message to `MoveToDlqAsync` results in the message being added to the dead-letter repository and the same message being returned.

### `ReviewAsync_WhenDeadLetterNotFound_ThrowsDeadLetterException()`
Confirms that attempting to review a non-existent dead-letter message via `ReviewAsync` throws a `DeadLetterException`.

### `ReviewAsync_WhenFound_MarksAsReviewedAndPersistsUpdate()`
Validates that `ReviewAsync` marks a found dead-letter message as reviewed and persists the update via the repository.

### `GetHealthAsync_WithNoUnreviewedMessages_ReturnsHealthy()`
Checks that `GetHealthAsync` returns a healthy status when no unreviewed messages exist in the DLQ.

### `GetHealthAsync_WithUnreviewedMessages_ReturnsUnhealthyWithCount()`
Ensures that `GetHealthAsync` returns an unhealthy status along with the count of unreviewed messages when such messages exist.

### `RequeueAsync_WhenDeadLetterNotFound_ThrowsDeadLetterException()`
Validates that calling `RequeueAsync` with a non-existent dead-letter message throws a `DeadLetterException`.

### `RequeueAsync_WhenOriginalMessageExists_ResetsToPendingAndMarksRequeued()`
Confirms that `RequeueAsync` resets the original message to a pending state and marks it as requeued when the original message exists.

### `GetUnreviewedAsync_DelegatesToRepository()`
Ensures that `GetUnreviewedAsync` delegates the retrieval of unreviewed messages to the underlying repository.

### `GetByTopicAsync_DelegatesToRepository()`
Verifies that `GetByTopicAsync` delegates message lookup by topic to the repository.

### `DeleteAsync_DelegatesToRepository()`
Confirms that `DeleteAsync` delegates the deletion operation to the repository.

### `RequeueAsync_WhenOriginalMessageDoesNotExist_CreatesNewMessage()`
Validates that `RequeueAsync` creates a new outbox message when the original message no longer exists.

### `GetUnreviewedCountAsync_DelegatesToRepository()`
Ensures that `GetUnreviewedCountAsync` delegates the count retrieval of unreviewed messages to the repository.

### `MoveToDlqAsync_WhenRepositoryThrows_ThrowsDeadLetterException()`
Verifies that `MoveToDlqAsync` throws a `DeadLetterException` when the repository operation fails.

### `ReviewAsync_WhenRepositoryThrows_ThrowsDeadLetterException()`
Ensures that `ReviewAsync` throws a `DeadLetterException` when the repository operation fails.

### `RequeueAsync_WhenRepositoryThrows_ThrowsDeadLetterException()`
Validates that `RequeueAsync` throws a `DeadLetterException` when the repository operation fails.

## Usage

### Example 1: Validating DLQ Health
