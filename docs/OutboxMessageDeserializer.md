# OutboxMessageDeserializer

`OutboxMessageDeserializer` converts the version-tolerant envelope in an `OutboxMessage.EventData` value back into its CLR payload. It delegates envelope decoding to an `IOutboxSerializer` and type-name resolution to an `IOutboxTypeResolver`.

If deserialization fails, the class logs the failure, calls `OutboxMessage.RecordFailure`, moves the message to the dead letter queue through `IDeadLetterService`, and returns `null`. This keeps an unresolvable type or incompatible payload from propagating an exception into the dispatch loop.

The class is sealed and implements `IOutboxMessageDeserializer`. The application registers that interface with a scoped `OutboxMessageDeserializer` implementation.

## Public API

### Constructor

```csharp
public OutboxMessageDeserializer(
    IOutboxSerializer serializer,
    IOutboxTypeResolver typeResolver,
    IDeadLetterService deadLetterService,
    ILogger<OutboxMessageDeserializer> logger)
```

The constructor receives the serializer, type resolver, dead letter service, and logger used by the deserialization workflow. It throws `ArgumentNullException` if any dependency is null.

### `DeserializeOrDeadLetterAsync`

```csharp
Task<object?> DeserializeOrDeadLetterAsync(
    OutboxMessage message,
    CancellationToken cancellationToken = default)
```

The method passes `message.EventData` and the configured type resolver to `IOutboxSerializer.DeserializeEnvelope`.

- When the returned result is successful, the method returns `OutboxDeserializationResult.Value`. A successful result can contain a null value.
- When the result is unsuccessful, the method logs the message ID, resolved envelope metadata, and error; records the error on the supplied `OutboxMessage`; awaits `IDeadLetterService.MoveToDlqAsync`; and returns `null`.
- If the result has no error text, the recorded failure message is `"version-tolerant deserialization failed"`.
- The cancellation token is passed to `MoveToDlqAsync`; it is not used on the successful path.
- Passing a null `message` throws `ArgumentNullException`.

On a failed result, `RecordFailure` increments `PublishAttempts`, updates `LastProcessedAt`, stores the error, clears the stack trace and processing lock, and can set the message state to `Failed` when the maximum attempt count is reached. The subsequent dead-letter behavior is owned by the configured `IDeadLetterService`.

## Usage example

Consumers can depend on `IOutboxMessageDeserializer` and check the returned runtime value before dispatching it:

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;

public sealed class OutboxDispatcher
{
    private readonly IOutboxMessageDeserializer _deserializer;

    public OutboxDispatcher(IOutboxMessageDeserializer deserializer)
    {
        _deserializer = deserializer;
    }

    public async Task DispatchAsync(
        OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        var payload = await _deserializer.DeserializeOrDeadLetterAsync(
            outboxMessage,
            cancellationToken);

        if (payload is OrderCreated orderCreated)
        {
            Console.WriteLine($"Dispatching order {orderCreated.OrderId}");
        }
        // Any value that is not the expected event type is not dispatched here.
    }
}

public sealed record OrderCreated(string OrderId);
```

Because the return type is `object?`, callers must test or cast it to the expected event type. A null return alone does not distinguish a failed result from a successful envelope whose JSON payload deserialized to null.
