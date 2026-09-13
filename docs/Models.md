# Domain models

`Domain/Models.cs` defines the circuit-breaker state and the result, configuration, statistics, publishing, and health models used by the outbox processor. All types are in the `DotnetOutboxPattern.Domain` namespace.

## `CircuitState`

Enum describing the operating state of a circuit breaker.

| Member | Meaning |
| --- | --- |
| `Closed` | Requests are allowed to pass through. |
| `Open` | Requests are blocked. |
| `HalfOpen` | A limited number of requests are allowed to test whether the downstream service has recovered. |

The members do not have explicit numeric assignments, so their underlying values are `0`, `1`, and `2`, respectively.

## `OutboxProcessingResult`

Sealed class representing the result of processing a batch of outbox messages.

| Property | Type | Access | Default or calculation | Description |
| --- | --- | --- | --- | --- |
| `Success` | `bool` | Get/set | `false` | Whether processing was successful. |
| `ProcessedCount` | `int` | Get/set | `0` | Number of messages processed. |
| `FailedCount` | `int` | Get/set | `0` | Number of messages that failed. |
| `DeadLetterCount` | `int` | Get/set | `0` | Number of messages moved to the dead-letter queue. |
| `ErrorMessage` | `string?` | Get/set | `null` | Error message when processing fails. |
| `StackTrace` | `string?` | Get/set | `null` | Stack trace associated with the processing error. |
| `StartedAt` | `DateTime` | Get/set | `default(DateTime)` | Time at which processing started. |
| `CompletedAt` | `DateTime` | Get/set | `default(DateTime)` | Time at which processing completed. |
| `Duration` | `TimeSpan` | Get only | `CompletedAt - StartedAt` | Duration of processing. |
| `ProcessedMessageIds` | `List<Guid>` | Get/set | Empty list | Identifiers of processed messages. |
| `FailedMessageIds` | `List<Guid>` | Get/set | Empty list | Identifiers of failed messages. |

## `OutboxProcessorConfig`

Sealed class representing configuration for an outbox processor.

| Property | Type | Access | Default | Description |
| --- | --- | --- | --- | --- |
| `BatchSize` | `int` | Get/set | `100` | Number of messages processed in each batch. |
| `LockDuration` | `TimeSpan` | Get/set | 5 minutes | How long a message remains locked while it is processed. |
| `DelayBetweenBatches` | `TimeSpan` | Get/set | 30 seconds | Delay between processing batches. |
| `MessagesBeforeBreak` | `int` | Get/set | `1000` | Number of messages processed before taking a break. |
| `BreakDuration` | `TimeSpan` | Get/set | 10 seconds | Length of the break after `MessagesBeforeBreak` messages. |
| `EnableParallelProcessing` | `bool` | Get/set | `true` | Whether messages are processed in parallel. |
| `MaxDegreeOfParallelism` | `int` | Get/set | `4` | Maximum degree of parallelism. |
| `EnableDeadLetterProcessing` | `bool` | Get/set | `true` | Whether dead-letter processing is enabled. |

## `OutboxStatistics`

Sealed class representing statistics about messages in the outbox.

| Property | Type | Access | Default or calculation | Description |
| --- | --- | --- | --- | --- |
| `TotalMessages` | `long` | Get/set | `0` | Total number of messages. |
| `PendingMessages` | `long` | Get/set | `0` | Number of pending messages. |
| `ProcessingMessages` | `long` | Get/set | `0` | Number of messages being processed. |
| `PublishedMessages` | `long` | Get/set | `0` | Number of published messages. |
| `FailedMessages` | `long` | Get/set | `0` | Number of failed messages. |
| `ArchivedMessages` | `long` | Get/set | `0` | Number of archived messages. |
| `DeadLetterCount` | `long` | Get/set | `0` | Number of messages in the dead-letter queue. |
| `AveragePublishTime` | `TimeSpan` | Get/set | `TimeSpan.Zero` | Average time required to publish a message. |
| `OldestPendingAge` | `TimeSpan?` | Get/set | `null` | Age of the oldest pending message. |
| `SuccessRate` | `double` | Get only | `PublishedMessages / TotalMessages * 100` when `TotalMessages > 0`; otherwise `0` | Percentage of messages successfully published. |

## `PublishingOptions`

Sealed class representing options that control message publishing.

| Property | Type | Access | Default | Description |
| --- | --- | --- | --- | --- |
| `MaxRetries` | `int` | Get/set | `5` | Maximum number of retry attempts. |
| `RetryPolicy` | `RetryPolicyType` | Get/set | `RetryPolicyType.ExponentialBackoff` | Retry policy to use. |
| `InitialRetryDelay` | `TimeSpan` | Get/set | 5 seconds | Initial delay before the first retry. |
| `MaxRetryDelay` | `TimeSpan` | Get/set | 5 minutes | Maximum delay between retries. |
| `BackoffMultiplier` | `double` | Get/set | `2.0` | Multiplier used for exponential backoff. |
| `DeliveryGuarantee` | `DeliveryGuarantee` | Get/set | `DeliveryGuarantee.AtLeastOnce` | Delivery guarantee level. |
| `UseJitter` | `bool` | Get/set | `true` | Whether jitter is added to retry delays. |
| `PublishTimeout` | `TimeSpan` | Get/set | 30 seconds | Timeout for publishing one message. |
| `ClockSkewTolerance` | `TimeSpan` | Get/set | 1 minute | Clock-skew tolerance applied to the deduplication window. |
| `UseBatchClaiming` | `bool` | Get/set | `true` | Whether competing consumers claim batches using row-level locking. |
| `MaxBatchClaimSize` | `int` | Get/set | `100` | Maximum number of messages claimed when batch claiming is enabled. |
| `LockDurationSeconds` | `int` | Get/set | `300` | Message lock duration, in seconds, used during batch claiming. |

## `HealthMetrics`

Sealed class representing health metrics for outbox message publication.

| Property | Type | Access | Default or calculation | Description |
| --- | --- | --- | --- | --- |
| `IsHealthy` | `bool` | Get/set | `true` | Whether the outbox processor is healthy. |
| `LastSuccessfulPublish` | `DateTime?` | Get/set | `null` | Time at which a message was last published successfully. |
| `ConsecutiveFailures` | `int` | Get/set | `0` | Number of consecutive publication failures. |
| `ErrorMessage` | `string?` | Get/set | `null` | Current error message when the processor is unhealthy. |
| `LastHealthCheckAt` | `DateTime` | Get/set | `DateTime.UtcNow` at instance initialization | Time at which the health check was last run. |
| `LockedMessagesCount` | `int` | Get/set | `0` | Number of messages currently locked. |
| `HasExpiredLocks` | `bool` | Get/set | `false` | Whether any messages have expired locks. |
| `OldestMessageAge` | `TimeSpan?` | Get/set | `null` | Age of the oldest unprocessed pending message, or `null` when there are no pending messages. |
| `CircuitState` | `CircuitState` | Get/set | `CircuitState.Closed` | Current circuit-breaker state. |
| `IsCircuitOpen` | `bool` | Get only | `CircuitState == CircuitState.Open` | Whether the circuit breaker is open. |
