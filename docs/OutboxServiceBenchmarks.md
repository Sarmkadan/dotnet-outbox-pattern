# OutboxServiceBenchmarks

Benchmark suite for measuring the performance characteristics of the `OutboxService` implementation in the `dotnet-outbox-pattern` project. These benchmarks evaluate the latency, throughput, and resource utilization of publishing events to an outbox and retrieving them for dispatch.

## API

### `Setup`

Initializes the benchmark environment before each test run. Sets up the required infrastructure (e.g., database, message broker, or in-memory components) and seeds any prerequisite data.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: May throw if initialization fails (e.g., connection errors, invalid configuration).

---

### `Cleanup`

Cleans up the benchmark environment after each test run. Releases resources, rolls back transactions, and resets the system to a clean state.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: May throw if cleanup fails (e.g., connection leaks, unmanaged resource errors).

---
### `Dispose`

Releases all resources held by the benchmark instance. Typically called once per benchmark class lifecycle.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: None (should handle cleanup gracefully).

---
### `PublishSingleEvent`

Benchmarks the time taken to publish a single event to the outbox. Measures the latency of the `OutboxService.Publish` operation for one message.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: May throw if the event cannot be serialized, persisted, or if the outbox is unavailable.

---
### `PublishMultipleEvents_Sequential`

Benchmarks the time taken to publish multiple events sequentially to the outbox. Measures the cumulative latency of `OutboxService.Publish` for a batch of messages processed one after another.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: May throw if any event in the sequence fails to publish (e.g., serialization error, database constraint violation).

---
### `GetStatistics`

Benchmarks the retrieval and aggregation of outbox statistics (e.g., total messages, pending count, oldest message age). Measures the read performance of the statistics endpoint.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: May throw if the statistics store is unavailable or if the query fails.

---
### `GetMessageById`

Benchmarks the lookup of a specific message by its unique identifier. Measures the latency of retrieving a single message from the outbox.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: May throw if the message ID does not exist or if the lookup fails (e.g., database error).

## Usage

### Example 1: Basic Benchmark Run
