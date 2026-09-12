#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Provides read-only convenience methods for <see cref="DeadLetter"/> instances.
/// </summary>
public static class DeadLetterExtensions
{
    /// <summary>
    /// Determines whether a dead letter has exceeded the specified retention period.
    /// </summary>
    /// <param name="deadLetter">The dead letter to evaluate.</param>
    /// <param name="retentionPeriod">The amount of time a dead letter may be retained.</param>
    /// <returns><see langword="true"/> when the retention period has elapsed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deadLetter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retentionPeriod"/> is negative.</exception>
    public static bool IsExpired(this DeadLetter deadLetter, TimeSpan retentionPeriod)
        => deadLetter.IsExpired(retentionPeriod, DateTime.UtcNow);

    /// <summary>
    /// Determines whether a dead letter has exceeded the specified retention period at a given time.
    /// </summary>
    /// <param name="deadLetter">The dead letter to evaluate.</param>
    /// <param name="retentionPeriod">The amount of time a dead letter may be retained.</param>
    /// <param name="asOf">The time at which expiration is evaluated.</param>
    /// <returns><see langword="true"/> when the retention period has elapsed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deadLetter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="retentionPeriod"/> is negative.</exception>
    public static bool IsExpired(this DeadLetter deadLetter, TimeSpan retentionPeriod, DateTime asOf)
    {
        ArgumentNullException.ThrowIfNull(deadLetter);

        if (retentionPeriod < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionPeriod), "Retention period cannot be negative.");
        }

        return asOf >= deadLetter.MovedToDlqAt
            && asOf - deadLetter.MovedToDlqAt >= retentionPeriod;
    }

    /// <summary>
    /// Creates a concise description of a dead letter without modifying it.
    /// </summary>
    /// <param name="deadLetter">The dead letter to summarize.</param>
    /// <returns>A summary containing the event, aggregate, delivery attempts, and final error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deadLetter"/> is <see langword="null"/>.</exception>
    public static string ToSummary(this DeadLetter deadLetter)
    {
        ArgumentNullException.ThrowIfNull(deadLetter);

        return $"{deadLetter.EventType} event for {deadLetter.AggregateType}/{deadLetter.AggregateId} "
            + $"failed after {deadLetter.TotalAttempts} attempt(s): {deadLetter.ErrorMessage}";
    }
}
