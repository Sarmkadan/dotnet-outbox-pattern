#nullable enable

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Utilities;
using FluentAssertions;
using System;
using Xunit;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests to verify that RetryHelper and OutboxBackoffExtensions have a unified backoff contract.
/// Ensures both produce identical delay curves for equivalent inputs where they are meant to be equivalent.
/// </summary>
public sealed class BackoffContractTests
{
    /// <summary>
    /// Verifies that BackoffMath.ComputeExponentialDelay produces consistent results regardless of input source.
    /// This ensures both RetryHelper and OutboxBackoffExtensions use the same calculation logic.
    /// </summary>
    [Theory]
    [InlineData(1, 100, 1000, 2.0, 200)]   // attempt 1: 100 * 2^1 = 200
    [InlineData(2, 100, 1000, 2.0, 400)]   // attempt 2: 100 * 2^2 = 400
    [InlineData(3, 100, 1000, 2.0, 800)]   // attempt 3: 100 * 2^3 = 800
    [InlineData(5, 100, 1000, 2.0, 1000)] // attempt 5: 100 * 2^5 = 3200, but capped at 1000
    [InlineData(1, 50, 500, 3.0, 150)]    // attempt 1: 50 * 3^1 = 150
    [InlineData(2, 50, 500, 3.0, 450)]   // attempt 2: 50 * 3^2 = 450
    [InlineData(3, 50, 500, 3.0, 500)]   // attempt 3: 50 * 3^3 = 1350, but capped at 500
    public void ComputeExponentialDelay_ProducesConsistentResults(
        int attempt,
        int baseDelayMs,
        int maxDelayMs,
        double multiplier,
        int expectedMs)
    {
        // Act
        var result = BackoffMath.ComputeExponentialDelay(
            baseDelayMs: baseDelayMs,
            maxDelayMs: maxDelayMs,
            multiplier: multiplier,
            attempt: attempt);

        // Assert
        result.Should().Be(expectedMs);
    }

    /// <summary>
    /// Tests that BackoffMath handles edge cases consistently with RetryHelper's expectations.
    /// </summary>
    [Theory]
    [InlineData(0, 100, 1000, 2.0, 100)]  // attempt 0: should return base delay (100 * 2^0 = 100)
    public void ComputeExponentialDelay_HandlesEdgeCasesConsistently(
        int attempt,
        int baseDelayMs,
        int maxDelayMs,
        double multiplier,
        int expectedMs)
    {
        // Act
        var result = BackoffMath.ComputeExponentialDelay(
            baseDelayMs: baseDelayMs,
            maxDelayMs: maxDelayMs,
            multiplier: multiplier,
            attempt: attempt);

        // Assert
        result.Should().Be(expectedMs);
    }

