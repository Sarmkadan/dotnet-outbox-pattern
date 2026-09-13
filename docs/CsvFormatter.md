# CsvFormatter

`CsvFormatter` converts a list of `OutboxMessage` instances into comma-separated text for reporting and data analysis. It implements `IDataFormatter` and is registered under the format name `csv` with the `text/csv` content type.

The formatter exports selected message metadata only. In particular, it does not include `EventData` or other `OutboxMessage` properties that are absent from the column list below.

## Output format

The first line is always this fixed header:

```text
MessageId,IdempotencyKey,AggregateId,AggregateType,EventType,Topic,State,PublishAttempts,MaxAttempts,CreatedAt,PublishedAt,ErrorMessage
```

Each input message produces one row with values in the same order:

| Column | `OutboxMessage` value | Formatting |
| --- | --- | --- |
| `MessageId` | `Id` | `Guid.ToString()` |
| `IdempotencyKey` | `IdempotencyKey` | CSV-escaped |
| `AggregateId` | `AggregateId` | CSV-escaped |
| `AggregateType` | `AggregateType` | CSV-escaped |
| `EventType` | `EventTypeName` | CSV-escaped |
| `Topic` | `Topic` | CSV-escaped |
| `State` | `State.ToString()` | CSV-escaped |
| `PublishAttempts` | `PublishAttempts` | `ToString()` |
| `MaxAttempts` | `MaxPublishAttempts` | `ToString()` |
| `CreatedAt` | `CreatedAt` | Round-trip (`"o"`) date/time format |
| `PublishedAt` | `PublishedAt` | Round-trip (`"o"`) date/time format, or an empty field when null |
| `ErrorMessage` | `ErrorMessage` | CSV-escaped; null is treated as empty |

An empty or null string is emitted as `""`. A string containing a comma, double quote, or line-feed character is enclosed in double quotes, and each embedded double quote is doubled. Other strings are emitted without quotes. Rows, including the header, are terminated using `StringBuilder.AppendLine`, so the line ending is the current environment's newline. The returned text has a trailing newline. An empty input list still produces the header and its trailing newline.

## Public API

### `string FormatName`

Returns the literal format identifier `"csv"`.

### `string ContentType`

Returns the literal MIME type `"text/csv"`.

### `string Format(List<OutboxMessage> messages)`

Returns a CSV string containing the fixed header followed by one row for every message, in input order. The method does not perform validation or null checking on the `messages` list.

Row formatting and field escaping are private implementation details; `CsvFormatter` exposes no other public methods.

## Usage example

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Formatters;

var formatter = new CsvFormatter();
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
        PublishedAt = null,
        ErrorMessage = null,
        EventData = "{}"
    }
};

string csv = formatter.Format(messages);
Console.Write(csv);
```

This writes:

```text
MessageId,IdempotencyKey,AggregateId,AggregateType,EventType,Topic,State,PublishAttempts,MaxAttempts,CreatedAt,PublishedAt,ErrorMessage
d2719a32-20fe-4c4b-a456-66c5ca0f9521,order-42-created,42,Order,OrderCreated,orders.created,Pending,0,5,2026-09-13T12:30:00.0000000Z,,""
```
