# OutboxMessageControllerExtensions

`OutboxMessageControllerExtensions` provides a comprehensive set of extension methods and supporting properties for integrating outbox message management directly into ASP.NET Core controllers. This type facilitates the monitoring, retrieval, and administration of outbox messages, allowing developers to implement operations such as querying message states, triggering retries for failed processes, and executing batch event publishing with minimal boilerplate within controller actions.

## API

### Methods

All methods are defined as `public static` extension methods intended for use within ASP.NET Core `ControllerBase` implementations.

*   **`GetFailedMessagesAsync`**: Retrieves a collection of outbox messages currently in the failed state.
*   **`GetPendingMessagesAsync`**: Retrieves a collection of outbox messages that are awaiting processing.
*   **`GetPublishedMessagesAsync`**: Retrieves a collection of outbox messages that have been successfully published.
*   **`GetArchivedMessagesAsync`**: Retrieves a collection of outbox messages that have been archived.
*   **`RetryAllFailedMessagesAsync`**: Triggers a re-processing attempt for all messages currently marked as failed.
*   **`GetMessageStateSummaryAsync`**: Provides a summary report detailing the count of messages across various states.
*   **`GetMessagesByTopicAsync`**: Retrieves outbox messages filtered by a specific topic.
*   **`PublishEventsBatchAsync`**: Initiates the publishing of a defined batch of events.

### Properties

*   **`Status` (string?)**: Gets or sets the current operational status of the outbox process or message.
*   **`Count` (int)**: Gets or sets the generic count associated with an operation.
*   **`SuccessCount` (int)**: Gets or sets the total number of successfully processed items.
*   **`FailedCount` (int)**: Gets or sets the total number of items that failed processing.
*   **`TotalEvents` (int)**: Gets or sets the total number of events contained within a batch.
*   **`PublishedMessages` (IEnumerable<OutboxMessageDto>?)**: Gets or sets the collection of successfully published messages.
*   **`TotalMessages` (int)**: Gets or sets the total number of outbox messages.
*   **`PendingCount` (int)**: Gets or sets the number of messages currently pending.
*   **`PublishedCount` (int)**: Gets or sets the total number of messages that have been published.
*   **`ArchivedCount` (int)**: Gets or sets the total number of archived messages.

## Usage

### Retrieving Failed Messages

```csharp
[HttpGet("failed")]
public async Task<IActionResult> GetFailed()
{
    return await this.GetFailedMessagesAsync();
}
```

### Retrying Failed Messages

```csharp
[HttpPost("retry")]
public async Task<IActionResult> RetryFailed()
{
    return await this.RetryAllFailedMessagesAsync();
}
```

## Notes

*   **Thread-Safety**: As these methods operate on the `ControllerBase` and typically rely on services resolved from the `HttpContext` request container, they inherit the thread-safety characteristics of the underlying ASP.NET Core infrastructure. Ensure that the persistence layer and service registrations are configured to handle concurrent requests appropriately.
*   **Async Execution**: All methods are asynchronous and must be awaited to ensure proper completion and to avoid blocking the thread pool.
*   **Error Handling**: These methods do not inherently catch all exceptions from the underlying data store; ensure appropriate middleware or filter-based exception handling is configured for the application.
*   **Data Integrity**: When utilizing `RetryAllFailedMessagesAsync`, ensure that the downstream event consumers are idempotent, as retrying may result in duplicate event processing if the previous failure occurred after the event was dispatched but before the message state was updated.
