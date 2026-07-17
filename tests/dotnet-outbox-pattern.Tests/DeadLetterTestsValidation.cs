#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Validation helpers for DeadLetter domain model
/// </summary>
public static class DeadLetterTestsValidation
{
    /// <summary>
    /// Validates a DeadLetter instance and returns a list of human-readable problems
    /// </summary>
    /// <param name="value">The DeadLetter to validate</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this global::DotnetOutboxPattern.Domain.DeadLetter value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate required string properties
        ValidateRequiredString(value.IdempotencyKey, nameof(value.IdempotencyKey), problems);
        ValidateRequiredString(value.AggregateId, nameof(value.AggregateId), problems);
        ValidateRequiredString(value.AggregateType, nameof(value.AggregateType), problems);
        ValidateRequiredString(value.EventData, nameof(value.EventData), problems);
        ValidateRequiredString(value.EventTypeName, nameof(value.EventTypeName), problems);
        ValidateRequiredString(value.Topic, nameof(value.Topic), problems);
        ValidateRequiredString(value.ErrorMessage, nameof(value.ErrorMessage), problems);

        // Validate optional string properties
        ValidateOptionalString(value.ErrorStackTrace, nameof(value.ErrorStackTrace), problems);
        ValidateOptionalString(value.CorrelationId, nameof(value.CorrelationId), problems);
        ValidateOptionalString(value.CausationId, nameof(value.CausationId), problems);
        ValidateOptionalString(value.Metadata, nameof(value.Metadata), problems);
        ValidateOptionalString(value.ReviewNotes, nameof(value.ReviewNotes), problems);
        ValidateOptionalString(value.RequeueReason, nameof(value.RequeueReason), problems);
        ValidateOptionalString(value.FailureReason, nameof(value.FailureReason), problems);
        ValidateOptionalString(value.SuggestedAction, nameof(value.SuggestedAction), problems);

        // Validate required value properties
        if (value.TotalAttempts < 0)
        {
            problems.Add($"{nameof(value.TotalAttempts)} must be non-negative, but was {value.TotalAttempts}");
        }

        // Validate Guid properties
        if (value.Id == Guid.Empty)
        {
            problems.Add($"{nameof(value.Id)} must not be empty Guid");
        }

        if (value.OutboxMessageId == Guid.Empty)
        {
            problems.Add($"{nameof(value.OutboxMessageId)} must not be empty Guid");
        }

        // Validate date properties
        ValidateDateTime(value.OriginalCreatedAt, nameof(value.OriginalCreatedAt), problems);
        ValidateDateTime(value.MovedToDlqAt, nameof(value.MovedToDlqAt), problems);

        if (value.LastAttemptAt.HasValue)
        {
            ValidateDateTime(value.LastAttemptAt.Value, nameof(value.LastAttemptAt), problems);
        }

        if (value.ReviewedAt.HasValue)
        {
            ValidateDateTime(value.ReviewedAt.Value, nameof(value.ReviewedAt), problems);
        }

        if (value.RequeuedAt.HasValue)
        {
            ValidateDateTime(value.RequeuedAt.Value, nameof(value.RequeuedAt), problems);
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if a DeadLetter instance is valid
    /// </summary>
    /// <param name="value">The DeadLetter to validate</param>
    /// <returns>True if valid or null, false otherwise</returns>
    public static bool IsValid(this global::DotnetOutboxPattern.Domain.DeadLetter? value) =>
        value is null || Validate(value).Count == 0;

    /// <summary>
    /// Ensures a DeadLetter instance is valid, throwing ArgumentException if not
    /// </summary>
    /// <param name="value">The DeadLetter to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ArgumentException">Thrown if value is invalid with list of problems</exception>
    /// <returns>True if valid</returns>
    public static bool EnsureValid(this global::DotnetOutboxPattern.Domain.DeadLetter value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count == 0)
        {
            return true;
        }

        throw new ArgumentException(
            $"DeadLetter is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }

    private static void ValidateRequiredString(string? value, string propertyName, List<string> problems)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (string.IsNullOrEmpty(value))
        {
            problems.Add($"{propertyName} must be a non-empty string");
        }
    }

    private static void ValidateOptionalString(string? value, string propertyName, List<string> problems)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        // Additional validation for non-empty optional strings
        if (value.Length > 1000)
        {
            problems.Add($"{propertyName} must be less than 1000 characters, but was {value.Length}");
        }
    }

    private static void ValidateDateTime(DateTime value, string propertyName, List<string> problems)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (value == default)
        {
            problems.Add($"{propertyName} must not be default DateTime");
        }

        if (value.Kind != DateTimeKind.Utc)
        {
            problems.Add($"{propertyName} must be in UTC kind, but was {value.Kind}");
        }

        if (value > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add($"{propertyName} cannot be in the future (more than 5 minutes ahead), but was {value}");
        }
    }
}