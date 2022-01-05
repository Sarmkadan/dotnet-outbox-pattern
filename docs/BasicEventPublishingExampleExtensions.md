# BasicEventPublishingExampleExtensions

`BasicEventPublishingExampleExtensions` provides utility and extension methods designed to simplify interactions with the outbox pattern within the `BasicEventPublishingExample` scenario. These methods facilitate the creation of event objects, user registration, service provider configuration, and event publication, encapsulating boilerplate logic into reusable components for consistent outbox implementation.

## API

### CreateUserRegisteredEvent
Creates a new `UserRegisteredEvent` instance suitable for the `BasicEventPublishingExample`.
- **Return Value:** `BasicEventPublishingExample.UserRegisteredEvent`

### RegisterUserWithValidationAsync
Registers a user with validation logic as part of the outbox workflow.
- **Return Value:** `Task<OutboxMessage>`
- **Exceptions:** `ValidationException` if provided user data fails validation.

### CreateServiceProvider
Initializes and returns an `IServiceProvider` configured for the example environment.
- **Return Value:** `IServiceProvider`
- **Exceptions:** `InvalidOperationException` if configuration fails.

### BatchRegisterUsersAsync
Performs asynchronous registration of multiple users in a single operation.
- **Return Value:** `Task`
- **Exceptions:** `Exception` if batch processing fails.

### PublishEventAsync
Publishes a specified event to the outbox asynchronously.
- **Return Value:** `Task<OutboxMessage>`
- **Exceptions:** `InvalidOperationException` if the event publication process fails.

### Id
- **Purpose:** Unique identifier for the extension instance.
- **Type:** `string`

### EventType
- **Purpose:** Defines the category or type of the event.
- **Type:** `string`

### EventId
- **Purpose:** Unique identifier for the specific event instance.
- **Type:** `string`

### Content
- **Purpose:** The serialized payload of the event.
- **Type:** `string`

### CreatedAt
- **Purpose:** The timestamp indicating when the event was created.
- **Type:** `DateTime`

## Usage

```csharp
// Example 1: Registering a user with the outbox pattern
var serviceProvider = BasicEventPublishingExampleExtensions.CreateServiceProvider();
var outboxMessage = await BasicEventPublishingExampleExtensions.RegisterUserWithValidationAsync(userData);

// Example 2: Publishing an event instance
var eventHandler = new BasicEventPublishingExampleExtensions();
var publishedMessage = await eventHandler.PublishEventAsync(eventData);
```

## Notes

- **Thread Safety:** While static methods are designed to be thread-safe regarding shared state, instance members rely on the configuration of the underlying `IServiceProvider` and associated repositories. Ensure the container is configured with appropriate lifetimes.
- **Asynchronous Operations:** All methods suffixed with `Async` follow the standard TAP pattern. Ensure that callers properly `await` these tasks to avoid potential deadlocks or swallowed exceptions.
- **Validation:** `RegisterUserWithValidationAsync` expects valid input parameters. Failure to adhere to the expected schema may result in validation exceptions or inconsistent state in the outbox.
- **Error Handling:** Exceptions should be handled at the calling layer, particularly when dealing with asynchronous registration and publication processes, as these may involve external database or network dependencies.
