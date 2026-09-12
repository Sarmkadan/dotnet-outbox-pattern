# IntegrationEventPublisher

`IntegrationEventPublisher` routes an event to one or more named, in-memory channel publishers. Registrations are grouped by the generic event type and channel name. Publishing to all channels invokes the registered publishers concurrently with `Task.WhenAll`; publishing to one channel invokes only the matching registration.

The registry is stored on the `IntegrationEventPublisher` instance. Registrations and lookups use the exact `typeof(T)` supplied to each generic method: registering a channel for `DomainEvent` does not also register it for a derived event type when that derived type is used as `T`.

Channel invocation errors are caught by `IntegrationEventPublisher`, logged, and are not propagated to the caller. A missing event-type or channel registration is also logged and returns without throwing.

## Public types and methods

### `IIntegrationEventPublisher`

- `Task PublishAsync<T>(T event) where T : class` publishes the event to every channel registered for `T`. It completes without invoking a channel when none is registered.
- `Task PublishToChannelAsync<T>(T event, string channel) where T : class` publishes the event to the named channel registered for `T`. It completes without invoking a publisher when the event type or channel is not registered.
- `void RegisterPublisher<T>(string channel, IIntegrationEventPublisherChannel<T> publisher) where T : class` registers a channel publisher for the exact type `T`. Registering another publisher under the same type and channel name replaces the previous publisher. A null publisher causes `ArgumentNullException`.

### `IIntegrationEventPublisherChannel<T>`

`Task PublishAsync(T event)` defines the operation that a channel publisher must provide. `T` is contravariant and must be a reference type.

### `IntegrationEventPublisher`

The constructor requires an `ILogger<IntegrationEventPublisher>` and throws `ArgumentNullException` when it is null. Its public methods implement `IIntegrationEventPublisher` as described above.

### Included `DomainEvent` channels

- `WebhookIntegrationEventPublisher.PublishAsync(DomainEvent event)` passes the runtime event type name and event to `IWebhookHandler.PublishToWebhooksAsync`. Its constructor requires a webhook handler and logger.
- `ExternalApiIntegrationEventPublisher.PublishAsync(DomainEvent event)` passes the configured API URL and event to `IExternalApiClient.CallAsync`, then logs success or a non-success status. Its constructor requires an API client, API URL, and logger.
- `InProcessIntegrationEventPublisher.PublishAsync(DomainEvent event)` delegates to `IEventPublisher.PublishAsync`. Its constructor requires an event publisher and logger.

Each included channel catches and logs exceptions from its dependency rather than propagating them. Each constructor throws `ArgumentNullException` for a null dependency, URL, or logger.

## Usage example

This example registers two channels for the same exact event type, publishes to both, and then publishes only to one named channel.

```csharp
using DotnetOutboxPattern.Events;
using DotnetOutboxPattern.Integration;
using Microsoft.Extensions.Logging;

public sealed class AuditChannel : IIntegrationEventPublisherChannel<MessagePublishedEvent>
{
    public Task PublishAsync(MessagePublishedEvent @event)
    {
        Console.WriteLine($"Audit: {@event.MessageId}");
        return Task.CompletedTask;
    }
}

public sealed class MetricsChannel : IIntegrationEventPublisherChannel<MessagePublishedEvent>
{
    public Task PublishAsync(MessagePublishedEvent @event)
    {
        Console.WriteLine($"Attempts: {@event.PublishAttempts}");
        return Task.CompletedTask;
    }
}

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var publisher = new IntegrationEventPublisher(
    loggerFactory.CreateLogger<IntegrationEventPublisher>());

publisher.RegisterPublisher<MessagePublishedEvent>("audit", new AuditChannel());
publisher.RegisterPublisher<MessagePublishedEvent>("metrics", new MetricsChannel());

var integrationEvent = new MessagePublishedEvent
{
    MessageId = Guid.NewGuid(),
    AggregateId = "order-123",
    PublishAttempts = 1
};

await publisher.PublishAsync(integrationEvent);
await publisher.PublishToChannelAsync(integrationEvent, "audit");
```

The built-in channels implement `IIntegrationEventPublisherChannel<DomainEvent>`. To use one with the registry, register and publish with `DomainEvent` as the generic type, for example:

```csharp
publisher.RegisterPublisher<DomainEvent>("webhook", webhookChannel);
await publisher.PublishAsync<DomainEvent>(integrationEvent);
```
