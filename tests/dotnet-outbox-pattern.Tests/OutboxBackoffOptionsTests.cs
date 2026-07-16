#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for the configurable batch-size and idle-backoff options on
/// <see cref="OutboxProcessorOptions"/> and the helpers in
/// <see cref="OutboxBackoffExtensions"/>: the fluent builders, the aggregate validation, and
/// the pure delay calculation.
/// </summary>
public sealed class OutboxBackoffOptionsTests
{
    [Fact]
    public void WithBatchSize_SetsValue()
    {
        var options = new OutboxProcessorOptions().WithBatchSize(250);
        options.BatchSize.Should().Be(250);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithBatchSize_WithNonPositive_Throws(int batchSize)
    {
        var act = () => new OutboxProcessorOptions().WithBatchSize(batchSize);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithExponentialBackoff_SetsAllFields()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 30000, multiplier: 3.0);

        options.BackoffStrategy.Should().Be(BackoffStrategy.Exponential);
        options.DelayBetweenBatches.Should().Be(1000);
        options.MaxDelayBetweenBatches.Should().Be(30000);
        options.BackoffMultiplier.Should().Be(3.0);
    }

    [Fact]
    public void WithExponentialBackoff_WhenMaxBelowBase_Throws()
    {
        var act = () => new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 5000, maxDelayMs: 1000);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithExponentialBackoff_WhenMultiplierBelowOne_Throws()
    {
        var act = () => new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 30000, multiplier: 0.5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithFixedDelay_SetsStrategyAndDelay()
    {
        var options = new OutboxProcessorOptions().WithFixedDelay(2500);
        options.BackoffStrategy.Should().Be(BackoffStrategy.Fixed);
        options.DelayBetweenBatches.Should().Be(2500);
    }

    [Fact]
    public void WithFixedDelay_WhenNegative_Throws()
    {
        var act = () => new OutboxProcessorOptions().WithFixedDelay(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ValidateBackoff_WithDefaults_Passes()
    {
        var options = new OutboxProcessorOptions();
        options.ValidateBackoff().Should().BeSameAs(options);
    }

    [Fact]
    public void ValidateBackoff_WithZeroBatchSize_Throws()
    {
        var options = new OutboxProcessorOptions { BatchSize = 0 };
        var act = () => options.ValidateBackoff();
        act.Should().Throw<ArgumentException>().WithMessage("*BatchSize*");
    }

    [Fact]
    public void ValidateBackoff_WithMaxDelayBelowBase_Throws()
    {
        var options = new OutboxProcessorOptions
        {
            DelayBetweenBatches = 10000,
            MaxDelayBetweenBatches = 5000
        };
        var act = () => options.ValidateBackoff();
        act.Should().Throw<ArgumentException>().WithMessage("*MaxDelayBetweenBatches*");
    }

    [Fact]
    public void ValidateBackoff_ReportsAllProblemsAtOnce()
    {
        var options = new OutboxProcessorOptions
        {
            BatchSize = -5,
            DelayBetweenBatches = 10000,
            MaxDelayBetweenBatches = 1000,
            BackoffStrategy = BackoffStrategy.Exponential,
            BackoffMultiplier = 0.1
        };

        var act = () => options.ValidateBackoff();

        act.Should().Throw<ArgumentException>()
            .Where(e => e.Message.Contains("BatchSize")
                     && e.Message.Contains("MaxDelayBetweenBatches")
                     && e.Message.Contains("BackoffMultiplier"));
    }

    [Fact]
    public void ComputeDelay_WithNoBackoff_AlwaysReturnsBaseDelay()
    {
        var options = new OutboxProcessorOptions
        {
            BackoffStrategy = BackoffStrategy.None,
            DelayBetweenBatches = 5000
        };

        options.ComputeDelay(0).Should().Be(TimeSpan.FromMilliseconds(5000));
        options.ComputeDelay(10).Should().Be(TimeSpan.FromMilliseconds(5000));
    }

    [Fact]
    public void ComputeDelay_WhenLastBatchDidWork_ReturnsBaseDelay()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 60000, multiplier: 2.0);

        options.ComputeDelay(0).Should().Be(TimeSpan.FromMilliseconds(1000));
    }

    [Theory]
    [InlineData(1, 2000)]
    [InlineData(2, 4000)]
    [InlineData(3, 8000)]
    public void ComputeDelay_GrowsGeometricallyPerEmptyBatch(int emptyBatches, int expectedMs)
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 60000, multiplier: 2.0);

        options.ComputeDelay(emptyBatches).Should().Be(TimeSpan.FromMilliseconds(expectedMs));
    }

    [Fact]
    public void ComputeDelay_IsCappedAtMaxDelay()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 10000, multiplier: 2.0);

        // 2^20 * 1000 would be enormous; it must clamp to the ceiling.
        options.ComputeDelay(20).Should().Be(TimeSpan.FromMilliseconds(10000));
    }

    [Fact]
    public void ComputeDelay_WithHugeEmptyCount_DoesNotOverflow()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 45000, multiplier: 2.0);

        var delay = options.ComputeDelay(int.MaxValue);
        delay.Should().Be(TimeSpan.FromMilliseconds(45000));
    }

    [Fact]
    public void ComputeDelay_WithNegativeEmptyCount_ReturnsBaseDelay()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 1500, maxDelayMs: 60000, multiplier: 2.0);

        options.ComputeDelay(-3).Should().Be(TimeSpan.FromMilliseconds(1500));
    }
}
