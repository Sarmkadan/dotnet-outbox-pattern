# EventPublisher

## Purpose

`Events/EventPublisher.cs` provides `IEventPublisher` and its in-process implementation, `EventPublisher`. Subscribers register asynchronous handlers for a reference type, and publishing an event invokes the handlers registered for that exact generic type within the same process.

`EventPublisher` protects its subscriber collection with a lock. Publishing takes a snapshot of the current handlers before invoking them, then awaits each handler sequentially in subscription order. Exceptions from an individual handler are logged and are not propagated, so the publisher continues with the remaining handlers. If no handlers are registered for the type, publishing returns without invoking anything.

Subscriptions belong to the `EventPublisher` instance that created them; the class does not persist or transport events outside the process.

## Public methods

### `EventPublisher(ILogger<EventPublisher> logger)`

Creates an event publisher using the supplied logger. A null logger causes `ArgumentNullException`.

### `Task PublishAsync<T>(T event) where T : class`

Publishes `event` to a snapshot of the handlers registered for the exact type `T`. Each matching `Func<T, Task>` is awaited before the next handler is invoked. Handler exceptions are logged and suppressed. A null event causes `ArgumentNullException`.

The generic type used for publishing controls the lookup. For example, a handler subscribed for `DomainEvent` is not selected by `PublishAsync(messagePublishedEvent)` when type inference chooses `MessagePublishedEvent`; publish with `PublishAsync<DomainEvent>(messagePublishedEvent)` to select the `DomainEvent` subscription.

### `IDisposable Subscribe<T>(Func<T, Task> handler) where T : class`

Adds `handler` to the handlers registered for the exact type `T`. It returns an `IDisposable`; disposing it removes that handler from this publisher. A null handler causes `ArgumentNullException`.

## Usage example

```csharp
using DotnetOutboxPattern.Events;
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { });
var publisher = new EventPublisher(
    loggerFactory.CreateLogger<EventPublisher>());

using IDisposable subscription = publisher.Subscribe<MessagePublishedEvent>(
    @event =>
    {
        Console.WriteLine(
            $"Published message {@event.MessageId} for {@event.AggregateId}");
        return Task.CompletedTask;
    });

var messagePublished = new MessagePublishedEvent
{
    MessageId = Guid.NewGuid(),
    AggregateId = "order-123",
    PublishAttempts = 1
};

await publisher.PublishAsync(messagePublished);

// Disposing the subscription unregisters the handler.
subscription.Dispose();
await publisher.PublishAsync(messagePublished); // No handler is invoked.
```

The `using` declaration also disposes the subscription when its scope ends; the explicit `Dispose` above demonstrates when unsubscription takes effect.
