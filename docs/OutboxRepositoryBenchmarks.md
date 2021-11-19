# OutboxRepositoryBenchmarks

A benchmark suite for measuring the performance and behavior of outbox repository implementations in the `dotnet-outbox-pattern` project. The class evaluates core operations such as message insertion, retrieval with and without partitioning, statistics gathering, and lock contention checks under simulated workloads.

## API

### `public void Setup()`

Initializes the benchmark environment before each test run. Sets up in-memory or test database contexts, seeds required data, and configures any mock services or dependencies needed for consistent benchmarking. Does not return a value and throws only if critical initialization steps fail (e.g., database connection cannot be established).

### `public void Cleanup()`

Releases resources and cleans up state after each benchmark completes. Typically rolls back transactions, disposes test contexts, and resets shared state to ensure test isolation. Does not return a value and throws only if cleanup operations encounter unrecoverable errors (e.g., failed transaction rollback).

### `public void Dispose()`

Releases all managed and unmanaged resources held by the benchmark instance. Ensures no resource leaks between benchmark runs or test sessions. Does not return a value and throws only if disposal of a critical resource fails (e.g., connection pool cannot be cleared).

### `public async Task AddSingleMessage()`

Measures the time to persist a single outbox message into the repository. Used to evaluate baseline write performance and serialization overhead. Accepts no parameters and returns a `Task`. Throws if message persistence fails (e.g., database constraint violation or serialization error).

### `public async Task GetPendingMessages_Batch100()`

Retrieves up to 100 pending outbox messages in a single batch. Used to assess read throughput and query efficiency under moderate load. Accepts no parameters and returns a `Task`. Throws if the query execution fails or if the underlying repository throws during retrieval.

### `public async Task GetPendingMessagesByPartition_Batch100()`

Retrieves up to 100 pending outbox messages filtered by a partition key. Used to evaluate partitioned query performance and scalability. Accepts no parameters and returns a `Task`. Throws if the partition-based query fails or if the partition key is invalid.

### `public async Task GetPendingMessages_WithLockCheck()`

Retrieves pending messages while performing an optimistic or pessimistic lock check (e.g., `SELECT ... FOR UPDATE`). Used to measure contention and locking overhead in high-concurrency scenarios. Accepts no parameters and returns a `Task`. Throws if the lock acquisition fails or if the query times out due to blocking.

### `public async Task GetStatistics()`

Retrieves aggregate statistics about the outbox state (e.g., total messages, pending count, processed count). Used to evaluate monitoring and observability overhead. Accepts no parameters and returns a `Task`. Throws if the statistics query fails or if the repository is in an inconsistent state.

### `public async Task GetPendingCount()`

Retrieves the total number of pending outbox messages. Used to assess lightweight read performance and system state queries. Accepts no parameters and returns a `Task`. Throws if the count query fails or if the repository is unavailable.

## Usage
