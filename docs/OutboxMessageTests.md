# OutboxMessageTests

Unit tests for the `OutboxMessage` type, verifying message validation, state transitions, failure handling, locking behavior, and retry logic. The tests ensure correct behavior for message processing workflows including idempotency, error handling, and concurrency control.

## API

### `Validate_WithValidMessage_DoesNotThrow`
Verifies that a properly constructed `OutboxMessage` passes validation without throwing exceptions.

### `Validate_WithEmptyIdempotencyKey_ThrowsArgumentException`
Ensures validation fails with an `ArgumentException` when the `IdempotencyKey` is empty or whitespace.

### `Validate_WithEmptyTopic_ThrowsArgumentException`
Ensures validation fails with an `ArgumentException` when the `Topic` is empty or whitespace.

### `Validate_WithNonPositiveMaxPublishAttempts_ThrowsArgumentException`
Ensures validation fails with an `ArgumentException` when `MaxPublishAttempts` is zero or negative.

### `MarkAsPublished_SetsStateToPublished`
Confirms that calling `MarkAsPublished` transitions the message state to `Published`.

### `MarkAsPublished_SetsPublishedAtAndClearsError`
Verifies that `MarkAsPublished` sets the `PublishedAt` timestamp and clears any previously recorded error.

### `RecordFailure_IncrementsPublishAttempts`
Ensures that each call to `RecordFailure` increments the `PublishAttempts` counter.

### `RecordFailure_StoresErrorMessageAndReleasesLock`
Confirms that `RecordFailure` stores the provided error message and releases the message lock.

### `RecordFailure_WhenMaxAttemptsReached_SetsStateFailed`
Validates that when `PublishAttempts` reaches `MaxPublishAttempts`, the message state is set to `Failed`.

### `RecordFailure_BelowMaxAttempts_DoesNotChangeState`
Ensures that calling `RecordFailure` does not alter the message state when `PublishAttempts` is below `MaxPublishAttempts`.

### `Lock_SetsIsLockedTrueAndStateToProcessing`
Confirms that `Lock` sets `IsLocked` to `true` and transitions the message state to `Processing`.

### `UnlockIfExpired_WhenLockHasExpired_ReturnsTrueAndResetsToPending`
Verifies that `UnlockIfExpired` returns `true` and resets the message state to `Pending` when the lock has expired.

### `UnlockIfExpired_WhenLockStillActive_ReturnsFalse`
Ensures that `UnlockIfExpired` returns `false` when the message lock is still active.

### `CanRetry_WhenBelowMaxAttempts_ReturnsTrue`
Confirms that `CanRetry` returns `true` when `PublishAttempts` is below `MaxPublishAttempts`.

### `CanRetry_WhenAttemptsEqualMax_ReturnsFalse`
Ensures that `CanRetry` returns `false` when `PublishAttempts` equals `MaxPublishAttempts`.

### `CanRetry_WhenStateIsPublished_ReturnsFalse`
Verifies that `CanRetry` returns `false` when the message state is `Published`.

## Usage
