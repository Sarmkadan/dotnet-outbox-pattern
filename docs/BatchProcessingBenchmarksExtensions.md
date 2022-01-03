# BatchProcessingBenchmarksExtensions

Provides extension methods for configuring and executing benchmark scenarios related to batch processing in the outbox pattern implementation.

## API

### ConfigureBatchSize
```csharp
public static BatchProcessingBenchmarks ConfigureBatchSize(
    this BatchProcessingBenchmarks benchmarks,
    int batchSize)
```
**Purpose**  
Sets the batch size to be used by the benchmark harness and returns the same instance to allow fluent configuration.

**Parameters**  
- `benchmarks`: The target `BatchProcessingBenchmarks` instance to configure.  
- `batchSize`: The number of items processed per batch. Must be greater than zero.

**Return value**  
The original `benchmarks` instance, enabling method chaining.

**Exceptions**  
- `ArgumentException` if `batchSize` is less than or equal to zero.  
- `InvalidOperationException` if the benchmarks instance has already been started (e.g., after a call to `WarmUpAsync` or `RunAllAsync`).

### WarmUpAsync
```csharp
public static async Task WarmUpAsync(
    this BatchProcessingBenchmarks benchmarks)
```
**Purpose**  
Performs any necessary warm‑up operations (e.g., initializing resources, populating caches) before actual measurement begins.

**Parameters**  
- `benchmarks`: The benchmark instance to warm up.

**Return value**  
A `Task` that completes when the warm‑up process finishes.

**Exceptions**  
- `InvalidOperationException` if the benchmarks have not been configured with a batch size.  
- Any exception thrown by the underlying warm‑up logic (e.g., `IOException` when initializing storage).

### RunAllAsync
```csharp
public static async Task<TimeSpan> RunAllAsync(
    this BatchProcessingBenchmarks benchmarks)
```
**Purpose**  
Executes the full benchmark suite, processing all pending items according to the configured batch size, and measures the total elapsed time.

**Parameters**  
- `benchmarks`: The benchmark instance to run.

**Return value**  
A `TimeSpan` representing the total time taken to complete the benchmark run.

**Exceptions**  
- `InvalidOperationException` if `ConfigureBatchSize` has not been called prior to invocation.  
- `OperationCanceledException` if an external cancellation token triggers cancellation (if the method internally accepts one).  
- Any exception propagated from the benchmark execution (e.g., `InvalidOperationException` from the outbox processor).

### MeasurePendingProcessingAsync
```csharp
public static async Task<TimeSpan> MeasurePendingProcessingAsync(
    this BatchProcessingBenchmarks benchmarks)
```
**Purpose**  
Measures the time required to process only the currently pending items without resetting or re‑queuing work, useful for incremental performance checks.

**Parameters**  
- `benchmarks`: The benchmark instance to measure.

**Return value**  
A `TimeSpan` indicating the duration taken to process the pending batch.

**Exceptions**  
- `InvalidOperationException` if the benchmarks lack a configured batch size or have not been warmed up.  
- Exceptions thrown by the processing logic (e.g., `TimeoutException` if processing exceeds internal limits).

## Usage

```csharp
var benchmarks = new BatchProcessingBenchmarks();

// Fluent configuration
benchmarks = benchmarks.ConfigureBatchSize(batchSize: 100);

// Prepare the benchmark harness
await benchmarks.WarmUpAsync();

// Run the full benchmark and capture elapsed time
TimeSpan total = await benchmarks.RunAllAsync();
Console.WriteLine($"Full run took {total.TotalMilliseconds} ms");
```

```csharp
// After a previous run, measure only the newly pending items
TimeSpan pending = await benchmarks.MeasurePendingProcessingAsync();
Console.WriteLine($"Pending processing took {pending.TotalMilliseconds} ms");

// Re‑configure for a different scenario if needed
benchmarks.ConfigureBatchSize(batchSize: 50);
await benchmarks.WarmUpAsync();
TimeSpan anotherRun = await benchmarks.RunAllAsync();
```

## Notes

- The extension methods are **not thread‑safe**; concurrent calls on the same `BatchProcessingBenchmarks` instance may lead to inconsistent state. Use a separate instance per thread or synchronize access externally.  
- `ConfigureBatchSize` must be called exactly once before any asynchronous operation; subsequent calls after the benchmark has started will throw `InvalidOperationException`.  
- Warm‑up is optional but recommended to eliminate one‑time initialization costs from measurements. Skipping `WarmUpAsync` may result in artificially high first‑run latencies.  
- The returned `TimeSpan` values are **wall‑clock** measurements and include any asynchronous delays introduced by the benchmark harness (e.g., awaiting I/O). They do not guarantee CPU‑time accuracy.  
- If the underlying outbox processor throws during execution, the exception propagates out of the async method; callers should handle it appropriately (e.g., logging or aborting the benchmark run).  
- The methods do not accept a `CancellationToken`; cancellation must be coordinated externally if required.  
- Repeated calls to `MeasurePendingProcessingAsync` without intervening work will measure zero or negligible time, as no pending items remain to process.
