# IMessageSearchService

Provides methods to query and retrieve outbox messages from the database, supporting pagination, filtering by topic, aggregate ID, error conditions, and time ranges.

## API

### `SearchAsync`

Searches for outbox messages with pagination support.

- **Parameters**
  - `request` (`MessageSearchRequest`) – Criteria for filtering messages.
- **Returns**
  - `Task<PaginatedResponse<OutboxMessageDto>>` – Paginated list of matching messages.
- **Exceptions**
  - Throws `ArgumentNullException` if `request` is null.

### `GetByTopicAsync`

Retrieves all outbox messages associated with a specific topic.

- **Parameters**
  - `topic` (`string`) – The topic to filter by.
- **Returns**
  - `Task<List<OutboxMessageDto>>` – List of matching messages.
- **Exceptions**
  - Throws `ArgumentNullException` if `topic` is null.

### `GetByAggregateAsync`

Retrieves all outbox messages associated with a specific aggregate ID.

- **Parameters**
  - `aggregateId` (`string`) – The aggregate ID to filter by.
- **Returns**
  - `Task<List<OutboxMessageDto>>` – List of matching messages.
- **Exceptions**
  - Throws `ArgumentNullException` if `aggregateId` is null.

### `FindErrorsAsync`

Retrieves all outbox messages that failed processing.

- **Returns**
  - `Task<List<OutboxMessageDto>>` – List of failed messages.
- **Exceptions**
  - None.

### `FindByErrorPatternAsync`

Retrieves outbox messages whose error messages match a given pattern.

- **Parameters**
  - `pattern` (`string`) – Regex pattern to match against error messages.
- **Returns**
  - `Task<List<OutboxMessageDto>>` – List of matching messages.
- **Exceptions**
  - Throws `ArgumentNullException` if `pattern` is null.

### `FindStuckMessagesAsync`

Retrieves outbox messages that are considered stuck based on processing time thresholds.

- **Returns**
  - `Task<List<OutboxMessageDto>>` – List of stuck messages.
- **Exceptions**
  - None.

### `GetByTimeRangeAsync`

Retrieves outbox messages within a specified time range.

- **Parameters**
  - `from` (`DateTime`) – Start of the time range (inclusive).
  - `to` (`DateTime`) – End of the time range (inclusive).
- **Returns**
  - `Task<List<OutboxMessageDto>>` – List of matching messages.
- **Exceptions**
  - Throws `ArgumentOutOfRangeException` if `from` is after `to`.

## Usage
