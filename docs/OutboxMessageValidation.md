# OutboxMessageValidation

`OutboxMessageValidation` provides extension methods for validating an `OutboxMessage`. Validation evaluates every applicable rule and returns the errors in the order documented below; it does not stop after the first error.

## Public API

### `Validate`

```csharp
IReadOnlyList<string> Validate(this OutboxMessage? value)
```

Returns a read-only list of validation errors. The list is empty when no rule adds an error. Passing `null` throws `ArgumentNullException` with `value` as the parameter name.

### `IsValid`

```csharp
bool IsValid(this OutboxMessage? value)
```

Returns `false` for `null`. For a non-null message, it calls `Validate` and returns `true` only when the returned list is empty.

### `EnsureValid`

```csharp
void EnsureValid(this OutboxMessage? value)
```

Returns normally when the message has no validation errors. Passing `null` throws `ArgumentNullException` with `value` as the parameter name. For a non-null invalid message, it throws `ArgumentException` containing every error in this format, with platform newlines:

```text
OutboxMessage validation failed:
- <first error>
- <next error>
```

## Rules and error messages

### Required strings

Each of these conditions uses `string.IsNullOrWhiteSpace`, so `null`, the empty string, and whitespace-only values are invalid.

| Condition | Error |
| --- | --- |
| `IdempotencyKey` is null, empty, or whitespace | `IdempotencyKey cannot be null, empty, or whitespace` |
| `AggregateId` is null, empty, or whitespace | `AggregateId cannot be null, empty, or whitespace` |
| `AggregateType` is null, empty, or whitespace | `AggregateType cannot be null, empty, or whitespace` |
| `EventData` is null, empty, or whitespace | `EventData cannot be null, empty, or whitespace` |
| `EventTypeName` is null, empty, or whitespace | `EventTypeName cannot be null, empty, or whitespace` |
| `Topic` is null, empty, or whitespace | `Topic cannot be null, empty, or whitespace` |

### Enum values

| Condition | Error |
| --- | --- |
| `EventType` is not a defined `EventType` value | `EventType must be a defined value, got <numeric value>` |
| `DeliveryGuarantee` is not a defined `DeliveryGuarantee` value | `DeliveryGuarantee must be a defined value, got <value>` |

The `EventType` error explicitly formats the underlying integer. The `DeliveryGuarantee` error interpolates the enum value itself; for an undefined numeric value, its standard enum formatting is used.

### Numeric ranges

| Condition | Error |
| --- | --- |
| `MaxPublishAttempts <= 0` | `MaxPublishAttempts must be greater than 0` |
| `PublishAttempts < 0` | `PublishAttempts cannot be negative` |
| `PublishAttempts > MaxPublishAttempts` | `PublishAttempts cannot exceed MaxPublishAttempts` |
| `Priority < 0` | `Priority cannot be negative` |

These checks are independent. For example, values can produce both a `MaxPublishAttempts` error and a `PublishAttempts` relationship error.

### Dates and times

| Condition | Error |
| --- | --- |
| `CreatedAt == default` | `CreatedAt must be set to a non-default DateTime value` |
| `CreatedAt` is non-default and `CreatedAt.Kind != DateTimeKind.Utc` | `CreatedAt must be in UTC timezone` |
| `LastProcessedAt` is set and its `Kind` is not UTC | `LastProcessedAt must be in UTC timezone if set` |
| `LastProcessedAt` is set and is earlier than `CreatedAt` | `LastProcessedAt cannot be earlier than CreatedAt` |
| `PublishedAt` is set and its `Kind` is not UTC | `PublishedAt must be in UTC timezone if set` |
| `PublishedAt` is set and is earlier than `CreatedAt` | `PublishedAt cannot be earlier than CreatedAt` |
| `ScheduledFor` is set and its `Kind` is not UTC | `ScheduledFor must be in UTC timezone if set` |
| `ScheduledFor` is set and is earlier than `CreatedAt` | `ScheduledFor cannot be earlier than CreatedAt` |
| `LockExpiresAt` is set and its `Kind` is not UTC | `LockExpiresAt must be in UTC timezone if set` |
| `LockExpiresAt` is set and is earlier than `DateTime.UtcNow` when checked | `LockExpiresAt cannot be in the past` |

For each optional date, the timezone and ordering/time checks are independent, so both errors can be added. A lock expiration exactly equal to the `DateTime.UtcNow` value read by the comparison is not considered earlier by that comparison, although elapsed time can make near-current values time-sensitive.

### State consistency

Additional checks depend on `State`.

| State | Condition | Error |
| --- | --- | --- |
| `Pending` | `PublishedAt` is set | `PublishedAt cannot be set when State is Pending` |
| `Pending` | `LastProcessedAt` is set | `LastProcessedAt cannot be set when State is Pending` |
| `Processing` | `IsLocked == false` | `State is Processing but IsLocked is false` |
| `Processing` | `LockExpiresAt` is not set | `State is Processing but LockExpiresAt is not set` |
| `Published` | `PublishedAt` is not set | `State is Published but PublishedAt is not set` |
| `Published` | `ErrorMessage` is not null | `State is Published but ErrorMessage is set` |
| `Failed` | `ErrorMessage` is null, empty, or whitespace | `State is Failed but ErrorMessage is not set` |
| `Failed` | `PublishAttempts < MaxPublishAttempts` | `State is Failed but PublishAttempts is less than MaxPublishAttempts` |

`Archived` adds no state-specific errors. An undefined `OutboxMessageState` value also adds no state-specific error because the switch has no default branch, and there is no separate defined-value check for `State`.

For `Published`, even an empty string counts as an error because the check is `ErrorMessage is not null`. For `Failed`, the error-message check rejects null, empty, and whitespace-only values.

### Optional identifiers

| Condition | Error |
| --- | --- |
| `CorrelationId` is non-null but empty or whitespace | `CorrelationId cannot be empty or whitespace if set` |
| `PartitionKey` is non-null but empty or whitespace | `PartitionKey cannot be empty or whitespace if set` |

A null `CorrelationId` or `PartitionKey` is allowed. `CausationId` is not validated by this class.

## Properties not validated

The validator adds no direct rules for `Id`, `ErrorStackTrace`, `CausationId`, `Metadata`, or `Headers`. It also does not validate the contents of `EventData` or `Metadata`; only the required-string check on `EventData` is performed.
