// entire file content ...
// ... goes in between

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
``` 
```