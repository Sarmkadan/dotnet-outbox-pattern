# BatchProcessingOptions

`BatchProcessingOptions` is a configuration container used to control the behavior of batch processing operations in the Outbox Pattern implementation. It defines parameters for chunking, parallelism, retry policies, and result tracking for batches of messages or operations.

## API

### `TotalBatchSize`
Gets or sets the total number of items to process in the batch. Must be a positive integer. Used to determine how many chunks will be created.

### `ChunkSize`
Gets or sets the maximum number of items per chunk. Must be a positive integer. Determines the granularity of each processing unit.

### `MaxParallelChunks`
Gets or sets the maximum number of chunks that can be processed concurrently. Must be a positive integer. Affects resource utilization and throughput.

### `EnableParallelChunks`
Gets or sets a value indicating whether parallel processing of chunks is enabled. When `false`, chunks are processed sequentially regardless of `MaxParallelChunks`.

### `DelayBetweenChunksMs`
Gets or sets the delay in milliseconds between starting consecutive chunks. Used to throttle processing and reduce resource contention.

### `StopOnChunkFailure`
Gets or sets a value indicating whether processing should halt if any chunk fails. When `true`, the entire batch stops on the first chunk failure.

### `ChunkIndex`
Gets or sets the current zero-based index of the chunk being processed. Used internally to track progress.

### `Success`
Gets or sets a value indicating whether the entire batch completed successfully. Updated after processing finishes.

### `ProcessedCount`
Gets or sets the total number of items successfully processed in the current chunk. Reset per chunk.

### `FailedCount`
Gets or sets the total number of items that failed processing in the current chunk. Reset per chunk.

### `ErrorMessage`
Gets or sets the error message associated with the most recent failure in the current chunk. `null` if no error occurred.

### `StartedAt`
Gets or sets the timestamp when batch processing began.

### `CompletedAt`
Gets or sets the timestamp when batch processing completed.

### `TotalProcessed`
Gets the total number of items successfully processed across all chunks.

### `TotalFailed`
Gets the total number of items that failed processing across all chunks.

### `TotalChunks`
Gets the total number of chunks derived from `TotalBatchSize` and `ChunkSize`.

### `SuccessfulChunks`
Gets the number of chunks that completed successfully.

### `FailedChunks`
Gets the number of chunks that failed during processing.

### `ChunkResults`
Gets the list of results for each processed chunk, including success status, item counts, and error messages.

## Usage

### Example 1: Basic Batch Processing with Parallelism
