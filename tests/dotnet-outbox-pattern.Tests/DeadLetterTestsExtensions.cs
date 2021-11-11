#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Extension methods for DeadLetterTests to provide additional test utilities
/// </summary>
public static class DeadLetterTestsExtensions
{
    /// <summary>
    /// Creates a test DeadLetter with default properties for testing
    /// </summary>
    /// <param name="errorMessage">Custom error message, defaults to "Test error"</param>
    /// <param name="totalAttempts">Total attempts, defaults to 3</param>
    /// <returns>Configured DeadLetter instance</returns>
    public static DeadLetter CreateTestDeadLetter(
        this DeadLetterTests _,
        string errorMessage = "Test error",
        int totalAttempts = 3)
    {
        return new DeadLetter
        {
            Id = Guid.NewGuid(),
            OutboxMessageId = Guid.NewGuid(),
            IdempotencyKey = "test-key-" + Guid.NewGuid().ToString()[..8],
            AggregateId = "test-aggregate-" + Guid.NewGuid().ToString()[..8],
            AggregateType = "TestAggregate",
            EventType = EventType.Created,
            EventData = "{\"testId\":\"test-value\"}",
            EventTypeName = "TestEvent",
            Topic = "test.topic",
            TotalAttempts = totalAttempts,
            ErrorMessage = errorMessage,
            ErrorStackTrace = "at TestClass.TestMethod() in line 42",
            OriginalCreatedAt = DateTime.UtcNow.AddHours(-1),
            MovedToDlqAt = DateTime.UtcNow,
            LastAttemptAt = DateTime.UtcNow.AddMinutes(-30),
            CorrelationId = "test-correlation-" + Guid.NewGuid().ToString()[..8],
            CausationId = "test-causation-" + Guid.NewGuid().ToString()[..8],
            Metadata = "{\"testMetadata\":\"test-value\"}",
            IsReviewed = false,
            IsRequeued = false
        };
    }

    /// <summary>
    /// Creates a test DeadLetter with failure reason and suggested action
    /// </summary>
    /// <param name="failureReason">Failure reason description</param>
    /// <param name="suggestedAction">Suggested resolution action</param>
    /// <returns>Configured DeadLetter instance with failure properties</returns>
    public static DeadLetter CreateTestDeadLetterWithFailure(
        this DeadLetterTests _,
        string failureReason,
        string suggestedAction)
    {
        var deadLetter = CreateTestDeadLetter(_);
        deadLetter.FailureReason = failureReason;
        deadLetter.SuggestedAction = suggestedAction;
        return deadLetter;
    }

    /// <summary>
    /// Asserts that a DeadLetter has the expected error state
    /// </summary>
    /// <param name="deadLetter">DeadLetter to assert</param>
    /// <param name="expectedErrorMessage">Expected error message</param>
    /// <param name="expectedAttempts">Expected total attempts</param>
    public static void ShouldHaveErrorState(
        this DeadLetterTests _,
        DeadLetter deadLetter,
        string expectedErrorMessage,
        int expectedAttempts)
    {
        deadLetter.ErrorMessage.Should().Be(expectedErrorMessage);
        deadLetter.TotalAttempts.Should().Be(expectedAttempts);
        deadLetter.IsReviewed.Should().BeFalse();
        deadLetter.IsRequeued.Should().BeFalse();
    }

    /// <summary>
    /// Asserts that a DeadLetter has been properly reviewed
    /// </summary>
    /// <param name="deadLetter">DeadLetter to assert</param>
    /// <param name="expectedReviewNotes">Expected review notes</param>
    public static void ShouldBeReviewed(
        this DeadLetterTests _,
        DeadLetter deadLetter,
        string expectedReviewNotes)
    {
        deadLetter.IsReviewed.Should().BeTrue();
        deadLetter.ReviewNotes.Should().Be(expectedReviewNotes);
        deadLetter.ReviewedAt.Should().NotBeNull();
        deadLetter.ReviewedAt.Should().BeOnOrAfter(deadLetter.MovedToDlqAt);
    }
}