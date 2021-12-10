# IWebhookHandler

`IWebhookHandler` defines the contract for processing outbound webhook deliveries in the `dotnet-outbox-pattern` library. Implementations are responsible for serializing integration events into webhook payloads, delivering them to external HTTP endpoints, and verifying cryptographic signatures to ensure payload integrity. The interface decouples webhook dispatch logic from the outbox processing pipeline, allowing different delivery strategies and signature schemes to be plugged in.

## API

### WebhookHandler

```csharp
public WebhookHandler WebhookHandler { get; }
```

Exposes the underlying `WebhookHandler` instance associated with this interface implementation. This property provides access to configuration details, such as the target URL, secret keys, and serialization settings, that govern how a webhook payload is constructed and delivered. It is expected to be set during initialization and remain immutable for the lifetime of the handler.

### HandleDeliveryAsync

```csharp
public async Task<bool> HandleDeliveryAsync()
```

Attempts to deliver the pending webhook payload to the configured endpoint. The method retrieves the next eligible outbox message, builds the webhook request, transmits it via HTTP, and processes the response. It returns `true` if the delivery succeeded (the endpoint acknowledged receipt with a success status code) and the outbox message can be marked as processed; otherwise `false`, indicating that the message should remain in the outbox for retry. Exceptions thrown during serialization or network communication are caught internally and translated into a `false` result rather than propagated.

### PublishToWebhooksAsync

```csharp
public async Task PublishToWebhooksAsync()
```

Publishes all pending outbox messages to their respective webhook endpoints in a batch. This method enumerates undelivered messages, groups them by target webhook configuration, and invokes delivery for each. It is designed for scheduled or trigger-based flush scenarios where multiple messages must be dispatched in a single operation. The method does not return a value; individual delivery failures are handled internally and do not cause the batch to abort.

### VerifySignature

```csharp
public bool VerifySignature(string payload, string signature, string secret)
```

Validates that a received webhook payload was signed with the expected secret. The method recomputes the HMAC (or equivalent cryptographic hash) of the `payload` using the provided `secret` and compares it against the `signature` string. Returns `true` if the signatures match, confirming authenticity and integrity; `false` otherwise. This member is typically used on the receiving side to verify incoming webhooks, but is exposed on the handler interface so that the same verification logic can be reused for self-testing or symmetric validation scenarios.

Parameters:
- `payload` — The raw request body string to verify.
- `signature` — The signature header value received from the sender.
- `secret` — The shared secret key used to compute the expected signature.

## Usage

### Example 1: Processing a single outbox message

```csharp
public class OutboxDispatcher
{
    private readonly IWebhookHandler _handler;

    public OutboxDispatcher(IWebhookHandler handler)
    {
        _handler = handler;
    }

    public async Task ProcessNextMessageAsync(CancellationToken ct)
    {
        bool delivered = await _handler.HandleDeliveryAsync();

        if (delivered)
        {
            Console.WriteLine("Message delivered and marked as processed.");
        }
        else
        {
            Console.WriteLine("Delivery failed; message will be retried.");
        }
    }
}
```

### Example 2: Flushing all pending messages on a schedule

```csharp
public class WebhookFlushJob : BackgroundService
{
    private readonly IWebhookHandler _handler;

    public WebhookFlushJob(IWebhookHandler handler)
    {
        _handler = handler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _handler.PublishToWebhooksAsync();
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

## Notes

- `HandleDeliveryAsync` is designed for at-least-once delivery semantics. A `false` return value leaves the outbox message unacknowledged, so the same message may be retried. Implementations should ensure that the external endpoint is idempotent or that duplicate deliveries are acceptable.
- `PublishToWebhooksAsync` iterates over all pending messages without short-circuiting on individual failures. Callers should not rely on exceptions to detect partial failures; instead, implementers are expected to log or track failures internally.
- `VerifySignature` performs a constant-time comparison where possible to mitigate timing attacks. The `secret` parameter is never logged or persisted by the interface itself; implementations must handle it securely.
- The `WebhookHandler` property is assumed to be thread-safe for reads after initialization. If an implementation allows mutation at runtime, the caller must synchronize access to avoid race conditions between configuration changes and in-flight deliveries.
- No members accept a `CancellationToken`. Long-running HTTP calls within `HandleDeliveryAsync` and `PublishToWebhooksAsync` should be governed by internal timeout policies rather than external cancellation.
