# EventsTests

The `EventsTests` class contains unit tests for the event types defined in the `dotnet-outbox-pattern` project. These tests validate the default constructors, property initialization, and overall correctness of domain events, entity-specific events (created, updated, deleted), custom domain events, notification events, and publishable events. Each test method is designed to run in isolation and asserts expected behavior using standard unit testing frameworks (e.g., xUnit, NUnit, MSTest).

## API

All methods are `public void` and accept no parameters. They return no value. Each method throws an `AssertFailedException` (or equivalent) if the underlying assertion fails.

- **`DomainEvent_DefaultConstructor_InitializesProperties`**  
  Verifies that the default constructor of a `DomainEvent` sets all properties to their expected default values (e.g., `Id` is not null, `OccurredOn` is a valid `DateTime`).

- **`DomainEvent_WithOptionalProperties_SetCorrectly`**  
  Ensures that optional properties of a `DomainEvent` (such as correlation ID or metadata) are correctly assigned when provided via a parameterized constructor or object initializer.

- **`EntityCreatedEvent_InitializesWithCorrectProperties`**  
  Confirms that an `EntityCreatedEvent` is properly initialized with the expected entity ID, timestamp, and any additional creation-specific data.

- **`EntityUpdatedEvent_InitializesWithCorrectProperties`**  
  Validates that an `EntityUpdatedEvent` contains the correct updated entity ID, previous and current state, and timestamp.

- **`EntityDeletedEvent_InitializesWithCorrectProperties`**  
  Checks that an `EntityDeletedEvent` holds the correct entity ID, deletion timestamp, and any relevant metadata.

- **`CustomDomainEvent_InitializesWithCorrectProperties`**  
  Tests that a user-defined custom domain event (inheriting from `DomainEvent`) initializes its own properties as well as the base class properties correctly.

- **`NotificationEvent_InitializesWithCorrectProperties`**  
  Verifies that a `NotificationEvent` (used for out-of-process notifications) is created with the expected recipient, message body, and timestamp.

- **`PublishableEvent_InitializesWithCorrectProperties`**  
  Asserts that a `PublishableEvent` (wrapping a domain event for outbox persistence) is correctly initialized with the inner event, publication status, and retry count.

- **`PublishableEvent_DefaultConstructor_SetsDefaults`**  
  Ensures that the default constructor of `PublishableEvent` sets sensible defaults (e.g., `Published` is `false`, `RetryCount` is `0`, and the inner event is `null`).

## Usage

The following examples demonstrate how to use the tested event types in a unit test or production-like scenario.

**Example 1: Creating and verifying an `EntityCreatedEvent`**

```csharp
[Fact]
public void CreateEntityCreatedEvent_ShouldHaveCorrectProperties()
{
    var entityId = Guid.NewGuid();
    var createdAt = DateTime.UtcNow;

    var createdEvent = new EntityCreatedEvent(entityId, createdAt);

    Assert.Equal(entityId, createdEvent.EntityId);
    Assert.Equal(createdAt, createdEvent.OccurredOn);
    Assert.NotNull(createdEvent.Id);
}
```

**Example 2: Using a `PublishableEvent` in an outbox workflow**

```csharp
[Fact]
public void PublishableEvent_ShouldWrapDomainEvent()
{
    var domainEvent = new EntityUpdatedEvent(Guid.NewGuid(), "old state", "new state");
    var publishable = new PublishableEvent(domainEvent);

    Assert.False(publishable.Published);
    Assert.Equal(0, publishable.RetryCount);
    Assert.Same(domainEvent, publishable.DomainEvent);
}
```

## Notes

- **Edge Cases**  
  - Default constructors for `DomainEvent` and `PublishableEvent` must produce non-null `Id` and valid `OccurredOn` values.  
  - Optional properties (e.g., correlation ID) may be `null` when not provided; tests ensure no `NullReferenceException` occurs.  
  - `PublishableEvent` with a `null` inner event is allowed only when using the default constructor; the parameterized constructor should reject `null` arguments (typically via `ArgumentNullException`).  
  - Timestamps are expected to be in UTC; tests may fail if local time is used inadvertently.

- **Thread Safety**  
  - The test methods themselves are not thread-safe and should be executed sequentially within a test runner.  
  - The event types under test are simple data transfer objects (DTOs) with no mutable shared state; they are inherently thread-safe for read operations after construction.  
  - `PublishableEvent` properties like `Published` and `RetryCount` are intended to be modified by a single outbox processor; concurrent writes are not guarded and must be handled externally (e.g., via database transactions or locks).
