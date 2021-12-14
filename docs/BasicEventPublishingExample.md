# BasicEventPublishingExample

Demonstrates the outbox pattern for reliable event publishing in a .NET application. This example type encapsulates user registration logic and ensures that a domain event is durably recorded in an outbox before publishing, decoupling the business transaction from external messaging infrastructure.

## API

### public string UserId

Gets the unique identifier assigned to the user upon successful registration. This value is populated after `RegisterUserAsync` completes without throwing.

### public string Email

Gets the email address provided during registration. Validated for non-null and basic format compliance inside `RegisterUserAsync`.

### public string FullName

Gets the full name supplied during registration. May be empty or whitespace; no semantic validation is enforced by the example.

### public DateTime RegisteredAt

Gets the UTC timestamp captured at the moment the user record is persisted. Set atomically with the outbox entry inside `RegisterUserAsync`.

### public UserService

Exposes the underlying domain service responsible for persistence and outbox coordination. Intended for dependency injection scenarios where callers need to inspect or extend the service configuration.

### public async Task RegisterUserAsync

Executes the full registration flow: validates input, persists the user entity, writes a corresponding `UserRegistered` event to the transactional outbox, and returns. The caller is responsible for subsequently triggering outbox dispatch. Throws `ArgumentException` when `Email` is null or empty. Throws `InvalidOperationException` if the user service is unavailable or the underlying database transaction fails.

### public static async Task Main

Application entry point that bootstraps the example, invokes `RegisterUserAsync`, and demonstrates outbox processing. Accepts command-line arguments (unvalidated in this example). Returns a task that completes when the demonstration finishes or an unhandled exception propagates.

## Usage

### Basic registration and outbox dispatch

```csharp
var example = new BasicEventPublishingExample
{
    Email = "alice@example.com",
    FullName = "Alice Marshall"
};

await example.RegisterUserAsync();

// In a real system, a background job would process the outbox.
// Here we simulate immediate dispatch for demonstration.
await example.UserService.ProcessOutboxAsync();

Console.WriteLine($"Registered user {example.UserId} at {example.RegisteredAt:O}");
```

### Registration with explicit validation and error handling

```csharp
var example = new BasicEventPublishingExample
{
    Email = "",   // intentionally invalid
    FullName = "Bob"
};

try
{
    await example.RegisterUserAsync();
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
    // Correct the input and retry
    example.Email = "bob@example.com";
    await example.RegisterUserAsync();
    await example.UserService.ProcessOutboxAsync();
}
```

## Notes

- The outbox entry and the user record are committed in a single transaction. If the transaction fails, no event is written and no user is persisted, preserving consistency.
- `RegisterUserAsync` does not publish the event to external brokers; it only writes to the outbox. A separate process (or an explicit call to the user service) must read and publish the outbox entries.
- The static `Main` method is provided for demonstration purposes and is not thread-safe. Multiple concurrent invocations of `RegisterUserAsync` on the same instance are not supported and will result in undefined `UserId` and `RegisteredAt` values.
- `RegisteredAt` uses UTC; callers converting to local time must account for timezone offsets themselves.
- `FullName` accepts empty strings. If business rules require a non-empty name, additional validation must be added externally before calling `RegisterUserAsync`.
