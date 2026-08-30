#nullable enable

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;

namespace DotnetOutboxPattern.Tests;

public sealed class CircuitBreakerTests
{
    [Fact]
    public void RecordFailure_BelowFailureThreshold_KeepsCircuitClosed()
    {
        using var breaker = CreateBreaker(failureThreshold: 3);

        breaker.RecordFailure(new InvalidOperationException("First failure"));
        breaker.RecordFailure(new InvalidOperationException("Second failure"));

        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.True(breaker.IsAllowed);
    }

    [Fact]
    public async Task ExecuteAsync_AtFailureThreshold_OpensCircuitAndRejectsCalls()
    {
        using var breaker = CreateBreaker(failureThreshold: 2);
        var rejectedActionWasCalled = false;

        Assert.False(await breaker.ExecuteAsync(FailingAction));
        Assert.False(await breaker.ExecuteAsync(FailingAction));

        var result = await breaker.ExecuteAsync(() =>
        {
            rejectedActionWasCalled = true;
            return Task.CompletedTask;
        });

        Assert.Equal(CircuitState.Open, breaker.State);
        Assert.False(breaker.IsAllowed);
        Assert.False(result);
        Assert.False(rejectedActionWasCalled);
    }

    [Fact]
    public async Task State_AfterOpenDuration_TransitionsToHalfOpen()
    {
        using var breaker = CreateBreaker(
            failureThreshold: 1,
            openDuration: TimeSpan.FromMilliseconds(50));
        breaker.RecordFailure(new InvalidOperationException("Failure"));

        await Task.Delay(TimeSpan.FromMilliseconds(75));

        Assert.Equal(CircuitState.HalfOpen, breaker.State);
        Assert.True(breaker.IsAllowed);
    }

    [Fact]
    public void RecordSuccess_InHalfOpenAfterRequiredSuccesses_ClosesCircuit()
    {
        using var breaker = CreateBreaker(failureThreshold: 1, halfOpenTestRequests: 2);
        breaker.RecordFailure(new InvalidOperationException("Failure"));
        breaker.ForceHalfOpen();

        breaker.RecordSuccess();
        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        breaker.RecordSuccess();

        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.True(breaker.IsAllowed);
        Assert.Null(breaker.LastException);
    }

    [Fact]
    public void RecordFailure_InHalfOpen_ReopensCircuit()
    {
        using var breaker = CreateBreaker(failureThreshold: 1);
        breaker.RecordFailure(new InvalidOperationException("Initial failure"));
        breaker.ForceHalfOpen();

        var halfOpenFailure = new InvalidOperationException("Half-open failure");
        breaker.RecordFailure(halfOpenFailure);

        Assert.Equal(CircuitState.Open, breaker.State);
        Assert.False(breaker.IsAllowed);
        Assert.Same(halfOpenFailure, breaker.LastException);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_BypassesCircuitStateAndNeverRejectsCalls()
    {
        using var breaker = CreateBreaker(failureThreshold: 1, enabled: false);
        var invocationCount = 0;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var result = await breaker.ExecuteAsync((Func<Task>)(() =>
            {
                invocationCount++;
                throw new InvalidOperationException("Failure");
            }));

            Assert.False(result);
            Assert.Equal(CircuitState.Closed, breaker.State);
            Assert.True(breaker.IsAllowed);
        }

        var successfulResult = await breaker.ExecuteAsync(() =>
        {
            invocationCount++;
            return Task.CompletedTask;
        });

        Assert.True(successfulResult);
        Assert.Equal(4, invocationCount);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    private static CircuitBreaker CreateBreaker(
        int failureThreshold,
        TimeSpan? openDuration = null,
        int halfOpenTestRequests = 1,
        bool enabled = true)
    {
        return new CircuitBreaker(new CircuitBreakerOptions
        {
            Enabled = enabled,
            FailureThreshold = failureThreshold,
            OpenDuration = openDuration ?? TimeSpan.FromMinutes(1),
            HalfOpenTestRequests = halfOpenTestRequests
        });
    }

    private static Task FailingAction()
    {
        throw new InvalidOperationException("Failure");
    }
}
