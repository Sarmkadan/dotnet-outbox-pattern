# IDeadLetterRepository

Provides an abstraction for persisting and retrieving dead-letter messages that failed processing in the outbox pattern. Implementations typically store failed messages along with metadata such as the original outbox message ID, topic, aggregate ID, and error details to enable later inspection, reprocessing, or archival.

## API

### `Task<DeadLetter> AddAsync(DeadLetter deadLetter)`

Persists a new dead-letter record.
- **Parameters**:
  - `deadLetter`: The dead-letter entity to store. Must not be null.
- **Return value**: The persisted `DeadLetter` instance, typically with generated identifiers populated.
- **Exceptions**: Throws `ArgumentNullException` if `deadLetter` is null.

### `Task<DeadLetter?> GetByIdAsync(Guid id)`

Retrieves a dead-letter by its unique identifier.
- **Parameters**:
  - `id`: The identifier of the dead-letter to fetch.
- **Return value**: The `DeadLetter` instance if found; otherwise `null`.
- **Exceptions**: None.

### `Task<DeadLetter?> GetByOutboxMessageIdAsync(string outboxMessageId)`

Retrieves a dead-letter by the original outbox message identifier.
- **Parameters**:
  - `outboxMessageId`: The outbox message ID to match. Must not be null or empty.
- **Return value**: The matching `DeadLetter` if found; otherwise `null`.
- **Exceptions**: Throws `ArgumentException` if `outboxMessageId` is null or whitespace.

### `Task<List<DeadLetter>> GetUnreviewedAsync()`

Retrieves all dead-letter messages that have not yet been reviewed.
- **Parameters**: None.
- **Return value**: A list of unreviewed `DeadLetter` instances. Never null; may be empty.
- **Exceptions**: None.

### `Task<List<DeadLetter>> GetAllAsync()`

Retrieves all dead-letter messages regardless of review status.
- **Parameters**: None.
- **Return value**: A list of all `DeadLetter` instances. Never null; may be empty.
- **Exceptions**: None.

### `Task<List<DeadLetter>> GetByTopicAsync(string topic)`

Retrieves dead-letter messages filtered by the message topic.
- **Parameters**:
  - `topic`: The topic to filter by. Must not be null or empty.
- **Return value**: A list of matching `DeadLetter` instances. Never null; may be empty.
- **Exceptions**: Throws `ArgumentException` if `topic` is null or whitespace.

### `Task<List<DeadLetter>> GetByAggregateIdAsync(string aggregateId)`

Retrieves dead-letter messages filtered by the aggregate identifier.
- **Parameters**:
  - `aggregateId`: The aggregate ID to filter by. Must not be null or empty.
- **Return value**: A list of matching `DeadLetter` instances. Never null; may be empty.
- **Exceptions**: Throws `ArgumentException` if `aggregateId` is null or whitespace.

### `Task UpdateAsync(DeadLetter deadLetter)`

Updates an existing dead-letter record.
- **Parameters**:
  - `deadLetter`: The updated `DeadLetter` instance. Must not be null.
- **Return value**: None.
- **Exceptions**: Throws `ArgumentNullException` if `deadLetter` is null.

### `Task DeleteAsync(Guid id)`

Removes a dead-letter record by its identifier.
- **Parameters**:
  - `id`: The identifier of the dead-letter to delete.
- **Return value**: None.
- **Exceptions**: None.

### `Task<int> GetCountAsync()`

Returns the total number of dead-letter messages stored.
- **Parameters**: None.
- **Return value**: The count of all dead-letter messages.
- **Exceptions**: None.

### `Task<int> GetUnreviewedCountAsync()`

Returns the number of unreviewed dead-letter messages.
- **Parameters**: None.
- **Return value**: The count of unreviewed dead-letter messages.
- **Exceptions**: None.

### `Task<int> GetRequeuedCountAsync()`

Returns the number of dead-letter messages that have been requeued for reprocessing.
- **Parameters**: None.
- **Return value**: The count of requeued dead-letter messages.
- **Exceptions**: None.

## Usage

### Example 1: Adding and retrieving a dead-letter
