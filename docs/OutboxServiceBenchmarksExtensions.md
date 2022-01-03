# OutboxServiceBenchmarksExtensions

This static class provides a set of asynchronous extension methods designed for benchmarking the `OutboxService`. The methods enable parallel publishing of multiple events, batch publishing of events, collection of detailed runtime statistics, and waiting for asynchronous processing to finish, facilitating realistic performance tests in a controlled environment.

## API

### PublishMultipleEvents_Parallel
**Purpose:** Publishes a specified number of events concurrently to the outbox, simulating high‑load publish scenarios.  
**Parameters:**  
- `service` – The `OutboxService` instance to publish to.  
- `eventCount` – The total number of events to publish. Must be greater than zero.  
- `cancellationToken` (optional) – A token to observe cancellation requests.  
**Return Value:** A `Task` that completes when all publish operations have finished.  
**Exceptions:**  
- `ArgumentNullException` if `service` is `null`.  
- `ArgumentOutOfRangeException` if `eventCount` is less than or equal to zero.  
- `OperationCanceledException` if the cancellation token is triggered.  
- Any exception thrown by the underlying publish mechanism is propagated.

### PublishEventBatch
**Purpose:** Publishes a predefined collection of events as a single batch, useful for measuring batch‑processing throughput.  
**Parameters:**  
- `service` – The `OutboxService` instance to publish to.  
- `events` – A read‑only collection of `OutboxEvent` objects to be published. The collection must not be `null` and must contain at least one element.  
- `cancellationToken` (optional) – A token to observe cancellation requests.  
**Return Value:** A `Task` that completes when the batch has been submitted for processing.  
**Exceptions:**  
- `ArgumentNullException` if `service` or `events` is `null`.  
- `ArgumentException` if `events` is empty.  
- `OperationCanceledException` if the cancellation token is triggered.  
- Propagates any exceptions from the publish pipeline.

### GetDetailedStatistics
**Purpose:** Retrieves a dictionary of detailed runtime statistics from the outbox, such as processed event counts, latency metrics, and queue depths.  
**Parameters:**  
- `service` – The `OutboxService` instance to query.  
- `cancellationToken` (optional) – A token to observe cancellation requests.  
**Return Value:** A `Task<Dictionary<string, object>>` where each key is a statistic name and the associated value is the metric (typically numeric or timestamp).  
**Exceptions:**  
- `ArgumentNullException` if `service` is `null`.  
- `OperationCanceledException` if the cancellation token is triggered.  
- Throws if the service does not support statistics collection (e.g., `NotSupportedException`).

### WaitForProcessingCompletion
**Purpose:** Blocks until all previously submitted events have been fully processed by the outbox, optionally respecting a timeout.  
**Parameters:**  
- `service` – The `OutboxService` instance to monitor.  
- `timeout` (optional) – The maximum time to wait for completion; if `null` the method waits indefinitely.  
- `cancellationToken` (optional) – A token to observe cancellation requests.  
**Return Value:** A `Task` that completes when processing is finished, or when the timeout or cancellation occurs.  
**Exceptions:**  
- `ArgumentNullException` if `service` is `null`.  
- `OperationCanceledException` if the cancellation token is triggered.  
- `TimeoutException` if the specified timeout elapses before processing finishes.

## Usage

```csharp
// Example 1: Parallel publish and wait for completion
var outbox = new OutboxService outbox);
int eventCount = 10_000;
await OutboxServiceBenchmarksExtensions.PublishMultipleEvents_Parallel(outbox, eventCount: 5000);
await OutboxServiceBenchmarksExtensions.WaitForProcessingCompletion(outbox);
```

```csharp
// Example 2: Batch publish, then gather statistics
var events = GenerateOutboxEvents(200);
await OutboxServiceBenchmarksExtensions.PublishEventBatch(outbox, events);
var stats = await OutboxServiceBenchmarksExtensions.GetDetailedStatistics(outbox);
Console.WriteLine($"Processed {stats["ProcessedCount"]} events with avg latency {stats["AvgLatencyMs"]} ms");
```

## Notes

- All methods are safe to invoke concurrently on distinct `OutboxService` instances. Simultaneous calls on the same instance without external synchronization may lead to race conditions and undefined behavior.  
- Passing a `null` service argument will always result in an `ArgumentNullException`; callers should validate dependencies before invoking these extensions.  
- Zero or negative `eventCount` values are rejected early to prevent meaningless benchmark runs.  
- The `GetDetailedStatistics` method returns an empty dictionary if the underlying service has not yet collected any metrics (e.g., before any events have been processed).  
- Timeout values supplied to `WaitForProcessingCompletion` should be chosen with consideration for the expected processing latency; excessively short timeouts will frequently throw `TimeoutException`.  
- Cancellation tokens are honored at the earliest feasible check point; if a token is signaled after the method has already begun awaiting internal completion, the method will still observe the cancellation and throw `OperationCanceledException`.  
- The returned statistics dictionary may contain values of varying types (e.g., `long`, `double`, `DateTime`). Consumers should perform appropriate type checks or conversions when interpreting the results.
