# IOutboxRepository
The `IOutboxRepository` interface is designed to manage outbox messages, providing a set of methods for adding, retrieving, updating, and deleting messages. It serves as a crucial component in the implementation of the outbox pattern, which helps to ensure reliable and fault-tolerant message processing in distributed systems.

## API
The `IOutboxRepository` interface includes the following members:
- `OutboxRepository`: The constructor for the `OutboxRepository` class.
- `AddAsync`: Adds a new outbox message to the repository. Returns the added `OutboxMessage`.
- `GetByIdAsync`: Retrieves an outbox message by its ID. Returns the `OutboxMessage` if found, or `null` if not found.
- `GetByIdempotencyKeyAsync`: Retrieves an outbox message by its idempotency key. Returns the `OutboxMessage` if found, or `null` if not found.
- `GetPendingMessagesAsync`: Retrieves all pending outbox messages. Returns a list of `OutboxMessage` objects.
- `GetPendingByPartitionAsync`: Retrieves pending outbox messages by partition. Returns a list of `OutboxMessage` objects.
- `GetScheduledMessagesAsync`: Retrieves scheduled outbox messages. Returns a list of `OutboxMessage` objects.
- `GetExpiredLocksAsync`: Retrieves expired locks for outbox messages. Returns a list of `OutboxMessage` objects.
- `UpdateAsync`: Updates an existing outbox message in the repository.
- `DeleteAsync`: Deletes an outbox message from the repository.
- `GetPendingCountAsync`: Retrieves the count of pending outbox messages. Returns the count as an integer.
- `GetPublishedCountAsync`: Retrieves the count of published outbox messages. Returns the count as an integer.
- `GetFailedCountAsync`: Retrieves the count of failed outbox messages. Returns the count as an integer.
- `GetStatisticsAsync`: Retrieves statistics about outbox messages. Returns an `OutboxStatistics` object.
- `GetByAggregateIdAsync`: Retrieves outbox messages by aggregate ID. Returns a list of `OutboxMessage` objects.
- `GetByTopicAsync`: Retrieves outbox messages by topic. Returns a list of `OutboxMessage` objects.
- `GetByCorrelationIdAsync`: Retrieves outbox messages by correlation ID. Returns a list of `OutboxMessage` objects.
- `GetByStateAsync`: Retrieves outbox messages by state. Returns a list of `OutboxMessage` objects.
- `GetByDateRangeAsync`: Retrieves outbox messages within a specified date range. Returns a list of `OutboxMessage` objects.
- `ArchiveOldMessagesAsync`: Archives old outbox messages.

## Usage
Here are two examples of using the `IOutboxRepository` interface:
```csharp
// Example 1: Adding a new outbox message
var repository = new OutboxRepository();
var message = new OutboxMessage { /* initialize message properties */ };
var addedMessage = await repository.AddAsync(message);

// Example 2: Retrieving pending outbox messages
var pendingMessages = await repository.GetPendingMessagesAsync();
foreach (var message in pendingMessages)
{
    // Process the pending message
}
```

## Notes
When using the `IOutboxRepository` interface, consider the following:
- The `AddAsync` method may throw an exception if the message cannot be added to the repository.
- The `GetByIdAsync` and `GetByIdempotencyKeyAsync` methods may return `null` if the message is not found.
- The `GetPendingMessagesAsync` and other retrieval methods may return an empty list if no messages match the specified criteria.
- The `UpdateAsync` and `DeleteAsync` methods may throw exceptions if the message cannot be updated or deleted.
- The `GetStatisticsAsync` method may return an `OutboxStatistics` object with default values if no statistics are available.
- The `IOutboxRepository` interface is designed to be thread-safe, allowing concurrent access to the repository. However, it is still important to follow proper synchronization and locking mechanisms when accessing and modifying outbox messages to ensure data consistency.
