# DomainEventValidation

`DomainEventValidation` provides extension methods that validate `DomainEvent` instances. Validation collects every applicable error in a read-only list: the common `DomainEvent` rules run first, followed by the rules for the event's recognized runtime type.

## Public API

### `Validate`

```csharp
IReadOnlyList<string> Validate(this DomainEvent? value)
```

Returns a read-only list containing the validation errors described below. The list is empty when the event satisfies every applicable rule. Passing `null` throws `ArgumentNullException` with `value` as the parameter name.

### `IsValid`

```csharp
bool IsValid(this DomainEvent? value)
```

Calls `Validate` and returns `true` when the returned list is empty. It returns `false` when at least one validation error exists. Because it delegates to `Validate`, passing `null` throws `ArgumentNullException`.

### `EnsureValid`

```csharp
void EnsureValid(this DomainEvent? value)
```

Returns normally when validation produces no errors. Passing `null` throws `ArgumentNullException`. For a non-null invalid event, it throws `ArgumentException` with this format, using the platform newline before and between errors:

```text
Domain event validation failed:
<first error>
<next error>
```

## Rules and error messages

### All domain events

| Condition | Error |
| --- | --- |
| `EventId == Guid.Empty` | `EventId must be a non-empty GUID` |
| `OccurredAt == default` | `OccurredAt must be a valid DateTime` |
| `OccurredAt > DateTime.UtcNow.AddMinutes(5)` | `OccurredAt cannot be in the future` |

The future-time rule therefore permits an `OccurredAt` value up to five minutes beyond the UTC time observed when that check runs. The future-time check is skipped when `OccurredAt` is the default value, so only the default-value error is added in that case.

### `EntityCreatedEvent`

| Condition | Error |
| --- | --- |
| `EntityId` is null, empty, or whitespace | `EntityId must be a non-empty string for EntityCreatedEvent` |
| `EntityType` is null, empty, or whitespace | `EntityType must be a non-empty string for EntityCreatedEvent` |
| `EntityData` is null | `EntityData must not be null for EntityCreatedEvent` |

### `EntityUpdatedEvent`

| Condition | Error |
| --- | --- |
| `EntityId` is null, empty, or whitespace | `EntityId must be a non-empty string for EntityUpdatedEvent` |
| `EntityType` is null, empty, or whitespace | `EntityType must be a non-empty string for EntityUpdatedEvent` |
| `OldData` is null | `OldData must not be null for EntityUpdatedEvent` |
| `NewData` is null | `NewData must not be null for EntityUpdatedEvent` |
| `ChangedProperties` is null | `ChangedProperties must not be null for EntityUpdatedEvent` |
| `ChangedProperties` is empty | `ChangedProperties must contain at least one changed property for EntityUpdatedEvent` |
| Any item in `ChangedProperties` is null, empty, or whitespace | `ChangedProperties must not contain empty or whitespace property names for EntityUpdatedEvent` |

The item check stops after the first invalid property name, so that error is added at most once.

### `EntityDeletedEvent`

| Condition | Error |
| --- | --- |
| `EntityId` is null, empty, or whitespace | `EntityId must be a non-empty string for EntityDeletedEvent` |
| `EntityType` is null, empty, or whitespace | `EntityType must be a non-empty string for EntityDeletedEvent` |
| `DeletedData` is null | `DeletedData must not be null for EntityDeletedEvent` |

### `CustomDomainEvent`

| Condition | Error |
| --- | --- |
| `EventName` is null, empty, or whitespace | `EventName must be a non-empty string for CustomDomainEvent` |
| `AggregateId` is null, empty, or whitespace | `AggregateId must be a non-empty string for CustomDomainEvent` |
| `AggregateType` is null, empty, or whitespace | `AggregateType must be a non-empty string for CustomDomainEvent` |
| `Payload` is null | `Payload must not be null for CustomDomainEvent` |

### `NotificationEvent`

| Condition | Error |
| --- | --- |
| `NotificationType` is null, empty, or whitespace | `NotificationType must be a non-empty string for NotificationEvent` |
| `RecipientId` is null, empty, or whitespace | `RecipientId must be a non-empty string for NotificationEvent` |
| `Subject` is null, empty, or whitespace | `Subject must be a non-empty string for NotificationEvent` |
| `Body` is null, empty, or whitespace | `Body must be a non-empty string for NotificationEvent` |

## Runtime type handling

Type-specific validation is selected with a switch over the event's runtime type. The recognized types are `EntityCreatedEvent`, `EntityUpdatedEvent`, `EntityDeletedEvent`, `CustomDomainEvent`, and `NotificationEvent`. A different `DomainEvent` subtype receives only the common `EventId` and `OccurredAt` checks because the switch has no default validation branch.
