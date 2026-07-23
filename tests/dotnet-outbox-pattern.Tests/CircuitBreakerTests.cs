#nullable enable
// =============================================================================
// Author: Test
// Circuit breaker tests
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

public sealed class CircuitBreakerTests
{
    [Fact]
    public void CircuitBreaker_InitialState_IsClosed()
    {
        // Arrange
        var options = new CircuitBreakerOptions();
        var loggerMock = new Mock<ILogger>();

        // Act
        var breaker = new CircuitBreaker(options, loggerMock.Object);

        // Assert
        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.True(breaker.IsAllowed);
    }

    [Fact]
    public void CircuitBreaker_RecordFailure_IncrementsFailureCount()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 3 };
        var loggerMock = new Mock<ILogger>();
        var breaker = new CircuitBreaker(options, loggerMock.Object);
        var exception = new Exception("Test failure");

        // Act
        breaker.RecordFailure(exception);
        breaker.RecordFailure(exception);

        // Assert
        Assert.Equal(2, breaker.State == CircuitState.Closed ? GetFailureCount(breaker) : 0);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void CircuitBreaker_ExceedsThreshold_OpensCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 2 };
        var loggerMock = new Mock<ILogger>();
        var breaker = new CircuitBreaker(options, loggerMock.Object);
        var exception = new Exception("Test failure");

        // Act - exceed threshold
        breaker.RecordFailure(exception);
        breaker.RecordFailure(exception);

        // Assert
        Assert.Equal(CircuitState.Open, breaker.State);
        Assert.False(breaker.IsAllowed);
    }

    [Fact]
    public void CircuitBreaker_InHalfOpen_AllowsLimitedRequests()
    {
        // Arrange
        var options = new CircuitBreakerOptions {
            FailureThreshold = 2,
            OpenDuration = TimeSpan.FromMilliseconds(10)
        };
        var loggerMock = new Mock<ILogger>();
        var breaker = new CircuitBreaker(options, loggerMock.Object);
        var exception = new Exception("Test failure");

        // Open the circuit
        breaker.RecordFailure(exception);
        breaker.RecordFailure(exception);

        // Force transition to half-open after open duration
        System.Threading.Thread.Sleep(20);
        var state = breaker.State; // This should trigger state transition

        // Assert
        Assert.Equal(CircuitState.HalfOpen, breaker.State);
        Assert.True(breaker.IsAllowed);
    }

    [Fact]
    public void CircuitBreaker_Reset_ReturnsToClosed()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 2 };
        var loggerMock = new Mock<ILogger>();
        var breaker = new CircuitBreaker(options, loggerMock.Object);
        var exception = new Exception("Test failure");

        // Open the circuit
        breaker.RecordFailure(exception);
        breaker.RecordFailure(exception);

        // Reset
        breaker.Reset();

        // Assert
        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.True(breaker.IsAllowed);
    }

    [Fact]
    public async Task CircuitBreaker_ExecuteAsync_BlocksWhenOpen()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 1 };
        var loggerMock = new Mock<ILogger>();
        var breaker = new CircuitBreaker(options, loggerMock.Object);
        var exception = new Exception("Test failure");

        // Open the circuit
        breaker.RecordFailure(exception);

        // Act - try to execute when circuit is open
        var executed = await breaker.ExecuteAsync(() => Task.CompletedTask);

        // Assert
        Assert.False(executed);
    }

    [Fact]
    public async Task CircuitBreaker_ExecuteAsync_AllowsWhenClosed()
    {
        // Arrange
        var options = new CircuitBreakerOptions();
        var loggerMock = new Mock<ILogger>();
        var breaker = new CircuitBreaker(options, loggerMock.Object);
        var executed = false;

        // Act - execute when circuit is closed
        var result = await breaker.ExecuteAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(result);
        Assert.True(executed);
    }

    [Fact]
    public void CircuitBreaker_RecordSuccess_ResetsFailureCount()
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = 5 };
        var loggerMock = new Mock<ILogger>();
        var breaker = new CircuitBreaker(options, loggerMock.Object);
        var exception = new Exception("Test failure");

        // Fail a few times
        breaker.RecordFailure(exception);
        breaker.RecordFailure(exception);

        // Succeed
        breaker.RecordSuccess();

        // Assert - failure count should reset
        Assert.Equal(0, GetFailureCount(breaker));
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    private static int GetFailureCount(CircuitBreaker breaker)
    {
        // Use reflection to get the private _failureCount field
        var field = typeof(CircuitBreaker).GetField("_failureCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int)field!.GetValue(breaker)!;
    }
}
