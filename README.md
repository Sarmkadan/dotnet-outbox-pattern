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

## QueryBuilderExtensions

The `QueryBuilderExtensions` static class provides a fluent, chainable API for constructing query
conditions on a `QueryBuilder` instance. It includes extension methods for common filtering
operations (equality, range, null checks, collection membership) and sorting, enabling you to
build expressive queries without manually managing the underlying condition list.

**Usage example**

```csharp
using DotnetOutboxPattern.Utilities;

var builder = new QueryBuilder()
    .Where("Status", "Active")
    .WhereGreaterThan("CreatedDate", DateTime.UtcNow.AddDays(-30))
    .WhereLessThan("Priority", 5)
    .WhereContains("Title", "urgent")
    .WhereIn("Category", "Finance", "HR", "IT")
    .WhereBetween("Amount", 1000, 5000)
    .WhereIsNotNull("Description")
    .OrderByDescending("CreatedDate")
    .And(new QueryBuilder().WhereIsNull("DeletedAt"))
    .Reset()
    .Where("OwnerId", GuidGenerator.NewGuid());

var summary = builder.GetFilterSummary();   // Dictionary<string, object?>
var conditions = builder.GetConditions();   // List<FilterCondition>
```

The example demonstrates chaining multiple extension methods, combining builders with `And`,
resetting the builder, and retrieving both a summary dictionary and the full list of
`FilterCondition` objects.

## ErrorResponseExtensions

The `ErrorResponseExtensions` class provides extension methods for `ErrorResponse` objects, allowing for additional functionality such as adding context, updating timestamps, and serializing to JSON. These methods enable you to create standardized error responses with ease.

### Usage Example

```csharp
using DotnetOutboxPattern.Dtos;

var errorResponse = new ErrorResponse
{
    Message = "An error occurred",
    Code = "500",
    Timestamp = DateTime.UtcNow,
    TraceId = Guid.NewGuid().ToString()
};

var errorResponseWithContext = errorResponse.WithContext("Additional context");
var errorResponseWithNewTimestamp = errorResponse.WithTimestamp(DateTime.UtcNow.AddHours(1));
var errorResponseWithNewTraceId = errorResponse.WithTraceId(Guid.NewGuid().ToString());

var json = errorResponse.ToJson(true);
Console.WriteLine(json);

var isClientError = errorResponse.IsClientError();
var isServerError = errorResponse.IsServerError();

var logFormat = errorResponse.ToLogFormat();
```

// ... goes in between
