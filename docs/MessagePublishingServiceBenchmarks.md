# MessagePublishingServiceBenchmarks

Benchmarking harness for evaluating the performance characteristics of message publishing operations in the outbox pattern implementation. This class measures throughput and latency under controlled conditions for various publishing scenarios, including batch and single-message operations.

## API

### `void Setup()`

Initializes the benchmark environment before each test run. Configures dependencies, test data, and any required state for accurate measurement. This method is called automatically by the benchmarking framework prior to each benchmark invocation.

- **Parameters**: None
- **Return value**: None
- **Throws**: May throw if initialization fails (e.g., dependency resolution, test data setup)

---

### `void Cleanup()`

Releases resources and resets state after each benchmark completes. Cleans up test data, disposes temporary objects, and ensures isolation between benchmark runs. Invoked automatically by the framework after each benchmark execution.

- **Parameters**: None
- **Return value**: None
- **Throws**: May throw if cleanup encounters unrecoverable errors

---

### `void Dispose()`

Releases all managed and unmanaged resources held by the benchmarking service. Called when the benchmark runner finalizes execution to ensure proper cleanup of heavy resources (e.g., database connections, file handles).

- **Parameters**: None
- **Return value**: None
- **Throws**: None

---

### `public async Task ProcessPendingMessages_Batch100()`

Benchmarks the processing of 100 pending outbox messages in a single batch. Measures end-to-end latency and throughput for bulk message publishing operations, simulating high-volume scenarios typical in outbox pattern usage.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Propagates exceptions from underlying publishing or persistence operations

---

### `public async Task ProcessPartition_Batch100()`

Benchmarks the processing of 100 messages within a single partition. Evaluates partitioning behavior and its impact on performance, particularly in systems where message ordering or sharding is required.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Propagates exceptions from partitioning logic or publishing pipeline

---
### `public async Task ProcessSingleMessage()`

Benchmarks the publishing of a single message through the outbox pattern. Measures per-message overhead, latency, and resource utilization under low-volume conditions.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Propagates exceptions from message serialization or publishing

---
### `public Task PublishAsync`

Benchmarks the core `PublishAsync` method of the outbox service. Measures raw publishing throughput and latency without additional processing overhead (e.g., batching, partitioning). This benchmark isolates the performance of the core outbox mechanism.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Propagates exceptions from message serialization or storage operations

## Usage

### Example 1: Running Benchmarks via BenchmarkDotNet
