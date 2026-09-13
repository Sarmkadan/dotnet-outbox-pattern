# OutboxEnvelope

`OutboxEnvelope` is the serialized wrapper used by `SystemTextJsonOutboxSerializer` to store a message type name and schema version alongside a JSON payload. The type is a sealed positional record in the `DotnetOutboxPattern.Services` namespace.

Keeping the type name outside the payload allows `DeserializeEnvelope` to resolve the stored name through an `IOutboxTypeResolver`. The resolver can therefore map an existing stored name to the CLR type that should currently receive the payload.

## Properties

The primary constructor defines three public properties:

| Property | Type | Meaning |
| --- | --- | --- |
| `MessageType` | `string` | The stored message type name used by `IOutboxTypeResolver` during deserialization. |
| `SchemaVersion` | `int` | The schema version supplied when the payload was serialized. |
| `Payload` | `string` | The message body serialized as JSON. |

`OutboxEnvelope` does not validate these values itself.

## Serialization

`SystemTextJsonOutboxSerializer.SerializeEnvelope<T>` creates an envelope as follows:

1. It rejects a null `value` with `ArgumentNullException`.
2. It selects `typeof(T).FullName`, falling back to `typeof(T).Name`, for `MessageType`.
3. It serializes `value` with the serializer's `JsonSerializerOptions` and stores the resulting JSON text in `Payload`.
4. It stores the supplied `schemaVersion`, whose default value is `1`.
5. It serializes the complete `OutboxEnvelope` with the same options and returns that outer JSON string.

Because `Payload` is a string, the payload JSON is escaped in the outer JSON. With the serializer's default options, this code:

```csharp
using DotnetOutboxPattern.Services;

var serializer = new SystemTextJsonOutboxSerializer();
var json = serializer.SerializeEnvelope(new OrderCreated("ORD-123"), schemaVersion: 2);

public sealed record OrderCreated(string OrderId);
```

produces an envelope with this shape (the exact `MessageType` includes the namespace when the declared type has one):

```json
{
  "MessageType": "OrderCreated",
  "SchemaVersion": 2,
  "Payload": "{\"OrderId\":\"ORD-123\"}"
}
```

The default serializer writes the JSON on one line; the example is formatted for readability. Custom `JsonSerializerOptions` supplied to `SystemTextJsonOutboxSerializer` affect both the payload and the outer envelope.

## Deserialization

`SystemTextJsonOutboxSerializer.DeserializeEnvelope` first deserializes the outer JSON into an `OutboxEnvelope`. It then:

- requires `MessageType` to contain a non-whitespace value;
- asks the supplied `IOutboxTypeResolver` to map that name to a CLR type;
- checks that the resolved type is permitted by the serializer's safety check; and
- deserializes `Payload` into the resolved CLR type with the serializer's configured options.

The method returns an `OutboxDeserializationResult` rather than returning the payload object directly. A typical round trip is:

```csharp
using DotnetOutboxPattern.Services;

var serializer = new SystemTextJsonOutboxSerializer();
var envelopeJson = serializer.SerializeEnvelope(new OrderCreated("ORD-123"), schemaVersion: 2);

var messageType = typeof(OrderCreated).FullName ?? typeof(OrderCreated).Name;
var resolver = new DefaultOutboxTypeResolver();
resolver.Register(messageType, typeof(OrderCreated));

var result = serializer.DeserializeEnvelope(envelopeJson, resolver);
if (result.Success && result.Value is OrderCreated message)
{
    Console.WriteLine(message.OrderId);
}

public sealed record OrderCreated(string OrderId);
```

Malformed envelope JSON, a missing or unresolved message type, a type rejected by the safety check, and an incompatible payload are represented as failed results. `DeserializeEnvelope` validates its direct arguments before processing: a null or empty `envelopeJson` throws `ArgumentException`, and a null resolver throws `ArgumentNullException`.

## OutboxDeserializationResult

`OutboxDeserializationResult` is a sealed class with properties whose setters are private:

| Property | Type | Meaning |
| --- | --- | --- |
| `Success` | `bool` | Indicates whether envelope deserialization succeeded. |
| `Value` | `object?` | The deserialized payload value on success; otherwise null. A successful JSON `null` payload can also produce a null value. |
| `MessageType` | `string?` | The type name read from the envelope when available. |
| `SchemaVersion` | `int` | The schema version read from the envelope when available; failed results default to `0` when no version is supplied. |
| `Error` | `string?` | The failure description on failure; otherwise null. |

The static `Ok` factory creates a successful result and requires a non-null, non-empty message type. The static `Failed` factory creates a failed result and requires a non-null, non-empty error; its optional message type defaults to null and its optional schema version defaults to `0`.
