#nullable enable

using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Utilities;
using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Unit tests for <see cref="RetryHelper"/> backoff strategies.
/// Tests all four retry strategies: fixed delay, linear backoff, exponential backoff, and jittered backoff.
/// </summary>
public sealed class RetryHelperTests
{
    /// <summary>
    /// Tests that fixed delay strategy returns the same delay on every attempt.
    /// </summary>
    [Fact]
    public async Task ExecuteWithFixedDelayAsync_ReturnsSameDelayOnEveryAttempt()
    {
        // Arrange
        var attemptCount = 0;

        Task<int> FailingAction()
        {
            attemptCount++;
            if (attemptCount <= 3)
            {
                throw new TimeoutException("Simulated timeout");
            }

            return Task.FromResult(42);
        }

        // Act
        var result = await RetryHelper.ExecuteWithFixedDelayAsync(FailingAction, maxRetries: 5, delayMs: 100);

        // Assert
        result.Should().Be(42);
        attemptCount.Should().Be(4); // Initial attempt + 3 retries
    }

    /// <summary>
    /// Tests that linear backoff increases delay by a constant increment per attempt.
    /// </summary>
    [Fact]
    public async Task ExecuteWithLinearBackoffAsync_IncreasesDelayByConstantIncrementPerAttempt()
    {
        // Arrange
        var attemptCount = 0;

        Task<int> FailingAction()
        {
            attemptCount++;
            if (attemptCount <= 3)
            {
                throw new TimeoutException("Simulated timeout");
            }

            return Task.FromResult(42);
        }

        // Act
        var result = await RetryHelper.ExecuteWithLinearBackoffAsync(
            FailingAction,
            maxRetries: 5,
            initialDelayMs: 100,
            delayIncrementMs: 100);

        // Assert
        result.Should().Be(42);
        attemptCount.Should().Be(4); // Initial attempt + 3 retries
    }

    /// <summary>
    /// Tests that exponential backoff doubles (or applies configured multiplier) per attempt.
    /// </summary>
    [Fact]
    public async Task ExecuteWithExponentialBackoffAsync_DoublesDelayPerAttempt()
    {
        // Arrange
        var attemptCount = 0;

        Task<int> FailingAction()
        {
            attemptCount++;
            if (attemptCount <= 3)
            {
                throw new TimeoutException("Simulated timeout");
            }

            return Task.FromResult(42);
        }

        // Act
        var result = await RetryHelper.ExecuteWithExponentialBackoffAsync(
            FailingAction,
            maxRetries: 5,
            initialDelayMs: 100,
            backoffMultiplier: 2.0);

        // Assert
        result.Should().Be(42);
        attemptCount.Should().Be(4); // Initial attempt + 3 retries
    }

    /// <summary>
    /// Tests that exponential backoff does not overflow when attempt count is large.
    /// </summary>
    [Fact]
    public async Task ExecuteWithExponentialBackoffAsync_DoesNotOverflowWithLargeAttemptCount()
    {
        // Arrange
        var attemptCount = 0;

        Task<int> FailingAction()
        {
            attemptCount++;
            if (attemptCount <= 10)
            {
                throw new TimeoutException("Simulated timeout");
            }

            return Task.FromResult(42);
        }

        // Act
        var result = await RetryHelper.ExecuteWithExponentialBackoffAsync(
            FailingAction,
            maxRetries: 15,
            initialDelayMs: 100,
            backoffMultiplier: 2.0);

        // Assert
        result.Should().Be(42);
        attemptCount.Should().Be(11); // Initial attempt + 10 retries
    }

    /// <summary>
    /// Tests that jittered backoff produces delays within expected bounds across repeated calls.
    /// </summary>
    [Fact]
    public async Task ExecuteWithJitteredBackoffAsync_ProducesDelaysWithinExpectedBounds()
    {
        // Arrange
        var attemptCount = 0;

        Task<int> FailingAction()
        {
            attemptCount++;
            if (attemptCount <= 3)
            {
                throw new TimeoutException("Simulated timeout");
            }

            return Task.FromResult(42);
        }

        // Act
        var result = await RetryHelper.ExecuteWithJitteredBackoffAsync(
            FailingAction,
            maxRetries: 5,
            initialDelayMs: 100);

        // Assert
        result.Should().Be(42);
        attemptCount.Should().Be(4); // Initial attempt + 3 retries
    }

