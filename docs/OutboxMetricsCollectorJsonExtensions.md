# OutboxMetricsCollectorJsonExtensions

The `OutboxMetricsCollectorJsonExtensions` class provides serialization capabilities for outbox metrics and serves as a facilitator for interacting with the core outbox service operations. It enables the conversion of metrics data to and from JSON formats, facilitating the export and persistence of system state, while also exposing methods for publishing events, retrieving message details, and managing the outbox lifecycle.

## API

### Static Methods

*   **`string ToJson`**: Serializes an instance of `OutboxMetricsCollector` into a JSON string representation.
*   **`OutboxMetricsCollector? FromJson`**: Deserializes a JSON string into an `OutboxMetricsCollector` object. Returns `null` if the input string is invalid or null.
*   **`bool TryFromJson`**: Attempts to deserialize a JSON string into an `OutboxMetricsCollector` object. Returns `true` if successful, and `false` otherwise.

### Properties

*   **`long TotalMessages`**: Gets the total count of messages currently in the outbox.
*   **`long PendingMessages`**: Gets the number of messages awaiting processing.
*   **`long PublishedMessages`**: Gets the number of messages successfully published.
*   **`long FailedMessages`**: Gets the number of messages that failed to publish.
*   **`long ArchivedMessages`**: Gets the number of messages that have been moved to the archive.
*   **`long DeadLetterCount`**: Gets the total number of messages currently in the dead-letter queue.
*   **`double AveragePublishTime`**: Gets the average duration taken to successfully publish a message, typically measured in milliseconds.
*   **`double? OldestPendingAge`**: Gets the age of the oldest pending message, or `null` if no messages are pending.
*   **`double SuccessRate`**: Gets the current percentage of successfully published messages relative to the total processed.

### Methods

*   **`Task<OutboxMessage> PublishEventAsync`**: Publishes an event to the outbox. Provides overloads for handling different event types and configurations.
*   **`Task<OutboxMessage?> GetMessageAsync`**: Retrieves a specific message by its identifier from the outbox. Returns `null` if the message is not found.
*   **`Task<OutboxStatistics> GetStatisticsAsync`**: Asynchronously fetches a snapshot of the current outbox statistics.
*   **`Task<bool> RetryFailedMessageAsync`**: Attempts to re-publish a previously failed message. Returns `true` if the retry operation is initiated successfully.
*   **`Task ArchiveOldMessagesAsync`**: Performs an operation to archive messages that have exceeded their retention period.
*   **`Task<List<OutboxMessage>> GetAllMessagesAsync`**: Retrieves a complete list of messages currently held in the outbox.
*   **`Task<List<OutboxMessage>> GetMessagesByTopicAsync`**: Retrieves a list of messages filtered by the specified topic.

## Usage

### Serializing and Deserializing Metrics

```csharp
var metrics = GetCurrentMetrics();

// Serialize metrics to JSON
string json = OutboxMetricsCollectorJsonExtensions.ToJson(metrics);
File.WriteAllText("metrics.json", json);

// Deserialize metrics from JSON
string loadedJson = File.ReadAllText("metrics.json");
if (OutboxMetricsCollectorJsonExtensions.TryFromJson(loadedJson, out var restoredMetrics))
{
    Console.WriteLine($"Published count: {restoredMetrics.PublishedMessages}");
}
```

### Performing Outbox Operations

```csharp
// Retrieve current statistics
OutboxStatistics stats = await OutboxMetricsCollectorJsonExtensions.GetStatisticsAsync();
Console.WriteLine($"Current Success Rate: {stats.SuccessRate}%");

// Retry a failed message
bool retrySuccessful = await OutboxMetricsCollectorJsonExtensions.RetryFailedMessageAsync(messageId);
if (retrySuccessful)
{
    Console.WriteLine("Retry operation initiated.");
}
```

## Notes

*   **Data Integrity**: When using `FromJson` or `TryFromJson`, ensure that the JSON input conforms to the expected schema of the `OutboxMetricsCollector` type. Invalid or malformed JSON will result in either `null` returns or `false` outcomes, depending on the method used.
*   **Asynchronous Operations**: All methods suffixed with `Async` are asynchronous and should be awaited to ensure proper execution flow and to prevent blocking the calling thread.
*   **Thread Safety**: While the serialization methods are generally thread-safe as they operate on input data, the asynchronous methods that interact with the outbox service rely on the underlying repository's thread-safety guarantees. Ensure the `OutboxService` instance is configured appropriately for concurrent access if needed.
*   **Performance**: `GetStatisticsAsync` and `GetAllMessagesAsync` may be resource-intensive if the outbox contains a very large volume of messages; consider implementing pagination or filtering where possible if performance becomes an issue.
