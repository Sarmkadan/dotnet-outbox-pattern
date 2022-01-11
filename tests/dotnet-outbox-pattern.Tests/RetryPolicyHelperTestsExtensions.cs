#nullable enable

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

public static class RetryPolicyHelperTestsExtensions
{
    /// <summary>
    /// Creates a test scenario with the specified retry policy and validates the delay calculation
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="policyType">The retry policy type to test</param>
    /// <param name="initialDelay">Initial retry delay</param>
    /// <param name="expectedDelayAtAttempt">Function that returns expected delay for each attempt</param>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="expectedDelayAtAttempt"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialDelay"/> is negative</exception>
    /// <returns>The test instance for method chaining</returns>
    public static RetryPolicyHelperTests CalculateDelay_WithPolicyType_ShouldProduceExpectedDelays(
        this RetryPolicyHelperTests test,
        RetryPolicyType policyType,
        TimeSpan initialDelay,
        Func<int, TimeSpan> expectedDelayAtAttempt)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(expectedDelayAtAttempt);
        ArgumentOutOfRangeException.ThrowIfNegative(initialDelay.TotalMilliseconds);

        var options = new PublishingOptions
        {
            RetryPolicy = policyType,
            InitialRetryDelay = initialDelay,
            UseJitter = false
        };

        var attemptCount = policyType == RetryPolicyType.NoRetry ? 1 : 5;

        for (int attempt = 1; attempt <= attemptCount; attempt++)
        {
            var actualDelay = RetryPolicyHelper.CalculateDelay(attempt, options);
            var expectedDelay = expectedDelayAtAttempt(attempt);

            actualDelay.Should().BeCloseTo(expectedDelay, TimeSpan.FromMilliseconds(100));
        }

        return test;
    }

    /// <summary>
    /// Validates that the statistics calculation matches expected values
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="options">Publishing options to use</param>
    /// <param name="maxAttempts">Maximum number of attempts</param>
    /// <param name="expectedTotalRetries">Expected total retries</param>
    /// <param name="expectedTotalDelay">Expected total delay time</param>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="options"/> is <see langword="null"/></exception>
    /// <returns>The test instance for method chaining</returns>
    public static RetryPolicyHelperTests CalculateStatistics_ShouldMatchExpectedValues(
        this RetryPolicyHelperTests test,
        PublishingOptions options,
        int maxAttempts,
        int expectedTotalRetries,
        TimeSpan expectedTotalDelay)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);

        var stats = RetryPolicyHelper.CalculateStatistics(options, maxAttempts);

        stats.TotalRetries.Should().Be(expectedTotalRetries);
        stats.TotalDelayTime.Should().BeCloseTo(expectedTotalDelay, TimeSpan.FromMilliseconds(100));
        stats.MaxAttempts.Should().Be(maxAttempts);

        return test;
    }

    /// <summary>
    /// Validates that jitter produces delays within expected range
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="policyType">The retry policy type</param>
    /// <param name="initialDelay">Initial retry delay</param>
    /// <param name="attemptNumber">Attempt number to test</param>
    /// <param name="minExpectedSeconds">Minimum expected delay in seconds</param>
    /// <param name="maxExpectedSeconds">Maximum expected delay in seconds</param>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialDelay"/> is negative or <paramref name="attemptNumber"/> is not positive</exception>
    /// <returns>The test instance for method chaining</returns>
    public static RetryPolicyHelperTests CalculateDelay_WithJitter_ShouldBeWithinRange(
        this RetryPolicyHelperTests test,
        RetryPolicyType policyType,
        TimeSpan initialDelay,
        int attemptNumber,
        double minExpectedSeconds,
        double maxExpectedSeconds)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentOutOfRangeException.ThrowIfNegative(initialDelay.TotalMilliseconds);
        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be positive");
        }
        ArgumentOutOfRangeException.ThrowIfNegative(minExpectedSeconds);
        ArgumentOutOfRangeException.ThrowIfNegative(maxExpectedSeconds);

        var options = new PublishingOptions
        {
            RetryPolicy = policyType,
            InitialRetryDelay = initialDelay,
            UseJitter = true
        };

        var delay = RetryPolicyHelper.CalculateDelay(attemptNumber, options);

        delay.TotalSeconds.Should().BeGreaterThanOrEqualTo(minExpectedSeconds);
        delay.TotalSeconds.Should().BeLessThanOrEqualTo(maxExpectedSeconds);

        return test;
    }
}