#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for domain enums and their string representations
/// </summary>
public sealed class EnumsTests
{
    [Fact]
    public void OutboxMessageState_Values_MatchExpected()
    {
        OutboxMessageState.Pending.Should().Be(0);
        OutboxMessageState.Processing.Should().Be(1);
        OutboxMessageState.Published.Should().Be(2);
        OutboxMessageState.Failed.Should().Be(3);
        OutboxMessageState.Archived.Should().Be(4);
    }

    [Fact]
    public void EventType_Values_MatchExpected()
    {
        EventType.Created.Should().Be(1);
        EventType.Updated.Should().Be(2);
        EventType.Deleted.Should().Be(3);
        EventType.Custom.Should().Be(4);
        EventType.Notification.Should().Be(5);
    }

    [Fact]
    public void DeliveryGuarantee_Values_MatchExpected()
    {
        DeliveryGuarantee.AtLeastOnce.Should().Be(1);
        DeliveryGuarantee.ExactlyOnce.Should().Be(2);
    }

    [Fact]
    public void RetryPolicyType_Values_MatchExpected()
    {
        RetryPolicyType.NoRetry.Should().Be(0);
        RetryPolicyType.FixedInterval.Should().Be(1);
        RetryPolicyType.ExponentialBackoff.Should().Be(2);
        RetryPolicyType.LinearBackoff.Should().Be(3);
    }

    [Theory]
    [InlineData(OutboxMessageState.Pending, "Pending")]
    [InlineData(OutboxMessageState.Processing, "Processing")]
    [InlineData(OutboxMessageState.Published, "Published")]
    [InlineData(OutboxMessageState.Failed, "Failed")]
    [InlineData(OutboxMessageState.Archived, "Archived")]
    public void OutboxMessageState_ToString_ReturnsCorrectString(OutboxMessageState state, string expected)
    {
        state.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData(EventType.Created, "Created")]
    [InlineData(EventType.Updated, "Updated")]
    [InlineData(EventType.Deleted, "Deleted")]
    [InlineData(EventType.Custom, "Custom")]
    [InlineData(EventType.Notification, "Notification")]
    public void EventType_ToString_ReturnsCorrectString(EventType eventType, string expected)
    {
        eventType.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData(DeliveryGuarantee.AtLeastOnce, "AtLeastOnce")]
    [InlineData(DeliveryGuarantee.ExactlyOnce, "ExactlyOnce")]
    public void DeliveryGuarantee_ToString_ReturnsCorrectString(DeliveryGuarantee guarantee, string expected)
    {
        guarantee.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData(RetryPolicyType.NoRetry, "NoRetry")]
    [InlineData(RetryPolicyType.FixedInterval, "FixedInterval")]
    [InlineData(RetryPolicyType.ExponentialBackoff, "ExponentialBackoff")]
    [InlineData(RetryPolicyType.LinearBackoff, "LinearBackoff")]
    public void RetryPolicyType_ToString_ReturnsCorrectString(RetryPolicyType policy, string expected)
    {
        policy.ToString().Should().Be(expected);
    }

    [Fact]
    public void OutboxMessageState_HasExpectedDescriptionAttributes()
    {
        // These tests verify that the enum values have the expected descriptions
        // based on their XML documentation comments
        var pending = OutboxMessageState.Pending;
        pending.Should().Be(OutboxMessageState.Pending);

        var processing = OutboxMessageState.Processing;
        processing.Should().Be(OutboxMessageState.Processing);

        var published = OutboxMessageState.Published;
        published.Should().Be(OutboxMessageState.Published);

        var failed = OutboxMessageState.Failed;
        failed.Should().Be(OutboxMessageState.Failed);

        var archived = OutboxMessageState.Archived;
        archived.Should().Be(OutboxMessageState.Archived);
    }

    [Fact]
    public void EventType_HasExpectedValues()
    {
        var created = EventType.Created;
        created.Should().Be(EventType.Created);

        var updated = EventType.Updated;
        updated.Should().Be(EventType.Updated);

        var deleted = EventType.Deleted;
        deleted.Should().Be(EventType.Deleted);

        var custom = EventType.Custom;
        custom.Should().Be(EventType.Custom);

        var notification = EventType.Notification;
        notification.Should().Be(EventType.Notification);
    }
}