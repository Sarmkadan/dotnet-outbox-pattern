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

## OutboxMetricsCollectorJsonExtensions

The `OutboxMetricsCollectorJsonExtensions` class provides static methods for serializing and deserializing `OutboxMetricsCollector` instances to and from JSON.

### Usage Example

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Examples;

public class OutboxMetricsCollectorExample
{
    public async Task RunExampleAsync()
    {
        var metrics = new OutboxMetricsCollector(
            new FakeOutboxService(),
            new FakeDeadLetterService(),
            new FakeLogger<OutboxMetricsCollector>());

        var json = OutboxMetricsCollectorJsonExtensions.ToJson(metrics);
        Console.WriteLine(json);

        var metricsFromJson = OutboxMetricsCollectorJsonExtensions.FromJson(json);
        Console.WriteLine(metricsFromJson);

        var success = OutboxMetricsCollectorJsonExtensions.TryFromJson(json, out var metricsFromJson2);
        Console.WriteLine(success);
        Console.WriteLine(metricsFromJson2);
    }
}
```

// ... goes in between
