// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

public class OutboxMessageTests
{
    private static OutboxMessage CreateValidMessage() => new()
    {
        Id = Guid.NewGuid(),
        IdempotencyKey = "key-001",
        AggregateId = "agg-1",
        AggregateType = "Order",
        EventType = EventType.Created,
        EventData = "{\"orderId\":\"1\"}",
        EventTypeName = "OrderCreatedEvent",
        Topic = "orders.created",
        MaxPublishAttempts = 5,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public void Validate_WithValidMessage_DoesNotThrow()
    {
        var message = CreateValidMessage();

        var act = () => message.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyIdempotencyKey_ThrowsArgumentException(string? key)
    {
        var message = CreateValidMessage();
        message.IdempotencyKey = key!;

        var act = () => message.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*IdempotencyKey*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyTopic_ThrowsArgumentException(string? topic)
    {
        var message = CreateValidMessage();
        message.Topic = topic!;

        var act = () => message.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*Topic*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithNonPositiveMaxPublishAttempts_ThrowsArgumentException(int maxAttempts)
    {
        var message = CreateValidMessage();
        message.MaxPublishAttempts = maxAttempts;

        var act = () => message.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*MaxPublishAttempts*");
    }

    [Fact]
    public void MarkAsPublished_SetsStateToPublished()
    {
        var message = CreateValidMessage();
        message.State = OutboxMessageState.Processing;

        message.MarkAsPublished();

        message.State.Should().Be(OutboxMessageState.Published);
    }

    [Fact]
    public void MarkAsPublished_SetsPublishedAtAndClearsError()
    {
        var before = DateTime.UtcNow;
        var message = CreateValidMessage();
        message.ErrorMessage = "transient failure";
        message.ErrorStackTrace = "at line 42";
        message.IsLocked = true;

        message.MarkAsPublished();

        message.PublishedAt.Should().NotBeNull();
        message.PublishedAt.Should().BeOnOrAfter(before);
        message.IsLocked.Should().BeFalse();
        message.ErrorMessage.Should().BeNull();
        message.ErrorStackTrace.Should().BeNull();
    }

    [Fact]
    public void RecordFailure_IncrementsPublishAttempts()
    {
        var message = CreateValidMessage();
        var initialAttempts = message.PublishAttempts;

        message.RecordFailure("timeout error");

        message.PublishAttempts.Should().Be(initialAttempts + 1);
    }

    [Fact]
    public void RecordFailure_StoresErrorMessageAndReleasesLock()
    {
        var message = CreateValidMessage();
        message.IsLocked = true;

        message.RecordFailure("connection refused", "at SomeMethod()");

        message.ErrorMessage.Should().Be("connection refused");
        message.ErrorStackTrace.Should().Be("at SomeMethod()");
        message.IsLocked.Should().BeFalse();
        message.LastProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordFailure_WhenMaxAttemptsReached_SetsStateFailed()
    {
        var message = CreateValidMessage();
        message.MaxPublishAttempts = 3;
        message.PublishAttempts = 2;

        message.RecordFailure("final failure");

        message.State.Should().Be(OutboxMessageState.Failed);
    }

    [Fact]
    public void RecordFailure_BelowMaxAttempts_DoesNotChangeState()
    {
        var message = CreateValidMessage();
        message.MaxPublishAttempts = 5;
        message.PublishAttempts = 0;
        message.State = OutboxMessageState.Pending;

        message.RecordFailure("intermittent failure");

        message.State.Should().Be(OutboxMessageState.Pending);
    }

    [Fact]
    public void Lock_SetsIsLockedTrueAndStateToProcessing()
    {
        var message = CreateValidMessage();
        var before = DateTime.UtcNow;

        message.Lock(TimeSpan.FromMinutes(5));

        message.IsLocked.Should().BeTrue();
        message.State.Should().Be(OutboxMessageState.Processing);
        message.LockExpiresAt.Should().BeOnOrAfter(before.AddMinutes(5));
    }

    [Fact]
    public void UnlockIfExpired_WhenLockHasExpired_ReturnsTrueAndResetsToPending()
    {
        var message = CreateValidMessage();
        message.IsLocked = true;
        message.LockExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        message.State = OutboxMessageState.Processing;

        var result = message.UnlockIfExpired();

        result.Should().BeTrue();
        message.IsLocked.Should().BeFalse();
        message.State.Should().Be(OutboxMessageState.Pending);
    }

    [Fact]
    public void UnlockIfExpired_WhenLockStillActive_ReturnsFalse()
    {
        var message = CreateValidMessage();
        message.IsLocked = true;
        message.LockExpiresAt = DateTime.UtcNow.AddMinutes(5);
        message.State = OutboxMessageState.Processing;

        var result = message.UnlockIfExpired();

        result.Should().BeFalse();
        message.IsLocked.Should().BeTrue();
        message.State.Should().Be(OutboxMessageState.Processing);
    }

    [Fact]
    public void CanRetry_WhenBelowMaxAttempts_ReturnsTrue()
    {
        var message = CreateValidMessage();
        message.MaxPublishAttempts = 5;
        message.PublishAttempts = 3;
        message.State = OutboxMessageState.Pending;

        message.CanRetry().Should().BeTrue();
    }

    [Fact]
    public void CanRetry_WhenAttemptsEqualMax_ReturnsFalse()
    {
        var message = CreateValidMessage();
        message.MaxPublishAttempts = 3;
        message.PublishAttempts = 3;

        message.CanRetry().Should().BeFalse();
    }

    [Fact]
    public void CanRetry_WhenStateIsPublished_ReturnsFalse()
    {
        var message = CreateValidMessage();
        message.PublishAttempts = 0;
        message.MaxPublishAttempts = 5;

        message.MarkAsPublished();

        message.CanRetry().Should().BeFalse();
    }
}
