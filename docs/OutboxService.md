# OutboxService

## Purpose
Core service for managing outbox message publishing. Handles transactional consistency and message deduplication to ensure reliable delivery of events through the outbox pattern.

## Public Methods

### PublishEventAsync(PublishableEvent publishableEvent, CancellationToken cancellationToken = default)
Publishes an event to the outbox with deduplication and transactional consistency.

**Parameters:**
- `publishableEvent` (PublishableEvent): The event to publish containing the domain event, topic, and optional settings
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<OutboxMessage>`: The published outbox message

**Exceptions:**
- `ArgumentNullException`: If publishableEvent or its Event property is null
- `ArgumentException`: If Topic is null or whitespace
- `OutboxException`: If publishing fails

### PublishEventAsync(DomainEvent domainEvent, string topic, string? partitionKey = null, CancellationToken cancellationToken = default)
Publishes a domain event to the outbox with default settings.

**Parameters:**
- `domainEvent` (DomainEvent): The domain event to publish
- `topic` (string): The topic name
- `partitionKey` (string?): Optional partition key for message routing
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<OutboxMessage>`: The published outbox message

**Exceptions:**
- `ArgumentException`: If topic is null or whitespace
- `OutboxException`: If publishing fails

### GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
Retrieves a message by its ID.

**Parameters:**
- `messageId` (Guid): The message ID to retrieve
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<OutboxMessage?>`: The message if found, otherwise null

**Exceptions:**
- `OutboxException`: If retrieval fails

### GetStatisticsAsync(CancellationToken cancellationToken = default)
Gets outbox statistics.

**Parameters:**
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<OutboxStatistics>`: The outbox statistics

**Exceptions:**
- `OutboxException`: If statistics retrieval fails

### RetryFailedMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
Retries a failed message by resetting its state to pending.

**Parameters:**
- `messageId` (Guid): The message ID to retry
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<bool>`: True if the message was successfully queued for retry, false otherwise

**Exceptions:**
- `OutboxMessageNotFoundException`: If message with given ID is not found
- `OutboxException`: If retry operation fails

### ArchiveOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default)
Archives messages older than the specified date.

**Parameters:**
- `olderThan` (DateTime): The date threshold for archiving (messages older than this date will be archived)
- `cancellationToken` (CancellationToken): Optional cancellation token

**Exceptions:**
- `ArgumentException`: If olderThan is DateTime.MinValue/default or in the future
- `OutboxException`: If archiving fails

### GetAllMessagesAsync(CancellationToken cancellationToken = default)
Retrieves up to 1,000 outbox messages to prevent unbounded queries.

**Parameters:**
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<List<OutboxMessage>>`: A list of outbox messages (limited to 1000)

**Exceptions:**
- `OutboxException`: If retrieval fails

### GetMessagesByTopicAsync(string topic, CancellationToken cancellationToken = default)
Retrieves all messages published to a specific topic.

**Parameters:**
- `topic` (string): The topic name
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<List<OutboxMessage>>`: A list of outbox messages for the specified topic

**Exceptions:**
- `OutboxException`: If retrieval fails

### GetMessagesByAggregateAsync(string aggregateId, CancellationToken cancellationToken = default)
Retrieves all messages for a specific aggregate.

**Parameters:**
- `aggregateId` (string): The aggregate ID
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<List<OutboxMessage>>`: A list of outbox messages for the specified aggregate

**Exceptions:**
- `OutboxException`: If retrieval fails

### GetMessagesByStateAsync(OutboxMessageState state, CancellationToken cancellationToken = default)
Retrieves all messages with a specific processing state.

**Parameters:**
- `state` (OutboxMessageState): The message state to filter by
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<List<OutboxMessage>>`: A list of outbox messages with the specified state

**Exceptions:**
- `OutboxException`: If retrieval fails

### GetMessagesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
Retrieves all messages created within the specified date range.

**Parameters:**
- `startDate` (DateTime): The start of the date range
- `endDate` (DateTime): The end of the date range
- `cancellationToken` (CancellationToken): Optional cancellation token

**Returns:**
- `Task<List<OutboxMessage>>`: A list of outbox messages created within the date range

**Exceptions:**
- `OutboxException`: If retrieval fails

## Usage Example

```csharp
// Using dependency injection
public class OrderService
{
    private readonly IOutboxService _outboxService;
    
    public OrderService(IOutboxService outboxService)
    {
        _outboxService = outboxService;
    }
    
    public async Task PlaceOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Process order...
        
        // Publish domain event via outbox
        var orderCreatedEvent = new EntityCreatedEvent
        {
            EntityId = order.Id.ToString(),
            // ... other event properties
        };
        
        await _outboxService.PublishEventAsync(
            orderCreatedEvent,
            "order-events",
            partitionKey: order.Id.ToString(),
            cancellationToken: cancellationToken);
    }
}

// Direct usage example
var outboxService = new OutboxService(
    repository,
    logger,
    serializer);

var userRegisteredEvent = new EntityCreatedEvent
{
    EntityId = user.Id.ToString(),
    Email = user.Email,
    Username = user.Username
};

var message = await outboxService.PublishEventAsync(
    userRegisteredEvent,
    "user-events",
    partitionKey: user.Id.ToString());

// Retrieve statistics
var stats = await outboxService.GetStatisticsAsync();

// Get all pending messages
var pendingMessages = await outboxService.GetMessagesByStateAsync(OutboxMessageState.Pending);

// Retry a failed message
await outboxService.RetryFailedMessageAsync(failedMessageId);

// Archive old messages
await outboxService.ArchiveOldMessagesAsync(DateTime.UtcNow.AddDays(-30));
```

## Implementation Details

The service ensures:
- **Idempotency**: Prevents duplicate message publishing using idempotency keys
- **Transactional Consistency**: Uses repository pattern for atomic operations
- **Distributed Tracing**: Captures W3C trace-context for observability
- **Error Handling**: Proper exception handling with logging and wrapping
- **Flexible Configuration**: Supports various delivery guarantees and retry policies

Dependencies:
- `IOutboxRepository`: For data persistence operations
- `ILogger<OutboxService>`: For structured logging
- `IOutboxSerializer`: For event serialization
- `TimeProvider`: For time abstraction (testability)