# DomainEvent

`DomainEvent` is a record type used to represent domain events in the outbox pattern implementation. It captures state changes in aggregates or entities, including before/after snapshots and metadata for tracing and debugging purposes.

## API

### `EventId`
A unique identifier for the event. Generated automatically when the event is created.

### `OccurredAt`
The timestamp when the event occurred, typically set to the current UTC time when the event is instantiated.

### `CorrelationId`
An optional identifier used to correlate this event with other events or operations in a distributed system. Useful for tracing event flows across services.

### `CausationId`
An optional identifier indicating the event or command that triggered this event. Helps reconstruct the causal chain of events.

### `UserId`
An optional identifier representing the user who initiated the action that caused this event. Used for auditing and access control.

### `EntityId`
The identifier of the entity or aggregate that this event pertains to. Required field.

### `EntityType`
The fully qualified type name of the entity or aggregate that this event pertains to. Required field.

### `EntityData`
A dictionary containing the current state of the entity or aggregate as key-value pairs. Used for snapshotting or replaying state.

### `OldData`
A dictionary containing the previous state of the entity or aggregate before the change. Used for tracking changes or reverting state.

### `NewData`
A dictionary containing the updated state of the entity or aggregate after the change. Used for applying changes or auditing.

### `ChangedProperties`
A list of property names that were modified in the entity or aggregate. Used for selective updates or change tracking.

### `DeletedData`
A dictionary containing the state of the entity or aggregate at the time of deletion. Used for soft-delete scenarios or audit trails.

### `EventName`
The name of the event, typically following a convention like `EntityType.EventType` (e.g., `Order.Created`). Used for event routing and handling.

### `AggregateId`
The identifier of the aggregate root that this event pertains to. Required field.

### `AggregateType`
The fully qualified type name of the aggregate root that this event pertains to. Required field.

### `Payload`
A dictionary containing additional, event-specific data. Used for extensibility without modifying the event structure.

## Usage

### Publishing a Domain Event
