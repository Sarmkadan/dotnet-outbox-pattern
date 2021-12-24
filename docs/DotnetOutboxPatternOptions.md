# DotnetOutboxPatternOptions

Configuration options for the Outbox Pattern processor in .NET applications. These options control batching behavior, retry policies, message durability guarantees, and other operational parameters for reliable message publishing.

## API

### `ProcessorEnabled`
Enables or disables the Outbox Pattern processor. When disabled, messages are not processed or published, but may still be stored in the outbox.

- **Type**: `bool`
- **Default**: `true`

### `BatchSize`
Maximum number of messages to process in a single batch. Larger batches improve throughput but increase memory usage and transaction duration.

- **Type**: `int`
- **Default**: `100`
- **Constraints**: Must be a positive integer.

### `DelayBetweenBatches`
Milliseconds to wait between processing batches. Introduces artificial delay to reduce system load or rate-limit external dependencies.

- **Type**: `int`
- **Default**: `100`
- **Constraints**: Must be a non-negative integer.

### `MaxRetries`
Maximum number of retry attempts for a failed message before it is moved to the dead-letter queue.

- **Type**: `int`
- **Default**: `5`
- **Constraints**: Must be a non-negative integer.

### `RetryPolicy`
Defines the retry strategy applied to failed messages. Determines how delays between retries are calculated.

- **Type**: `RetryPolicyType`
- **Default**: `RetryPolicyType.Exponential`
- **Possible Values**:
  - `RetryPolicyType.Linear`: Fixed delay between retries.
  - `RetryPolicyType.Exponential`: Delay increases exponentially.
  - `RetryPolicyType.Random`: Randomized delay within bounds.

### `InitialRetryDelaySeconds`
Initial delay (in seconds) before the first retry attempt. Used as the base for exponential backoff or as the fixed delay in linear retry policies.

- **Type**: `int`
- **Default**: `5`
- **Constraints**: Must be a positive integer.

### `MaxRetryDelaySeconds`
Maximum delay (in seconds) between retry attempts. Caps exponential growth to prevent excessively long waits.

- **Type**: `int`
- **Default**: `300`
- **Constraints**: Must be greater than or equal to `InitialRetryDelaySeconds`.

### `BackoffMultiplier`
Multiplier applied to the delay between retry attempts in exponential backoff policies.

- **Type**: `double`
- **Default**: `2.0`
- **Constraints**: Must be greater than `1.0`.

### `DeliveryGuarantee`
Specifies the level of message delivery assurance provided by the outbox processor.

- **Type**: `DeliveryGuarantee`
- **Default**: `DeliveryGuarantee.AtLeastOnce`
- **Possible Values**:
  - `DeliveryGuarantee.AtLeastOnce`: Messages may be delivered more than once.
  - `DeliveryGuarantee.AtMostOnce`: Messages are delivered zero or one time.
  - `DeliveryGuarantee.ExactlyOnce`: Messages are delivered exactly once (requires transactional outbox and idempotency).

### `UseJitter`
Adds random jitter (up to 50% of delay) to retry timings to avoid thundering herd problems during retries.

- **Type**: `bool`
- **Default**: `true`

### `PublishTimeoutSeconds`
Maximum time (in seconds) to wait for a message to be published before considering the operation failed.

- **Type**: `int`
- **Default**: `30`
- **Constraints**: Must be a positive integer.

### `MessageTtlDays`
Time-to-live (in days) for messages in the outbox. Messages older than this are automatically purged.

- **Type**: `int`
- **Default**: `30`
- **Constraints**: Must be a non-negative integer.

### `PreservePartitionOrdering`
Ensures that messages within the same partition are processed in the order they were enqueued, even across retries.

- **Type**: `bool`
- **Default**: `true`

### `LockDurationSeconds`
Duration (in seconds) for which a message is locked during processing. Prevents concurrent processing of the same message.

- **Type**: `int`
- **Default**: `30`
- **Constraints**: Must be a positive integer.

### `ClockSkewToleranceSeconds`
Maximum acceptable difference (in seconds) between system clocks when validating message timestamps or ordering.

- **Type**: `int`
- **Default**: `10`
- **Constraints**: Must be a non-negative integer.

### `Validate()`
Validates the current configuration and returns a collection of validation results indicating any issues.

- **Returns**: `IEnumerable<ValidationResult>`
- **Behavior**: Checks constraints such as `BatchSize > 0`, `MaxRetryDelaySeconds >= InitialRetryDelaySeconds`, etc. Returns empty collection if all validations pass.

## Usage

### Example 1: Basic Configuration
