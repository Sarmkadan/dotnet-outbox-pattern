# OutboxProcessingResult

Represents the outcome of a single outbox processing cycle. It captures success/failure status, per-message results, batch configuration parameters, and timing information. Instances are typically produced by an outbox processor and consumed by callers to determine whether processing completed successfully, how many messages were handled, and to diagnose failures.

## API

### `public bool Success`
Indicates whether the entire batch was processed without any unhandled exceptions. `true` if no messages failed and no dead-letter processing errors occurred; otherwise `false`. Does not throw.

### `public int ProcessedCount`
The number of messages that were successfully processed (committed or forwarded). Does not throw.

### `public int FailedCount`
The number of messages that failed processing but were not moved to the dead-letter queue. Does not throw.

### `public int DeadLetterCount`
The number of messages that were moved to the dead-letter queue due to repeated failures or configuration. Does not throw.

### `public string? ErrorMessage`
A human-readable description of the first error encountered during processing, or `null` if `Success` is `true`. Does not throw.

### `public string? StackTrace`
The stack trace associated with the first error, or `null` if no error occurred. Does not throw.

### `public DateTime StartedAt`
The UTC timestamp when the processing cycle began. Does not throw.

### `public DateTime CompletedAt`
The UTC timestamp when the processing cycle ended (including any dead-letter operations). Does not throw.

### `public List<Guid> ProcessedMessageIds`
A list of unique identifiers for messages that were successfully processed. The list is never `null` but may be empty. Does not throw.

### `public List<Guid> FailedMessageIds`
A list of unique identifiers for messages that failed processing. The list is never `null` but may be empty. Does not throw.

### `public int BatchSize`
The maximum number of messages that were fetched from the outbox for this cycle. Does not throw.

### `public TimeSpan LockDuration`
The duration for which messages were locked during processing. Does not throw.

### `public TimeSpan DelayBetweenBatches`
The configured delay between consecutive batch processing cycles. Does not throw.

### `public int MessagesBeforeBreak`
The number of messages processed before a scheduled break (pause) is taken. Does not throw.

### `public TimeSpan BreakDuration`
The duration of the break taken after processing `MessagesBeforeBreak` messages. Does not throw.

### `public bool EnableParallelProcessing`
Whether parallel processing was enabled for this cycle. Does not throw.

### `public int MaxDegreeOfParallelism`
The maximum number of concurrent tasks used when parallel processing is enabled. Does not throw.

### `public bool EnableDeadLetterProcessing`
Whether dead-letter processing (moving persistently failing messages to a separate queue) was enabled. Does not throw.

### `public long TotalMessages`
The total number of messages in the outbox at the start of the cycle. Does not throw.

### `public long PendingMessages`
The number of messages remaining in the outbox after the cycle completed. Does not throw.

## Usage

### Example 1: Basic processing and logging

```csharp
var result = await outboxProcessor.ProcessAsync(cancellationToken);

Console.WriteLine($"Batch started at {result.StartedAt:O}, completed at {result.CompletedAt:O}");
Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Processed: {result.ProcessedCount}, Failed: {result.FailedCount}, Dead-lettered: {result.DeadLetterCount}");

if (!result.Success)
{
    Console.Error.WriteLine($"Error: {result.ErrorMessage}");
    Console.Error.WriteLine(result.StackTrace);
}
```

### Example 2: Inspecting failed messages and retrying

```csharp
var result = await outboxProcessor.ProcessAsync(cancellationToken);

if (result.FailedCount > 0)
{
    Console.WriteLine($"Retrying {result.FailedCount} failed messages...");
    foreach (var id in result.FailedMessageIds)
    {
        // Re-enqueue or reprocess the message
        await outboxProcessor.RetryMessageAsync(id, cancellationToken);
    }
}

if (result.DeadLetterCount > 0)
{
    Console.WriteLine($"Dead-lettered {result.DeadLetterCount} messages. Check dead-letter queue.");
}
```

## Notes

- **Thread safety**: The `ProcessedMessageIds` and `FailedMessageIds` properties expose mutable `List<Guid>` instances. If the same `OutboxProcessingResult` object is accessed from multiple threads, external synchronization is required when reading or modifying these lists. All other properties are immutable after construction and are safe to read concurrently.
- **Timestamps**: `StartedAt` and `CompletedAt` are provided in UTC. Callers should convert to local time if needed for display.
- **Zero-message cycles**: When the outbox is empty, `ProcessedCount`, `FailedCount`, and `DeadLetterCount` will all be zero, `Success` will be `true`, and both message ID lists will be empty. `TotalMessages` and `PendingMessages` will reflect the actual counts.
- **Error information**: `ErrorMessage` and `StackTrace` capture only the first error encountered. If multiple messages fail, only the first failure is recorded in these properties; all failed message IDs are available in `FailedMessageIds`.
- **Configuration properties**: `BatchSize`, `LockDuration`, `DelayBetweenBatches`, `MessagesBeforeBreak`, `BreakDuration`, `EnableParallelProcessing`, `MaxDegreeOfParallelism`, and `EnableDeadLetterProcessing` reflect the settings used during the cycle, not necessarily the current global configuration if it changed mid-cycle.
