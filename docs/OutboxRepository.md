# OutboxRepository

## Purpose
The `OutboxRepository` class is responsible for managing outbox messages in the database. It provides methods to add, retrieve, update, delete, and query outbox messages, as well as specialized methods for claiming messages for processing with row-level locking to prevent multiple instances from processing the same messages.

## Public Methods

### Interface Methods (`IOutboxRepository`)

| Method | Description |
|--------|-------------|
| `Task<OutboxMessage> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)` | Adds a new outbox message to the database after validation. |
| `Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)` | Retrieves a message by its ID. |
| `Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)` | Retrieves a message by its idempotency key for deduplication. |
| `Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)` | Retrieves pending messages that should be published, ordered by priority and creation time. |
| `Task<List<OutboxMessage>> GetPendingByPartitionAsync(string partitionKey, int batchSize, CancellationToken cancellationToken = default)` | Retrieves pending messages for a specific partition to maintain ordering. |
| `Task<List<OutboxMessage>> GetScheduledMessagesAsync(int batchSize, CancellationToken cancellationToken = default)` | Retrieves messages scheduled for future delivery. |
| `Task<List<OutboxMessage>> GetExpiredLocksAsync(CancellationToken cancellationToken = default)` | Retrieves messages with expired processing locks for recovery. |
| `Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)` | Updates an existing outbox message. |
| `Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)` | Deletes an outbox message. |
| `Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)` | Gets the count of pending messages. |
| `Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)` | Gets the count of published messages. |
| `Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default)` | Gets the count of failed messages. |
| `Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)` | Gets comprehensive statistics about the outbox. |
| `Task<List<OutboxMessage>> GetByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)` | Retrieves all messages for a specific aggregate. |
| `Task<List<OutboxMessage>> GetByTopicAsync(string topic, int limit = 1000, CancellationToken cancellationToken = default)` | Retrieves all messages for a specific topic. |
| `Task<List<OutboxMessage>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)` | Retrieves all messages with a specific correlation ID. |
| `Task<List<OutboxMessage>> GetByStateAsync(OutboxMessageState state, CancellationToken cancellationToken = default)` | Retrieves all messages with a specific processing state. |
| `Task<List<OutboxMessage>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)` | Retrieves all messages created within the specified date range. |
| `Task ArchiveOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default)` | Archives old published messages. |
| `Task<int> DeleteArchivedMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default)` | Deletes archived messages older than specified date. |
| `Task<List<OutboxMessage>> GetAllAsync(int limit = 10000, CancellationToken cancellationToken = default)` | Retrieves all messages (limited). |
| `Task<DateTime?> GetOldestPendingMessageCreatedAtAsync(CancellationToken cancellationToken = default)` | Returns the creation timestamp of the oldest pending (unprocessed) message, or null if there are no pending messages. |
| `Task<List<OutboxMessage>> ClaimPendingMessagesBatchAsync(int batchSize, int lockDurationSeconds, CancellationToken cancellationToken = default)` | Claims a batch of pending messages with row-level locking for competing consumers. Uses SQL Server's UPDLOCK/ROWLOCK/READPAST to atomically claim messages and prevent multiple instances from processing the same messages. |
| `Task<List<OutboxMessage>> ClaimPendingMessagesByPartitionBatchAsync(string partitionKey, int batchSize, int lockDurationSeconds, CancellationToken cancellationToken = default)` | Claims pending messages for a specific partition with row-level locking. |
| `Task<List<OutboxMessage>> ClaimScheduledMessagesBatchAsync(int batchSize, int lockDurationSeconds, CancellationToken cancellationToken = default)` | Claims scheduled messages that are due for processing with row-level locking. |

### Implementation Details
The `OutboxRepository` class implements `IOutboxRepository` using Entity Framework Core. It depends on an `OutboxDbContext` and an `ILogger<OutboxRepository>`.

## Query Behavior
- Most query methods use `AsNoTracking()` to improve performance since the entities are typically used for read-only operations.
- Methods that retrieve lists of messages typically order results by relevant criteria (e.g., priority, creation time, scheduled time).
- Methods that return counts use `CountAsync()` for efficiency.
- The repository includes logging for read operations via the `LogRead` helper method.
- Exception handling wraps database operations in try/catch blocks and throws domain-specific exceptions (`OutboxRepositoryException`, `InvalidMessageException`) on failure.

## Usage Example

```csharp
// Assuming you have an OutboxDbContext instance
var dbContext = new OutboxDbContext(options);
var repository = new OutboxRepository(dbContext);

// Add a new outbox message
var message = new OutboxMessage
{
    Id = Guid.NewGuid(),
    Topic = "user.created",
    AggregateId = userId.ToString(),
    Payload = JsonSerializer.Serialize(new { UserId = userId, Email = user.Email }),
    CreatedAt = DateTime.UtcNow
};
await repository.AddAsync(message);

// Retrieve pending messages for processing
var pendingMessages = await repository.GetPendingMessagesAsync(batchSize: 10);

// Claim messages for processing (prevents concurrent processing)
var claimedMessages = await repository.ClaimPendingMessagesBatchAsync(
    batchSize: 10,
    lockDurationSeconds: 30);

// Process each claimed message
foreach (var msg in claimedMessages)
{
    try
    {
        // Process the message (e.g., publish to message broker)
        await ProcessMessageAsync(msg);
        
        // Mark as published
        msg.State = OutboxMessageState.Published;
        msg.PublishedAt = DateTime.UtcNow;
        await repository.UpdateAsync(msg);
    }
    catch (Exception ex)
    {
        // Mark as failed
        msg.State = OutboxMessageState.Failed;
        msg.Error = ex.Message;
        await repository.UpdateAsync(msg);
    }
}

// Get statistics
var stats = await repository.GetStatisticsAsync();
Console.WriteLine($"Pending messages: {stats.PendingMessages}");
```

## Notes
- The repository uses raw SQL with locking hints (UPDLOCK, ROWLOCK, READPAST) for the claim methods to ensure atomic message claiming in competitive consumer scenarios.
- All methods accept an optional `CancellationToken` for graceful cancellation.
- The repository validates messages before adding them to the database.