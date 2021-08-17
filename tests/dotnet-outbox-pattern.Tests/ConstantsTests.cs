#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for constants and configuration values
/// </summary>
public sealed class ConstantsTests
{
    [Fact]
    public void OutboxConstants_DefaultValues_MatchExpected()
    {
        OutboxConstants.DefaultTopic.Should().Be("domain-events");
        OutboxConstants.DeadLetterTopic.Should().Be("dead-letters");
        OutboxConstants.MaxAggregateIdLength.Should().Be(256);
        OutboxConstants.MaxTopicLength.Should().Be(128);
        OutboxConstants.MaxEventTypeNameLength.Should().Be(256);
        OutboxConstants.MaxErrorMessageLength.Should().Be(2000);
        OutboxConstants.MaxCorrelationIdLength.Should().Be(256);
        OutboxConstants.DefaultMaxPublishAttempts.Should().Be(5);
        OutboxConstants.DefaultBatchSize.Should().Be(100);
        OutboxConstants.DefaultDelayBetweenBatches.Should().Be(5000);
        OutboxConstants.DefaultLockDurationSeconds.Should().Be(300);
        OutboxConstants.DefaultPublishTimeoutSeconds.Should().Be(30);
        OutboxConstants.DefaultCheckExpiredLocksInterval.Should().Be(60000);
        OutboxConstants.MinBatchSize.Should().Be(1);
        OutboxConstants.MaxBatchSize.Should().Be(10000);
        OutboxConstants.DefaultDegreeOfParallelism.Should().Be(4);
        OutboxConstants.DefaultArchiveDaysOld.Should().Be(30);
    }

    [Fact]
    public void StandardTopics_ContainsExpectedTopicNames()
    {
        StandardTopics.Orders.Should().Be("orders.events");
        StandardTopics.Customers.Should().Be("customers.events");
        StandardTopics.Payments.Should().Be("payments.events");
        StandardTopics.Inventory.Should().Be("inventory.events");
        StandardTopics.Notifications.Should().Be("notifications.events");
        StandardTopics.System.Should().Be("system.events");
    }

    [Fact]
    public void LogProperties_ContainsExpectedPropertyNames()
    {
        LogProperties.MessageId.Should().Be("MessageId");
        LogProperties.CorrelationId.Should().Be("CorrelationId");
        LogProperties.CausationId.Should().Be("CausationId");
        LogProperties.AggregateId.Should().Be("AggregateId");
        LogProperties.Topic.Should().Be("Topic");
        LogProperties.State.Should().Be("State");
        LogProperties.Attempts.Should().Be("Attempts");
        LogProperties.Duration.Should().Be("Duration");
        LogProperties.Success.Should().Be("Success");
    }

    [Fact]
    public void ErrorCodes_ContainsExpectedErrorCodes()
    {
        ErrorCodes.MessageNotFound.Should().Be("MSG_NOT_FOUND");
        ErrorCodes.PublishingFailed.Should().Be("PUBLISH_FAILED");
        ErrorCodes.SerializationError.Should().Be("SERIALIZATION_ERROR");
        ErrorCodes.DeserializationError.Should().Be("DESERIALIZATION_ERROR");
        ErrorCodes.DatabaseError.Should().Be("DATABASE_ERROR");
        ErrorCodes.InvalidMessage.Should().Be("INVALID_MESSAGE");
        ErrorCodes.InvalidConfiguration.Should().Be("INVALID_CONFIG");
        ErrorCodes.OperationTimeout.Should().Be("OPERATION_TIMEOUT");
        ErrorCodes.ConcurrencyConflict.Should().Be("CONCURRENCY_CONFLICT");
        ErrorCodes.DeadLetterQueueError.Should().Be("DLQ_ERROR");
    }

    [Fact]
    public void HttpHeaders_ContainsExpectedHeaderNames()
    {
        HttpHeaders.CorrelationId.Should().Be("X-Correlation-Id");
        HttpHeaders.CausationId.Should().Be("X-Causation-Id");
        HttpHeaders.IdempotencyKey.Should().Be("X-Idempotency-Key");
        HttpHeaders.RequestId.Should().Be("X-Request-Id");
    }

    [Fact]
    public void OutboxConstants_ValidationConstants_AreReasonable()
    {
        // Validate that constants are within reasonable bounds
        OutboxConstants.MaxAggregateIdLength.Should().BeLessThanOrEqualTo(512);
        OutboxConstants.MaxTopicLength.Should().BeLessThanOrEqualTo(256);
        OutboxConstants.MaxEventTypeNameLength.Should().BeLessThanOrEqualTo(512);
        OutboxConstants.MaxErrorMessageLength.Should().BeLessThanOrEqualTo(5000);
        OutboxConstants.MaxCorrelationIdLength.Should().BeLessThanOrEqualTo(512);
        OutboxConstants.DefaultBatchSize.Should().BeLessThanOrEqualTo(10000);
        OutboxConstants.MaxBatchSize.Should().BeGreaterThan(100);
        OutboxConstants.DefaultDegreeOfParallelism.Should().BeLessThanOrEqualTo(16);
        OutboxConstants.DefaultArchiveDaysOld.Should().BeLessThanOrEqualTo(365);
    }
}