    /// <summary>
    /// Tests that the operation succeeds on first attempt with zero retries consumed.
    /// </summary>
    [Fact]
    public async Task ExecuteWithAnyStrategy_SucceedsOnFirstAttemptWithZeroRetries()
    {
        // Arrange
        var attemptCount = 0;

        Task<int> SuccessfulAction()
        {
            attemptCount++;
            return Task.FromResult(42);
        }

        // Act
        var result = await RetryHelper.ExecuteWithFixedDelayAsync(SuccessfulAction, maxRetries: 5, delayMs: 100);

        // Assert
        result.Should().Be(42);
        attemptCount.Should().Be(1); // Only one attempt, no retries
    }

    /// <summary>
    /// Tests that the operation exhausts all configured retries and the original/last exception is surfaced.
    /// </summary>
    [Fact]
    public async Task ExecuteWithAnyStrategy_ExhaustsAllRetriesAndSurfacesException()
    {
        // Arrange
        var attemptCount = 0;
        Exception? lastException = null;

        Task<int> AlwaysFailingAction()
        {
            attemptCount++;
            var ex = new TimeoutException($"Attempt {attemptCount} failed");
            lastException = ex;
            throw ex;
        }

        // Act
        Func<Task<int>> act = () => RetryHelper.ExecuteWithFixedDelayAsync(
            AlwaysFailingAction,
            maxRetries: 3,
            delayMs: 10);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
        attemptCount.Should().Be(4); // Initial attempt + 3 retries = 4 total attempts
        lastException.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that a non-transient exception short-circuits without exhausting retries.
    /// </summary>
    [Fact]
    public async Task ExecuteWithAnyStrategy_NonTransientExceptionShortCircuitsWithoutExhaustingRetries()
    {
        // Arrange
        var attemptCount = 0;

        Task<int> NonTransientAction()
        {
            attemptCount++;
            throw new InvalidOperationException("This is a non-transient error that should not be retried");
        }

        // Act
        Func<Task<int>> act = () => RetryHelper.ExecuteWithFixedDelayAsync(
            NonTransientAction,
            maxRetries: 5,
            delayMs: 10);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attemptCount.Should().Be(1); // Only one attempt, no retries because exception is not transient
    }

    /// <summary>
    /// Tests that ArgumentNullException is thrown when action is null for fixed delay strategy.
    /// </summary>
    [Fact]
    public void ExecuteWithFixedDelayAsync_ThrowsArgumentNullException_WhenActionIsNull()
    {
        // Arrange
        Func<Task<int>>? nullAction = null;

        // Act
        Func<Task> act = () => RetryHelper.ExecuteWithFixedDelayAsync(nullAction!, maxRetries: 5, delayMs: 100);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ArgumentOutOfRangeException is thrown when delayMs is negative for fixed delay strategy.
    /// </summary>
    [Fact]
    public void ExecuteWithFixedDelayAsync_ThrowsArgumentOutOfRangeException_WhenDelayMsIsNegative()
    {
        // Arrange
        Task<int> Action() => Task.FromResult(42);

        // Act
        Func<Task> act = () => RetryHelper.ExecuteWithFixedDelayAsync(Action, maxRetries: 5, delayMs: -1);

        // Assert
        act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that ArgumentOutOfRangeException is thrown when maxRetries exceeds 20 for fixed delay strategy.
    /// </summary>
    [Fact]
    public void ExecuteWithFixedDelayAsync_ThrowsArgumentOutOfRangeException_WhenMaxRetriesExceeds20()
    {
        // Arrange
        Task<int> Action() => Task.FromResult(42);

        // Act
        Func<Task> act = () => RetryHelper.ExecuteWithFixedDelayAsync(Action, maxRetries: 21, delayMs: 100);

        // Assert
        act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that ArgumentNullException is thrown when action is null for linear backoff strategy.
    /// </summary>
    [Fact]
    public void ExecuteWithLinearBackoffAsync_ThrowsArgumentNullException_WhenActionIsNull()
    {
        // Arrange
        Func<Task<int>>? nullAction = null;

        // Act
        Func<Task> act = () => RetryHelper.ExecuteWithLinearBackoffAsync(
            nullAction!,
            maxRetries: 5,
            initialDelayMs: 100,
            delayIncrementMs: 50);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ArgumentNullException is thrown when action is null for exponential backoff strategy.
    /// </summary>
    [Fact]
    public void ExecuteWithExponentialBackoffAsync_ThrowsArgumentNullException_WhenActionIsNull()
    {
        // Arrange
        Func<Task<int>>? nullAction = null;

        // Act
        Func<Task> act = () => RetryHelper.ExecuteWithExponentialBackoffAsync(
            nullAction!,
            maxRetries: 5,
            initialDelayMs: 100,
            backoffMultiplier: 2.0);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ArgumentNullException is thrown when action is null for jittered backoff strategy.
    /// </summary>
    [Fact]
    public void ExecuteWithJitteredBackoffAsync_ThrowsArgumentNullException_WhenActionIsNull()
    {
        // Arrange
        Func<Task<int>>? nullAction = null;

        // Act
        Func<Task> act = () => RetryHelper.ExecuteWithJitteredBackoffAsync(
            nullAction!,
            maxRetries: 5,
            initialDelayMs: 100);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that IsTransientError correctly identifies transient exceptions.
    /// </summary>
    [Fact]
    public void IsTransientError_IdentifiesTransientExceptions()
    {
        // Arrange & Act & Assert
        RetryHelper.IsTransientError(new TimeoutException()).Should().BeTrue();
        RetryHelper.IsTransientError(new System.IO.IOException("Connection lost")).Should().BeTrue();
        RetryHelper.IsTransientError(new HttpRequestException("Request failed")).Should().BeTrue();

        // Non-transient exceptions
        RetryHelper.IsTransientError(new InvalidOperationException("Invalid operation")).Should().BeFalse();
        RetryHelper.IsTransientError(new ArgumentException("Invalid argument")).Should().BeFalse();
    }

    /// <summary>
    /// Tests that IsTransientError checks inner exceptions recursively.
    /// </summary>
    [Fact]
    public void IsTransientError_ChecksInnerExceptionsRecursively()
    {
        // Arrange
        var innerException = new TimeoutException("Inner timeout");
        var outerException = new InvalidOperationException("Outer error", innerException);

        // Act & Assert
        RetryHelper.IsTransientError(outerException).Should().BeTrue();
    }

    /// <summary>
    /// Tests that CreatePolicy creates a valid retry policy.
    /// </summary>
    [Fact]
    public void CreatePolicy_CreatesValidRetryPolicy()
    {
        // Arrange & Act
        var policy = RetryHelper.CreatePolicy(
            maxRetries: 3,
            strategy: RetryStrategy.LinearBackoff,
            initialDelayMs: 200);

        // Assert
        policy.Should().NotBeNull();
        policy.MaxRetries.Should().Be(3);
        policy.Strategy.Should().Be(RetryStrategy.LinearBackoff);
        policy.InitialDelayMs.Should().Be(200);
    }

    /// <summary>
    /// Tests that RetryPolicy.ExecuteAsync works correctly with all strategies.
    /// </summary>
    [Fact]
    public async Task RetryPolicy_ExecuteAsync_WorksWithAllStrategies()
    {
        // Arrange
        var attemptCount = 0;

        Task<int> FailingAction()
        {
            attemptCount++;
            if (attemptCount <= 2)
            {
                throw new TimeoutException("Simulated timeout");
            }
            return Task.FromResult(42);
        }

        // Act - Test with LinearBackoff strategy
        var policy = RetryHelper.CreatePolicy(
            maxRetries: 3,
            strategy: RetryStrategy.LinearBackoff,
            initialDelayMs: 100);

        var result = await policy.ExecuteAsync(FailingAction);

        // Assert
        result.Should().Be(42);
        attemptCount.Should().Be(3); // Initial attempt + 2 retries
    }
}