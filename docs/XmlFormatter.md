# XmlFormatter

`XmlFormatter` converts a list of `OutboxMessage` instances into an indented XML document for export to enterprise or legacy integrations. It implements `IDataFormatter`, uses the format name `xml`, and advertises the `application/xml` content type.

## Output format

The document has an `OutboxMessages` root element with two attributes:

- `count` is the number of messages in the input list.
- `exportedAt` is the UTC time at which `Format` creates the document, formatted with the round-trip (`"o"`) format string.

Each input message produces one `Message` element, in input order. Its child elements are emitted in this order:

| Element | `OutboxMessage` value | Formatting and presence |
| --- | --- | --- |
| `Id` | `Id` | Always emitted |
| `IdempotencyKey` | `IdempotencyKey` | Always emitted |
| `AggregateId` | `AggregateId` | Always emitted |
| `AggregateType` | `AggregateType` | Always emitted |
| `EventType` | `EventTypeName` | Always emitted |
| `Topic` | `Topic` | Always emitted |
| `State` | `State.ToString()` | Always emitted |
| `PublishAttempts` | `PublishAttempts` | Always emitted |
| `MaxPublishAttempts` | `MaxPublishAttempts` | Always emitted |
| `CreatedAt` | `CreatedAt` | Always emitted using the round-trip (`"o"`) format |
| `PublishedAt` | `PublishedAt` | Emitted only when it has a value, using the round-trip (`"o"`) format |
| `ErrorMessage` | `ErrorMessage` | Emitted only when non-null and non-empty |
| `PartitionKey` | `PartitionKey` | Emitted only when non-null and non-empty |
| `CorrelationId` | `CorrelationId` | Emitted only when non-null and non-empty |
| `EventData` | `EventData` | Emitted only when non-null and non-empty; stored as XML text, not parsed as XML or JSON |

XML-sensitive characters in element values are escaped by `XElement`. Properties not listed above, including `LastProcessedAt`, `ScheduledFor`, `ErrorStackTrace`, `DeliveryGuarantee`, `CausationId`, `Metadata`, `Headers`, `Priority`, `IsLocked`, and `LockExpiresAt`, are not exported.

The output is indented with two spaces. It begins with an XML declaration whose encoding is `utf-16`: although the writer settings specify UTF-8, the implementation writes to a `StringWriter`, which reports UTF-16. An empty input list produces an empty `OutboxMessages` root with `count="0"` and a runtime `exportedAt` value.

## Public API

### `string FormatName`

Returns the literal format identifier `"xml"`.

### `string ContentType`

Returns the literal MIME type `"application/xml"`.

### `string Format(List<OutboxMessage> messages)`

Returns the XML document described above. The method does not validate the messages or check whether the list is null.

Formatting an individual message is a private implementation detail; `XmlFormatter` exposes no other public members.

## Usage example

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Formatters;

var formatter = new XmlFormatter();
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
        EventData = "{\"orderId\":42}"
    }
};

string xml = formatter.Format(messages);
Console.WriteLine(xml);
```

The result has this shape; the `exportedAt` value is generated from `DateTime.UtcNow` when `Format` runs:

```xml
<?xml version="1.0" encoding="utf-16"?>
<OutboxMessages count="1" exportedAt="2026-09-13T12:31:00.0000000Z">
  <Message>
    <Id>d2719a32-20fe-4c4b-a456-66c5ca0f9521</Id>
    <IdempotencyKey>order-42-created</IdempotencyKey>
    <AggregateId>42</AggregateId>
    <AggregateType>Order</AggregateType>
    <EventType>OrderCreated</EventType>
    <Topic>orders.created</Topic>
    <State>Pending</State>
    <PublishAttempts>0</PublishAttempts>
    <MaxPublishAttempts>5</MaxPublishAttempts>
    <CreatedAt>2026-09-13T12:30:00.0000000Z</CreatedAt>
    <EventData>{"orderId":42}</EventData>
  </Message>
</OutboxMessages>
```
