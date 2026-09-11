# BatchProcessingService

## Purpose

The `BatchProcessingService` processes outbox messages in configurable-size chunks with optional parallel execution. It wraps an `IMessagePublishingService` to subdivide large batches into smaller chunks, supporting sequential ordering, bounded parallelism, inter-chunk delays, and per-chunk metrics.

## Batching Logic

The service processes messages by dividing them into chunks based on configured batch and chunk sizes:

1. **Chunk Calculation**: 
   - Total messages to process are determined either by `BatchProcessingOptions.TotalBatchSize` or a provided `totalMessages` parameter
   - Messages are split into chunks of size `BatchProcessingOptions.ChunkSize`
   - The final chunk may be smaller if the total isn't evenly divisible

2. **Execution Modes**:
   - **Sequential**: Chunks are processed one after another when `EnableParallelChunks` is false
   - **Parallel**: Chunks are processed concurrently when `EnableParallelChunks` is true, with degree of parallelism limited by `MaxParallelChunks`

3. **Chunk Processing**:
   - Each chunk is processed by the underlying `IMessagePublishingService` via either:
     - `ProcessPendingMessagesAsync` (for regular outbox messages)
     - `ProcessScheduledMessagesAsync` (for scheduled messages)
   - A chunk is considered successful if it completes without unhandled exceptions (regardless of individual message outcomes within the chunk)

4. **Failure Handling**:
   - If `StopOnChunkFailure` is true, processing stops after the first failed chunk
   - Between sequential chunks, a delay of `DelayBetweenChunksMs` milliseconds can be applied
   - Overall batch success requires all chunks to succeed

5. **Metrics Collection**:
   - Tracks processed/failed message counts per chunk
   - Records successful/failed chunk counts
   - Measures total processing duration
   - Logs summary information upon completion

## Public Methods

### `Task<BatchProcessingSummary> ProcessInChunksAsync(CancellationToken cancellationToken = default)`
Processes pending messages using the configured `TotalBatchSize` split into chunks of `ChunkSize`.

### `Task<BatchProcessingSummary> ProcessInChunksAsync(int totalMessages, CancellationToken cancellationToken = default)`
Processes up to `totalMessages` pending messages split into chunks of `ChunkSize`.

### `Task<BatchProcessingSummary> ProcessScheduledInChunksAsync(CancellationToken cancellationToken = default)`
Processes scheduled messages in configurable-size chunks (using `TotalBatchSize` and `ChunkSize`).

All methods return a `BatchProcessingSummary` containing:
- `StartedAt`/`CompletedAt` timestamps
- `TotalChunks`, `SuccessfulChunks`, `FailedChunks`
- `TotalProcessed`/`TotalFailed` message counts
- `Success` boolean indicating overall batch outcome
- `ErrorMessage` (if any)
- Detailed `ChunkResults` list

## Usage Example

```csharp
// Configure batch processing options
var options = new BatchProcessingOptions
{
    TotalBatchSize = 1000,    // Process up to 1000 messages per batch
    ChunkSize = 100,          // Split into chunks of 100 messages
    EnableParallelChunks = true,  // Process chunks in parallel
    MaxParallelChunks = 4,    // Maximum 4 concurrent chunks
    DelayBetweenChunksMs = 50, // 50ms delay between sequential chunks (not used when parallel)
    StopOnChunkFailure = false // Continue processing even if individual chunks fail
};

// Initialize service (typically via DI container)
var batchProcessor = new BatchProcessingService(
    publishingService: messagePublishingService,
    options: options,
    logger: loggerFactory.CreateLogger<BatchProcessingService>()
);

// Process pending messages in batches
var summary = await batchProcessor.ProcessInChunksAsync();

// Or process a specific number of messages
// var summary = await batchProcessor.ProcessInChunksAsync(500);

// Process scheduled messages
// var summary = await batchProcessor.ProcessScheduledInChunksAsync();

// Check results
if (summary.Success)
{
    logger.LogInformation(
        "Batch processed successfully: {Processed} messages published, {Failed} failed",
        summary.TotalProcessed, summary.TotalFailed);
}
else
{
    logger.LogError(
        "Batch processing failed: {ErrorMessage}", summary.ErrorMessage);
}
```