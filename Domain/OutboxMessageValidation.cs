#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Provides validation helpers for <see cref="OutboxMessage"/> instances.
/// Validates required fields, ranges, and business rules for reliable message delivery.
/// </summary>
public static class OutboxMessageValidation
{
    /// <summary>
    /// Validates an <see cref="OutboxMessage"/> and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The outbox message to validate</param>
    /// <returns>An immutable list of validation error messages; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this OutboxMessage? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate required string properties
        if (string.IsNullOrWhiteSpace(value.IdempotencyKey))
        {
            errors.Add("IdempotencyKey cannot be null, empty, or whitespace");
        }

        if (string.IsNullOrWhiteSpace(value.AggregateId))
        {
            errors.Add("AggregateId cannot be null, empty, or whitespace");
        }

        if (string.IsNullOrWhiteSpace(value.AggregateType))
        {
            errors.Add("AggregateType cannot be null, empty, or whitespace");
        }

        if (string.IsNullOrWhiteSpace(value.EventData))
        {
            errors.Add("EventData cannot be null, empty, or whitespace");
        }

        if (string.IsNullOrWhiteSpace(value.EventTypeName))
        {
            errors.Add("EventTypeName cannot be null, empty, or whitespace");
        }

        if (string.IsNullOrWhiteSpace(value.Topic))
        {
            errors.Add("Topic cannot be null, empty, or whitespace");
        }

        // Validate EventType enum value
        if (!Enum.IsDefined(typeof(EventType), value.EventType))
        {
            errors.Add($"EventType must be a defined value, got {(int)value.EventType}");
        }

        // Validate numeric ranges
        if (value.MaxPublishAttempts <= 0)
        {
            errors.Add("MaxPublishAttempts must be greater than 0");
        }

        if (value.PublishAttempts < 0)
        {
            errors.Add("PublishAttempts cannot be negative");
        }

        if (value.PublishAttempts > value.MaxPublishAttempts)
        {
            errors.Add("PublishAttempts cannot exceed MaxPublishAttempts");
        }

        if (value.Priority < 0)
        {
            errors.Add("Priority cannot be negative");
        }

        // Validate dates
        if (value.CreatedAt == default)
        {
            errors.Add("CreatedAt must be set to a non-default DateTime value");
        }
        else if (value.CreatedAt.Kind != DateTimeKind.Utc)
        {
            errors.Add("CreatedAt must be in UTC timezone");
        }

        if (value.LastProcessedAt.HasValue)
        {
            if (value.LastProcessedAt.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("LastProcessedAt must be in UTC timezone if set");
            }

            if (value.LastProcessedAt.Value < value.CreatedAt)
            {
                errors.Add("LastProcessedAt cannot be earlier than CreatedAt");
            }
        }

        if (value.PublishedAt.HasValue)
        {
            if (value.PublishedAt.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("PublishedAt must be in UTC timezone if set");
            }

            if (value.PublishedAt.Value < value.CreatedAt)
            {
                errors.Add("PublishedAt cannot be earlier than CreatedAt");
            }
        }

        if (value.ScheduledFor.HasValue)
        {
            if (value.ScheduledFor.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("ScheduledFor must be in UTC timezone if set");
            }

            if (value.ScheduledFor.Value < value.CreatedAt)
            {
                errors.Add("ScheduledFor cannot be earlier than CreatedAt");
            }
        }

        if (value.LockExpiresAt.HasValue)
        {
            if (value.LockExpiresAt.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add("LockExpiresAt must be in UTC timezone if set");
            }

            if (value.LockExpiresAt.Value < DateTime.UtcNow)
            {
                errors.Add("LockExpiresAt cannot be in the past");
            }
        }

        // Validate state consistency
        switch (value.State)
        {
            case OutboxMessageState.Pending:
                if (value.PublishedAt.HasValue)
                {
                    errors.Add("PublishedAt cannot be set when State is Pending");
                }

                if (value.LastProcessedAt.HasValue)
                {
                    errors.Add("LastProcessedAt cannot be set when State is Pending");
                }
                break;

            case OutboxMessageState.Processing:
                if (!value.IsLocked)
                {
                    errors.Add("State is Processing but IsLocked is false");
                }

                if (!value.LockExpiresAt.HasValue)
                {
                    errors.Add("State is Processing but LockExpiresAt is not set");
                }
                break;

            case OutboxMessageState.Published:
                if (!value.PublishedAt.HasValue)
                {
                    errors.Add("State is Published but PublishedAt is not set");
                }

                if (value.ErrorMessage is not null)
                {
                    errors.Add("State is Published but ErrorMessage is set");
                }
                break;

            case OutboxMessageState.Failed:
                if (string.IsNullOrWhiteSpace(value.ErrorMessage))
                {
                    errors.Add("State is Failed but ErrorMessage is not set");
                }

                if (value.PublishAttempts < value.MaxPublishAttempts)
                {
                    errors.Add("State is Failed but PublishAttempts is less than MaxPublishAttempts");
                }
                break;

            case OutboxMessageState.Archived:
                // Archived state has no additional constraints
                break;
        }

        // Validate delivery guarantee
        if (!Enum.IsDefined(typeof(DeliveryGuarantee), value.DeliveryGuarantee))
        {
            errors.Add($"DeliveryGuarantee must be a defined value, got {value.DeliveryGuarantee}");
        }

        // Validate correlation IDs if present
        if (value.CorrelationId is not null && string.IsNullOrWhiteSpace(value.CorrelationId))
        {
            errors.Add("CorrelationId cannot be empty or whitespace if set");
        }

        if (value.PartitionKey is not null && string.IsNullOrWhiteSpace(value.PartitionKey))
        {
            errors.Add("PartitionKey cannot be empty or whitespace if set");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an <see cref="OutboxMessage"/> is valid.
    /// </summary>
    /// <param name="value">The outbox message to check</param>
    /// <returns>True if the message is valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static bool IsValid(this OutboxMessage? value)
        => value is not null && Validate(value).Count == 0;

    /// <summary>
    /// Ensures that an <see cref="OutboxMessage"/> is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The outbox message to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if the message is invalid, containing all validation errors</exception>
    public static void EnsureValid(this OutboxMessage? value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException($"OutboxMessage validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }
}