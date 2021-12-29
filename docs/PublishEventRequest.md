# PublishEventRequest
The `PublishEventRequest` type is used to encapsulate the data and metadata required to publish an event, allowing for flexible and customizable event publishing. It provides properties to specify the aggregate identifier, event type, topic, and other relevant details, making it a versatile and essential component in event-driven systems.

## API
The `PublishEventRequest` type exposes the following public members:
* `AggregateId`: A string representing the identifier of the aggregate that the event belongs to.
* `AggregateType`: A string indicating the type of the aggregate.
* `EventType`: A string specifying the type of the event being published.
* `EventData`: A dictionary containing the event data, where the key is a string and the value is an object.
* `Topic`: A string representing the topic that the event is being published to.
* `PartitionKey`: An optional string used for partitioning the event.
* `CorrelationId`: An optional string used for correlating the event with other events or requests.
* `IdempotencyKey`: An optional string used to ensure idempotency of the event publication.
* `Url`: A string representing the URL where the event will be published.
* `Events`: A list of strings representing the events being published.
* `Headers`: An optional dictionary containing headers for the event publication, where the key and value are strings.
* `IsActive`: A boolean indicating whether the event publication is active.
* `DaysOld`: An integer representing the number of days old the event is.
* `MaxMessages`: An optional integer representing the maximum number of messages to be published.
* `DryRun`: A boolean indicating whether the event publication should be performed as a dry run.
* `MessageIds`: A list of GUIDs representing the identifiers of the messages being published.

## Usage
Here are two examples of using the `PublishEventRequest` type:
```csharp
// Example 1: Creating a PublishEventRequest instance
var request = new PublishEventRequest
{
    AggregateId = "aggregate-123",
    AggregateType = "Order",
    EventType = "OrderCreated",
    EventData = new Dictionary<string, object> { { "OrderId", 123 } },
    Topic = "orders",
    PartitionKey = "order-123",
    CorrelationId = "correlation-123",
    IdempotencyKey = "idempotency-123",
    Url = "https://example.com/orders",
    Events = new List<string> { "OrderCreated" },
    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
    IsActive = true,
    DaysOld = 0,
    MaxMessages = 10,
    DryRun = false,
    MessageIds = new List<Guid> { Guid.NewGuid() }
};

// Example 2: Publishing an event using the PublishEventRequest instance
var publishEventRequest = new PublishEventRequest
{
    AggregateId = "aggregate-456",
    AggregateType = "Product",
    EventType = "ProductUpdated",
    EventData = new Dictionary<string, object> { { "ProductId", 456 } },
    Topic = "products",
    PartitionKey = "product-456",
    CorrelationId = "correlation-456",
    IdempotencyKey = "idempotency-456",
    Url = "https://example.com/products",
    Events = new List<string> { "ProductUpdated" },
    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
    IsActive = true,
    DaysOld = 0,
    MaxMessages = 10,
    DryRun = false,
    MessageIds = new List<Guid> { Guid.NewGuid() }
};
```

## Notes
When using the `PublishEventRequest` type, consider the following edge cases and thread-safety remarks:
* The `EventData` dictionary can contain any type of object, but it is recommended to use serializable objects to ensure proper serialization and deserialization.
* The `PartitionKey` and `CorrelationId` properties are optional, but they can be crucial for ensuring proper event partitioning and correlation.
* The `IdempotencyKey` property is optional, but it can be used to ensure idempotency of the event publication.
* The `DryRun` property can be used to perform a dry run of the event publication, which can be useful for testing and debugging purposes.
* The `MessageIds` list can contain multiple GUIDs, which can be used to identify multiple messages being published.
* The `PublishEventRequest` type is not thread-safe by default, so it is recommended to use synchronization mechanisms, such as locks or concurrent collections, when accessing and modifying instances of this type in a multi-threaded environment.
