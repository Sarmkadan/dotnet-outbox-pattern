#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for the pure delay calculation in <see cref="OutboxBackoffExtensions.ComputeDelay"/>
/// that is kept separate from <see cref="OutboxProcessorOptions"/> so it can be unit-tested in isolation.
/// </summary>
public sealed class OutboxBackoffExtensionsTests
{
    [Fact]
    public void ComputeDelay_WithNullOptions_Throws()
    {
        OutboxProcessorOptions? options = null;
        var act = () => options!.ComputeDelay(0);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeDelay_WithZeroConsecutiveEmptyBatches_ReturnsBaseDelay()
    {
        var options = new OutboxProcessorOptions
        {
            BackoffStrategy = BackoffStrategy.Exponential,
            DelayBetweenBatches = 1000,
            MaxDelayBetweenBatches = 60000,
            BackoffMultiplier = 2.0
        };

        var delay = options.ComputeDelay(0);
        delay.Should().Be(TimeSpan.FromMilliseconds(1000));
    }

    [Fact]
    public void ComputeDelay_WithNegativeConsecutiveEmptyBatches_ReturnsBaseDelay()
    {
        var options = new OutboxProcessorOptions
        {
            BackoffStrategy = BackoffStrategy.Exponential,
            DelayBetweenBatches = 1500,
            MaxDelayBetweenBatches = 60000,
            BackoffMultiplier = 2.0
        };

        options.ComputeDelay(-1).Should().Be(TimeSpan.FromMilliseconds(1500));
        options.ComputeDelay(-100).Should().Be(TimeSpan.FromMilliseconds(1500));
        options.ComputeDelay(int.MinValue).Should().Be(TimeSpan.FromMilliseconds(1500));
    }

    [Fact]
    public void ComputeDelay_WithFixedStrategy_AlwaysReturnsBaseDelay()
    {
        var options = new OutboxProcessorOptions
        {
            BackoffStrategy = BackoffStrategy.Fixed,
            DelayBetweenBatches = 2500
        };

        options.ComputeDelay(0).Should().Be(TimeSpan.FromMilliseconds(2500));
        options.ComputeDelay(10).Should().Be(TimeSpan.FromMilliseconds(2500));
        options.ComputeDelay(100).Should().Be(TimeSpan.FromMilliseconds(2500));
    }

    [Fact]
    public void ComputeDelay_WithNoneStrategy_AlwaysReturnsBaseDelay()
    {
        var options = new OutboxProcessorOptions
        {
            BackoffStrategy = BackoffStrategy.None,
            DelayBetweenBatches = 3000
        };

        options.ComputeDelay(0).Should().Be(TimeSpan.FromMilliseconds(3000));
        options.ComputeDelay(5).Should().Be(TimeSpan.FromMilliseconds(3000));
        options.ComputeDelay(1000).Should().Be(TimeSpan.FromMilliseconds(3000));
    }

    [Theory]
    [InlineData(1, 1000, 2000)]
    [InlineData(2, 1000, 4000)]
    [InlineData(3, 1000, 8000)]
    [InlineData(5, 1000, 32000)]
    public void ComputeDelay_GrowsExponentiallyPerEmptyBatch(int emptyBatches, int baseDelayMs, int expectedMs)
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: baseDelayMs, maxDelayMs: 60000, multiplier: 2.0);

