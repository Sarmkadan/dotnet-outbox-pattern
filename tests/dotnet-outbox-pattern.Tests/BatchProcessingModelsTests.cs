#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for batch processing models and DTOs
/// </summary>
public sealed class BatchProcessingModelsTests
{
    [Fact]
    public void BatchProcessingOptions_DefaultValues_AreCorrect()
    {
        var options = new BatchProcessingOptions();

        options.TotalBatchSize.Should().Be(1000);
        options.ChunkSize.Should().Be(100);
        options.MaxParallelChunks.Should().Be(2);
        options.EnableParallelChunks.Should().BeFalse();
        options.DelayBetweenChunksMs.Should().Be(0);
        options.StopOnChunkFailure.Should().BeFalse();
    }

    [Fact]
    public void BatchProcessingOptions_CustomValues_AreApplied()
    {
        var options = new BatchProcessingOptions
        {
            TotalBatchSize = 5000,
            ChunkSize = 200,
            MaxParallelChunks = 4,
            EnableParallelChunks = true,
            DelayBetweenChunksMs = 100,
            StopOnChunkFailure = true
        };

        options.TotalBatchSize.Should().Be(5000);
        options.ChunkSize.Should().Be(200);
        options.MaxParallelChunks.Should().Be(4);
        options.EnableParallelChunks.Should().BeTrue();
        options.DelayBetweenChunksMs.Should().Be(100);
        options.StopOnChunkFailure.Should().BeTrue();
    }

    [Fact]
    public void BatchProcessingOptions_WithMinimalValues_Works()
    {
        var options = new BatchProcessingOptions
        {
            TotalBatchSize = 1,
            ChunkSize = 1,
            MaxParallelChunks = 1,
            DelayBetweenChunksMs = 1
        };

        options.TotalBatchSize.Should().Be(1);
        options.ChunkSize.Should().Be(1);
        options.MaxParallelChunks.Should().Be(1);
        options.DelayBetweenChunksMs.Should().Be(1);
    }

    [Fact]
    public void BatchChunkResult_DefaultConstructor_InitializesProperties()
    {
        var result = new BatchChunkResult();

        result.ChunkIndex.Should().Be(0);
        result.Success.Should().BeFalse();
        result.ProcessedCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
        result.StartedAt.Should().Be(default);
        result.CompletedAt.Should().Be(default);
    }

    [Fact]
    public void BatchChunkResult_WithValues_SetsPropertiesCorrectly()
    {
        var before = DateTime.UtcNow;
        var result = new BatchChunkResult
        {
            ChunkIndex = 5,
            Success = true,
            ProcessedCount = 25,
            FailedCount = 2,
            ErrorMessage = "Some messages failed",
            StartedAt = before,
            CompletedAt = before.AddSeconds(2)
        };

        result.ChunkIndex.Should().Be(5);
        result.Success.Should().BeTrue();
        result.ProcessedCount.Should().Be(25);
        result.FailedCount.Should().Be(2);
        result.ErrorMessage.Should().Be("Some messages failed");
        result.StartedAt.Should().Be(before);
        result.CompletedAt.Should().Be(before.AddSeconds(2));
        result.Duration.Should().BeCloseTo(TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void BatchChunkResult_Duration_CalculatesCorrectly()
    {
        var start = DateTime.UtcNow;
        var result = new BatchChunkResult
        {
            StartedAt = start,
            CompletedAt = start.AddMilliseconds(1500)
        };

        result.Duration.Should().BeCloseTo(TimeSpan.FromMilliseconds(1500), TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void BatchProcessingSummary_DefaultConstructor_InitializesCollections()
    {
        var summary = new BatchProcessingSummary();

        summary.Success.Should().BeFalse();
        summary.TotalProcessed.Should().Be(0);
        summary.TotalFailed.Should().Be(0);
        summary.TotalChunks.Should().Be(0);
        summary.SuccessfulChunks.Should().Be(0);
        summary.FailedChunks.Should().Be(0);
        summary.ChunkResults.Should().NotBeNull();
        summary.ChunkResults.Should().BeEmpty();
        summary.StartedAt.Should().Be(default);
        summary.CompletedAt.Should().Be(default);
        summary.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void BatchProcessingSummary_WithValues_SetsPropertiesCorrectly()
    {
        var before = DateTime.UtcNow;
        var chunk1 = new BatchChunkResult { Success = true, ProcessedCount = 50, FailedCount = 0 };
        var chunk2 = new BatchChunkResult { Success = false, ProcessedCount = 30, FailedCount = 5 };

        var summary = new BatchProcessingSummary
        {
            Success = false,
            TotalProcessed = 80,
            TotalFailed = 5,
            TotalChunks = 2,
            SuccessfulChunks = 1,
            FailedChunks = 1,
            ChunkResults = new List<BatchChunkResult> { chunk1, chunk2 },
            StartedAt = before,
            CompletedAt = before.AddSeconds(5),
            ErrorMessage = "One chunk failed"
        };

        summary.Success.Should().BeFalse();
        summary.TotalProcessed.Should().Be(80);
        summary.TotalFailed.Should().Be(5);
        summary.TotalChunks.Should().Be(2);
        summary.SuccessfulChunks.Should().Be(1);
        summary.FailedChunks.Should().Be(1);
        summary.ChunkResults.Should().HaveCount(2);
        summary.StartedAt.Should().Be(before);
        summary.CompletedAt.Should().Be(before.AddSeconds(5));
        summary.Duration.Should().BeCloseTo(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
        summary.ErrorMessage.Should().Be("One chunk failed");
    }

    [Fact]
    public void BatchProcessingSummary_Duration_CalculatesCorrectly()
    {
        var start = DateTime.UtcNow;
        var summary = new BatchProcessingSummary
        {
            StartedAt = start,
            CompletedAt = start.AddSeconds(10)
        };

        summary.Duration.Should().BeCloseTo(TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void BatchProcessingSummary_Accumulate_AddsChunkResults()
    {
        var summary = new BatchProcessingSummary { StartedAt = DateTime.UtcNow };
        var chunk1 = new BatchChunkResult { Success = true, ProcessedCount = 50, FailedCount = 0 };
        var chunk2 = new BatchChunkResult { Success = false, ProcessedCount = 30, FailedCount = 5 };

        // Simulate accumulation
        summary.TotalProcessed += chunk1.ProcessedCount;
        summary.TotalProcessed += chunk2.ProcessedCount;
        summary.TotalFailed += chunk1.FailedCount;
        summary.TotalFailed += chunk2.FailedCount;
        summary.SuccessfulChunks += chunk1.Success ? 1 : 0;
        summary.FailedChunks += chunk2.Success ? 0 : 1;
        summary.ChunkResults.Add(chunk1);
        summary.ChunkResults.Add(chunk2);

        summary.TotalProcessed.Should().Be(80);
        summary.TotalFailed.Should().Be(5);
        summary.SuccessfulChunks.Should().Be(1);
        summary.FailedChunks.Should().Be(1);
        summary.ChunkResults.Should().HaveCount(2);
    }
}
