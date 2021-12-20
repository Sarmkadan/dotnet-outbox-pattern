# IOutboxService

The `IOutboxService` interface defines the contract for interacting with the outbox persistence layer in the *dotnet-outbox-pattern* library. It provides methods to publish events, query outbox entries, manage retries, and perform maintenance operations such as archiving old messages.

## API

### `OutboxService { get; }`
Provides access to the concrete `OutboxService` implementation that backs this interface. This property is useful when direct access to the underlying service is required (e.g., for advanced configuration). It never returns `null`.

### `Task<OutboxMessage> PublishEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)`
Publishes a new event to the outbox.  
- **Parameters**  
  - `@event`: The domain event to be persisted and later dispatched. Must not be `null`.  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - An `OutboxMessage` representing the persisted entry, including its identifier and initial state.  
- **Exceptions**  
  - `ArgumentNullException` if `@event` is `null`.  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task<OutboxMessage> PublishEventAsync<TEvent>(TEvent @event, string topic, CancellationToken cancellationToken = default)`
Publishes a new event to the outbox and associates it with a specific topic.  
- **Parameters**  
  - `@event`: The domain event to be persisted. Must not be `null`.  
  - `topic`: The logical topic or routing key under which the event should be published. Must not be `null` or whitespace.  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - An `OutboxMessage` representing the persisted entry, including its identifier, topic, and initial state.  
- **Exceptions**  
  - `ArgumentNullException` if `@event` or `topic` is `null`.  
  - `ArgumentException` if `topic` is empty or consists only of whitespace.  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task<OutboxMessage?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)`
Retrieves a single outbox message by its unique identifier.  
- **Parameters**  
  - `messageId`: The `Guid` of the message to fetch.  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - The `OutboxMessage` if found; otherwise `null`.  
- **Exceptions**  
  - `ArgumentException` if `messageId` is `Guid.Empty`.  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)`
Returns aggregate statistics about the outbox store.  
- **Parameters**  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - An `OutboxStatistics` instance containing counts of messages by state, age, and other metrics.  
- **Exceptions**  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task<bool> RetryFailedMessageAsync(Guid messageId, CancellationToken cancellationToken = default)`
Attempts to retry a single failed outbox message.  
- **Parameters**  
  - `messageId`: The `Guid` of the failed message to retry.  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - `true` if the message was successfully retried and moved to a retry‑eligible state; `false` if the message was not found or is not in a failed state.  
- **Exceptions**  
  - `ArgumentException` if `messageId` is `Guid.Empty`.  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task<List<OutboxMessage>> GetAllMessagesAsync(CancellationToken cancellationToken = default)`
Retrieves every outbox message currently stored.  
- **Parameters**  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - A list containing all `OutboxMessage` entries. The list may be empty but never `null`.  
- **Exceptions**  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task<List<OutboxMessage>> GetMessagesByTopicAsync(string topic, CancellationToken cancellationToken = default)`
Retrieves all outbox messages associated with a specific topic.  
- **Parameters**  
  - `topic`: The topic to filter by. Must not be `null` or whitespace.  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - A list of `OutboxMessage` entries matching the topic. The list may be empty but never `null`.  
- **Exceptions**  
  - `ArgumentNullException` if `topic` is `null`.  
  - `ArgumentException` if `topic` is empty or consists only of whitespace.  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task<List<OutboxMessage>> GetMessagesByAggregateAsync(Guid aggregateId, CancellationToken cancellationToken = default)`
Retrieves all outbox messages related to a particular aggregate root.  
- **Parameters**  
  - `aggregateId`: The `Guid` identifying the aggregate. Must not be `Guid.Empty`.  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - A list of `OutboxMessage` entries for the aggregate. The list may be empty but never `null`.  
- **Exceptions**  
  - `ArgumentException` if `aggregateId` is `Guid.Empty`.  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task<List<OutboxMessage>> GetMessagesByStateAsync(OutboxMessageState state, CancellationToken cancellationToken = default)`
Retrieves all outbox messages that are in a given state.  
- **Parameters**  
  - `state`: The `OutboxMessageState` to filter by (e.g., `Pending`, `Published`, `Failed`).  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - A list of `OutboxMessage` entries matching the state. The list may be empty but never `null`.  
