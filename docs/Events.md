# Domain events

`Domain/Events.cs` defines the domain-event base class, five built-in event types, and the `PublishableEvent` wrapper used to submit an event to the outbox.

The file defines event data only. It does not monitor entities or automatically raise events. Application code creates the appropriate event when the corresponding domain action occurs and passes it to `IOutboxService.PublishEventAsync`; the HTTP endpoint `POST /api/outbox/events` also accepts a `PublishableEvent`. Publishing persists an outbox message for later processing.

## `DomainEvent`

`DomainEvent` is the abstract base class for domain events. All of its properties are init-only.

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `EventId` | `Guid` | `Guid.NewGuid()` | Unique identifier for the event instance. |
| `OccurredAt` | `DateTime` | `DateTime.UtcNow` | Time at which the event occurred. |
| `CorrelationId` | `string?` | `null` | Optional correlation identifier for tracing. |
| `CausationId` | `string?` | `null` | Optional identifier of the command or event that caused this event. |
| `UserId` | `string?` | `null` | Optional identifier of the user who triggered the event. |

`DomainEvent` uses System.Text.Json polymorphic metadata. Its discriminator property is `eventKind`; serialization fails for an unregistered derived type when it is serialized polymorphically as `DomainEvent`.

## Built-in event types

Each built-in type is sealed and inherits the properties of `DomainEvent`.

### `EntityCreatedEvent`

Represents an event created by application code when an entity is created. Its JSON discriminator is `entityCreated`. When published by `OutboxService`, it maps to `EventType.Created`, and `EntityId` becomes the outbox message's aggregate ID.

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `EntityId` | `string` | `null!` | Identifier of the created entity. |
| `EntityType` | `string` | `null!` | Type of the created entity. |
| `EntityData` | `Dictionary<string, object>` | Empty dictionary | Data for the created entity. |

### `EntityUpdatedEvent`

Represents an event created by application code when an entity is updated. Its JSON discriminator is `entityUpdated`. When published by `OutboxService`, it maps to `EventType.Updated`, and `EntityId` becomes the outbox message's aggregate ID.

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `EntityId` | `string` | `null!` | Identifier of the updated entity. |
| `EntityType` | `string` | `null!` | Type of the updated entity. |
| `OldData` | `Dictionary<string, object>` | Empty dictionary | Entity state before the update. |
| `NewData` | `Dictionary<string, object>` | Empty dictionary | Entity state after the update. |
| `ChangedProperties` | `List<string>` | Empty list | Names of the changed properties. |

### `EntityDeletedEvent`

Represents an event created by application code when an entity is deleted. Its JSON discriminator is `entityDeleted`. When published by `OutboxService`, it maps to `EventType.Deleted`, and `EntityId` becomes the outbox message's aggregate ID.

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `EntityId` | `string` | `null!` | Identifier of the deleted entity. |
| `EntityType` | `string` | `null!` | Type of the deleted entity. |
| `DeletedData` | `Dictionary<string, object>` | Empty dictionary | Entity state at deletion. |

### `CustomDomainEvent`

Represents an application-defined business occurrence that does not use one of the entity lifecycle shapes. Its JSON discriminator is `custom`. When published by `OutboxService`, it maps to `EventType.Custom`, and `AggregateId` becomes the outbox message's aggregate ID.

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `EventName` | `string` | `null!` | Name or type of the custom event. |
| `AggregateId` | `string` | `null!` | Identifier of the aggregate or entity to which the event applies. |
| `AggregateType` | `string` | `null!` | Type of that aggregate. |
| `Payload` | `Dictionary<string, object>` | Empty dictionary | Custom event payload. |

### `NotificationEvent`

Represents an event created by application code to alert subscribers. Its JSON discriminator is `notification`. When published by `OutboxService`, it maps to `EventType.Notification`, and `RecipientId` becomes the outbox message's aggregate ID.

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `NotificationType` | `string` | `null!` | Type of notification. |
| `RecipientId` | `string` | `null!` | Notification recipient. |
| `Subject` | `string` | `null!` | Notification subject. |
| `Body` | `string` | `null!` | Notification body or content. |
| `IsCritical` | `bool` | `false` | Whether the notification is critical or urgent. |
| `ActionUrl` | `string?` | `null` | Optional action URL. |

## `PublishableEvent`

`PublishableEvent` is a sealed publishing envelope; it does not inherit from `DomainEvent`.

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Event` | `DomainEvent` | `null!` | Domain event to publish. |
| `Topic` | `string` | `null!` | Destination topic. |
| `PartitionKey` | `string?` | `null` | Optional key used to maintain ordering. |
| `MaxAttempts` | `int` | `5` | Maximum publication attempts. |
| `DeliveryGuarantee` | `DeliveryGuarantee` | `AtLeastOnce` | Requested delivery guarantee. |
| `IdempotencyKey` | `string?` | `null` | Optional explicit deduplication key; `OutboxService` uses the event ID when this is absent. |
| `ScheduledTime` | `DateTime?` | `null` | Optional future time at which the message becomes eligible for publishing. |

When `DeliveryGuarantee` is `AtMostOnce`, `OutboxService` stores one maximum publish attempt regardless of `MaxAttempts`; otherwise it stores `MaxAttempts`. The service records `ScheduledTime` as the outbox message's scheduled time.
