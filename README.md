// existing content ...

## MessagePublishingServiceBenchmarks

The `MessagePublishingServiceBenchmarks` class provides a set of performance benchmarks for the outbox message publishing service. It sets up an in‑memory database, pre‑loads a batch of messages, and measures the time taken to process pending messages, process a specific partition, or publish a single message. The benchmarks are intended to help developers understand the throughput and latency characteristics of the outbox implementation under realistic workloads.

### Example

```csharp
using DotnetOutboxPattern.Benchmarks;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var benchmarks = new MessagePublishingServiceBenchmarks();

        // Prepare the benchmark environment
        benchmarks.Setup();

        // Run the benchmark methods
        await benchmarks.ProcessPendingMessages_Batch100();
        await benchmarks.ProcessPartition_Batch100();
        await benchmarks.ProcessSingleMessage();

        // Clean up resources
        benchmarks.Cleanup();
        benchmarks.Dispose();
    }
}
```
