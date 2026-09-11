# OutboxProcessor

## Overview
`OutboxProcessor` is a `BackgroundService` that continuously polls for pending and scheduled outbox messages, publishes them, and manages their lifecycle. It incorporates resilience patterns, health monitoring, and idle backoff strategies to efficiently process messages while minimizing database load.

## Responsibilities
- **Message Processing**: Polls for and publishes pending outbox messages in configurable batches.
- **Scheduled Message Handling**: Processes messages scheduled for future delivery.
- **Lock Management**: Periodically identifies and releases locks on messages that have exceeded their lock duration, allowing them to be retried.
- **Health Monitoring**: Tracks and exposes health metrics including circuit breaker state, processing success/failure counts, expired locks, and the age of the oldest unprocessed message.
- **Resilience**: Implements a circuit breaker to prevent overwhelming downstream services during failures.
- **Idle Backoff**: Dynamically increases the polling delay when no work is found, reducing unnecessary database round-trips.

## Processing Loop
The processor runs in a continuous loop (`ExecuteAsync`) until cancellation is requested:
1. **Initialization Check**: Verifies if the processor is enabled via configuration.
2. **Circuit Breaker Check**: If the circuit breaker is open, it sleeps for the configured delay and skips processing to allow downstream recovery.
3. **Expired Lock Release**: Periodically checks for and releases locks on messages that have exceeded `LockDurationSeconds`.
4. **Staleness Check**: Evaluates the age of the oldest unprocessed message and logs a warning if it exceeds `OldestMessageAgeThresholdMinutes`.
5. **Batch Processing**:
   - Processes pending messages via `IMessagePublishingService`.
   - Processes scheduled messages via `IMessagePublishingService`.
6. **Backoff Tracking**: Increments a counter for consecutive empty batches. Resets it when work is processed.
7. **Delay**: Waits for the next poll interval, applying the configured `BackoffStrategy` (`None`, `Fixed`, or `Exponential`) to scale the delay when idle.
8. **Error Handling**: Catches exceptions, updates health metrics, records failures in the circuit breaker, and waits 10 seconds before retrying.

## Configuration Options
The processor is configured via `OutboxProcessorOptions` (implementing `IOutboxProcessorOptions`):

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Toggles the background processor on or off. |
| `BatchSize` | `int` | `100` | Number of messages to process per batch. |
| `DelayBetweenBatches` | `int` | `5000` | Base delay (ms) between processing batches. |
| `CheckExpiredLocksInterval` | `int` | `60000` | Interval (ms) between checks for expired locks. |
| `LockDurationSeconds` | `int` | `300` | Duration (seconds) a message lock remains valid before expiring. |
| `UseBatchClaiming` | `bool` | `true` | Enables batch claiming with row-level locking for competing consumers. |
| `MaxBatchClaimSize` | `int` | `100` | Maximum messages to claim in a single batch when batch claiming is enabled. |
| `PreservePartitionOrdering` | `bool` | `true` | Ensures partitioned messages are processed sequentially. |
| `OldestMessageAgeThresholdMinutes` | `int` | `5` | Age threshold (minutes) to trigger a warning log for stuck messages. |
| `BackoffStrategy` | `BackoffStrategy` | `None` | Strategy for scaling poll delay when idle (`None`, `Fixed`, `Exponential`). |
| `BackoffMultiplier` | `double` | `2.0` | Multiplier applied per empty batch when using exponential backoff. |
| `MaxDelayBetweenBatches` | `int` | `60000` | Upper bound (ms) for the backoff delay. |

## Usage Example

### 1. Register the Processor and Options
