## OutboxProcessingResult

The `OutboxProcessingResult` class provides a comprehensive result object for outbox message processing operations. It encapsulates key information about the processing outcome, including success status, processed message count, failed message count, dead letter count, error message, stack trace, start and completion timestamps, processed message IDs, failed message IDs, batch size, lock duration, delay between batches, messages before break, break duration, and whether parallel processing is enabled.

### Example Usage
```csharp
public bool Success { get; set; }
public int ProcessedCount { get; set; }
public int FailedCount { get; set; }
public int DeadLetterCount { get; set; }
public string? ErrorMessage { get; set; }
public string? StackTrace { get; set; }
public DateTime StartedAt { get; set; }
public DateTime CompletedAt { get; set; }
public List<Guid> ProcessedMessageIds { get; set; }
public List<Guid> FailedMessageIds { get; set; }
public int BatchSize { get; set; }
public TimeSpan LockDuration { get; set; }
public TimeSpan DelayBetweenBatches { get; set; }
public int MessagesBeforeBreak { get; set; }
public TimeSpan BreakDuration { get; set; }
public bool EnableParallelProcessing { get; set; }
public int MaxDegreeOfParallelism { get; set; }
public bool EnableDeadLetterProcessing { get; set; }
public long TotalMessages { get; set; }
public long PendingMessages { get; set; }
```