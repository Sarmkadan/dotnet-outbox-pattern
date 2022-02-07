// existing content ...

## BatchProcessingBenchmarks

The `BatchProcessingBenchmarks` class provides a set of performance benchmarks for the outbox message processing service. It sets up an in-memory database, pre-loads a batch of messages, and measures the time taken to process pending messages or process a specific partition. The benchmarks are intended to help developers understand the throughput and latency characteristics of the outbox implementation under realistic workloads.

### Example

```csharp
using DotnetOutboxPattern.Benchmarks;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var benchmarks = new BatchProcessingBenchmarks { BatchSize = 100 };

        // Prepare the benchmark environment
        benchmarks.Setup();

        // Run the benchmark methods
        await benchmarks.ProcessPendingMessages();
        await benchmarks.ProcessPartitionMessages();

        // Clean up resources
        benchmarks.Cleanup();
        benchmarks.Dispose();
    }
}
```
