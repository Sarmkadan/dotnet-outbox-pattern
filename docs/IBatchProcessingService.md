# IBatchProcessingService

The `IBatchProcessingService` interface defines the contract for processing outbox messages in manageable chunks, supporting both immediate and scheduled execution patterns. Implementations are responsible for retrieving messages from the outbox, applying business logic, and persisting processing results while providing a summary of the operation.

## API

### BatchProcessingService()
Initializes a new instance of the class that implements `IBatchProcessingService`.  
- **Parameters:** None.  
- **Return value:** A ready‑to‑use service instance.  
- **Exceptions:**  
  - `ArgumentNullException` – if any required dependency supplied via dependency injection is null.  
  - `InvalidOperationException` – if the service cannot be initialized due to missing configuration.

### Task<BatchProcessingSummary> ProcessInChunksAsync()
Processes outbox messages in chunks without using the `async` keyword on the method itself.  
- **Parameters:** None.  
- **Return value:** A `Task` that completes with a `BatchProcessingSummary` containing counts of processed, succeeded, and failed messages.  
- **Exceptions:**  
  - `OperationCanceledException` – if a cancellation token associated with the operation is triggered.  
  - `InvalidOperationException` – if the service has not been properly started.  
  - `IOException` – if an error occurs while reading from or writing to the outbox store.

### async Task<BatchProcessingSummary> ProcessInChunksAsync()
Asynchronously processes outbox messages in chunks, allowing the caller to `await` the operation.  
- **Parameters:** None.  
- **Return value:** A `Task<BatchProcessingSummary>` that yields a summary when completed.  
- **Exceptions:** Same as the synchronous‑signature variant: `OperationCanceledException`, `InvalidOperationException`, and `IOException`.

### async Task<BatchProcessingSummary> ProcessScheduledInChunksAsync()
Processes outbox messages according to an internal schedule, executing work in chunks asynchronously.  
- **Parameters:** None.  
- **Return value:** A `Task<BatchProcessingSummary>` describing the outcome of the scheduled run.  
- **Exceptions:**  
  - `OperationCanceledException` – if the scheduled operation is cancelled.  
  - `InvalidOperationException` – if the scheduler is not running or the service is mis‑configured.  
  - `IOException` – for storage‑related failures during chunk retrieval or update.

## Usage

```csharp
// Example 1: Immediate chunked processing
using var scope = host.Services.CreateScope();
var batchProcessor = scope.ServiceProvider.GetRequiredService<IBatchProcessingService>(); // resolves IBatchProcessingService
var summary = await batchProcessor.ProcessInChunksAsync();
Console.WriteLine($"Processed {summary.Processed} messages, {summary.Failed} failed.");
```

```csharp
// Example 2: Starting scheduled chunk processing
using var scope = host.Services.CreateScope();
var batchProcessor = scope.ServiceProvider.GetRequiredService<IBatchProcessingService>();
// Assume the implementation begins scheduling internally when started.
await batchProcessor.ProcessScheduledInChunksAsync();
// The method returns once a scheduled cycle completes; the implementation may continue
// running in the background until stopped.
```

## Notes

- **Empty outbox:** If no messages are available, the methods return a `BatchProcessingSummary`BatchProcessingSummary`` with zero counts and do not throw.
- **Concurrency:** The interface does not impose thread‑safety guarantees. Implementations that hold mutable state should document their own concurrency behavior; stateless wrappers are safe for concurrent calls.
- **Cancellation:** Both asynchronous overloads honor a cancellation token if one is supplied via the underlying infrastructure (e.g., `IHostApplicationLifetime`). Passing a token that is already cancelled results in an immediate `OperationCanceledException`.
- **Error handling:** Transient storage errors are typically retried internally by the implementation; persistent failures propagate as `IOException`. Consumers should treat any non‑zero `Failed` count as indicative of partial processing and may choose to retry the batch.
- **Lifecycle:** The service should be started before invoking any processing method and stopped gracefully to avoid leaving incomplete chunks. Failure to do so may lead to `InvalidOperationException` on subsequent calls.