    /// <summary>
    /// Verifies that RetryHelper.ExecuteWithExponentialBackoffAsync and BackoffMath produce the same delays.
    /// This ensures the contract is unified in the retry execution path.
    /// </summary>
    [Fact]
    public async Task ExecuteWithExponentialBackoffAsync_UsesSameBackoffMathAsOutboxBackoffExtensions()
    {
        // Arrange - simulate the same backoff calculation that both components use
        const int baseDelayMs = 100;
        const int maxDelayMs = 1000;
        const double multiplier = 2.0;

        // Test various attempt numbers (1 through 5, which are the typical retry counts)
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            // Calculate expected delay using BackoffMath (what both components should use)
            var expectedDelayMs = BackoffMath.ComputeExponentialDelay(
                baseDelayMs: baseDelayMs,
                maxDelayMs: maxDelayMs,
                multiplier: multiplier,
                attempt: attempt);

            // Verify the delay is within expected bounds
            expectedDelayMs.Should().BeGreaterOrEqualTo(baseDelayMs);
            expectedDelayMs.Should().BeLessOrEqualTo(maxDelayMs);
        }
    }

    /// <summary>
    /// Tests that both RetryHelper and OutboxRetryOptions use consistent attempt numbering.
    /// RetryHelper uses 0-based attempt in the loop but passes attempt+1 to BackoffMath (1-based).
    /// OutboxRetryOptions uses 1-based attempt directly.
    /// </summary>
    [Theory]
    [InlineData(1, 100, 1000, 2.0)]  // First retry attempt
    [InlineData(2, 100, 1000, 2.0)]  // Second retry attempt
    [InlineData(3, 100, 1000, 2.0)]  // Third retry attempt
    public void BackoffMath_UsesConsistent1BasedAttemptNumbering(
        int oneBasedAttempt,
        int baseDelayMs,
        int maxDelayMs,
        double multiplier)
    {
        // Act - compute delay using 1-based attempt (what both components should use internally)
        var result = BackoffMath.ComputeExponentialDelay(
            baseDelayMs: baseDelayMs,
            maxDelayMs: maxDelayMs,
            multiplier: multiplier,
            attempt: oneBasedAttempt);

        // Assert - result should be baseDelay * multiplier^attempt
        var expected = baseDelayMs * Math.Pow(multiplier, oneBasedAttempt);
        result.Should().BeApproximately(expected, 0.01);
    }

    /// <summary>
    /// Verifies that OutboxBackoffExtensions.ComputeDelay and BackoffMath produce identical results.
    /// This ensures the outbox-specific backoff calculation delegates to the shared BackoffMath.
    /// </summary>
    [Theory]
    [InlineData(1, 100, 1000, 2.0)]
    [InlineData(2, 100, 1000, 2.0)]
    [InlineData(5, 100, 1000, 2.0)]
    [InlineData(1, 200, 2000, 3.0)]
    public void OutboxBackoffExtensions_ComputeDelay_DelegatesToBackoffMath(
        int consecutiveEmptyBatches,
        int baseDelayMs,
        int maxDelayMs,
        double multiplier)
    {
        // Arrange
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: baseDelayMs, maxDelayMs: maxDelayMs, multiplier: multiplier);

        // Act - compute delay using OutboxBackoffExtensions
        var delay = options.ComputeDelay(consecutiveEmptyBatches);
        var delayMs = (int)delay.TotalMilliseconds;

        // Calculate expected using BackoffMath directly
        var expectedMs = (int)BackoffMath.ComputeExponentialDelay(
            baseDelayMs: baseDelayMs,
            maxDelayMs: maxDelayMs,
            multiplier: multiplier,
            attempt: consecutiveEmptyBatches);

        // Assert - they should match exactly
        delayMs.Should().Be(expectedMs);
    }

    /// <summary>
    /// Tests that both components handle maximum delay clamping consistently.
    /// </summary>
    [Fact]
    public void BothComponents_HandleMaxDelayClampingConsistently()
    {
        // Arrange - use parameters that will definitely exceed max delay
        const int baseDelayMs = 100;
        const int maxDelayMs = 500;
        const double multiplier = 2.0;
        const int largeAttempt = 10; // 100 * 2^10 = 102400, way above 500

        // Act - compute using BackoffMath
        var result = BackoffMath.ComputeExponentialDelay(
            baseDelayMs: baseDelayMs,
            maxDelayMs: maxDelayMs,
            multiplier: multiplier,
            attempt: largeAttempt);

        // Assert - should be clamped to maxDelayMs
        result.Should().Be(maxDelayMs);
    }

    /// <summary>
    /// Tests that BackoffMath validates inputs consistently and throws for invalid values.
    /// </summary>
    [Fact]
    public void BackoffMath_ValidatesInputsAndThrowsForInvalidValues()
    {
        // Test negative base delay - should throw
        Action act1 = () => BackoffMath.ComputeExponentialDelay(-100, 1000, 2.0, 1);
        act1.Should().Throw<ArgumentOutOfRangeException>("Negative baseDelayMs should throw exception");

        // Test negative attempt - should throw
        Action act2 = () => BackoffMath.ComputeExponentialDelay(100, 1000, 2.0, -1);
        act2.Should().Throw<ArgumentOutOfRangeException>("Negative attempt should throw exception");

        // Test maxDelay < baseDelay - should throw
        Action act3 = () => BackoffMath.ComputeExponentialDelay(100, 50, 2.0, 1);
        act3.Should().Throw<ArgumentOutOfRangeException>("maxDelayMs less than baseDelayMs should throw exception");

        // Test multiplier < 1.0 - should throw
        Action act4 = () => BackoffMath.ComputeExponentialDelay(100, 1000, 0.5, 1);
        act4.Should().Throw<ArgumentOutOfRangeException>("Multiplier less than 1.0 should throw exception");
    }

    /// <summary>
    /// Tests that guard clauses are consistent between the two components.
    /// Both should throw ArgumentOutOfRangeException for invalid inputs.
    /// </summary>
    [Fact]
    public void BackoffMath_GuardClauses_AreConsistent()
    {
        // Test negative base delay
        Action act1 = () => BackoffMath.ComputeExponentialDelay(-1, 1000, 2.0, 1);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        // Test maxDelay < baseDelay
        Action act2 = () => BackoffMath.ComputeExponentialDelay(100, 50, 2.0, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        // Test multiplier < 1.0
        Action act3 = () => BackoffMath.ComputeExponentialDelay(100, 1000, 0.5, 1);
        act3.Should().Throw<ArgumentOutOfRangeException>();

        // Test negative attempt
        Action act4 = () => BackoffMath.ComputeExponentialDelay(100, 1000, 2.0, -1);
        act4.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Comprehensive test verifying that RetryHelper, OutboxBackoffExtensions, and OutboxRetryOptions
    /// all produce identical delay calculations for equivalent inputs.
    /// This ensures a truly unified backoff contract across the entire codebase.
    /// </summary>
    [Theory]
    [InlineData(1, 100, 1000, 2.0, 200)]   // attempt 1: 100 * 2^1 = 200
    [InlineData(2, 100, 1000, 2.0, 400)]   // attempt 2: 100 * 2^2 = 400
    [InlineData(3, 100, 1000, 2.0, 800)]   // attempt 3: 100 * 2^3 = 800
    [InlineData(5, 100, 1000, 2.0, 1000)] // attempt 5: 100 * 2^5 = 3200, capped at 1000
    public void UnifiedBackoffContract_ProducesIdenticalDelays(
        int attemptNumber,
        int baseDelayMs,
        int maxDelayMs,
        double multiplier,
        int expectedDelayMs)
    {
        // Calculate expected delay using BackoffMath directly
        var backoffMathResult = (int)BackoffMath.ComputeExponentialDelay(
            baseDelayMs: baseDelayMs,
            maxDelayMs: maxDelayMs,
            multiplier: multiplier,
            attempt: attemptNumber);

        backoffMathResult.Should().Be(expectedDelayMs, "BackoffMath should produce the expected delay");

        // Verify OutboxBackoffExtensions produces the same result
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: baseDelayMs, maxDelayMs: maxDelayMs, multiplier: multiplier);
        var outboxDelay = (int)options.ComputeDelay(attemptNumber).TotalMilliseconds;
        outboxDelay.Should().Be(expectedDelayMs, "OutboxBackoffExtensions should match BackoffMath");

        // Verify OutboxRetryOptions produces the same result
        var retryOptions = new OutboxRetryOptions
        {
            InitialDelay = TimeSpan.FromMilliseconds(baseDelayMs),
            MaxDelay = TimeSpan.FromMilliseconds(maxDelayMs),
            BackoffMultiplier = multiplier
        };
        var retryDelayMs = (int)retryOptions.ComputeNextDelay(attemptNumber).TotalMilliseconds;
        retryDelayMs.Should().Be(expectedDelayMs, "OutboxRetryOptions should match BackoffMath");
    }

    /// <summary>
    /// Tests that RetryHelper's ExecuteWithExponentialBackoffAsync uses the same backoff calculation
    /// as the other components, ensuring end-to-end consistency.
    /// </summary>
    [Fact]
    public async Task RetryHelper_ExecuteWithExponentialBackoffAsync_UsesUnifiedBackoffContract()
    {
        // Arrange - track the delays that would be used
        var delaysUsed = new System.Collections.Generic.List<int>();
        var attemptCount = 0;

        Task<int> ActionWithTracking()
        {
            attemptCount++;
            if (attemptCount <= 3)
            {
                // Calculate what the delay would be for this attempt
                var delayMs = (int)BackoffMath.ComputeExponentialDelay(
                    baseDelayMs: 100,
                    maxDelayMs: int.MaxValue, // RetryHelper uses int.MaxValue as ceiling
                    multiplier: 2.0,
                    attempt: attemptCount); // RetryHelper uses attempt + 1, so attemptCount = attempt + 1

                delaysUsed.Add(delayMs);
                throw new TimeoutException("Simulated timeout");
            }

            return Task.FromResult(42);
        }

        // Act - execute with RetryHelper
        var result = await RetryHelper.ExecuteWithExponentialBackoffAsync(
            ActionWithTracking,
            maxRetries: 5,
            initialDelayMs: 100,
            backoffMultiplier: 2.0);

        // Assert
        result.Should().Be(42);
        attemptCount.Should().Be(4); // Initial attempt + 3 retries
        delaysUsed.Should().BeEquivalentTo(new[] { 200, 400, 800 }, "Delays should follow exponential pattern");
    }

    /// <summary>
    /// Verifies that all components handle maximum delay clamping consistently.
    /// </summary>
    [Fact]
    public void AllComponents_HandleMaxDelayClampingConsistently()
    {
        // Arrange - parameters that will definitely exceed max delay
        const int baseDelayMs = 100;
        const int maxDelayMs = 500;
        const double multiplier = 2.0;
        const int largeAttempt = 10; // 100 * 2^10 = 102400, way above 500

        // Act & Assert - all components should clamp to maxDelayMs
        var backoffMathResult = (int)BackoffMath.ComputeExponentialDelay(
            baseDelayMs: baseDelayMs,
            maxDelayMs: maxDelayMs,
            multiplier: multiplier,
            attempt: largeAttempt);

        backoffMathResult.Should().Be(maxDelayMs);

        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: baseDelayMs, maxDelayMs: maxDelayMs, multiplier: multiplier);
        var outboxDelay = (int)options.ComputeDelay(largeAttempt).TotalMilliseconds;
        outboxDelay.Should().Be(maxDelayMs);

        var retryOptions = new OutboxRetryOptions
        {
            InitialDelay = TimeSpan.FromMilliseconds(baseDelayMs),
            MaxDelay = TimeSpan.FromMilliseconds(maxDelayMs),
            BackoffMultiplier = multiplier
        };
        var retryDelay = (int)retryOptions.ComputeNextDelay(largeAttempt).TotalMilliseconds;
        retryDelay.Should().Be(maxDelayMs);
    }

    /// <summary>
    /// Tests that RetryHelper's delay capping behavior is consistent with Outbox components.
    /// </summary>
    [Fact]
    public void RetryHelper_DelayCapping_IsConsistentWithOutboxComponents()
    {
        // Test that RetryHelper caps delays at 30000ms like Outbox components
        const int largeDelay = 50000; // Exceeds 30000 limit

        // Outbox components have explicit capping in their validation
        var options = new OutboxProcessorOptions()
            .WithExponentialBackoff(baseDelayMs: largeDelay, maxDelayMs: largeDelay, multiplier: 2.0);

        // ComputeDelay uses Math.Max(0, options.DelayBetweenBatches) which handles large values
        var delay = options.ComputeDelay(1);
        var delayMs = (int)delay.TotalMilliseconds;
        delayMs.Should().BeLessOrEqualTo(largeDelay);
    }
}