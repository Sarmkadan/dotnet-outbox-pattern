# BatchProcessingBenchmarks

A benchmarking harness for evaluating the performance characteristics of batch processing operations within the Outbox Pattern implementation. This class measures throughput, latency, and resource utilization when processing pending messages in configurable batch sizes, enabling data-driven tuning of batch-related parameters.

## API

### `public int BatchSize`
Gets or sets the number of messages to include in each processing batch. Larger values may improve throughput but increase latency and memory usage. Must be a positive integer.

### `public void Setup()`
Initializes the benchmark environment prior to execution. Creates required test infrastructure, seeds the message store with pending messages, and configures the outbox processor. Throws `InvalidOperationException` if the environment cannot be prepared (e.g., storage unavailable, insufficient permissions).

### `public void Cleanup()`
Releases resources and resets the benchmark environment to a clean state. Removes seeded data and stops any active processors. Safe to call even if `Setup()` has not been invoked.

### `public void Dispose()`
Releases all managed and unmanaged resources held by the benchmark instance. Ensures no background tasks or file handles remain open. Idempotent; may be called multiple times without error.

### `public async Task ProcessPendingMessages()`
Processes all pending messages in the outbox in batches of size `BatchSize`. Returns when the queue is empty or an unrecoverable error occurs. Throws `OperationCanceledException` if cancellation is requested via the linked `CancellationToken`. Throws `InvalidOperationException` if the outbox is not in a valid state for processing.

### `public async Task ProcessPartitionMessages()`
Processes messages from a specific partition in batches of size `BatchSize`. Enables benchmarking of partitioned outbox scenarios. Returns when the partition is empty or an unrecoverable error occurs. Accepts a `partitionKey` parameter identifying the partition to process. Throws `ArgumentException` if `partitionKey` is null or empty. Throws `OperationCanceledException` if cancellation is requested. Throws `InvalidOperationException` if the partition does not exist or is not ready for processing.

## Usage
