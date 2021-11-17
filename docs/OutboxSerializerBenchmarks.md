# OutboxSerializerBenchmarks

The `OutboxSerializerBenchmarks` class serves as a dedicated harness for performance testing of serialization and deserialization operations within the outbox pattern implementation. It encapsulates the logic required to measure the throughput and latency of converting domain events to their wire formats and back, specifically targeting scenarios involving standard and large payload sizes to identify bottlenecks in the messaging pipeline.

## API

### `public OutboxSerializerBenchmarks()`
Initializes a new instance of the `OutboxSerializerBenchmarks` class. This constructor prepares the necessary internal state and dependencies required to execute serialization benchmarks. It does not accept parameters and does not throw exceptions under normal operating conditions.

### `public string SerializeEvent()`
Executes the serialization of a standard-sized event object into its string representation.
*   **Purpose**: Measures the time and resource cost of converting a typical domain event to a format suitable for storage in the outbox.
*   **Parameters**: None.
*   **Return Value**: A `string` containing the serialized data.
*   **Throws**: May throw serialization-specific exceptions if the internal event model is invalid or if the underlying serializer encounters an error, though specific exception types depend on the configured serializer implementation.

### `public PublishableEvent DeserializeEvent()`
Executes the deserialization of a previously serialized event string back into a strongly-typed event object.
*   **Purpose**: Measures the performance of reconstructing domain objects from their stored string representations.
*   **Parameters**: None.
*   **Return Value**: A `PublishableEvent` instance representing the deserialized data.
*   **Throws**: May throw format exceptions or deserialization errors if the internal state contains malformed data or if the schema has changed incompatibly.

### `public string SerializeLargeEvent()`
Executes the serialization of an event object containing a significantly larger payload than the standard event.
*   **Purpose**: Evaluates serializer performance and memory allocation behavior under stress conditions with large data volumes, simulating edge cases in production traffic.
*   **Parameters**: None.
*   **Return Value**: A `string` containing the serialized large event data.
*   **Throws**: May throw `OutOfMemoryException` if the payload exceeds available heap space, or standard serialization exceptions if the large object graph is invalid.

## Usage

The following examples demonstrate how to instantiate the benchmark class and invoke its methods to measure specific serialization paths.

**Example 1: Benchmarking Standard Event Throughput**
This example shows how to run the standard serialization and deserialization cycle, typically used within a benchmarking framework loop to calculate operations per second.

```csharp
var benchmarks = new OutboxSerializerBenchmarks();

// Simulate a single iteration of the benchmark loop
string serializedData = benchmarks.SerializeEvent();
PublishableEvent reconstructedEvent = benchmarks.DeserializeEvent();

// Validation logic would typically follow here to ensure correctness
if (reconstructedEvent == null)
{
    throw new InvalidOperationException("Deserialization failed to produce a valid event.");
}
```

**Example 2: Stress Testing with Large Payloads**
This example focuses on the large event serialization path, useful for identifying memory pressure or latency spikes when handling bulky messages.

```csharp
var benchmarks = new OutboxSerializerBenchmarks();

// Execute the large payload serialization
string largePayload = benchmarks.SerializeLargeEvent();

// Analyze payload size or log metrics
Console.WriteLine($"Generated large payload size: {largePayload.Length} bytes");
```

## Notes

*   **Thread Safety**: The `OutboxSerializerBenchmarks` class is not guaranteed to be thread-safe. Instances should not be shared across multiple threads concurrently unless external synchronization is applied. Benchmarking frameworks typically manage instance lifecycle to ensure isolated execution contexts.
*   **State Dependency**: The `DeserializeEvent` method relies on the state established by prior calls (implicitly or explicitly) within the benchmark harness. Calling `DeserializeEvent` without a preceding serialization step in a fresh instance may result in undefined behavior or exceptions if the internal buffer is uninitialized.
*   **Memory Allocation**: The `SerializeLargeEvent` method is designed to trigger significant garbage collection activity. When integrating this class into long-running performance tests, monitor GC pressure to distinguish between serializer inefficiencies and general heap fragmentation.
*   **Exception Handling**: As this class is intended for benchmarking, it does not wrap internal exceptions. Callers must handle potential `FormatException`, `InvalidOperationException`, or serializer-specific errors that arise from data corruption or configuration mismatches.
