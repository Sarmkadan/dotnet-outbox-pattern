# Batch processing models

[`Domain/BatchProcessingModels.cs`](../Domain/BatchProcessingModels.cs) defines the three mutable model types used to configure a chunked batch run and report its results. All three types are sealed and belong to the `DotnetOutboxPattern.Domain` namespace.

## `BatchProcessingOptions`

`BatchProcessingOptions` controls how a batch is divided into chunks and how those chunks are executed. `BatchProcessingService` consumes these values when it processes pending or scheduled outbox messages.

| Property | Type | Default | Role |
| --- | --- | --- | --- |
| `TotalBatchSize` | `int` | `1000` | Maximum number of messages requested for a configured processing cycle. The overload of `ProcessInChunksAsync` that accepts `totalMessages` uses that argument instead; scheduled processing uses this property. |
| `ChunkSize` | `int` | `100` | Requested maximum number of messages in each chunk. The final chunk can be smaller. The service protects its chunk-building logic with a minimum effective value of `1`. |
| `MaxParallelChunks` | `int` | `2` | Maximum degree of parallel chunk execution when `EnableParallelChunks` is `true`. The service uses a minimum effective value of `1`. |
| `EnableParallelChunks` | `bool` | `false` | Selects bounded parallel execution when `true`; otherwise chunks run sequentially in index order. |
| `DelayBetweenChunksMs` | `int` | `0` | Delay, in milliseconds, inserted between sequential chunks. It is not used by the parallel path or after the last chunk. |
| `StopOnChunkFailure` | `bool` | `false` | In sequential mode, stops scheduling remaining chunks after a chunk whose `Success` is `false`. The parallel path does not consult this value. |

Every property has a public getter and setter. This model itself does not validate assigned values.

## `BatchChunkResult`

`BatchChunkResult` records the outcome of one chunk. The processing service creates one result for each chunk it executes and later adds it to the batch summary.

| Property | Type | Default | Role |
| --- | --- | --- | --- |
| `ChunkIndex` | `int` | `0` | Zero-based position of the chunk in the batch. |
| `Success` | `bool` | `false` | Indicates whether chunk processing completed without an unhandled exception. Individual message failures are counted separately and do not by themselves make this value `false`. |
| `ProcessedCount` | `int` | `0` | Number of messages reported as successfully processed in the chunk. |
| `FailedCount` | `int` | `0` | Number of messages reported as failed or routed to dead-letter processing in the chunk. |
| `ErrorMessage` | `string?` | `null` | Error text associated with the chunk, when present. The service copies the publishing result's error text after a completed call or the exception message after an unhandled error. |
| `StartedAt` | `DateTime` | `default` | UTC timestamp assigned by the service immediately before processing the chunk. |
| `CompletedAt` | `DateTime` | `default` | UTC timestamp assigned by the service when chunk processing finishes. |
| `Duration` | `TimeSpan` | Computed | Read-only value calculated as `CompletedAt - StartedAt`. |

All properties except `Duration` have public getters and setters.

## `BatchProcessingSummary`

`BatchProcessingSummary` aggregates the results of one complete batch run. The service returns it from each batch-processing entry point and uses it for completion logging.

| Property | Type | Default | Role |
| --- | --- | --- | --- |
| `Success` | `bool` | `false` | Overall outcome. The service sets it to `true` when no chunk failed, and to `false` for chunk failures, cancellation, or a top-level exception. |
| `TotalProcessed` | `int` | `0` | Sum of `ProcessedCount` across accumulated chunk results. |
| `TotalFailed` | `int` | `0` | Sum of `FailedCount` across accumulated chunk results. |
| `TotalChunks` | `int` | `0` | Number of chunks calculated for the run. If sequential processing stops early, this can be greater than `ChunkResults.Count`. |
| `SuccessfulChunks` | `int` | `0` | Number of accumulated chunks whose `Success` is `true`. |
| `FailedChunks` | `int` | `0` | Number of accumulated chunks whose `Success` is `false`. |
| `ChunkResults` | `List<BatchChunkResult>` | Empty list | Mutable per-chunk detail. Results are accumulated in processing order for sequential execution and in chunk-index order after parallel execution. |
| `StartedAt` | `DateTime` | `default` | UTC timestamp assigned when the service begins the batch run. |
| `CompletedAt` | `DateTime` | `default` | UTC timestamp assigned when the service leaves the batch run, including cancellation and error paths. |
| `Duration` | `TimeSpan` | Computed | Read-only value calculated as `CompletedAt - StartedAt`. |
| `ErrorMessage` | `string?` | `null` | Top-level error text. The service supplies cancellation text, an unexpected exception message, or joins the non-empty error messages from failed chunks. |

All properties except `Duration` have public getters and setters. `ChunkResults` is initialized by the model, but because its setter is public, callers can replace the list, including with `null` despite the non-nullable declaration.

## Processing flow

1. A `BatchProcessingOptions` instance determines the requested total, chunk size, and execution mode.
2. Each executed chunk produces a `BatchChunkResult` with counts, timestamps, and any error text.
3. The service accumulates those results into a `BatchProcessingSummary`, derives the overall outcome, stamps `CompletedAt`, logs the summary, and returns it.
