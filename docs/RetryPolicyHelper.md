# RetryPolicyHelper

The `RetryPolicyHelper` class provides utility functionality for managing, calculating, and analyzing retry strategies within the `dotnet-outbox-pattern` framework. It enables developers to determine appropriate delay intervals based on configured `RetryPolicyType` settings and provides comprehensive analytical metrics for tracking retry performance across operations.

## API

### Methods

*   **`public static TimeSpan CalculateDelay(RetryPolicyType policy, int attempt)`**
    Calculates the backoff duration for a specific retry attempt based on the defined `RetryPolicyType`.
    *   **Parameters:** 
        *   `policy` (`RetryPolicyType`): The retry strategy to apply.
        *   `attempt` (`int`): The current attempt index (zero-based).
    *   **Returns:** A `TimeSpan` representing the delay before the next retry.
    *   **Exceptions:** Throws `ArgumentOutOfRangeException` if `attempt` is less than zero.

*   **`public static RetryStatistics CalculateStatistics(IEnumerable<TimeSpan> retryDelays)`**
    Processes a collection of observed retry delays to generate a summary of performance metrics.
    *   **Parameters:**
        *   `retryDelays` (`IEnumerable<TimeSpan>`): A sequence of durations representing past retry intervals.
    *   **Returns:** A `RetryStatistics` object containing calculated metrics.
    *   **Exceptions:** Throws `ArgumentNullException` if `retryDelays` is null.

### Properties

*   **`public RetryPolicyType RetryPolicy { get; set; }`**
    Gets or sets the current `RetryPolicyType` associated with the helper instance.
*   **`public int MaxAttempts { get; set; }`**
    Gets or sets the maximum number of allowed retry attempts.
*   **`public int TotalRetries { get; }`**
    Gets the total number of retries performed.
*   **`public TimeSpan TotalDelayTime { get; }`**
    Gets the cumulative duration spent in retry delays.
*   **`public TimeSpan AverageRetryDelay { get; }`**
    Gets the average duration of the recorded retry delays.
*   **`public TimeSpan MaxRetryDelay { get; }`**
    Gets the longest single duration among the recorded retry delays.

## Usage

### Example 1: Calculating a Retry Delay
This example demonstrates how to use the static `CalculateDelay` method to determine the next wait interval based on an exponential backoff policy.

```csharp
var policy = RetryPolicyType.Exponential;
int attempt = 3;

TimeSpan delay = RetryPolicyHelper.CalculateDelay(policy, attempt);
Console.WriteLine($"Delay for attempt {attempt}: {delay.TotalMilliseconds}ms");
```

### Example 2: Analyzing Retry Statistics
This example shows how to aggregate historical retry data into a `RetryStatistics` object.

```csharp
var historicalDelays = new List<TimeSpan> 
{ 
    TimeSpan.FromSeconds(1), 
    TimeSpan.FromSeconds(2), 
    TimeSpan.FromSeconds(4) 
};

RetryStatistics stats = RetryPolicyHelper.CalculateStatistics(historicalDelays);
Console.WriteLine($"Average delay: {stats.AverageRetryDelay.TotalSeconds}s");
Console.WriteLine($"Max delay: {stats.MaxRetryDelay.TotalSeconds}s");
```

## Notes

*   **Thread Safety:** The static methods `CalculateDelay` and `CalculateStatistics` are thread-safe as they rely on provided input parameters and do not maintain internal mutable state. Instance properties of `RetryPolicyHelper` are not inherently thread-safe; concurrent access for reading and writing should be synchronized externally if the instance is shared across threads.
*   **Edge Cases:**
    *   Passing an `attempt` value that exceeds the intended logic of a specific `RetryPolicyType` may result in excessively large `TimeSpan` values.
    *   If `CalculateStatistics` is called with an empty collection of delays, the returned `RetryStatistics` object will contain default values (e.g., `TimeSpan.Zero`).
