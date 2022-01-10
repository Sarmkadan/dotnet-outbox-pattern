#nullable enable

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides unit tests for the <see cref="RetryPolicyHelper"/> class.
/// Tests various retry policy calculation scenarios including fixed interval, linear backoff,
/// exponential backoff, jitter, and edge cases.
/// </summary>
public sealed class RetryPolicyHelperTests
{
    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> throws an <see cref="ArgumentException"/>
    /// when the attempt number is zero.
    /// </summary>
    [Fact]
    public void CalculateDelay_WithZeroAttempt_ThrowsArgumentException()
    {
        var options = new PublishingOptions();
        var act = () => RetryPolicyHelper.CalculateDelay(0, options);
        act.Should().Throw<ArgumentException>().WithParameterName("attemptNumber");
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> throws an <see cref="ArgumentException"/>
    /// when the attempt number is negative.
    /// </summary>
    [Fact]
    public void CalculateDelay_WithNegativeAttempt_ThrowsArgumentException()
    {
        var options = new PublishingOptions();
        var act = () => RetryPolicyHelper.CalculateDelay(-1, options);
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> returns <see cref="TimeSpan.Zero"/>
    /// when the retry policy is set to <see cref="RetryPolicyType.NoRetry"/>.
    /// </summary>
    [Fact]
    public void CalculateDelay_WithNoRetryPolicy_ReturnsZero()
    {
        var options = new PublishingOptions { RetryPolicy = RetryPolicyType.NoRetry };
        var delay = RetryPolicyHelper.CalculateDelay(1, options);
        delay.Should().Be(TimeSpan.Zero);
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> returns the same delay for each attempt
    /// when using <see cref="RetryPolicyType.FixedInterval"/> retry policy.
    /// </summary>
    [Fact]
    public void CalculateDelay_WithFixedIntervalPolicy_ReturnsSameDelayEachAttempt()
    {
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.FixedInterval,
            InitialRetryDelay = TimeSpan.FromSeconds(2)
        };

        var delay1 = RetryPolicyHelper.CalculateDelay(1, options);
        var delay2 = RetryPolicyHelper.CalculateDelay(2, options);
        var delay3 = RetryPolicyHelper.CalculateDelay(3, options);

        delay1.TotalSeconds.Should().BeApproximately(2, 0.5);
        delay2.TotalSeconds.Should().BeApproximately(2, 0.5);
        delay3.TotalSeconds.Should().BeApproximately(2, 0.5);
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> returns progressively increasing delays
    /// when using <see cref="RetryPolicyType.LinearBackoff"/> retry policy.
    /// </summary>
    [Fact]
    public void CalculateDelay_WithLinearBackoffPolicy_IncreasesProperly()
    {
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.LinearBackoff,
            InitialRetryDelay = TimeSpan.FromSeconds(1),
            MaxRetryDelay = TimeSpan.FromSeconds(60)
        };

        var delay1 = RetryPolicyHelper.CalculateDelay(1, options);
        var delay2 = RetryPolicyHelper.CalculateDelay(2, options);
        var delay3 = RetryPolicyHelper.CalculateDelay(3, options);

        delay1.TotalSeconds.Should().BeGreaterThan(0);
        delay2.TotalSeconds.Should().BeGreaterThan(delay1.TotalSeconds);
        delay3.TotalSeconds.Should().BeGreaterThan(delay2.TotalSeconds);
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> returns exponentially increasing delays
    /// when using <see cref="RetryPolicyType.ExponentialBackoff"/> retry policy.
    /// </summary>
    [Fact]
    public void CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases()
    {
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.ExponentialBackoff,
            InitialRetryDelay = TimeSpan.FromSeconds(1),
            BackoffMultiplier = 2,
            MaxRetryDelay = TimeSpan.FromSeconds(60),
            UseJitter = false
        };

        var delay1 = RetryPolicyHelper.CalculateDelay(1, options);
        var delay2 = RetryPolicyHelper.CalculateDelay(2, options);
        var delay3 = RetryPolicyHelper.CalculateDelay(3, options);

        delay1.TotalSeconds.Should().BeApproximately(1, 0.1);
        delay2.TotalSeconds.Should().BeApproximately(2, 0.1);
        delay3.TotalSeconds.Should().BeApproximately(4, 0.1);
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> respects the maximum delay limit
    /// specified in the publishing options when using exponential backoff.
    /// </summary>
    [Fact]
    public void CalculateDelay_RespectMaxDelayLimit()
    {
        var maxDelay = TimeSpan.FromSeconds(10);
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.ExponentialBackoff,
            InitialRetryDelay = TimeSpan.FromSeconds(1),
            BackoffMultiplier = 2,
            MaxRetryDelay = maxDelay,
            UseJitter = false
        };

        for (int i = 1; i <= 10; i++)
        {
            var delay = RetryPolicyHelper.CalculateDelay(i, options);
            delay.Should().BeLessThanOrEqualTo(maxDelay.Add(TimeSpan.FromMilliseconds(100)));
        }
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> adds randomness to the delay
    /// when jitter is enabled in the publishing options.
    /// </summary>
    [Fact]
    public void CalculateDelay_WithJitterEnabled_AddsRandomness()
    {
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.FixedInterval,
            InitialRetryDelay = TimeSpan.FromSeconds(10),
            UseJitter = true
        };

        var delays = Enumerable.Range(0, 5)
            .Select(_ => RetryPolicyHelper.CalculateDelay(1, options).TotalSeconds)
            .ToList();

        var hasVariation = delays.Distinct().Count() > 1;
        hasVariation.Should().BeTrue("jitter should produce variation");
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> produces the same delay value
    /// when jitter is disabled in the publishing options.
    /// </summary>
    [Fact]
    public void CalculateDelay_WithJitterDisabled_ProducesSameDelay()
    {
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.FixedInterval,
            InitialRetryDelay = TimeSpan.FromSeconds(5),
            UseJitter = false
        };

        var delay1 = RetryPolicyHelper.CalculateDelay(1, options).TotalSeconds;
        var delay2 = RetryPolicyHelper.CalculateDelay(1, options).TotalSeconds;

        delay1.Should().Be(delay2);
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateDelay"/> returns a minimum delay of one second
    /// even when the initial retry delay is set to a smaller value.
    /// </summary>
    [Fact]
    public void CalculateDelay_ReturnsMinimumOneSecond()
    {
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.FixedInterval,
            InitialRetryDelay = TimeSpan.FromMilliseconds(100),
            UseJitter = false
        };

        var delay = RetryPolicyHelper.CalculateDelay(1, options);
        delay.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateStatistics"/> returns correct statistics
    /// including max attempts, total retries, retry policy type, and total delay time.
    /// </summary>
    [Fact]
    public void CalculateStatistics_ReturnsProperValues()
    {
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.FixedInterval,
            InitialRetryDelay = TimeSpan.FromSeconds(2)
        };

        var stats = RetryPolicyHelper.CalculateStatistics(options, maxAttempts: 5);

        stats.MaxAttempts.Should().Be(5);
        stats.TotalRetries.Should().Be(4);
        stats.RetryPolicy.Should().Be(RetryPolicyType.FixedInterval);
        stats.TotalDelayTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateStatistics"/> returns zero retries and zero total delay time
    /// when maxAttempts is set to 1.
    /// </summary>
    [Fact]
    public void CalculateStatistics_WithOneAttempt_HasZeroRetries()
    {
        var options = new PublishingOptions();
        var stats = RetryPolicyHelper.CalculateStatistics(options, maxAttempts: 1);

        stats.TotalRetries.Should().Be(0);
        stats.TotalDelayTime.Should().Be(TimeSpan.Zero);
    }

    /// <summary>
    /// Tests that <see cref="RetryPolicyHelper.CalculateStatistics"/> correctly calculates the average retry delay
    /// by comparing it to the total delay divided by the number of retries.
    /// </summary>
    [Fact]
    public void CalculateStatistics_CalculatesAverageCorrectly()
    {
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.FixedInterval,
            InitialRetryDelay = TimeSpan.FromSeconds(2),
            UseJitter = false
        };

        var stats = RetryPolicyHelper.CalculateStatistics(options, maxAttempts: 4);

        var totalSeconds = stats.TotalDelayTime.TotalSeconds;
        var averageSeconds = stats.AverageRetryDelay.TotalSeconds;
        averageSeconds.Should().BeApproximately(totalSeconds / stats.TotalRetries, 0.1);
    }
}