        options.ComputeDelay(emptyBatches).Should().Be(TimeSpan.FromMilliseconds(expectedMs));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(5, 0, 0)]
    public void ComputeDelay_WithBaseDelayZero_AlwaysReturnsZero(int emptyBatches, int baseDelayMs, int expectedMs)
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: baseDelayMs, maxDelayMs: 60000, multiplier: 2.0);

        options.ComputeDelay(emptyBatches).Should().Be(TimeSpan.FromMilliseconds(expectedMs));
    }

    [Fact]
    public void ComputeDelay_IsCappedAtMaxDelay()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 10000, multiplier: 2.0);

        // 2^10 * 1000 = 1024000, but should clamp to ceiling of 10000
        options.ComputeDelay(10).Should().Be(TimeSpan.FromMilliseconds(10000));

        // Even with very large attempt count, should never exceed max
        options.ComputeDelay(100).Should().Be(TimeSpan.FromMilliseconds(10000));
        options.ComputeDelay(int.MaxValue).Should().Be(TimeSpan.FromMilliseconds(10000));
    }

    [Fact]
    public void ComputeDelay_WithMaxDelayEqualToBaseDelay_ReturnsBaseDelay()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 5000, maxDelayMs: 5000, multiplier: 2.0);

        options.ComputeDelay(0).Should().Be(TimeSpan.FromMilliseconds(5000));
        options.ComputeDelay(1).Should().Be(TimeSpan.FromMilliseconds(5000));
        options.ComputeDelay(10).Should().Be(TimeSpan.FromMilliseconds(5000));
    }

    [Fact]
    public void ComputeDelay_WithCustomMultiplier_GrowsCorrectly()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 100, maxDelayMs: 10000, multiplier: 3.0);

        options.ComputeDelay(1).Should().Be(TimeSpan.FromMilliseconds(300)); // 100 * 3^1
        options.ComputeDelay(2).Should().Be(TimeSpan.FromMilliseconds(900)); // 100 * 3^2
        options.ComputeDelay(3).Should().Be(TimeSpan.FromMilliseconds(2700)); // 100 * 3^3
    }

    [Fact]
    public void ComputeDelay_WithMultiplierOne_GrowsLinearly()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 100, maxDelayMs: 10000, multiplier: 1.0);

        options.ComputeDelay(1).Should().Be(TimeSpan.FromMilliseconds(100)); // 100 * 1^1
        options.ComputeDelay(5).Should().Be(TimeSpan.FromMilliseconds(100)); // 100 * 1^5
        options.ComputeDelay(100).Should().Be(TimeSpan.FromMilliseconds(100)); // 100 * 1^100
    }

    [Fact]
    public void ComputeDelay_DoesNotOverflowWithLargeAttemptCount()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 45000, multiplier: 2.0);

        // Should not overflow even with int.MaxValue attempts
        var delay = options.ComputeDelay(int.MaxValue);
        delay.Should().Be(TimeSpan.FromMilliseconds(45000));
    }

    [Fact]
    public void ComputeDelay_WithMaxDelayLessThanBaseDelay_UsesBaseDelay()
    {
        // WithExponentialBackoff validates that max >= base, so we need to set it up manually
        var options = new OutboxProcessorOptions
        {
            BackoffStrategy = BackoffStrategy.Exponential,
            DelayBetweenBatches = 10000,
            MaxDelayBetweenBatches = 1000, // Less than base
            BackoffMultiplier = 2.0
        };

        // When max < base, the ceiling calculation uses Math.Max(base, max) which gives base
        options.ComputeDelay(0).Should().Be(TimeSpan.FromMilliseconds(10000));
        options.ComputeDelay(1).Should().Be(TimeSpan.FromMilliseconds(10000));
        options.ComputeDelay(5).Should().Be(TimeSpan.FromMilliseconds(10000));
    }

    [Theory]
    [InlineData(0, 100, 200, 2.0, 100)]
    [InlineData(1, 100, 200, 2.0, 200)]
    [InlineData(2, 100, 200, 2.0, 200)] // Capped at max
    [InlineData(1, 50, 150, 3.0, 150)]
    [InlineData(2, 50, 200, 3.0, 200)] // 50 * 3^2 = 450, capped at 200
    public void ComputeDelay_ComprehensiveScenarios(
        int emptyBatches,
        int baseDelayMs,
        int maxDelayMs,
        double multiplier,
        int expectedMs)
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: baseDelayMs, maxDelayMs: maxDelayMs, multiplier: multiplier);

        options.ComputeDelay(emptyBatches).Should().Be(TimeSpan.FromMilliseconds(expectedMs));
    }

    [Fact]
    public void ComputeDelay_WithNegativeBaseDelay_UsesZero()
    {
        // This shouldn't happen in practice due to WithExponentialBackoff validation,
        // but ComputeDelay should be defensive
        var options = new OutboxProcessorOptions
        {
            BackoffStrategy = BackoffStrategy.Exponential,
            DelayBetweenBatches = -1000, // Negative base delay
            MaxDelayBetweenBatches = 60000,
            BackoffMultiplier = 2.0
        };

        // ComputeDelay uses Math.Max(0, options.DelayBetweenBatches)
        options.ComputeDelay(0).Should().Be(TimeSpan.FromMilliseconds(0));
        options.ComputeDelay(1).Should().Be(TimeSpan.FromMilliseconds(0));
        options.ComputeDelay(5).Should().Be(TimeSpan.FromMilliseconds(0));
    }

    [Fact]
    public void ComputeDelay_WithZeroBaseDelayAndZeroMaxDelay_ReturnsZero()
    {
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: 0, maxDelayMs: 0, multiplier: 2.0);

        options.ComputeDelay(0).Should().Be(TimeSpan.Zero);
        options.ComputeDelay(1).Should().Be(TimeSpan.Zero);
        options.ComputeDelay(100).Should().Be(TimeSpan.Zero);
    }
}