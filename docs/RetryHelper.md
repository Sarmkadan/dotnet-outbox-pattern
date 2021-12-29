# RetryHelper

A utility class providing configurable retry mechanisms with exponential backoff, linear backoff, fixed delay, and jittered backoff strategies for transient fault handling in .NET applications.

## API

### `ExecuteWithExponentialBackoffAsync<T>`
Executes the provided action with an exponential backoff retry strategy. The delay between retries grows exponentially based on the retry count.

- **Parameters**:
  - `action` (`Func<Task<T>>`): The asynchronous operation to execute.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.
- **Returns**: `Task<T>`: The result of the executed action.
- **Throws**: `ArgumentNullException` if `action` is `null`.
- **Remarks**: Uses `BackoffMultiplier` and `MaxDelayMs` to compute delays.

### `ExecuteWithFixedDelayAsync<T>`
Executes the provided action with a fixed delay between retries.

- **Parameters**:
  - `action` (`Func<Task<T>>`): The asynchronous operation to execute.
  - `delayMs` (`int`): The fixed delay in milliseconds between retries.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.
- **Returns**: `Task<T>`: The result of the executed action.
- **Throws**: `ArgumentNullException` if `action` is `null`; `ArgumentOutOfRangeException` if `delayMs` is negative.
- **Remarks**: Retries immediately if the delay is zero.

### `ExecuteWithLinearBackoffAsync<T>`
Executes the provided action with a linear backoff retry strategy. The delay increases linearly with each retry.

- **Parameters**:
  - `action` (`Func<Task<T>>`): The asynchronous operation to execute.
  - `initialDelayMs` (`int`): The initial delay in milliseconds before the first retry.
  - `incrementMs` (`int`): The additional delay in milliseconds added per retry.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.
- **Returns**: `Task<T>`: The result of the executed action.
- **Throws**: `ArgumentNullException` if `action` is `null`; `ArgumentOutOfRangeException` if `initialDelayMs` or `incrementMs` is negative.
- **Remarks**: The delay for the nth retry is `initialDelayMs + (n * incrementMs)`.

### `ExecuteWithJitteredBackoffAsync<T>`
Executes the provided action with a jittered exponential backoff retry strategy. Adds randomness to exponential backoff to avoid thundering herds.

- **Parameters**:
  - `action` (`Func<Task<T>>`): The asynchronous operation to execute.
  - `maxDelayMs` (`int`): The maximum delay in milliseconds allowed between retries.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.
- **Returns**: `Task<T>`: The result of the executed action.
- **Throws**: `ArgumentNullException` if `action` is `null`; `ArgumentOutOfRangeException` if `maxDelayMs` is negative.
- **Remarks**: Uses `BackoffMultiplier` and applies jitter within `[0.5, 1.5]` of the computed delay.

### `IsTransientError`
Determines whether a given exception should be considered transient and eligible for retry.

- **Parameters**:
  - `ex` (`Exception`): The exception to evaluate.
- **Returns**: `bool`: `true` if the exception is transient; otherwise, `false`.
- **Remarks**: Considers exceptions matching `RetryPolicy` transient error types as retryable.

### `CreatePolicy`
Creates a `RetryPolicy` instance configured with the current retry settings.

- **Parameters**: None.
- **Returns**: `RetryPolicy`: A new policy instance using the current `Strategy`, `MaxRetries`, `InitialDelayMs`, `MaxDelayMs`, and `BackoffMultiplier`.
- **Remarks**: The policy can be reused across multiple retry operations.

### `MaxRetries`
Gets or sets the maximum number of retry attempts.

- **Type**: `int`
- **Default**: `3`
- **Remarks**: Must be non-negative. Affects all retry strategies.

### `Strategy`
Gets or sets the retry strategy to use.

- **Type**: `RetryStrategy`
- **Default**: `RetryStrategy.Exponential`
- **Remarks**: Changing this affects subsequent retry operations.

### `InitialDelayMs`
Gets or sets the initial delay in milliseconds for exponential and linear backoff.

- **Type**: `int`
- **Default**: `100`
- **Remarks**: Must be non-negative. Used by exponential and linear strategies.

### `MaxDelayMs`
Gets or sets the maximum delay in milliseconds for exponential and jittered backoff.

- **Type**: `int`
- **Default**: `30000` (30 seconds)
- **Remarks**: Must be non-negative and greater than or equal to `InitialDelayMs`.

### `BackoffMultiplier`
Gets or sets the multiplier used to compute exponential backoff delays.

- **Type**: `double`
- **Default**: `2.0`
- **Remarks**: Must be greater than `1.0`. Affects exponential growth rate.

### `ExecuteAsync<T>`
Executes the provided action using the configured retry strategy and settings.

- **Parameters**:
  - `action` (`Func<Task<T>>`): The asynchronous operation to execute.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.
- **Returns**: `Task<T>`: The result of the executed action.
- **Throws**: `ArgumentNullException` if `action` is `null`; `RetryFailedException` if all retries are exhausted.
- **Remarks**: Uses the current `Strategy`, `MaxRetries`, and delay configuration.

## Usage

### Example 1: HTTP Request with Exponential Backoff
