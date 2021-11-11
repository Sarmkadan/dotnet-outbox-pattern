#nullable enable

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Extension methods for BatchProcessingModelsTests to provide additional utility functionality
/// </summary>
public static class BatchProcessingModelsTestsExtensions
{
    /// <summary>
    /// Creates a BatchProcessingOptions instance with default values for testing
    /// </summary>
    /// <returns>Configured BatchProcessingOptions instance</returns>
    public static BatchProcessingOptions CreateDefaultOptions(this BatchProcessingModelsTests _) => new();

    /// <summary>
    /// Creates a BatchProcessingOptions instance with custom values for testing
    /// </summary>
    /// <param name="totalBatchSize">Total batch size</param>
    /// <param name="chunkSize">Chunk size</param>
    /// <param name="maxParallelChunks">Maximum parallel chunks</param>
    /// <param name="enableParallelChunks">Enable parallel chunks</param>
    /// <param name="delayBetweenChunksMs">Delay between chunks in milliseconds</param>
    /// <param name="stopOnChunkFailure">Stop on chunk failure</param>
    /// <returns>Configured BatchProcessingOptions instance</returns>
    public static BatchProcessingOptions CreateCustomOptions(
        this BatchProcessingModelsTests _,
        int totalBatchSize = 1000,
        int chunkSize = 100,
        int maxParallelChunks = 2,
        bool enableParallelChunks = false,
        int delayBetweenChunksMs = 0,
        bool stopOnChunkFailure = false) =>
        new BatchProcessingOptions
        {
            TotalBatchSize = totalBatchSize,
            ChunkSize = chunkSize,
            MaxParallelChunks = maxParallelChunks,
            EnableParallelChunks = enableParallelChunks,
            DelayBetweenChunksMs = delayBetweenChunksMs,
            StopOnChunkFailure = stopOnChunkFailure
        };

    /// <summary>
    /// Creates a BatchChunkResult instance with default values for testing
    /// </summary>
    /// <returns>Configured BatchChunkResult instance</returns>
    public static BatchChunkResult CreateDefaultChunkResult(this BatchProcessingModelsTests _) => new();

    /// <summary>
    /// Creates a BatchChunkResult instance with specified values for testing
    /// </summary>
    /// <param name="chunkIndex">Chunk index</param>
    /// <param name="success">Success status</param>
    /// <param name="processedCount">Number of processed items</param>
    /// <param name="failedCount">Number of failed items</param>
    /// <param name="errorMessage">Error message if any</param>
    /// <returns>Configured BatchChunkResult instance</returns>
    public static BatchChunkResult CreateChunkResult(
        this BatchProcessingModelsTests _,
        int chunkIndex = 0,
        bool success = false,
        int processedCount = 0,
        int failedCount = 0,
        string? errorMessage = null) =>
        new BatchChunkResult
        {
            ChunkIndex = chunkIndex,
            Success = success,
            ProcessedCount = processedCount,
            FailedCount = failedCount,
            ErrorMessage = errorMessage
        };

    /// <summary>
    /// Creates a BatchProcessingSummary instance with default values for testing
    /// </summary>
    /// <returns>Configured BatchProcessingSummary instance</returns>
    public static BatchProcessingSummary CreateDefaultSummary(this BatchProcessingModelsTests _) => new();

    /// <summary>
    /// Creates a BatchProcessingSummary instance with specified values for testing
    /// </summary>
    /// <param name="success">Overall success status</param>
    /// <param name="totalProcessed">Total processed count</param>
    /// <param name="totalFailed">Total failed count</param>
    /// <param name="totalChunks">Total chunks count</param>
    /// <param name="successfulChunks">Successful chunks count</param>
    /// <param name="failedChunks">Failed chunks count</param>
    /// <param name="chunkResults">List of chunk results</param>
    /// <param name="errorMessage">Error message if any</param>
    /// <returns>Configured BatchProcessingSummary instance</returns>
    public static BatchProcessingSummary CreateSummary(
        this BatchProcessingModelsTests _,
        bool success = false,
        int totalProcessed = 0,
        int totalFailed = 0,
        int totalChunks = 0,
        int successfulChunks = 0,
        int failedChunks = 0,
        List<BatchChunkResult>? chunkResults = null,
        string? errorMessage = null) =>
        new BatchProcessingSummary
        {
            Success = success,
            TotalProcessed = totalProcessed,
            TotalFailed = totalFailed,
            TotalChunks = totalChunks,
            SuccessfulChunks = successfulChunks,
            FailedChunks = failedChunks,
            ChunkResults = chunkResults ?? new List<BatchChunkResult>(),
            ErrorMessage = errorMessage
        };

    /// <summary>
    /// Asserts that two BatchProcessingOptions instances are equal
    /// </summary>
    /// <param name="test">Test instance</param>
    /// <param name="expected">Expected options</param>
    /// <param name="actual">Actual options to assert</param>
    public static void ShouldBeEquivalentTo(
        this BatchProcessingModelsTests _,
        BatchProcessingOptions expected,
        BatchProcessingOptions actual)
    {
        actual.TotalBatchSize.Should().Be(expected.TotalBatchSize);
        actual.ChunkSize.Should().Be(expected.ChunkSize);
        actual.MaxParallelChunks.Should().Be(expected.MaxParallelChunks);
        actual.EnableParallelChunks.Should().Be(expected.EnableParallelChunks);
        actual.DelayBetweenChunksMs.Should().Be(expected.DelayBetweenChunksMs);
        actual.StopOnChunkFailure.Should().Be(expected.StopOnChunkFailure);
    }

    /// <summary>
    /// Asserts that two BatchChunkResult instances are equal
    /// </summary>
    /// <param name="test">Test instance</param>
    /// <param name="expected">Expected result</param>
    /// <param name="actual">Actual result to assert</param>
    public static void ShouldBeEquivalentTo(
        this BatchProcessingModelsTests _,
        BatchChunkResult expected,
        BatchChunkResult actual)
    {
        actual.ChunkIndex.Should().Be(expected.ChunkIndex);
        actual.Success.Should().Be(expected.Success);
        actual.ProcessedCount.Should().Be(expected.ProcessedCount);
        actual.FailedCount.Should().Be(expected.FailedCount);
        actual.ErrorMessage.Should().Be(expected.ErrorMessage);
        actual.StartedAt.Should().Be(expected.StartedAt);
        actual.CompletedAt.Should().Be(expected.CompletedAt);
    }

    /// <summary>
    /// Asserts that two BatchProcessingSummary instances are equal
    /// </summary>
    /// <param name="test">Test instance</param>
    /// <param name="expected">Expected summary</param>
    /// <param name="actual">Actual summary to assert</param>
    public static void ShouldBeEquivalentTo(
        this BatchProcessingModelsTests _,
        BatchProcessingSummary expected,
        BatchProcessingSummary actual)
    {
        actual.Success.Should().Be(expected.Success);
        actual.TotalProcessed.Should().Be(expected.TotalProcessed);
        actual.TotalFailed.Should().Be(expected.TotalFailed);
        actual.TotalChunks.Should().Be(expected.TotalChunks);
        actual.SuccessfulChunks.Should().Be(expected.SuccessfulChunks);
        actual.FailedChunks.Should().Be(expected.FailedChunks);
        actual.ChunkResults.Should().BeEquivalentTo(expected.ChunkResults);
        actual.StartedAt.Should().Be(expected.StartedAt);
        actual.CompletedAt.Should().Be(expected.CompletedAt);
        actual.ErrorMessage.Should().Be(expected.ErrorMessage);
    }
}