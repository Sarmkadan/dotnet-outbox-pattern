# OutboxProcessorOptions
The `OutboxProcessorOptions` type in the `dotnet-outbox-pattern` project is used to configure the behavior of the outbox processor, which is responsible for processing messages in the outbox. This configuration type allows developers to fine-tune the performance, reliability, and ordering of message processing, making it a crucial component in distributed systems and event-driven architectures.

## API
* `public bool Enabled`: A boolean indicating whether the outbox processor is enabled. This property does not throw any exceptions.
* `public int BatchSize`: The number of messages to process in a single batch. This property does not throw any exceptions.
* `public int DelayBetweenBatches`: The delay in milliseconds between processing batches of messages. This property does not throw any exceptions.
* `public int CheckExpiredLocksInterval`: The interval in milliseconds at which to check for expired locks. This property does not throw any exceptions.
* `public int LockDurationSeconds`: The duration in seconds for which a lock is held. This property does not throw any exceptions.
* `public bool PreservePartitionOrdering`: A boolean indicating whether to preserve the ordering of messages within a partition. This property does not throw any exceptions.
* `public int OldestMessageAgeThresholdMinutes`: The age threshold in minutes for the oldest message to be considered for processing. This property does not throw any exceptions.
* `public OutboxProcessor`: The outbox processor instance associated with these options. This property does not throw any exceptions.
* `public HealthMetrics GetHealth`: Returns the health metrics of the outbox processor. This method does not throw any exceptions.

## Usage
The following examples demonstrate how to use the `OutboxProcessorOptions` type:
```csharp
// Example 1: Basic configuration
var options = new OutboxProcessorOptions
{
    Enabled = true,
    BatchSize = 100,
    DelayBetweenBatches = 500,
    PreservePartitionOrdering = true
};

// Example 2: Advanced configuration with custom lock duration and oldest message age threshold
var advancedOptions = new OutboxProcessorOptions
{
    Enabled = true,
    BatchSize = 50,
    DelayBetweenBatches = 1000,
    LockDurationSeconds = 30,
    OldestMessageAgeThresholdMinutes = 10,
    PreservePartitionOrdering = false
};
```

## Notes
When using the `OutboxProcessorOptions` type, consider the following edge cases and thread-safety remarks:
* The `BatchSize` and `DelayBetweenBatches` properties can significantly impact the performance of the outbox processor. Large batch sizes can lead to increased memory usage, while small batch sizes can result in increased overhead due to frequent processing.
* The `LockDurationSeconds` property should be carefully chosen to balance between preventing concurrent processing of the same message and avoiding unnecessary delays.
* The `OldestMessageAgeThresholdMinutes` property can help prevent the outbox processor from processing very old messages, which may no longer be relevant.
* The `PreservePartitionOrdering` property is crucial in maintaining the correct order of messages within a partition, especially in systems where message ordering is critical.
* The `GetHealth` method provides valuable insights into the health of the outbox processor, allowing developers to monitor and troubleshoot issues.
* The `OutboxProcessorOptions` type is designed to be thread-safe, allowing multiple threads to access and modify the configuration without fear of data corruption or other concurrency-related issues. However, it is still important to follow standard threading best practices when working with this type.
