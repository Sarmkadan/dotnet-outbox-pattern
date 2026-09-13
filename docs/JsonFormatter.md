# JsonFormatter

`JsonFormatter` converts a list of `OutboxMessage` instances into an indented JSON export. It implements `IDataFormatter`, uses the format name `json`, and advertises the `application/json` content type.

## Output format

The top-level JSON value is an object with these camel-case properties, in order:

| Property | Value |
| --- | --- |
| `messages` | An array containing one object for each input message, in input order |
| `exportedAt` | `DateTime.UtcNow` when `Format` constructs the export |
| `count` | The number of messages in the input list |

Each object in `messages` contains these properties:

| JSON property | `OutboxMessage` value | Formatting |
| --- | --- | --- |
| `id` | `Id` | Serialized as a JSON string |
| `idempotencyKey` | `IdempotencyKey` | Serialized as a JSON string |
| `aggregateId` | `AggregateId` | Serialized as a JSON string |
| `aggregateType` | `AggregateType` | Serialized as a JSON string |
| `eventTypeName` | `EventTypeName` | Serialized as a JSON string |
| `topic` | `Topic` | Serialized as a JSON string |
| `state` | `State.ToString()` | Serialized as a JSON string |
| `publishAttempts` | `PublishAttempts` | Serialized as a JSON number |
| `maxPublishAttempts` | `MaxPublishAttempts` | Serialized as a JSON number |
| `createdAt` | `CreatedAt` | Serialized by `System.Text.Json` |
| `publishedAt` | `PublishedAt` | Serialized by `System.Text.Json`; omitted when null |
| `errorMessage` | `ErrorMessage` | Omitted when null |
| `partitionKey` | `PartitionKey` | Omitted when null |
| `correlationId` | `CorrelationId` | Omitted when null |
| `causationId` | `CausationId` | Omitted when null |
| `eventData` | `EventData` | Embedded as a parsed JSON value when valid; emitted as the original string when invalid; omitted when null or empty |
| `metadata` | `Metadata` | Embedded as a parsed JSON value when valid; emitted as the original string when invalid; omitted when null or empty |

Serialization uses `System.Text.Json` with indentation enabled, camel-case property naming, and `JsonIgnoreCondition.WhenWritingNull`. A valid JSON scalar, array, or object in `EventData` or `Metadata` therefore appears as that JSON value rather than as an escaped JSON string. A valid JSON `null` value is omitted because parsing produces null. Parsing failures are caught without being exposed to the caller, and the original value is serialized as a string.

Properties not listed above, including `EventType`, `LastProcessedAt`, `ScheduledFor`, `ErrorStackTrace`, `DeliveryGuarantee`, `Headers`, `Priority`, `IsLocked`, and `LockExpiresAt`, are not exported. An empty input list produces an object with an empty `messages` array, a runtime `exportedAt` value, and `count` equal to `0`.

## Public API

### `string FormatName`

Returns the literal format identifier `"json"`.

### `string ContentType`

Returns the literal MIME type `"application/json"`.

### `string Format(List<OutboxMessage> messages)`

Returns the JSON string described above. The method does not validate individual messages or check whether the list is null.

JSON parsing is a private implementation detail; `JsonFormatter` exposes no other public members.

## Usage example

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Formatters;

var formatter = new JsonFormatter();
var messages = new List<OutboxMessage>
{
    new()
    {
        Id = Guid.Parse("d2719a32-20fe-4c4b-a456-66c5ca0f9521"),
        IdempotencyKey = "order-42-created",
        AggregateId = "42",
        AggregateType = "Order",
        EventTypeName = "OrderCreated",
        Topic = "orders.created",
        State = OutboxMessageState.Pending,
        PublishAttempts = 0,
        MaxPublishAttempts = 5,
        CreatedAt = new DateTime(2026, 9, 13, 12, 30, 0, DateTimeKind.Utc),
        EventData = "{\"orderId\":42}",
        Metadata = "{\"source\":\"checkout\"}"
    }
};

string json = formatter.Format(messages);
Console.WriteLine(json);
```

The result has this shape; `exportedAt` is generated from `DateTime.UtcNow` when `Format` runs:

```json
{
  "messages": [
    {
      "id": "d2719a32-20fe-4c4b-a456-66c5ca0f9521",
      "idempotencyKey": "order-42-created",
      "aggregateId": "42",
      "aggregateType": "Order",
      "eventTypeName": "OrderCreated",
      "topic": "orders.created",
      "state": "Pending",
      "publishAttempts": 0,
      "maxPublishAttempts": 5,
      "createdAt": "2026-09-13T12:30:00Z",
      "eventData": {
        "orderId": 42
      },
      "metadata": {
        "source": "checkout"
      }
    }
  ],
  "exportedAt": "2026-09-13T12:31:00Z",
  "count": 1
}
```
