# OutboxMessageController

The `OutboxMessageController` serves as the HTTP API entry point for managing domain events within the outbox pattern implementation. It provides endpoints for publishing new events, querying message states by ID or aggregate root, retrieving statistical data, manually retrying failed dispatches, and archiving processed messages to maintain database hygiene. This controller facilitates both operational monitoring and administrative intervention in the event delivery lifecycle.

## API

### `public OutboxMessageController`
Initializes a new instance of the `OutboxMessageController`. This constructor typically injects required services such as the outbox repository or event dispatcher, though specific dependencies are resolved internally by the dependency injection container.

### `public async Task<IActionResult> PublishEventAsync`
Accepts a domain event payload and persists it to the outbox storage for asynchronous processing.
*   **Parameters**: Accepts the event data via the request body (typically deserialized from JSON).
*   **Return Value**: Returns an `IActionResult`. Usually returns `201 Created` with the location of the new message upon success, or `400 Bad Request` if the payload is invalid.
*   **Throws**: May throw exceptions related to serialization failures or database connectivity issues if not handled internally by the service layer.

### `public async Task<IActionResult> GetMessageByIdAsync`
Retrieves a specific outbox message based on its unique identifier.
*   **Parameters**: Requires a message ID (usually passed as a route parameter).
*   **Return Value**: Returns an `IActionResult`. Returns `200 OK` with the message details if found, or `404 Not Found` if the ID does not exist.
*   **Throws**: Throws format exceptions if the provided ID cannot be parsed into the expected GUID or integer format.

### `public async Task<IActionResult> GetMessagesByAggregateAsync`
Fetches a collection of outbox messages associated with a specific aggregate root ID.
*   **Parameters**: Requires the aggregate ID (usually passed as a route or query parameter).
*   **Return Value**: Returns an `IActionResult`. Returns `200 OK` containing a list of messages, or an empty list if no messages are associated with the aggregate.
*   **Throws**: Throws format exceptions if the aggregate ID parameter is malformed.

### `public async Task<IActionResult> GetMessagesAsync`
Retrieves a paginated or filtered list of outbox messages, typically supporting queries based on status (e.g., Pending, Failed, Published).
*   **Parameters**: Accepts optional query parameters for filtering (status, date range) and pagination (page number, page size).
*   **Return Value**: Returns an `IActionResult` containing the collection of messages matching the criteria.
*   **Throws**: May throw exceptions if invalid filter combinations or negative pagination values are provided.

### `public async Task<IActionResult> RetryMessageAsync`
Manually triggers a retry attempt for a specific message that previously failed to publish.
*   **Parameters**: Requires the message ID of the failed event.
*   **Return Value**: Returns an `IActionResult`. Returns `200 OK` or `202 Accepted` if the retry is queued successfully, `404 Not Found` if the message doesn't exist, or `409 Conflict` if the message is not in a retryable state (e.g., already published).
*   **Throws**: Throws exceptions if the underlying transport mechanism is unavailable during the forced retry.

### `public async Task<IActionResult> ArchivePublishedMessagesAsync`
Moves successfully published messages older than a specified threshold to an archive table or marks them for deletion to optimize performance.
*   **Parameters**: Optionally accepts a cutoff date or retention period via query parameters.
*   **Return Value**: Returns an `IActionResult` indicating the number of messages archived or a success status.
*   **Throws**: May throw timeouts or database concurrency exceptions if the volume of messages to archive is excessively large.

### `public async Task<IActionResult> GetStatisticsAsync`
Provides aggregated metrics regarding the outbox state, such as counts of pending, failed, and published messages.
*   **Parameters**: No parameters required.
*   **Return Value**: Returns an `IActionResult` containing a statistical summary object.
*   **Throws**: Unlikely to throw unless the database is unreachable.

## Usage

### Example 1: Publishing an Event and Verifying Status
This example demonstrates posting a new event to the outbox and immediately querying its status by ID to ensure it was persisted correctly.

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class EventPublisher
{
    private readonly HttpClient _httpClient;

    public EventPublisher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task PublishAndVerifyAsync()
    {
        var eventData = new { AggregateId = "agg-123", Type = "OrderCreated", Data = new { OrderId = 55 } };
        var content = new StringContent(JsonSerializer.Serialize(eventData), Encoding.UTF8, "application/json");

        // Publish the event
        var postResponse = await _httpClient.PostAsync("/api/outbox/publish", content);
        postResponse.EnsureSuccessStatusCode();

        // Assume the API returns the ID in the location header or body; here we simulate knowing the ID
        string messageId = "msg-9876"; 
        
        // Verify the message exists
        var getResponse = await _httpClient.GetAsync($"/api/outbox/{messageId}");
        if (getResponse.IsSuccessStatusCode)
        {
            var messageJson = await getResponse.Content.ReadAsStringAsync();
            // Process message details
        }
    }
}
```

### Example 2: Administrative Retry and Archival
This example illustrates an administrative workflow where an operator identifies failed messages, retries them, and subsequently archives old successful messages.

```csharp
using System.Net.Http;
using System.Threading.Tasks;

public class OutboxMaintenance
{
    private readonly HttpClient _adminClient;

    public OutboxMaintenance(HttpClient adminClient)
    {
        _adminClient = adminClient;
    }

    public async Task PerformMaintenanceAsync(string failedMessageId)
    {
        // Retry a specific failed message
        var retryResponse = await _adminClient.PostAsync($"/api/outbox/{failedMessageId}/retry", null);
        
        if (retryResponse.IsSuccessStatusCode)
        {
            // Archive messages published before today
            var archiveResponse = await _adminClient.PostAsync("/api/outbox/archive?cutoffDate=2023-10-01", null);
            archiveResponse.EnsureSuccessStatusCode();
        }
        else
        {
            // Handle retry failure (e.g., message not found or already processed)
            var errorContent = await retryResponse.Content.ReadAsStringAsync();
            System.Console.WriteLine($"Retry failed: {errorContent}");
        }
    }
}
```

## Notes

*   **Idempotency**: While the outbox pattern inherently supports idempotency on the consumer side, the `RetryMessageAsync` endpoint should be called with caution. Repeated manual retries of the same message without resolving the underlying transient error may lead to duplicate processing attempts if the consumer does not strictly enforce idempotency checks.
*   **Concurrency**: The `ArchivePublishedMessagesAsync` operation may involve bulk database updates. Executing this endpoint concurrently from multiple instances or clients could lead to database locking or deadlocks depending on the underlying persistence implementation. It is recommended to trigger archival via a single scheduled task rather than ad-hoc API calls in high-throughput environments.
*   **Thread Safety**: As an ASP.NET Core controller, `OutboxMessageController` is instantiated per request. The instance itself is not shared across threads, but the injected services it relies on (repositories, dispatchers) must be thread-safe if registered as singletons. The asynchronous nature of all public methods ensures non-blocking I/O, but callers should await tasks properly to avoid thread pool starvation.
*   **Error Handling**: Methods returning `IActionResult` encapsulate HTTP status codes. Clients must inspect the status code rather than relying solely on exception throwing, as business logic errors (e.g., message not found, invalid state for retry) are returned as appropriate HTTP responses (404, 409) rather than thrown exceptions.
