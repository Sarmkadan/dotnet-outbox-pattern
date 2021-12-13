# IIntegrationEventPublisher
The `IIntegrationEventPublisher` type is designed to facilitate the publishing of integration events in a decoupled manner, allowing for various publishing strategies to be employed. This interface serves as a contract for different publisher implementations, enabling the publication of events to different channels or endpoints, such as webhooks, external APIs, or in-process handlers.

## API
* `IntegrationEventPublisher`: The constructor for creating an instance of the publisher.
* `async Task PublishAsync<T>`: Publishes an event of type `T` asynchronously. The event is sent to the registered publisher for handling. Parameters: `T` - the type of event to publish. Return value: An asynchronous task. Throws: Exceptions may be thrown if the event cannot be published or if the registered publisher fails to handle the event.
* `async Task PublishToChannelAsync<T>`: Publishes an event of type `T` to a specific channel asynchronously. Parameters: `T` - the type of event to publish. Return value: An asynchronous task. Throws: Exceptions may be thrown if the event cannot be published to the channel or if the channel's handler fails to process the event.
* `void RegisterPublisher<T>`: Registers a publisher for events of type `T`. Parameters: `T` - the type of event for which the publisher is registered.
* `WebhookIntegrationEventPublisher`, `ExternalApiIntegrationEventPublisher`, `InProcessIntegrationEventPublisher`: These are specific implementations of the `IIntegrationEventPublisher` interface, each tailored for publishing events via webhooks, external APIs, or in-process handlers, respectively. Each has its own `async Task PublishAsync` method for publishing events.

## Usage
The following examples demonstrate how to use the `IIntegrationEventPublisher` interface:
```csharp
// Example 1: Publishing an event using the InProcessIntegrationEventPublisher
var publisher = new InProcessIntegrationEventPublisher();
publisher.RegisterPublisher<MyEvent>();
await publisher.PublishAsync(new MyEvent { /* event data */ });

// Example 2: Publishing an event to a specific channel using the ExternalApiIntegrationEventPublisher
var externalApiPublisher = new ExternalApiIntegrationEventPublisher();
externalApiPublisher.RegisterPublisher<MyEvent>();
await externalApiPublisher.PublishToChannelAsync(new MyEvent { /* event data */ });
```

## Notes
When using the `IIntegrationEventPublisher` interface, consider the following:
- Thread-safety: The implementations of `IIntegrationEventPublisher` should be designed with thread-safety in mind, especially when publishing events concurrently.
- Error handling: Implementations should handle errors that may occur during event publication, such as network errors when publishing to external APIs or webhooks.
- Registration: The `RegisterPublisher<T>` method allows for registering publishers for specific event types, enabling flexible event handling strategies.
- Channel-specific publication: The `PublishToChannelAsync<T>` method enables publishing events to specific channels, which can be useful for scenarios where events need to be routed to different endpoints based on their type or content.
