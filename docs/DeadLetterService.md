# DeadLetterService

## Purpose

The `DeadLetterService` manages dead letter messages for the outbox pattern. It handles moving failed outbox messages to a dead letter queue (DLQ), reviewing them, and requeuing them for retry after issues are resolved.

## Public API

### Interface: `IDeadLetterService`

Defines the contract for dead letter queue operations:

- `Task<DeadLetter> MoveToDlqAsync(OutboxMessage message, CancellationToken cancellationToken = default)`
  - Moves an outbox message to the dead letter queue
- `Task<DeadLetter?> GetAsync(Guid deadLetterId, CancellationToken cancellationToken = default)`
  - Retrieves a dead letter by its ID
- `Task<List<DeadLetter>> GetUnreviewedAsync(int limit = 100, CancellationToken cancellationToken = default)`
  - Retrieves unreviewed dead letters requiring operator action
- `Task ReviewAsync(Guid deadLetterId, string notes, CancellationToken cancellationToken = default)`
  - Marks a dead letter as reviewed with operator notes
- `Task RequeueAsync(Guid deadLetterId, string reason, CancellationToken cancellationToken = default)`
  - Requeues a dead letter message back to the outbox for retry
- `Task<int> GetUnreviewedCountAsync(CancellationToken cancellationToken = default)`
  - Gets the count of unreviewed dead letters
- `Task<List<DeadLetter>> GetByTopicAsync(string topic, CancellationToken cancellationToken = default)`
  - Gets dead letters for a specific topic
- `Task DeleteAsync(Guid deadLetterId, CancellationToken cancellationToken = default)`
  - Deletes a dead letter record after review/resolution
- `Task<HealthMetrics> GetHealthAsync(CancellationToken cancellationToken = default)`
  - Gets health metrics for the dead letter queue

### Class: `DeadLetterService`

Sealed implementation of `IDeadLetterService` with the following dependencies:
- `IDeadLetterRepository` - Persists and queries dead letter records
- `IOutboxRepository` - Reads and updates outbox messages on requeue
- `IOutboxService` - Used by dependent operations
- `ILogger<DeadLetterService>` - Logger for dead letter diagnostics
- `OutboxRetryOptions` - Retry-with-backoff policy for requeued messages

## How Dead-Letter Records Are Produced

Dead letter records are created when an outbox message fails after exceeding its maximum publish attempts. The process occurs via:

1. **MoveToDlqAsync** method:
   - Takes a failed `OutboxMessage` and converts it to a `DeadLetter` entity using `DeadLetter.FromOutboxMessage(message)`
   - Persists the dead letter via `_dlRepository.AddAsync`
   - Logs the movement with details: message ID, topic, attempts, and error message
   - Returns the created `DeadLetter` record
   - Throws `DeadLetterException` on failure

This method is typically called by the outbox processing pipeline when a message has failed repeatedly.

## How Dead-Letter Records Are Requeued

Dead letters can be returned to the outbox for retry via the **RequeueAsync** method:

1. **Validation**:
   - Ensures `deadLetterId` is not empty and `reason` is provided
   - Retrieves the dead letter from the repository

2. **Message Restoration**:
   - Attempts to retrieve the original outbox message by `OutboxMessageId`
   - If the original message exists:
     - Resets its state to `Pending`
     - Resets `PublishAttempts` to 0
     - Restores `MaxPublishAttempts` from retry options
     - Clears error fields (`ErrorMessage`, `ErrorStackTrace`, `LastProcessedAt`)
     - Updates the message via `_outboxRepository.UpdateAsync`
   - If the original message does not exist:
     - Creates a new outbox message from dead letter data
     - Generates a new IdempotencyKey with `_requeue_` suffix
     - Sets initial state to `Pending` with zero publish attempts
     - Adds the new message via `_outboxRepository.AddAsync`

3. **Dead Letter Update**:
   - Marks the dead letter as requeued with the provided reason via `deadLetter.MarkAsRequeued(reason)`
   - Persists the update via `_dlRepository.UpdateAsync`

4. **Logging**:
   - Logs the requeue operation with dead letter ID, new message ID, and reason

This process allows operators to investigate and resolve issues causing message failures, then retry the message processing.

## Health Monitoring

The service provides health metrics via `GetHealthAsync`:
- Considers the queue healthy when there are zero unreviewed dead letters
- Returns `IsHealthy` boolean, timestamp of last check, and error message if unhealthy
- Unhealthy state indicates presence of unreviewed dead letters requiring attention

## Error Handling

All methods catch exceptions and:
- Log the error with contextual information
- Rethrow as `DeadLetterException` with appropriate message
- Preserve original exception as inner exception for debugging

## Thread Safety

The service is designed for concurrent use:
- Stateless except for dependencies
- Dependencies are expected to be thread-safe
- No mutable shared state within the service itself