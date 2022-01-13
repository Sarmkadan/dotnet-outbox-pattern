#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

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
    /// <exception cref="ArgumentNullException"><paramref name="_" /> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="totalAttempts" /> is negative</exception>
    public static DeadLetter CreateTestDeadLetter(
        this DeadLetterTests _,
        string errorMessage = "Test error",
        int totalAttempts = 3)
    {
        ArgumentNullException.ThrowIfNull(_);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalAttempts, 0);

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
    /// <exception cref="ArgumentNullException"><paramref name="_" /> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="failureReason" /> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="suggestedAction" /> is <see langword="null"/></exception>
    public static DeadLetter CreateTestDeadLetterWithFailure(
        this DeadLetterTests _,
        string failureReason,
        string suggestedAction)
    {
        ArgumentNullException.ThrowIfNull(_);
        ArgumentNullException.ThrowIfNull(failureReason);
        ArgumentNullException.ThrowIfNull(suggestedAction);

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
    /// <exception cref="ArgumentNullException"><paramref name="_" /> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="deadLetter" /> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="expectedErrorMessage" /> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expectedAttempts" /> is negative</exception>
    public static void ShouldHaveErrorState(
        this DeadLetterTests _,
        DeadLetter deadLetter,
        string expectedErrorMessage,
        int expectedAttempts)
    {
        ArgumentNullException.ThrowIfNull(_);
        ArgumentNullException.ThrowIfNull(deadLetter);
        ArgumentNullException.ThrowIfNull(expectedErrorMessage);
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedAttempts, 0);

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
    /// <exception cref="ArgumentNullException"><paramref name="_" /> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="deadLetter" /> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="expectedReviewNotes" /> is <see langword="null"/></exception>
    public static void ShouldBeReviewed(
        this DeadLetterTests _,
        DeadLetter deadLetter,
        string expectedReviewNotes)
    {
        ArgumentNullException.ThrowIfNull(_);
        ArgumentNullException.ThrowIfNull(deadLetter);
        ArgumentNullException.ThrowIfNull(expectedReviewNotes);

        deadLetter.IsReviewed.Should().BeTrue();
        deadLetter.ReviewNotes.Should().Be(expectedReviewNotes);
        deadLetter.ReviewedAt.Should().NotBeNull();
        deadLetter.ReviewedAt.Should().BeOnOrAfter(deadLetter.MovedToDlqAt);
    }
}