# DeadLetterRepository

## Purpose
Manages dead letter messages in the outbox pattern implementation. Dead letters are messages that failed processing after multiple retry attempts and have been moved to a dead letter queue for manual inspection and handling.

## Public Methods

### `Task<DeadLetter> AddAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default)`
Adds a new dead letter record to the database.

**Parameters:**
- `deadLetter`: The dead letter entity to add
- `cancellationToken`: Optional cancellation token

**Returns:** The added dead letter entity

**Exceptions:**
- `ArgumentNullException`: If `deadLetter` is null
- `DeadLetterException`: If database update fails

### `Task<DeadLetter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)`
Retrieves a dead letter by its unique identifier.

**Parameters:**
- `id`: The unique identifier of the dead letter
- `cancellationToken`: Optional cancellation token

**Returns:** The dead letter entity if found, otherwise null

**Exceptions:**
- `DeadLetterException`: If database query fails

### `Task<DeadLetter?> GetByOutboxMessageIdAsync(Guid outboxMessageId, CancellationToken cancellationToken = default)`
Retrieves a dead letter by its associated outbox message ID.

**Parameters:**
- `outboxMessageId`: The outbox message identifier
- `cancellationToken`: Optional cancellation token

**Returns:** The dead letter entity if found, otherwise null

**Exceptions:**
- `DeadLetterException`: If database query fails

### `Task<List<DeadLetter>> GetUnreviewedAsync(int limit = 100, CancellationToken cancellationToken = default)`
Retrieves unreviewed dead letters that require operator attention, ordered by the time they were moved to the dead letter queue (oldest first).

**Parameters:**
- `limit`: Maximum number of dead letters to return (default: 100)
- `cancellationToken`: Optional cancellation token

**Returns:** List of unreviewed dead letter entities

**Exceptions:**
- `DeadLetterException`: If database query fails

### `Task<List<DeadLetter>> GetAllAsync(int skip = 0, int limit = 1000, CancellationToken cancellationToken = default)`
Retrieves all dead letters with pagination, ordered by the time they were moved to the dead letter queue (newest first).

**Parameters:**
- `skip`: Number of dead letters to skip
- `limit`: Maximum number of dead letters to return (default: 1000)
- `cancellationToken`: Optional cancellation token

**Returns:** List of dead letter entities

**Exceptions:**
- `DeadLetterException`: If database query fails

### `Task<List<DeadLetter>> GetByTopicAsync(string topic, CancellationToken cancellationToken = default)`
Retrieves dead letters for a specific topic, ordered by the time they were moved to the dead letter queue (newest first).

**Parameters:**
- `topic`: The topic name
- `cancellationToken`: Optional cancellation token

**Returns:** List of dead letter entities for the specified topic

**Exceptions:**
- `ArgumentNullException`: If `topic` is null
- `DeadLetterException`: If database query fails

### `Task<List<DeadLetter>> GetByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)`
Retrieves dead letters for a specific aggregate identifier, ordered by the time they were moved to the dead letter queue (oldest first).

**Parameters:**
- `aggregateId`: The aggregate identifier
- `cancellationToken`: Optional cancellation token

**Returns:** List of dead letter entities for the specified aggregate

**Exceptions:**
- `ArgumentNullException`: If `aggregateId` is null
- `DeadLetterException`: If database query fails

### `Task UpdateAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default)`
Updates an existing dead letter record.

**Parameters:**
- `deadLetter`: The dead letter entity to update
- `cancellationToken`: Optional cancellation token

**Exceptions:**
- `ArgumentNullException`: If `deadLetter` is null
- `DeadLetterException`: If database update fails (including concurrency conflicts)

### `Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)`
Deletes a dead letter record by its identifier.

**Parameters:**
- `id`: The unique identifier of the dead letter to delete
- `cancellationToken`: Optional cancellation token

**Exceptions:**
- `DeadLetterException`: If database query or delete operation fails

### `Task<int> GetCountAsync(CancellationToken cancellationToken = default)`
Gets the total count of dead letters in the database.

**Parameters:**
- `cancellationToken`: Optional cancellation token

**Returns:** Total number of dead letter records

**Exceptions:**
- `DeadLetterException`: If database query fails

### `Task<int> GetUnreviewedCountAsync(CancellationToken cancellationToken = default)`
Gets the count of unreviewed dead letters.

**Parameters:**
- `cancellationToken`: Optional cancellation token

**Returns:** Number of unreviewed dead letter records

**Exceptions:**
- `DeadLetterException`: If database query fails

### `Task<int> GetRequeuedCountAsync(CancellationToken cancellationToken = default)`
Gets the count of dead letters that have been requeued for retry.

**Parameters:**
- `cancellationToken`: Optional cancellation token

**Returns:** Number of dead letter records marked as requeued

**Exceptions:**
- `DeadLetterException`: If database query fails

## Query Behavior
- All read operations use `AsNoTracking()` for performance optimization
- Query methods order results by `MovedToDlqAt` timestamp:
  - `GetUnreviewedAsync`: Oldest first (ascending)
  - `GetAllAsync`: Newest first (descending)
  - `GetByTopicAsync`: Newest first (descending)
  - `GetByAggregateIdAsync`: Oldest first (ascending)
- Methods that return collections support filtering by topic or aggregate ID
- Pagination is supported via `skip` and `limit` parameters in `GetAllAsync`

## Usage Example
```csharp
// Assuming dependency injection of IDeadLetterRepository
public class DeadLetterHandler
{
    private readonly IDeadLetterRepository _deadLetterRepository;
    
    public DeadLetterHandler(IDeadLetterRepository deadLetterRepository)
    {
        _deadLetterRepository = deadLetterRepository;
    }
    
    public async Task ProcessUnreviewedDeadLetters()
    {
        // Get unreviewed dead letters (default limit of 100)
        var unreviewed = await _deadLetterRepository.GetUnreviewedAsync();
        
        foreach (var deadLetter in unreviewed)
        {
            // Process dead letter (e.g., manual inspection, fix and requeue)
            // ...
            
            // Mark as reviewed after processing
            deadLetter.IsReviewed = true;
            await _deadLetterRepository.UpdateAsync(deadLetter);
        }
    }
    
    public async Task<DeadLetter?> FindDeadLetterByOutboxMessage(Guid outboxMessageId)
    {
        return await _deadLetterRepository.GetByOutboxMessageIdAsync(outboxMessageId);
    }
    
    public async Task<int> GetUnreviewedCount()
    {
        return await _deadLetterRepository.GetUnreviewedCountAsync();
    }
}
```