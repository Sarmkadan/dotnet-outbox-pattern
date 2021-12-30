# DeadLetterController

Provides endpoints and properties for inspecting, managing, and acting on messages that failed processing within the outbox pattern, including reviewing, requeuing, deleting, and exporting dead-lettered messages.

## API

### `DeadLetterController`
Initializes a new controller instance with services required for dead-letter management.

### `async Task<IActionResult> GetUnreviewedAsync`
Returns a list of dead-lettered messages that have not yet been reviewed.
Returns:
- `200 OK` with a list of unreviewed messages on success.
- `500 InternalServerError` if the operation fails.

### `async Task<IActionResult> GetDeadLettersAsync`
Returns a paginated list of all dead-lettered messages.
Returns:
- `200 OK` with a list of dead-lettered messages on success.
- `500 InternalServerError` if the operation fails.

### `async Task<IActionResult> GetDeadLetterAsync`
Returns a single dead-lettered message by its unique identifier.
Parameters:
- `id` (string): The unique identifier of the dead-lettered message.
Returns:
- `200 OK` with the message details on success.
- `404 NotFound` if the message does not exist.
- `500 InternalServerError` if the operation fails.

### `async Task<IActionResult> ReviewAsync`
Marks a dead-lettered message as reviewed by its unique identifier.
Parameters:
- `id` (string): The unique identifier of the dead-lettered message.
Returns:
- `204 NoContent` on successful review.
- `404 NotFound` if the message does not exist.
- `500 InternalServerError` if the operation fails.

### `async Task<IActionResult> RequeueAsync`
Requeues a dead-lettered message back into the outbox for reprocessing by its unique identifier.
Parameters:
- `id` (string): The unique identifier of the dead-lettered message.
Returns:
- `204 NoContent` on successful requeue.
- `404 NotFound` if the message does not exist.
- `500 InternalServerError` if the operation fails.

### `async Task<IActionResult> GetStatisticsAsync`
Returns aggregated statistics about the dead-letter store, including total count, unreviewed count, reviewed count, error distribution, oldest timestamp, and last update time.
Returns:
- `200 OK` with a statistics object on success.
- `500 InternalServerError` if the operation fails.

### `async Task<IActionResult> DeleteAsync`
Deletes a dead-lettered message by its unique identifier.
Parameters:
- `id` (string): The unique identifier of the dead-lettered message.
Returns:
- `204 NoContent` on successful deletion.
- `404 NotFound` if the message does not exist.
- `500 InternalServerError` if the operation fails.

### `async Task<IActionResult> ExportAsync`
Exports dead-lettered messages matching optional filters into a structured format (e.g., JSON).
Parameters:
- `since` (DateTime?, optional): Filter messages created after this timestamp.
- `until` (DateTime?, optional): Filter messages created before this timestamp.
- `errorType` (string?, optional): Filter messages by error type.
Returns:
- `200 OK` with the exported data on success.
- `400 BadRequest` if parameters are invalid.
- `500 InternalServerError` if the operation fails.

### `TotalDeadLetters` (int)
Gets the total number of dead-lettered messages currently stored.

### `UnreviewedCount` (int)
Gets the number of dead-lettered messages that have not been reviewed.

### `ReviewedCount` (int)
Gets the number of dead-lettered messages that have been reviewed.

### `ErrorsByType` (Dictionary<string, int>)
Gets a dictionary mapping error types to their respective counts among dead-lettered messages.

### `OldestDeadLetter` (DateTime?)
Gets the timestamp of the oldest dead-lettered message, or `null` if no messages exist.

### `LastUpdated` (DateTime)
Gets the timestamp of the last update to the dead-letter store.

## Usage
