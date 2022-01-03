# OutboxRepositoryBenchmarksExtensions

Extension methods providing benchmarking scenarios for `IOutboxRepository` implementations. These methods simulate common repository operations under load, enabling performance comparisons between different storage backends or configurations. Each method returns a `Task` to allow asynchronous benchmarking with tools like BenchmarkDotNet.

## API

### `AddMultipleMessages_Batch100`
Benchmarks the insertion of 100 messages in a single batch. Measures throughput and latency for bulk operations, useful for evaluating write performance under moderate load. No parameters are required; the method internally generates 100 unique messages. Returns a `Task` representing the asynchronous operation. Throws `ArgumentNullException` if the repository instance is `null`.

### `AddMessages_DifferentPartitions`
Benchmarks the insertion of messages across multiple partitions. Each message is assigned to a distinct partition key, simulating real-world workloads where messages are distributed across logical shards. Returns a `Task`. Throws `ArgumentNullException` if the repository instance is `null`.

### `GetPendingMessages_Limited50`
Benchmarks retrieval of up to 50 pending messages from the outbox. Measures read performance for small, bounded queries. Returns a `Task` containing the collection of retrieved messages. Throws `ArgumentNullException` if the repository instance is `null`.

### `GetPendingMessagesByPartition_Limited50`
Benchmarks retrieval of up to 50 pending messages from a specific partition. Useful for evaluating partition-aware read performance. Returns a `Task` containing the collection of retrieved messages. Throws `ArgumentNullException` if the repository instance is `null`.

### `GetPendingCount_LargeDataset`
Benchmarks retrieval of the total count of pending messages in a large dataset. Measures efficiency of count operations on large tables or collections. Returns a `Task` containing the count. Throws `ArgumentNullException` if the repository instance is `null`.

### `GetStatistics_WithData`
Benchmarks retrieval of repository statistics (e.g., total messages, pending count, processed count) when data is present. Useful for evaluating metadata query performance. Returns a `Task` containing the statistics object. Throws `ArgumentNullException` if the repository instance is `null`.

### `GetPendingMessages_MultiplePartitions`
Benchmarks retrieval of pending messages across multiple partitions simultaneously. Simulates a workload where consumers read from several partitions in parallel. Returns a `Task` containing the collection of retrieved messages. Throws `ArgumentNullException` if the repository instance is `null`.

### `AddLargeDataset_1000Messages`
Benchmarks insertion of 1,000 messages in a single operation. Measures high-volume write performance and potential bottlenecks. Returns a `Task`. Throws `ArgumentNullException` if the repository instance is `null`.

### `GetPendingMessages_AfterLargeInsert`
Benchmarks retrieval of pending messages immediately after a large insert operation. Useful for evaluating read-after-write consistency and performance under contention. Returns a `Task` containing the collection of retrieved messages. Throws `ArgumentNullException` if the repository instance is `null`.

### `GetStatistics_AfterLargeInsert`
Benchmarks retrieval of repository statistics immediately after a large insert operation. Measures metadata query performance under write load. Returns a `Task` containing the statistics object. Throws `ArgumentNullException` if the repository instance is `null`.

## Usage