- **Exceptions**  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task<List<OutboxMessage>> GetMessagesByDateRangeAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)`
Retrieves all outbox messages whose creation timestamp falls within the specified range (inclusive of start, exclusive of end).  
- **Parameters**  
  - `start`: The lower bound of the date range. Must be earlier than `end`.  
  - `end`: The upper bound of the date range. Must be later than `start`.  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - A list of `OutboxMessage` entries created within the range. The list may be empty but never `null`.  
- **Exceptions**  
  - `ArgumentException` if `start` is not earlier than `end`.  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

### `Task ArchiveOldMessagesAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)`
Permanently removes outbox messages that were created before the supplied cutoff date.  
- **Parameters**  
  - `olderThan`: Messages with a `CreatedOn` value earlier than this point are eligible for archiving.  
  - `cancellationToken`: Optional token to observe while waiting for the operation to complete.  
- **Return Value**  
  - Completes when the archive operation has finished. No result is returned.  
- **Exceptions**  
  - `InvalidOperationException` if the outbox store is unavailable or mis‑configured.  
  - `OperationCanceledException` if the operation is cancelled via `cancellationToken`.

## Usage

```csharp
// Example 1: Publishing an event and later retrieving it for processing
public class OrderService
{
    private readonly IOutboxService _outbox;

    public OrderService(IOutboxService outbox) => _outbox = outbox;

    public async Task PlaceOrderAsync(Order order, CancellationToken ct)
    {
        // Persist the event in the outbox
        var outboxMsg = await _outbox.PublishEventAsync(
            new OrderPlacedEvent(order.Id, order.CustomerId, order.Total), ct);

        // Later, a background worker can fetch pending messages
        var pending = await _outbox.GetMessagesByStateAsync(
            OutboxMessageState.Pending, ct);

        foreach (var msg in pending)
        {
            // Dispatch the event to the message broker …
            await _outbox.RetryFailedMessageAsync(msg.Id, ct); // mark as processed
        }
    }
}
```

```csharp
// Example 2: Maintenance routine that archives stale messages and reports statistics
public class OutboxMaintenanceJob
{
    private readonly IOutboxService _outbox;
    private readonly ILogger<OutboxMaintenanceJob> _log;

    public OutboxMaintenanceJob(IOutboxService outbox, ILogger<OutboxMaintenanceJob> log)
    {
        _outbox = outbox;
        _log = log;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        // Remove messages older than 30 days
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        await _outbox.ArchiveOldMessagesAsync(cutoff, ct);
        _log.Information("Archived outbox messages older than {Cutoff}", cutoff);

        // Log current statistics for observability
        var stats = await _outbox.GetStatisticsAsync(ct);
        _log.Information("Outbox stats: Pending={Pending}, Published={Published}, Failed={Failed}",
            stats.Pending, stats.Published, stats.Failed);
    }
}
```

## Notes

- All methods are designed to be safe for concurrent invocation; implementations should handle internal synchronization if needed.  
- The service does **not** guarantee ordering of messages across different topics or aggregates; ordering guarantees, if required, must be enforced by the caller or the underlying message broker.  
- Methods that return collections never return `null`; they return an empty list when no matching records exist.  
- Passing `Guid.Empty`, `null`, or whitespace‑only strings for identifier or topic parameters will result in an `ArgumentException` or `ArgumentNullException`.  
- Cancellation tokens are respected; if cancellation is requested, the operation will cease as soon as feasible and throw `OperationCanceledException`.  
- The `ArchiveOldMessagesAsync` method performs a hard delete; archived messages cannot be recovered through the outbox API.  
- Implementations may throw `InvalidOperationException` when the underlying storage (e.g., a database table) is inaccessible, mis‑configured, or locked by another process.  
- The `PublishEventAsync` overloads differ only by the optional `topic` parameter; both persist the event with an initial state of `Pending`.  
- Consumers should treat the returned `OutboxMessage` as immutable; mutating the instance after retrieval has no effect on the stored record.
