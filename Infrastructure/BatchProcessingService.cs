#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Processes outbox messages in configurable-size chunks with optional parallel execution
/// </summary>
public interface IBatchProcessingService
{
    /// <summary>
    /// Processes pending messages using the configured <see cref="BatchProcessingOptions.TotalBatchSize"/>
    /// split into chunks of <see cref="BatchProcessingOptions.ChunkSize"/>
    /// </summary>
    Task<BatchProcessingSummary> ProcessInChunksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes up to <paramref name="totalMessages"/> pending messages split into chunks
    /// </summary>
    Task<BatchProcessingSummary> ProcessInChunksAsync(int totalMessages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes scheduled messages in configurable-size chunks
    /// </summary>
    Task<BatchProcessingSummary> ProcessScheduledInChunksAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Wraps <see cref="IMessagePublishingService"/> to subdivide large batches into smaller chunks,
/// supporting sequential ordering, bounded parallelism, inter-chunk delays, and per-chunk metrics
/// </summary>
public sealed class BatchProcessingService : IBatchProcessingService
{
    private readonly IMessagePublishingService _publishingService;
    private readonly BatchProcessingOptions _options;
    private readonly ILogger<BatchProcessingService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="BatchProcessingService"/>
    /// </summary>
    public BatchProcessingService(
        IMessagePublishingService publishingService,
        BatchProcessingOptions options,
        ILogger<BatchProcessingService> logger)
    {
        _publishingService = publishingService ?? throw new ArgumentNullException(nameof(publishingService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<BatchProcessingSummary> ProcessInChunksAsync(CancellationToken cancellationToken = default)
        => ProcessInChunksAsync(_options.TotalBatchSize, cancellationToken);

    /// <inheritdoc />
    public async Task<BatchProcessingSummary> ProcessInChunksAsync(
        int totalMessages,
        CancellationToken cancellationToken = default)
    {
        var summary = new BatchProcessingSummary { StartedAt = DateTime.UtcNow };

        try
        {
            var chunks = BuildChunkSizes(totalMessages, Math.Max(1, _options.ChunkSize));
            summary.TotalChunks = chunks.Count;

            _logger.LogInformation(
                "Batch processing started: {Total} messages across {Chunks} chunks of up to {ChunkSize} each",
                totalMessages, chunks.Count, _options.ChunkSize);

            if (_options.EnableParallelChunks)
                await ProcessParallelAsync(chunks, summary, ProcessPendingChunkAsync, cancellationToken);
            else
                await ProcessSequentialAsync(chunks, summary, ProcessPendingChunkAsync, cancellationToken);

            ApplyOverallOutcome(summary);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Batch processing cancelled");
            summary.Success = false;
            summary.ErrorMessage = "Processing was cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during batch processing");
            summary.Success = false;
            summary.ErrorMessage = ex.Message;
        }
        finally
        {
            summary.CompletedAt = DateTime.UtcNow;
            LogSummary(summary);
        }

        return summary;
    }

    /// <inheritdoc />
    public async Task<BatchProcessingSummary> ProcessScheduledInChunksAsync(CancellationToken cancellationToken = default)
    {
        var summary = new BatchProcessingSummary { StartedAt = DateTime.UtcNow };

        try
        {
            var chunks = BuildChunkSizes(_options.TotalBatchSize, Math.Max(1, _options.ChunkSize));
            summary.TotalChunks = chunks.Count;

            _logger.LogInformation(
                "Scheduled batch processing started: {Total} messages across {Chunks} chunks",
                _options.TotalBatchSize, chunks.Count);

            if (_options.EnableParallelChunks)
                await ProcessParallelAsync(chunks, summary, ProcessScheduledChunkAsync, cancellationToken);
            else
                await ProcessSequentialAsync(chunks, summary, ProcessScheduledChunkAsync, cancellationToken);

            ApplyOverallOutcome(summary);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Scheduled batch processing cancelled");
            summary.Success = false;
            summary.ErrorMessage = "Processing was cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during scheduled batch processing");
            summary.Success = false;
            summary.ErrorMessage = ex.Message;
        }
        finally
        {
            summary.CompletedAt = DateTime.UtcNow;
            LogSummary(summary);
        }

        return summary;
    }

    private async Task ProcessSequentialAsync(
        List<int> chunks,
        BatchProcessingSummary summary,
        Func<int, int, CancellationToken, Task<BatchChunkResult>> processChunk,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < chunks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunkResult = await processChunk(i, chunks[i], cancellationToken);
            Accumulate(summary, chunkResult);

            if (!chunkResult.Success && _options.StopOnChunkFailure)
            {
                _logger.LogWarning("Stopping batch after chunk {Index} failure", i);
                break;
            }

            if (_options.DelayBetweenChunksMs > 0 && i < chunks.Count - 1)
                await Task.Delay(_options.DelayBetweenChunksMs, cancellationToken);
        }
    }

    private async Task ProcessParallelAsync(
        List<int> chunks,
        BatchProcessingSummary summary,
        Func<int, int, CancellationToken, Task<BatchChunkResult>> processChunk,
        CancellationToken cancellationToken)
    {
        var results = new BatchChunkResult[chunks.Count];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, chunks.Count),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, _options.MaxParallelChunks),
                CancellationToken = cancellationToken
            },
            async (i, ct) => { results[i] = await processChunk(i, chunks[i], ct); });

        foreach (var result in results)
            Accumulate(summary, result);
    }

    private async Task<BatchChunkResult> ProcessPendingChunkAsync(
        int index, int size, CancellationToken cancellationToken)
    {
        var chunk = new BatchChunkResult { ChunkIndex = index, StartedAt = DateTime.UtcNow };

        try
        {
            var result = await _publishingService.ProcessPendingMessagesAsync(size, cancellationToken);
            // A chunk "succeeds" when it completes without an unhandled exception - the
            // per-message ProcessedCount/FailedCount inside result is normal operational
            // detail, not a chunk-level failure signal.
            chunk.Success = true;
            chunk.ProcessedCount = result.ProcessedCount;
            chunk.FailedCount = result.FailedCount;
            chunk.ErrorMessage = result.ErrorMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in pending chunk {Index}", index);
            chunk.Success = false;
            chunk.ErrorMessage = ex.Message;
        }
        finally
        {
            chunk.CompletedAt = DateTime.UtcNow;
        }

        return chunk;
    }

    private async Task<BatchChunkResult> ProcessScheduledChunkAsync(
        int index, int size, CancellationToken cancellationToken)
    {
        var chunk = new BatchChunkResult { ChunkIndex = index, StartedAt = DateTime.UtcNow };

        try
        {
            var result = await _publishingService.ProcessScheduledMessagesAsync(size, cancellationToken);
            // Same rationale as ProcessPendingChunkAsync: absence of an unhandled exception
            // is what defines chunk-level success here.
            chunk.Success = true;
            chunk.ProcessedCount = result.ProcessedCount;
            chunk.FailedCount = result.FailedCount;
            chunk.ErrorMessage = result.ErrorMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in scheduled chunk {Index}", index);
            chunk.Success = false;
            chunk.ErrorMessage = ex.Message;
        }
        finally
        {
            chunk.CompletedAt = DateTime.UtcNow;
        }

        return chunk;
    }

    private static List<int> BuildChunkSizes(int total, int chunkSize)
    {
        var chunks = new List<int>();
        var remaining = Math.Max(0, total);

        while (remaining > 0)
        {
            chunks.Add(Math.Min(remaining, chunkSize));
            remaining -= chunkSize;
        }

        return chunks;
    }

    private static void Accumulate(BatchProcessingSummary summary, BatchChunkResult chunk)
    {
        summary.ChunkResults.Add(chunk);
        summary.TotalProcessed += chunk.ProcessedCount;
        summary.TotalFailed += chunk.FailedCount;

        if (chunk.Success) summary.SuccessfulChunks++;
        else summary.FailedChunks++;
    }

    /// <summary>
    /// Derives the overall success/error of a batch run from its accumulated chunk results.
    /// A chunk that threw or reported failure must not be silently reported as an overall success.
    /// </summary>
    private static void ApplyOverallOutcome(BatchProcessingSummary summary)
    {
        if (summary.FailedChunks == 0)
        {
            summary.Success = true;
            return;
        }

        summary.Success = false;
        summary.ErrorMessage = string.Join(
            "; ",
            summary.ChunkResults
                .Where(c => !c.Success && !string.IsNullOrEmpty(c.ErrorMessage))
                .Select(c => c.ErrorMessage));
    }

    private void LogSummary(BatchProcessingSummary summary)
    {
        _logger.LogInformation(
            "Batch processing complete — chunks: {Total} ({Ok} ok / {Bad} failed), " +
            "messages: {Processed} published / {Failed} failed, duration: {Ms}ms",
            summary.TotalChunks, summary.SuccessfulChunks, summary.FailedChunks,
            summary.TotalProcessed, summary.TotalFailed,
            (int)summary.Duration.TotalMilliseconds);
    }
}
