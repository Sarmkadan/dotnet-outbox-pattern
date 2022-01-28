// entire file content ...
// ... goes in between

## BasicEventPublishingExampleExtensions

The `BasicEventPublishingExampleExtensions` class provides extension methods for the `BasicEventPublishingExample` class, allowing for easier creation and registration of users with validation.

### Usage Example

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class BasicEventPublishingExampleExample
{
    public async Task RunExampleAsync()
    {
        var example = new BasicEventPublishingExample();
        var userService = new UserService();
        var serviceProvider = example.CreateServiceProvider();

        var userRegisteredEvent = example.CreateUserRegisteredEvent(
            "user123",
            "user@example.com",
            "John Doe");

        var outboxMessage = await example.RegisterUserWithValidationAsync(
            userService,
            userRegisteredEvent.UserId,
            userRegisteredEvent.Email,
            userRegisteredEvent.FullName);

        await example.BatchRegisterUsersAsync(
            new[]
            {
                ("user456", "user456@example.com", "Jane Doe"),
                ("user789", "user789@example.com", "Bob Smith")
            });
    }
}
```

// ... goes in between
