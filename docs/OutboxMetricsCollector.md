# OutboxMetricsCollector
The `OutboxMetricsCollector` class is designed to collect and analyze metrics related to the outbox pattern in a .NET application. It provides functionality to collect metrics, perform health checks, and alert when certain conditions are met. This class is a crucial component in monitoring and maintaining the health of an application that utilizes the outbox pattern.

## API
* `public OutboxMetricsCollector`: The constructor for the `OutboxMetricsCollector` class.
* `public async Task CollectMetricsAsync`: Collects metrics related to the outbox pattern. This method does not take any parameters and does not return a value. It may throw exceptions if there are issues collecting metrics.
* `public async Task<string> GetDetailedMetricsAsync`: Retrieves detailed metrics related to the outbox pattern. This method does not take any parameters and returns a string containing the detailed metrics. It may throw exceptions if there are issues retrieving metrics.
* `public OutboxHealthCheck`: A property that provides access to the `OutboxHealthCheck` instance associated with this collector.
* `public async Task<(int statusCode, string message)> CheckHealthAsync`: Performs a health check on the outbox pattern. This method does not take any parameters and returns a tuple containing a status code and a message. It may throw exceptions if there are issues performing the health check.
* `public async Task<bool> IsReadyAsync`: Checks if the outbox pattern is ready for use. This method does not take any parameters and returns a boolean indicating whether the outbox pattern is ready. It may throw exceptions if there are issues checking readiness.
* `public bool IsAlive`: A property that indicates whether the outbox pattern is currently alive.
* `public OutboxAlertingService`: A property that provides access to the `OutboxAlertingService` instance associated with this collector.
* `public async Task CheckAndAlertAsync`: Checks the outbox pattern and alerts if necessary. This method does not take any parameters and does not return a value. It may throw exceptions if there are issues checking or alerting.
* `public enum AlertLevel`: An enumeration of possible alert levels.
* `public int MaxPendingMessages`: A property that gets or sets the maximum number of pending messages.
* `public int MaxDlqMessages`: A property that gets or sets the maximum number of dead-letter queue messages.
* `public int MaxRetries`: A property that gets or sets the maximum number of retries.
* `public int StaleProcessingSeconds`: A property that gets or sets the number of seconds after which processing is considered stale.
* `public double MaxSuccessRateDecrease`: A property that gets or sets the maximum allowed decrease in success rate.
* `public double MaxFailureRate`: A property that gets or sets the maximum allowed failure rate.
* `public MetricsCollectionJob`: A property that provides access to the `MetricsCollectionJob` instance associated with this collector.
* `public async Task RunAsync`: Runs the metrics collection job. This method does not take any parameters and does not return a value. It may throw exceptions if there are issues running the job.
* `public static void Configure`: Configures the `OutboxMetricsCollector` instance.
* `public static async Task Main`: The main entry point for the `OutboxMetricsCollector` class.

## Usage
The following example demonstrates how to use the `OutboxMetricsCollector` class to collect metrics and perform a health check:
```csharp
var collector = new OutboxMetricsCollector();
await collector.CollectMetricsAsync();
var healthCheckResult = await collector.CheckHealthAsync();
if (healthCheckResult.statusCode != 200)
{
    Console.WriteLine($"Health check failed: {healthCheckResult.message}");
}
```
Another example shows how to use the `OutboxMetricsCollector` class to check if the outbox pattern is ready and alert if necessary:
```csharp
var collector = new OutboxMetricsCollector();
var isReady = await collector.IsReadyAsync();
if (!isReady)
{
    await collector.CheckAndAlertAsync();
}
```

## Notes
The `OutboxMetricsCollector` class is designed to be thread-safe, allowing it to be safely used in concurrent environments. However, it is still important to ensure that the instance is properly configured and initialized before use. Additionally, the `CollectMetricsAsync` and `CheckHealthAsync` methods may throw exceptions if there are issues collecting metrics or performing the health check, respectively. These exceptions should be caught and handled accordingly to prevent application crashes. The `MaxPendingMessages`, `MaxDlqMessages`, `MaxRetries`, `StaleProcessingSeconds`, `MaxSuccessRateDecrease`, and `MaxFailureRate` properties can be adjusted to fine-tune the behavior of the `OutboxMetricsCollector` instance.
