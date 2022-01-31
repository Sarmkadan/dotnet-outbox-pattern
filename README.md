## OutboxMessageControllerExtensions

The `OutboxMessageControllerExtensions` class provides a set of extension methods for the `OutboxMessageController` class, enabling additional functionality for outbox message management, such as retrieving messages by state, retrying failed messages, and publishing events in batches.

### Usage Example

```csharp
using DotnetOutboxPattern.Controllers;
using DotnetOutboxPattern.Dtos;
using Microsoft.AspNetCore.Mvc;

public class OutboxMessageControllerExample
{
    private readonly OutboxMessageController _controller;

    public OutboxMessageControllerExample(OutboxMessageController controller)
    {
        _controller = controller;
    }

    public async Task RunExampleAsync()
    {
        // Get all failed messages
        var failedMessagesResult = await _controller.GetFailedMessagesAsync();
        if (failedMessagesResult is OkObjectResult okResult && okResult.Value is PaginatedResponse<OutboxMessageDto> failedMessages)
        {
            Console.WriteLine($"Failed messages: {failedMessages.Items.Count}");
        }

        // Retry all failed messages
        var retryResult = await _controller.RetryAllFailedMessagesAsync();
        if (retryResult is OkObjectResult retryOkResult && retryOkResult.Value is BatchResult retryBatchResult)
        {
            Console.WriteLine($"Retry result: {retryBatchResult.SuccessCount} succeeded, {retryBatchResult.FailedCount} failed");
        }

        // Publish events in batch
        var eventsToPublish = new[] { new PublishableEvent { /* initialize event properties */ } };
        var publishResult = await _controller.PublishEventsBatchAsync(eventsToPublish);
        if (publishResult is OkObjectResult publishOkResult && publishOkResult.Value is BatchPublishResult publishBatchResult)
        {
            Console.WriteLine($"Published {publishBatchResult.SuccessCount} events, failed {publishBatchResult.FailedCount}");
        }
    }
}

public class BatchResult 
{
    public string? Status { get; set; }
    public int Count { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}

public class BatchPublishResult : BatchResult 
{
    public int TotalEvents { get; set; }
    public IEnumerable<OutboxMessageDto>? PublishedMessages { get; set; }
}

public class MessageStateSummary 
{
    public int TotalMessages { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public int PublishedCount { get; set; }
    public int ArchivedCount { get; set; }
}

## ExportControllerExtensions

The `ExportControllerExtensions` class provides a set of extension methods for the `ExportController` class, enabling additional functionality for exporting outbox messages in various formats. It allows for creating export requests, exporting messages, getting format details, and checking if a format is supported. 

### Usage Example

```csharp
using DotnetOutboxPattern.Controllers;
using DotnetOutboxPattern.Dtos;
using Microsoft.AspNetCore.Mvc;

public class ExportControllerExample
{
    private readonly ExportController _controller;

    public ExportControllerExample(ExportController controller)
    {
        _controller = controller;
    }

    public async Task RunExampleAsync()
    {
        // Create an export request
        var request = _controller.CreateExportRequest("json");

        // Export messages
        var result = await _controller.ExportMessagesAsync("json");

        // Get format details
        var formatDetails = _controller.GetFormatDetails("json");

        // Check if a format is supported
        var isSupported = _controller.IsFormatSupported("json");

        // Get supported formats as JSON
        var supportedFormatsJson = _controller.GetSupportedFormatsJson();

        // Export messages in JSON format
        var jsonResult = await _controller.ExportJsonAsync();

        // Export messages in CSV format
        var csvResult = await _controller.ExportCsvAsync();
    }
}

## SerializationHelper

The `SerializationHelper` class provides consistent JSON serialization and deserialization across the application, with built-in support for custom type handling (Guid, DateTime) and error resilience. It includes methods for standard serialization, pretty-printed output, and JSON validation.

### Usage Example

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SerializationExample
{
    public void Run()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            CreatedAt = DateTime.UtcNow
        };

        // Serialize to compact JSON
        string json = SerializationHelper.Serialize(user);
        
        // Serialize to pretty-printed JSON
        string prettyJson = SerializationHelper.SerializePretty(user);
        
        // Deserialize back to object
        var deserializedUser = SerializationHelper.Deserialize<User>(json);
        
        // Validate JSON string
        bool isValid = SerializationHelper.IsValidJson(json);
        
        // Deserialize to dynamic object
        var dynamicUser = SerializationHelper.DeserializeDynamic(json, typeof(User));
    }
}

## RetryPolicyHelper

The `RetryPolicyHelper` class provides utilities for calculating retry delays and statistics based on different retry policies. It supports fixed interval, linear backoff, and exponential backoff strategies with optional jitter to prevent thundering herd problems.



### Usage Example

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;

public class RetryPolicyExample
{
    public void RunExample()
    {
        // Configure publishing options with exponential backoff
        var options = new PublishingOptions
        {
            RetryPolicy = RetryPolicyType.ExponentialBackoff,
            InitialRetryDelay = TimeSpan.FromSeconds(1),
            MaxRetryDelay = TimeSpan.FromSeconds(30),
            BackoffMultiplier = 2.0,
            UseJitter = true
        };

        // Calculate delay for specific attempt
        var delay = RetryPolicyHelper.CalculateDelay(3, options);
        Console.WriteLine($"Delay for attempt 3: {delay.TotalSeconds} seconds");

        // Calculate statistics for diagnostic purposes
        var stats = RetryPolicyHelper.CalculateStatistics(options, maxAttempts: 6);
        
        Console.WriteLine($"Retry Policy: {stats.RetryPolicy}");
        Console.WriteLine($"Max Attempts: {stats.MaxAttempts}");
        Console.WriteLine($"Total Retries: {stats.TotalRetries}");
        Console.WriteLine($"Total Delay Time: {stats.TotalDelayTime.TotalSeconds} seconds");
        Console.WriteLine($"Average Retry Delay: {stats.AverageRetryDelay.TotalSeconds} seconds");
        Console.WriteLine($"Max Retry Delay: {stats.MaxRetryDelay.TotalSeconds} seconds");
    }
}

## MessageContext

The `MessageContext` class manages distributed tracing context for outbox messages, providing correlation and causation IDs for message flow tracing.

### Usage Example

```csharp
using DotnetOutboxPattern.Infrastructure;
using System.Diagnostics;

public class MessageContextExample
{
    public void Run()
    {
        // Create a correlation ID
        string correlationId = MessageContext.GetOrCreateCorrelationId();

        // Create a causation ID
        string causationId = MessageContext.GetOrCreateCausationId();

        // Start an activity for a message
        var message = new OutboxMessage { /* initialize message properties */ };
        var activity = MessageContext.StartActivity(message, "ProcessMessage");

        // Record events
        MessageContext.RecordEvent("MessageReceived", new Dictionary<string, object> { { "message_id", message.Id } });

        // Dispose of the activity
        activity?.Dispose();

        // Or use a scope
        using var scope = MessageContext.StartActivity(message, "ProcessMessage").UseScope();
    }
}
```