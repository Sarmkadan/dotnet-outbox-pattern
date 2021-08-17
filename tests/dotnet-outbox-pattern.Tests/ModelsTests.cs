#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for domain models and DTOs
/// </summary>
public sealed class ModelsTests
{
    [Fact]
    public void OutboxProcessingResult_DefaultConstructor_InitializesCollections()
    {
        var result = new OutboxProcessingResult();

        result.ProcessedMessageIds.Should().NotBeNull();
        result.ProcessedMessageIds.Should().BeEmpty();
        result.FailedMessageIds.Should().NotBeNull();
        result.FailedMessageIds.Should().BeEmpty();
        result.Success.Should().BeFalse();
        result.ProcessedCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.DeadLetterCount.Should().Be(0);
    }

    [Fact]
    public void OutboxProcessingResult_Duration_ReturnsCorrectTimeSpan()
    {
        var start = DateTime.UtcNow;
        var result = new OutboxProcessingResult
        {
            StartedAt = start,
            CompletedAt = start.AddSeconds(45)
        };

        result.Duration.Should().BeCloseTo(TimeSpan.FromSeconds(45), TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void OutboxProcessorConfig_DefaultValues_AreCorrect()
    {
        var config = new OutboxProcessorConfig();

        config.BatchSize.Should().Be(100);
        config.LockDuration.Should().Be(TimeSpan.FromMinutes(5));
        config.DelayBetweenBatches.Should().Be(TimeSpan.FromSeconds(30));
        config.MessagesBeforeBreak.Should().Be(1000);
        config.BreakDuration.Should().Be(TimeSpan.FromSeconds(10));
        config.EnableParallelProcessing.Should().BeTrue();
        config.MaxDegreeOfParallelism.Should().Be(4);
        config.EnableDeadLetterProcessing.Should().BeTrue();
    }

    [Fact]
    public void OutboxProcessorConfig_CustomValues_AreApplied()
    {
        var config = new OutboxProcessorConfig
        {
            BatchSize = 200,
            LockDuration = TimeSpan.FromMinutes(10),
            DelayBetweenBatches = TimeSpan.FromSeconds(60),
            MessagesBeforeBreak = 2000,
            BreakDuration = TimeSpan.FromSeconds(30),
            EnableParallelProcessing = false,
            MaxDegreeOfParallelism = 8,
            EnableDeadLetterProcessing = false
        };

        config.BatchSize.Should().Be(200);
        config.LockDuration.Should().Be(TimeSpan.FromMinutes(10));
        config.DelayBetweenBatches.Should().Be(TimeSpan.FromSeconds(60));
        config.MessagesBeforeBreak.Should().Be(2000);
        config.BreakDuration.Should().Be(TimeSpan.FromSeconds(30));
        config.EnableParallelProcessing.Should().BeFalse();
        config.MaxDegreeOfParallelism.Should().Be(8);
        config.EnableDeadLetterProcessing.Should().BeFalse();
    }

    [Fact]
    public void OutboxStatistics_DefaultValues_AreCorrect()
    {
        var stats = new OutboxStatistics();

        stats.TotalMessages.Should().Be(0);
        stats.PendingMessages.Should().Be(0);
        stats.ProcessingMessages.Should().Be(0);
        stats.PublishedMessages.Should().Be(0);
        stats.FailedMessages.Should().Be(0);
        stats.ArchivedMessages.Should().Be(0);
        stats.DeadLetterCount.Should().Be(0);
        stats.SuccessRate.Should().Be(0);
        stats.AveragePublishTime.Should().Be(TimeSpan.Zero);
        stats.OldestPendingAge.Should().BeNull();
    }

    [Fact]
    public void OutboxStatistics_SuccessRate_CalculatesCorrectly()
    {
        var stats = new OutboxStatistics
        {
            TotalMessages = 1000,
            PublishedMessages = 950,
            FailedMessages = 50
        };

        stats.SuccessRate.Should().Be(95.0);
    }

    [Fact]
    public void OutboxStatistics_SuccessRate_WithZeroTotal_ReturnsZero()
    {
        var stats = new OutboxStatistics { TotalMessages = 0 };

        stats.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void PublishingOptions_DefaultValues_AreCorrect()
    {
        var options = new PublishingOptions();

        options.MaxRetries.Should().Be(5);
        options.RetryPolicy.Should().Be(RetryPolicyType.ExponentialBackoff);
        options.InitialRetryDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.MaxRetryDelay.Should().Be(TimeSpan.FromMinutes(5));
        options.BackoffMultiplier.Should().Be(2.0);
        options.DeliveryGuarantee.Should().Be(DeliveryGuarantee.AtLeastOnce);
        options.UseJitter.Should().BeTrue();
        options.PublishTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.ClockSkewTolerance.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void PublishingOptions_CustomValues_AreApplied()
    {
        var options = new PublishingOptions
        {
            MaxRetries = 10,
            RetryPolicy = RetryPolicyType.Linear,
            InitialRetryDelay = TimeSpan.FromSeconds(10),
            MaxRetryDelay = TimeSpan.FromMinutes(10),
            BackoffMultiplier = 3.0,
            DeliveryGuarantee = DeliveryGuarantee.ExactlyOnce,
            UseJitter = false,
            PublishTimeout = TimeSpan.FromSeconds(60),
            ClockSkewTolerance = TimeSpan.FromMinutes(5)
        };

        options.MaxRetries.Should().Be(10);
        options.RetryPolicy.Should().Be(RetryPolicyType.Linear);
        options.InitialRetryDelay.Should().Be(TimeSpan.FromSeconds(10));
        options.MaxRetryDelay.Should().Be(TimeSpan.FromMinutes(10));
        options.BackoffMultiplier.Should().Be(3.0);
        options.DeliveryGuarantee.Should().Be(DeliveryGuarantee.ExactlyOnce);
        options.UseJitter.Should().BeFalse();
        options.PublishTimeout.Should().Be(TimeSpan.FromSeconds(60));
        options.ClockSkewTolerance.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void HealthMetrics_DefaultValues_AreCorrect()
    {
        var metrics = new HealthMetrics();

        metrics.IsHealthy.Should().BeTrue();
        metrics.LastSuccessfulPublish.Should().BeNull();
        metrics.ConsecutiveFailures.Should().Be(0);
        metrics.ErrorMessage.Should().BeNull();
        metrics.LastHealthCheckAt.Should().BeOnOrBefore(DateTime.UtcNow);
        metrics.LockedMessagesCount.Should().Be(0);
        metrics.HasExpiredLocks.Should().BeFalse();
        metrics.OldestMessageAge.Should().BeNull();
    }

    [Fact]
    public void HealthMetrics_UpdateProperties_WorksCorrectly()
    {
        var now = DateTime.UtcNow;
        var metrics = new HealthMetrics
        {
            IsHealthy = false,
            LastSuccessfulPublish = now,
            ConsecutiveFailures = 3,
            ErrorMessage = "Database connection failed",
            LastHealthCheckAt = now.AddMinutes(-2),
            LockedMessagesCount = 5,
            HasExpiredLocks = true,
            OldestMessageAge = TimeSpan.FromMinutes(15)
        };

        metrics.IsHealthy.Should().BeFalse();
        metrics.LastSuccessfulPublish.Should().Be(now);
        metrics.ConsecutiveFailures.Should().Be(3);
        metrics.ErrorMessage.Should().Be("Database connection failed");
        metrics.LastHealthCheckAt.Should().Be(now.AddMinutes(-2));
        metrics.LockedMessagesCount.Should().Be(5);
        metrics.HasExpiredLocks.Should().BeTrue();
        metrics.OldestMessageAge.Should().Be(TimeSpan.FromMinutes(15));
    }
}
