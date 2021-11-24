# RetryPolicyHelperTests

The `RetryPolicyHelperTests` class serves as the comprehensive test suite for validating the delay calculation logic and statistical reporting capabilities of the retry policy helper within the `dotnet-outbox-pattern` project. It ensures that various backoff strategies, including fixed interval, linear, and exponential models, behave correctly under different configurations, while also verifying boundary conditions such as invalid attempt counts, maximum delay caps, and jitter application.

## API

### `CalculateDelay_WithZeroAttempt_ThrowsArgumentException`
Verifies that the delay calculation method throws an `ArgumentException` when invoked with an attempt count of zero, as retry attempts are expected to be 1-indexed.
*   **Parameters**: None (test harness supplies invalid input).
*   **Return Value**: `void` (test passes if exception is thrown).
*   **Throws**: Expects `ArgumentException`.

### `CalculateDelay_WithNegativeAttempt_ThrowsArgumentException`
Ensures that providing a negative integer for the attempt count results in an `ArgumentException`, preventing invalid state progression in retry loops.
*   **Parameters**: None (test harness supplies invalid input).
*   **Return Value**: `void`.
*   **Throws**: Expects `ArgumentException`.

### `CalculateDelay_WithNoRetryPolicy_ReturnsZero`
Validates that when no specific retry policy is configured or applied, the calculated delay is zero, indicating immediate retry or no wait state.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts result equals zero).
*   **Throws**: None.

### `CalculateDelay_WithFixedIntervalPolicy_ReturnsSameDelayEachAttempt`
Confirms that a fixed interval policy returns an identical delay duration regardless of the current attempt number.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts consistency across attempts).
*   **Throws**: None.

### `CalculateDelay_WithLinearBackoffPolicy_IncreasesProperly`
Tests that a linear backoff strategy increases the delay duration by a constant factor proportional to the attempt number.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts linear progression).
*   **Throws**: None.

### `CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases`
Verifies that an exponential backoff policy correctly doubles (or applies the configured exponent to) the delay for each subsequent attempt.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts exponential growth).
*   **Throws**: None.

### `CalculateDelay_RespectMaxDelayLimit`
Ensures that regardless of the backoff strategy or attempt count, the calculated delay never exceeds the configured maximum delay threshold.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts result ≤ max delay).
*   **Throws**: None.

### `CalculateDelay_WithJitterEnabled_AddsRandomness`
Validates that when jitter is enabled, the resulting delay varies between calls for the same attempt, introducing randomness to prevent thundering herd scenarios.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts variance in results).
*   **Throws**: None.

### `CalculateDelay_WithJitterDisabled_ProducesSameDelay`
Confirms that disabling jitter results in deterministic delay calculations, returning the exact same value for identical inputs.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts deterministic results).
*   **Throws**: None.

### `CalculateDelay_ReturnsMinimumOneSecond`
Checks that the system enforces a minimum delay floor of one second, ensuring that even aggressive policies do not result in sub-second retry intervals.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts result ≥ 1 second).
*   **Throws**: None.

### `CalculateStatistics_ReturnsProperValues`
Tests the statistical aggregation method to ensure it returns a valid object containing accurate metrics for total attempts, retries, and timing data.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts object integrity).
*   **Throws**: None.

### `CalculateStatistics_WithOneAttempt_HasZeroRetries`
Verifies edge case logic where a single attempt results in a statistics report indicating zero retries occurred.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts retry count is 0).
*   **Throws**: None.

### `CalculateStatistics_CalculatesAverageCorrectly`
Ensures that the average delay or duration calculated within the statistics object is mathematically correct based on the provided sample data.
*   **Parameters**: None.
*   **Return Value**: `void` (asserts mathematical accuracy).
*   **Throws**: None.

## Usage

The following examples demonstrate how the logic verified by this test suite is typically consumed in a production retry handler scenario.

### Example 1: Implementing a Resilient Database Operation
This example shows how to utilize the calculated delay within a retry loop, relying on the guarantees tested by `CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases` and `CalculateDelay_RespectMaxDelayLimit`.

```csharp
public async Task ExecuteWithRetryAsync(Func<Task> operation, int maxAttempts)
{
    int attempt = 1;
    while (true)
    {
        try
        {
            await operation();
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            // Logic validated by CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases
            var delay = RetryPolicyHelper.CalculateDelay(attempt, policyType: BackoffType.Exponential, maxDelay: TimeSpan.FromMinutes(5));
            
            // Logic validated by CalculateDelay_ReturnsMinimumOneSecond ensures delay >= 1s
            await Task.Delay(delay);
            attempt++;
        }
        catch
        {
            throw;
        }
    }
}
```

### Example 2: Generating Retry Metrics for Monitoring
This example illustrates the collection of retry statistics, leveraging the behaviors verified by `CalculateStatistics_WithOneAttempt_HasZeroRetries` and `CalculateStatistics_CalculatesAverageCorrectly`.

```csharp
public void LogRetryMetrics(List<TimeSpan> executionTimes)
{
    // Generates stats based on collected execution times
    var stats = RetryPolicyHelper.CalculateStatistics(executionTimes);
    
    // Validated by CalculateStatistics_WithOneAttempt_HasZeroRetries
    if (stats.TotalAttempts == 1 && stats.RetryCount != 0)
    {
        throw new InvalidOperationException("Statistics calculation error: Single attempt should yield zero retries.");
    }

    // Validated by CalculateStatistics_CalculatesAverageCorrectly
    Console.WriteLine($"Average Delay: {stats.AverageDelay.TotalMilliseconds}ms");
    Console.WriteLine($"Total Retries: {stats.RetryCount}");
}
```

## Notes

*   **Input Validation**: The implementation strictly enforces positive integers for attempt counts. Calls with zero or negative values will result in an `ArgumentException`, as verified by the corresponding test members.
*   **Determinism vs. Randomness**: When jitter is disabled, the delay calculation is purely deterministic. Enabling jitter introduces non-deterministic behavior suitable for production load distribution but requires careful handling in unit tests that expect exact values.
*   **Boundary Constraints**: The system enforces both a lower bound (minimum 1 second) and an upper bound (configured max delay). Developers should not assume raw backoff formulas apply without these clamps.
*   **Thread Safety**: While the calculation methods themselves appear to be stateless functions based on the test signatures, the `CalculateStatistics` method implies aggregation over a collection. If collecting data from concurrent threads, the input collection passed to `CalculateStatistics` must be thread-safe or synchronized externally before invocation.
*   **Statistics Semantics**: A "retry" is defined strictly as any attempt beyond the first. Therefore, a total attempt count of $N$ results in $N-1$ retries.
