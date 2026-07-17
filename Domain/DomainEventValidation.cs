#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics.CodeAnalysis;

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Provides validation helpers for domain events to ensure data integrity
/// </summary>
[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
public static class DomainEventValidation
{
    /// <summary>
    /// Validates a domain event and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The domain event to validate.</param>
    /// <returns>Empty list if valid, otherwise list of human-readable error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this DomainEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>(4);

        // Validate base DomainEvent properties
        if (value.EventId == Guid.Empty)
        {
            errors.Add("EventId must be a non-empty GUID");
        }

        if (value.OccurredAt == default)
        {
            errors.Add("OccurredAt must be a valid DateTime");
        }
        else if (value.OccurredAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("OccurredAt cannot be in the future");
        }

        // Validate derived event properties based on type using exhaustive pattern matching
        switch (value)
        {
            case EntityCreatedEvent entityCreated:
                ValidateEntityCreatedEvent(entityCreated, errors);
                break;

            case EntityUpdatedEvent entityUpdated:
                ValidateEntityUpdatedEvent(entityUpdated, errors);
                break;

            case EntityDeletedEvent entityDeleted:
                ValidateEntityDeletedEvent(entityDeleted, errors);
                break;

            case CustomDomainEvent customEvent:
                ValidateCustomDomainEvent(customEvent, errors);
                break;

            case NotificationEvent notification:
                ValidateNotificationEvent(notification, errors);
                break;

            // Exhaustive pattern matching - ensures all DomainEvent subtypes are handled
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if a domain event is valid (has no validation errors).
    /// </summary>
    /// <param name="value">The domain event to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this DomainEvent? value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures a domain event is valid, throwing an exception if it's not.
    /// </summary>
    /// <param name="value">The domain event to validate.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is invalid with detailed error messages.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static void EnsureValid(this DomainEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Domain event validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    private static void ValidateEntityCreatedEvent(EntityCreatedEvent entityCreated, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(entityCreated.EntityId))
        {
            errors.Add("EntityId must be a non-empty string for EntityCreatedEvent");
        }

        if (string.IsNullOrWhiteSpace(entityCreated.EntityType))
        {
            errors.Add("EntityType must be a non-empty string for EntityCreatedEvent");
        }

        if (entityCreated.EntityData is null)
        {
            errors.Add("EntityData must not be null for EntityCreatedEvent");
        }
    }

    private static void ValidateEntityUpdatedEvent(EntityUpdatedEvent entityUpdated, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(entityUpdated.EntityId))
        {
            errors.Add("EntityId must be a non-empty string for EntityUpdatedEvent");
        }

        if (string.IsNullOrWhiteSpace(entityUpdated.EntityType))
        {
            errors.Add("EntityType must be a non-empty string for EntityUpdatedEvent");
        }

        if (entityUpdated.OldData is null)
        {
            errors.Add("OldData must not be null for EntityUpdatedEvent");
        }

        if (entityUpdated.NewData is null)
        {
            errors.Add("NewData must not be null for EntityUpdatedEvent");
        }

        if (entityUpdated.ChangedProperties is null)
        {
            errors.Add("ChangedProperties must not be null for EntityUpdatedEvent");
        }
        else if (entityUpdated.ChangedProperties.Count == 0)
        {
            errors.Add("ChangedProperties must contain at least one changed property for EntityUpdatedEvent");
        }
        else
        {
            foreach (var property in entityUpdated.ChangedProperties)
            {
                if (string.IsNullOrWhiteSpace(property))
                {
                    errors.Add("ChangedProperties must not contain empty or whitespace property names for EntityUpdatedEvent");
                    break;
                }
            }
        }
    }

    private static void ValidateEntityDeletedEvent(EntityDeletedEvent entityDeleted, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(entityDeleted.EntityId))
        {
            errors.Add("EntityId must be a non-empty string for EntityDeletedEvent");
        }

        if (string.IsNullOrWhiteSpace(entityDeleted.EntityType))
        {
            errors.Add("EntityType must be a non-empty string for EntityDeletedEvent");
        }

        if (entityDeleted.DeletedData is null)
        {
            errors.Add("DeletedData must not be null for EntityDeletedEvent");
        }
    }

    private static void ValidateCustomDomainEvent(CustomDomainEvent customEvent, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(customEvent.EventName))
        {
            errors.Add("EventName must be a non-empty string for CustomDomainEvent");
        }

        if (string.IsNullOrWhiteSpace(customEvent.AggregateId))
        {
            errors.Add("AggregateId must be a non-empty string for CustomDomainEvent");
        }

        if (string.IsNullOrWhiteSpace(customEvent.AggregateType))
        {
            errors.Add("AggregateType must be a non-empty string for CustomDomainEvent");
        }

        if (customEvent.Payload is null)
        {
            errors.Add("Payload must not be null for CustomDomainEvent");
        }
    }

    private static void ValidateNotificationEvent(NotificationEvent notification, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(notification.NotificationType))
        {
            errors.Add("NotificationType must be a non-empty string for NotificationEvent");
        }

        if (string.IsNullOrWhiteSpace(notification.RecipientId))
        {
            errors.Add("RecipientId must be a non-empty string for NotificationEvent");
        }

        if (string.IsNullOrWhiteSpace(notification.Subject))
        {
            errors.Add("Subject must be a non-empty string for NotificationEvent");
        }

        if (string.IsNullOrWhiteSpace(notification.Body))
        {
            errors.Add("Body must be a non-empty string for NotificationEvent");
        }
    }
}