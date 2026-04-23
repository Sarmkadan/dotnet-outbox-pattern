#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Configuration options for batch processing with configurable chunk size
/// </summary>
public sealed class BatchProcessingOptions
{
    /// <summary>
    /// Total number of messages to process per cycle across all chunks
    /// </summary>
    public int TotalBatchSize { get; set; } = 1000;

    /// <summary>
    /// Number of messages per individual chunk within a batch
    /// A smaller value reduces memory pressure; a larger value reduces round-trips
    /// </summary>
    public int ChunkSize { get; set; } = 100;

    /// <summary>
    /// Maximum number of chunks to execute concurrently
    /// Only applies when <see cref="EnableParallelChunks"/> is <c>true</c>
    /// </summary>
    public int MaxParallelChunks { get; set; } = 2;

    /// <summary>
    /// Whether to process chunks concurrently
    /// When <c>false</c> chunks are processed sequentially in order
    /// </summary>
    public bool EnableParallelChunks { get; set; } = false;

    /// <summary>
    /// Milliseconds to wait between sequential chunks
    /// Use to throttle downstream throughput without blocking the thread
    /// </summary>
    public int DelayBetweenChunksMs { get; set; } = 0;

    /// <summary>
    /// Whether to abort remaining chunks when a chunk encounters a failure
    /// Only applies to sequential mode; parallel chunks always run to completion
    /// </summary>
    public bool StopOnChunkFailure { get; set; } = false;
}

/// <summary>
/// Processing result for a single chunk within a batch run
/// </summary>
public sealed class BatchChunkResult
{
    /// <summary>
    /// Zero-based position of this chunk in the overall batch
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Whether this chunk completed without unhandled errors
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of messages successfully published in this chunk
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Number of messages that failed or were routed to dead-letter in this chunk
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Error message when <see cref="Success"/> is <c>false</c>
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// UTC timestamp when this chunk began processing
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// UTC timestamp when this chunk finished processing
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Wall-clock duration for this chunk
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;
}

/// <summary>
/// Aggregated result across all chunks in a single batch processing run
/// </summary>
public sealed class BatchProcessingSummary
{
    /// <summary>
    /// Whether the batch completed without a top-level error
    /// Individual chunk failures are reflected in <see cref="FailedChunks"/>
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total messages successfully published across all chunks
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Total messages that failed across all chunks
    /// </summary>
    public int TotalFailed { get; set; }

    /// <summary>
    /// Number of chunks scheduled for this run
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// Number of chunks that completed successfully
    /// </summary>
    public int SuccessfulChunks { get; set; }

    /// <summary>
    /// Number of chunks that reported a failure
    /// </summary>
    public int FailedChunks { get; set; }

    /// <summary>
    /// Per-chunk breakdown for detailed inspection or metrics
    /// </summary>
    public List<BatchChunkResult> ChunkResults { get; set; } = new();

    /// <summary>
    /// UTC timestamp when the batch run started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the batch run completed
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Total wall-clock duration of the entire batch run
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Top-level error message when the batch could not be initiated
    /// </summary>
    public string? ErrorMessage { get; set; }
}
