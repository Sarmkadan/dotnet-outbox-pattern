# RabbitMqMessagePublisher

Publishes outbox messages to a RabbitMQ broker using the configured exchange and routing strategy. This type is part of the outbox pattern implementation and handles serialization, connection management, and delivery confirmation for messages that have been persisted in the outbox store.

## API

### RabbitMqMessagePublisher

```csharp
public RabbitMqMessagePublisher()
```

Default constructor. Initializes a new instance of the publisher with settings obtained from the application configuration. No explicit parameters are required; the constructor reads broker address, exchange name, and credentials from the current configuration context.

Does not throw under normal circumstances. May throw `InvalidOperationException` if required configuration sections are missing or malformed.

### PublishAsync

```csharp
public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
```

Serializes the given outbox message and publishes it to the configured RabbitMQ exchange. Awaits broker acknowledgment before returning.

**Parameters:**
- `message` — The outbox message to publish. Must contain a non-null body and routing metadata.
- `cancellationToken` — Optional token to cancel the publish operation.

**Returns:**
A `Task` that completes when the broker has confirmed receipt of the message.

**Throws:**
- `ArgumentNullException` if `message` is null.
- `RabbitMQ.Client.Exceptions.BrokerUnreachableException` if the broker cannot be contacted.
- `OperationCanceledException` if the cancellation token is signaled before acknowledgment arrives.
- `SerializationException` if the message body cannot be serialized.

### KafkaMessagePublisher

```csharp
public KafkaMessagePublisher()
```

Constructor for the Kafka publisher variant. Creates an instance configured for Kafka-based outbox delivery. Reads bootstrap servers, topic, and serializer settings from configuration.

Does not throw under normal circumstances. May throw `InvalidOperationException` if required configuration sections are missing.

### PublishAsync (Kafka)

```csharp
public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
```

Publishes the outbox message to a Kafka topic. Produces the message asynchronously and awaits delivery report confirmation.

**Parameters:**
- `message` — The outbox message to publish. Must contain a non-null body and topic routing information.
- `cancellationToken` — Optional token to cancel the produce operation.

**Returns:**
A `Task` that completes when the Kafka producer has confirmed delivery.

**Throws:**
- `ArgumentNullException` if `message` is null.
- `KafkaException` if the broker cluster is unreachable or the topic does not exist.
- `OperationCanceledException` if the cancellation token is signaled.
- `SerializationException` if the message body cannot be serialized.

### AzureServiceBusPublisher

```csharp
public AzureServiceBusPublisher()
```

Constructor for the Azure Service Bus publisher variant. Initializes a sender bound to a queue or topic as specified in configuration. Reads connection string and entity path from the current configuration context.

Does not throw under normal circumstances. May throw `InvalidOperationException` if required configuration sections are missing.

### PublishAsync (Azure Service Bus)

```csharp
public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
```

Sends the outbox message to an Azure Service Bus queue or topic. Uses the AMQP-based sender and awaits completion.

**Parameters:**
- `message` — The outbox message to send. Must contain a non-null body.
- `cancellationToken` — Optional token to cancel the send operation.

**Returns:**
A `Task` that completes when Service Bus accepts the message.

**Throws:**
- `ArgumentNullException` if `message` is null.
- `ServiceBusException` if the namespace is unreachable, the entity is disabled, or authentication fails.
- `OperationCanceledException` if the cancellation token is signaled.
- `SerializationException` if the message body cannot be serialized.

### WebhookPublisher

```csharp
public WebhookPublisher()
```

Constructor for the webhook publisher variant. Initializes an HTTP client configured with the target URL and authentication settings from configuration.

Does not throw under normal circumstances. May throw `InvalidOperationException` if required configuration sections are missing or the URL is malformed.

### PublishAsync (Webhook)

```csharp
public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
```

Delivers the outbox message payload as an HTTP POST to the configured webhook endpoint. The body is sent as JSON with a content type of `application/json`.

**Parameters:**
- `message` — The outbox message to deliver. Must contain a non-null body.
- `cancellationToken` — Optional token to cancel the HTTP request.

**Returns:**
A `Task` that completes when the HTTP response is received. Non-success status codes do not cause exceptions; the caller should inspect the response if needed.

**Throws:**
- `ArgumentNullException` if `message` is null.
- `HttpRequestException` if the endpoint is unreachable or the request fails at the transport level.
- `OperationCanceledException` if the cancellation token is signaled.
- `SerializationException` if the message body cannot be serialized to JSON.

### Main

```csharp
public static void Main(string[] args)
```

Entry point for running the outbox publisher as a standalone console application. Parses command-line arguments to select the publisher type and processes pending outbox messages in a loop until the process is terminated.

**Parameters:**
- `args` — Command-line arguments specifying publisher selection and optional runtime flags.

Does not return under normal operation; runs until externally terminated.

## Usage

### Example 1: Publishing a single outbox message to RabbitMQ

```csharp
var publisher = new RabbitMqMessagePublisher();
var outboxMessage = new OutboxMessage
{
    Id = Guid.NewGuid(),
    Body = JsonSerializer.Serialize(new { OrderId = 42, Status = "Shipped" }),
    RoutingKey = "orders.shipped",
    CreatedAt = DateTime.UtcNow
};

try
{
    await publisher.PublishAsync(outboxMessage, CancellationToken.None);
    Console.WriteLine($"Message {outboxMessage.Id} published successfully.");
}
catch (BrokerUnreachableException ex)
{
    Console.WriteLine($"Broker unavailable: {ex.Message}");
    // Message remains in outbox for retry
}
```

### Example 2: Batch processing with cancellation support

```csharp
var publisher = new KafkaMessagePublisher();
var outboxStore = new OutboxStore();
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

var pendingMessages = await outboxStore.GetUnpublishedMessagesAsync(batchSize: 50);

foreach (var message in pendingMessages)
{
    if (cts.Token.IsCancellationRequested)
    {
        Console.WriteLine("Batch cancelled, remaining messages stay in outbox.");
        break;
    }

    try
    {
        await publisher.PublishAsync(message, cts.Token);
        await outboxStore.MarkAsPublishedAsync(message.Id);
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        Console.WriteLine($"Failed to publish {message.Id}: {ex.Message}");
        // Leave unpublished for next retry cycle
    }
}
```

## Notes

- All `PublishAsync` overloads accept a `CancellationToken` and will cooperatively cancel in-flight operations. Cancellation does not roll back a message that has already been acknowledged by the broker; the outbox state should be updated only after successful acknowledgment.
- The constructors read configuration at instantiation time. Changes to configuration while the publisher is in use are not reflected until a new instance is created.
- `WebhookPublisher.PublishAsync` does not throw on non-2xx HTTP responses. Callers that need to distinguish delivery failures from transport failures should wrap the call and inspect the response status code.
- None of the publishers guarantee exactly-once delivery. Duplicate publishes are possible if acknowledgment is received but the outbox update fails. Consumers should implement idempotency.
- The `Main` method is intended for console-hosted scenarios and blocks indefinitely. It is not suitable for use within ASP.NET Core or other long-running hosted services without adaptation.
- These types are not thread-safe by design. Each publisher instance should be used from a single logical thread or synchronized externally if shared across concurrent operations. Connection pools and channels within each publisher are not safe for concurrent access.
