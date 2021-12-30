# WebhookController

The `WebhookController` serves as the primary HTTP interface for managing webhook subscriptions and delivery lifecycles within the `dotnet-outbox-pattern` architecture. It exposes endpoints for clients to register interest in specific events, retrieve subscription details, list active subscriptions, remove subscriptions, inspect historical delivery attempts, manually trigger delivery processing, and validate endpoint connectivity through test payloads.

## API

### `public WebhookController`
Initializes a new instance of the `WebhookController` class. This constructor typically injects required services such as repository interfaces for subscription storage and outbox processing logic, though specific dependency signatures are encapsulated within the implementation.

### `public async Task<IActionResult> SubscribeAsync`
Registers a new webhook subscription for the calling client.
*   **Purpose**: Creates a persistent record linking a target URL to specific event types.
*   **Parameters**: Accepts standard HTTP request body data containing the target URL and event filters (deserialized from the incoming request).
*   **Return Value**: Returns an `IActionResult`, typically `OkObjectResult` containing the created subscription ID or `BadRequestResult` if validation fails.
*   **Throws**: May throw exceptions related to database connectivity or constraint violations if the underlying storage layer fails during the commit phase.

### `public async Task<IActionResult> GetSubscriptionAsync`
Retrieves the details of a specific webhook subscription by its identifier.
*   **Purpose**: Fetches the current configuration and status of a single subscription.
*   **Parameters**: Requires a subscription identifier, usually passed as a route parameter or query string.
*   **Return Value**: Returns an `IActionResult`; `OkObjectResult` with the subscription data if found, or `NotFoundResult` if the identifier does not exist.
*   **Throws**: Throws data access exceptions if the storage backend is unavailable.

### `public async Task<IActionResult> ListSubscriptionsAsync`
Retrieves a collection of all active webhook subscriptions.
*   **Purpose**: Provides an overview of registered endpoints, often supporting pagination or filtering logic internally.
*   **Parameters**: May accept optional query parameters for filtering by event type or status.
*   **Return Value**: Returns an `IActionResult` containing a collection of subscription objects (`OkObjectResult`) or an empty collection if none exist.
*   **Throws**: Propagates storage exceptions if the listing operation cannot be completed.

### `public async Task<IActionResult> DeleteSubscriptionAsync`
Removes an existing webhook subscription.
*   **Purpose**: Unregisters a webhook endpoint, stopping future event deliveries to that URL.
*   **Parameters**: Requires the unique identifier of the subscription to delete.
*   **Return Value**: Returns `NoContentResult` upon successful deletion or `NotFoundResult` if the subscription was not found.
*   **Throws**: May throw concurrency exceptions if the resource was modified between retrieval and deletion, or storage exceptions on failure.

### `public async Task<IActionResult> GetDeliveriesAsync`
Retrieves the history of delivery attempts for a specific subscription or event.
*   **Purpose**: Allows clients to audit successful and failed delivery attempts, including retry counts and error messages.
*   **Parameters**: Accepts identifiers for the subscription or specific event batch.
*   **Return Value**: Returns an `IActionResult` containing a list of delivery records (`OkObjectResult`).
*   **Throws**: Throws if the underlying event store or outbox table is inaccessible.

### `public async Task<IActionResult> DeliverAsync`
Manually triggers the processing of pending events in the outbox for webhook delivery.
*   **Purpose**: Forces an immediate sweep of the outbox table to dispatch queued events, bypassing standard background hosted service intervals.
*   **Parameters**: No explicit parameters required; operates on the global outbox state or scoped context.
*   **Return Value**: Returns `OkObjectResult` indicating the number of events processed or `InternalServerErrorResult` if critical failures occurred during dispatch.
*   **Throws**: Throws network-related exceptions if remote webhook endpoints are unreachable during the forced delivery, or transactional exceptions if state updates fail.

### `public async Task<IActionResult> TestWebhookAsync`
Sends a simulated test payload to a specified URL to verify connectivity and configuration.
*   **Purpose**: Validates that the target endpoint can receive and correctly respond to webhook requests without triggering actual business events.
*   **Parameters**: Accepts the target URL and optional test payload configuration from the request body.
*   **Return Value**: Returns `OkObjectResult` with the response details from the target server or `BadRequestResult` if the test connection fails.
*   **Throws**: Throws timeout exceptions or HTTP request exceptions if the target URL is unreachable or returns an invalid response.

## Usage

### Registering a New Subscription
The following example demonstrates how to invoke the subscription endpoint to register a new webhook for order events.

```csharp
var httpClient = new HttpClient();
var subscriptionRequest = new 
{
    Url = "https://api.example.com/webhooks/orders",
    Events = new[] { "OrderCreated", "OrderShipped" }
};

var content = new StringContent(
    JsonSerializer.Serialize(subscriptionRequest), 
    Encoding.UTF8, 
    "application/json"
);

var response = await httpClient.PostAsync("/api/webhook/subscribe", content);

if (response.IsSuccessStatusCode)
{
    var result = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
    Console.WriteLine($"Subscription created with ID: {result.Id}");
}
```

### Manually Triggering Delivery
This example illustrates how to call the delivery endpoint to force the immediate processing of queued outbox events.

```csharp
var httpClient = new HttpClient();

// Force immediate processing of pending outbox items
var response = await httpClient.PostAsync("/api/webhook/deliver", null);

if (response.IsSuccessStatusCode)
{
    var stats = await response.Content.ReadFromJsonAsync<DeliveryStatsDto>();
    Console.WriteLine($"Processed {stats.EventsDispatched} events.");
}
else
{
    Console.WriteLine("Failed to trigger delivery sweep.");
}
```

## Notes

*   **Concurrency**: As the controller methods are `async` and likely stateless regarding instance fields, they are generally thread-safe for concurrent requests. However, underlying storage operations (e.g., deleting a subscription while simultaneously reading it) rely on the database's isolation levels and may result in race conditions if not handled by the repository layer.
*   **Idempotency**: The `SubscribeAsync` method should be treated carefully; calling it multiple times with identical parameters may result in duplicate entries unless the backend enforces unique constraints on the URL/Event combination.
*   **Timeouts**: The `DeliverAsync` and `TestWebhookAsync` methods involve outbound HTTP calls. These operations are susceptible to network latency and remote server timeouts. Callers should implement appropriate retry policies or timeout configurations on the `HttpClient` used to invoke these endpoints.
*   **Error Handling**: Since these methods return `IActionResult`, specific failure modes (such as `404 Not Found` or `500 Internal Server Error`) are encoded in the HTTP status code rather than thrown as unhandled exceptions, except for critical infrastructure failures like database disconnections.
