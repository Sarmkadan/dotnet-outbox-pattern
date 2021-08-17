#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Additional comprehensive tests for OutboxMessage domain model
/// </summary>
public sealed class OutboxMessageAdditionalTests
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
        CreatedAt = DateTime.UtcNow,
        PartitionKey = "order-123",
        DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce,
        Priority = 5
    };

    [Fact]
    public void Validate_WithValidMessage_DoesNotThrowException()
    {
        var message = CreateValidMessage();

        var act = () => message.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithValidMessage_SetsDefaultValues()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "key-1",
            AggregateId = "agg-1",
            AggregateType = "Type",
            EventType = EventType.Created,
            EventData = "data",
            EventTypeName = "Event",
            Topic = "topic",
            MaxPublishAttempts = 3
        };

        message.Validate();

        message.State.Should().Be(OutboxMessageState.Pending);
        message.PublishAttempts.Should().Be(0);
        message.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        message.DeliveryGuarantee.Should().Be(DeliveryGuarantee.AtLeastOnce);
        message.Priority.Should().Be(0);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Validate_WithWhitespaceIdempotencyKey_ThrowsArgumentException(string key)
    {
        var message = CreateValidMessage();
        message.IdempotencyKey = key;

        var act = () => message.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*IdempotencyKey*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyAggregateId_ThrowsArgumentException(string? id)
    {
        var message = CreateValidMessage();
        message.AggregateId = id!;

        var act = () => message.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*AggregateId*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyAggregateType_ThrowsArgumentException(string? type)
    {
        var message = CreateValidMessage();
        message.AggregateType = type!;

        var act = () => message.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*AggregateType*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyEventData_ThrowsArgumentException(string? data)
    {
        var message = CreateValidMessage();
        message.EventData = data!;

        var act = () => message.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*EventData*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyEventTypeName_ThrowsArgumentException(string? typeName)
    {
        var message = CreateValidMessage();
        message.EventTypeName = typeName!;

        var act = () => message.Validate();

        act.Should().Throw<ArgumentException>().WithMessage("*EventTypeName*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
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
    public void MarkAsPublished_FromPendingState_SetsCorrectState()
    {
        var message = CreateValidMessage();
        message.State = OutboxMessageState.Pending;

        message.MarkAsPublished();

        message.State.Should().Be(OutboxMessageState.Published);
    }

    [Fact]
    public void MarkAsPublished_FromProcessingState_SetsCorrectState()
    {
        var message = CreateValidMessage();
        message.State = OutboxMessageState.Processing;

        message.MarkAsPublished();

        message.State.Should().Be(OutboxMessageState.Published);
    }

    [Fact]
    public void MarkAsPublished_SetsPublishedAtToCurrentTime()
    {
        var before = DateTime.UtcNow;
        var message = CreateValidMessage();

        message.MarkAsPublished();

        message.PublishedAt.Should().BeOnOrAfter(before);
        message.PublishedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void MarkAsPublished_ClearsErrorState()
    {
        var message = CreateValidMessage();
        message.ErrorMessage = "previous error";
        message.ErrorStackTrace = "stack trace";
        message.IsLocked = true;

        message.MarkAsPublished();

        message.ErrorMessage.Should().BeNull();
        message.ErrorStackTrace.Should().BeNull();
        message.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_WithErrorMessage_StoresError()
    {
        var message = CreateValidMessage();
        var error = "connection timeout";

        message.RecordFailure(error);

        message.ErrorMessage.Should().Be(error);
        message.ErrorStackTrace.Should().BeNull();
    }

    [Fact]
    public void RecordFailure_WithErrorMessageAndStackTrace_StoresBoth()
    {
        var message = CreateValidMessage();
        var error = "connection timeout";
        var stackTrace = "at Publish() line 42";

        message.RecordFailure(error, stackTrace);

        message.ErrorMessage.Should().Be(error);
        message.ErrorStackTrace.Should().Be(stackTrace);
    }

    [Fact]
    public void RecordFailure_SetsLastProcessedAt()
    {
        var before = DateTime.UtcNow;
        var message = CreateValidMessage();

        message.RecordFailure("error");

        message.LastProcessedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void RecordFailure_ReleasesLock()
    {
        var message = CreateValidMessage();
        message.IsLocked = true;

        message.RecordFailure("error");

        message.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_BelowMaxAttempts_RemainsPending()
    {
        var message = CreateValidMessage();
        message.MaxPublishAttempts = 5;
        message.PublishAttempts = 2;
        message.State = OutboxMessageState.Pending;

        message.RecordFailure("error");

        message.State.Should().Be(OutboxMessageState.Pending);
    }

    [Fact]
    public void RecordFailure_AtMaxAttempts_SetsToFailed()
    {
        var message = CreateValidMessage();
        message.MaxPublishAttempts = 3;
        message.PublishAttempts = 2;
        message.State = OutboxMessageState.Pending;

        message.RecordFailure("final error");

        message.State.Should().Be(OutboxMessageState.Failed);
    }

    [Fact]
    public void RecordFailure_ExceedsMaxAttempts_SetsToFailed()
    {
        var message = CreateValidMessage();
        message.MaxPublishAttempts = 3;
        message.PublishAttempts = 3;
        message.State = OutboxMessageState.Pending;

        message.RecordFailure("error");

        message.State.Should().Be(OutboxMessageState.Failed);
    }

    [Fact]
    public void Lock_SetsIsLockedToTrue()
    {
        var message = CreateValidMessage();
        message.IsLocked = false;

        message.Lock(TimeSpan.FromMinutes(5));

        message.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void Lock_SetsStateToProcessing()
    {
        var message = CreateValidMessage();
        message.State = OutboxMessageState.Pending;

        message.Lock(TimeSpan.FromMinutes(5));

        message.State.Should().Be(OutboxMessageState.Processing);
    }

    [Fact]
    public void Lock_SetsLockExpiresAt()
    {
        var before = DateTime.UtcNow;
        var message = CreateValidMessage();

        message.Lock(TimeSpan.FromMinutes(5));

        message.LockExpiresAt.Should().BeOnOrAfter(before.AddMinutes(5));
    }

    [Fact]
    public void UnlockIfExpired_WhenNotLocked_ReturnsFalse()
    {
        var message = CreateValidMessage();
        message.IsLocked = false;

        var result = message.UnlockIfExpired();

        result.Should().BeFalse();
        message.IsLocked.Should().BeFalse();
        message.State.Should().Be(OutboxMessageState.Pending);
    }

    [Fact]
    public void UnlockIfExpired_WhenLockNotExpired_ReturnsFalse()
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
    public void UnlockIfExpired_WhenLockExpired_UnlocksAndResetsState()
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
    public void CanRetry_WithZeroAttemptsAndPendingState_ReturnsTrue()
    {
        var message = CreateValidMessage();
        message.PublishAttempts = 0;
        message.MaxPublishAttempts = 5;
        message.State = OutboxMessageState.Pending;

        message.CanRetry().Should().BeTrue();
    }

    [Fact]
    public void CanRetry_WithSomeAttemptsAndPendingState_ReturnsTrue()
    {
        var message = CreateValidMessage();
        message.PublishAttempts = 3;
        message.MaxPublishAttempts = 5;
        message.State = OutboxMessageState.Pending;

        message.CanRetry().Should().BeTrue();
    }

    [Fact]
    public void CanRetry_WithMaxAttemptsReached_ReturnsFalse()
    {
        var message = CreateValidMessage();
        message.PublishAttempts = 5;
        message.MaxPublishAttempts = 5;
        message.State = OutboxMessageState.Pending;

        message.CanRetry().Should().BeFalse();
    }

    [Fact]
    public void CanRetry_WhenPublished_ReturnsFalse()
    {
        var message = CreateValidMessage();
        message.PublishAttempts = 0;
        message.MaxPublishAttempts = 5;
        message.MarkAsPublished();

        message.CanRetry().Should().BeFalse();
    }

    [Fact]
    public void CanRetry_WhenFailed_ReturnsFalse()
    {
        var message = CreateValidMessage();
        message.PublishAttempts = 2;
        message.MaxPublishAttempts = 5;
        message.State = OutboxMessageState.Failed;

        message.CanRetry().Should().BeFalse();
    }

    [Fact]
    public void CanRetry_WhenArchived_ReturnsFalse()
    {
        var message = CreateValidMessage();
        message.PublishAttempts = 0;
        message.MaxPublishAttempts = 5;
        message.State = OutboxMessageState.Archived;

        message.CanRetry().Should().BeFalse();
    }

    [Fact]
    public void OutboxMessage_DefaultConstructor_InitializesProperties()
    {
        var message = new OutboxMessage();

        message.Id.Should().NotBe(Guid.Empty);
        message.State.Should().Be(OutboxMessageState.Pending);
        message.PublishAttempts.Should().Be(0);
        message.MaxPublishAttempts.Should().Be(5);
        message.DeliveryGuarantee.Should().Be(DeliveryGuarantee.AtLeastOnce);
        message.Priority.Should().Be(0);
        message.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void ScheduledFor_WithFutureDate_SetsScheduledFor()
    {
        var message = CreateValidMessage();
        var future = DateTime.UtcNow.AddHours(1);

        message.ScheduledFor = future;

        message.ScheduledFor.Should().Be(future);
    }

    [Fact]
    public void CorrelationId_WithValue_SetsCorrelationId()
    {
        var message = CreateValidMessage();
        var correlationId = Guid.NewGuid().ToString();

        message.CorrelationId = correlationId;

        message.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void CausationId_WithValue_SetsCausationId()
    {
        var message = CreateValidMessage();
        var causationId = Guid.NewGuid().ToString();

        message.CausationId = causationId;

        message.CausationId.Should().Be(causationId);
    }

    [Fact]
    public void Metadata_WithJsonString_SetsMetadata()
    {
        var message = CreateValidMessage();
        var metadata = "{\"key\":\"value\",\"priority\":10}";

        message.Metadata = metadata;

        message.Metadata.Should().Be(metadata);
    }
}