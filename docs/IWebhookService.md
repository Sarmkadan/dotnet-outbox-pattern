# IWebhookService

Defines the contract for a service that manages webhook registrations, deliveries, and testing operations within the `dotnet-outbox-pattern` project.

## API

### `public WebhookService`

The concrete implementation of `IWebhookService` that handles webhook registration, retrieval, deletion, and delivery tracking. This class is designed to integrate with the outbox pattern for reliable message delivery.

### `public async Task<dynamic> RegisterWebhookAsync(string url, string eventType, Dictionary<string, string>? headers = null)`

Registers a new webhook endpoint for a specific event type.

- **Parameters**:
  - `url` (string): The absolute URL where the webhook payload will be delivered.
  - `eventType` (string): The type of event to subscribe to (e.g., `OrderCreated`).
  - `headers` (Dictionary<string, string>?, optional): Optional HTTP headers to include in the webhook request.
- **Return value**: A dynamic object representing the registered webhook, including its unique identifier and metadata.
- **Exceptions**:
  - Throws `ArgumentException` if `url` is null or empty, or if `eventType` is null or empty.
  - Throws `InvalidOperationException` if the webhook URL is unreachable during validation.

### `public async Task<dynamic?> GetWebhookAsync(Guid id)`

Retrieves a registered webhook by its unique identifier.

- **Parameters**:
  - `id` (Guid): The unique identifier of the webhook to retrieve.
- **Return value**: A dynamic object representing the webhook if found; otherwise, `null`.
- **Exceptions**:
  - Throws `ArgumentException` if `id` is empty.

### `public async Task<List<dynamic>> GetWebhooksAsync(string? eventType = null)`

Retrieves all registered webhooks, optionally filtered by event type.

- **Parameters**:
  - `eventType` (string?, optional): The event type to filter by. If `null`, returns all webhooks.
- **Return value**: A list of dynamic objects representing the matching webhooks.
- **Exceptions**: None.

### `public async Task<bool> DeleteWebhookAsync(Guid id)`

Removes a registered webhook by its unique identifier.

- **Parameters**:
  - `id` (Guid): The unique identifier of the webhook to delete.
- **Return value**: `true` if the webhook was found and deleted; otherwise, `false`.
- **Exceptions**:
  - Throws `ArgumentException` if `id` is empty.

### `public async Task<List<dynamic>> GetDeliveriesAsync(Guid webhookId, int? limit = null)`

Retrieves the delivery history for a specific webhook.

- **Parameters**:
  - `webhookId` (Guid): The unique identifier of the webhook.
  - `limit` (int?, optional): The maximum number of deliveries to return. If `null`, returns all deliveries.
- **Return value**: A list of dynamic objects representing the delivery attempts, including status and timestamps.
- **Exceptions**:
  - Throws `ArgumentException` if `webhookId` is empty.

### `public async Task<dynamic?> TestWebhookAsync(Guid id)`

Validates that a registered webhook endpoint is reachable and responds correctly.

- **Parameters**:
  - `id` (Guid): The unique identifier of the webhook to test.
- **Return value**: A dynamic object representing the test result (e.g., HTTP status, response time) if successful; otherwise, `null`.
- **Exceptions**:
  - Throws `ArgumentException` if `id` is empty.
  - Throws `InvalidOperationException` if the webhook URL is unreachable or returns an unexpected response.

## Usage

### Registering a Webhook
