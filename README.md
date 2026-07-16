## DomainEvent

The `DomainEvent` class is an abstract base class that serves as the foundation for all domain events in the system. It provides essential event metadata including a unique identifier, timestamp, and correlation/causation tracking for distributed tracing scenarios. Domain events are used throughout the application to capture and propagate state changes and business-relevant occurrences.

### Base Properties

```csharp
public Guid EventId { get; init; } = Guid.NewGuid();
public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
public string? CorrelationId { get; init; }
public string? CausationId { get; init; }
public string? UserId { get; init; }
```

### Example Usage

```csharp
// Creating a custom domain event
var customEvent = new CustomDomainEvent
{
    EventName = "OrderStatusChanged",
    AggregateId = "order-12345",
    AggregateType = "Order",
    CorrelationId = "corr-8675309",
    CausationId = "command-98765",
    UserId = "user-42",
    Payload = new Dictionary<string, object>
    {
        ["orderId"] = "order-12345",
        ["oldStatus"] = "Pending",
        ["newStatus"] = "Processing",
        ["timestamp"] = DateTime.UtcNow.ToString("o")
    }
};

// Creating an entity created event
var createdEvent = new EntityCreatedEvent
{
    EntityId = "customer-789",
    EntityType = "Customer",
    EntityData = new Dictionary<string, object>
    {
        ["name"] = "John Doe",
        ["email"] = "john.doe@example.com",
        ["createdAt"] = DateTime.UtcNow.ToString("o")
    },
    CorrelationId = "corr-12345",
    UserId = "user-42"
};
```

## BatchProcessingOptions

The `BatchProcessingOptions` class provides configuration for batch processing operations with configurable chunk sizes and parallel execution. It allows fine-tuning of memory usage, throughput, and fault tolerance when processing large volumes of outbox messages. Key features include adjustable chunk sizes for memory management, configurable parallel chunk processing for throughput optimization, and sequential chunk processing with delay options for controlled downstream impact.

### Example Usage

```csharp
// Configure batch processing for high-throughput scenario with parallel chunks
var batchOptions = new BatchProcessingOptions
{
    TotalBatchSize = 5000,
    ChunkSize = 250,
    MaxParallelChunks = 4,
    EnableParallelChunks = true,
    DelayBetweenChunksMs = 100,
    StopOnChunkFailure = false
};

// Configure batch processing for memory-sensitive scenario with sequential chunks
var memorySensitiveOptions = new BatchProcessingOptions
{
    TotalBatchSize = 1000,
    ChunkSize = 50,
    MaxParallelChunks = 1,
    EnableParallelChunks = false,
    DelayBetweenChunksMs = 500,
    StopOnChunkFailure = true
};
```

## DeadLetter

The `DeadLetter` class represents a message that has been moved to the dead-letter queue (DLQ) after repeated processing failures. It captures the original message context, failure details, and metadata to enable analysis and potential reprocessing. Dead letters are used to prevent poison messages from blocking the outbox processing pipeline while preserving diagnostic information for troubleshooting.

### Example Usage

```csharp
// Create a dead letter for a failed order event processing
var deadLetter = new DeadLetter
{
    OutboxMessageId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa"),
    IdempotencyKey = "order-created-12345",
    AggregateId = "order-12345",
    AggregateType = "Order",
    EventType = EventType.OrderCreated,
    EventData = JsonSerializer.Serialize(new { OrderId = "order-12345", Amount = 99.99m }),
    EventTypeName = typeof(OrderCreatedEvent).FullName,
    Topic = "orders",
    PartitionKey = "order-12345",
    TotalAttempts = 5,
    ErrorMessage = "Failed to process order: inventory check timeout",
    ErrorStackTrace = "at OrderService.ProcessOrder() in OrderService.cs:line 42\n...",
    OriginalCreatedAt = DateTime.UtcNow.AddMinutes(-30),
    MovedToDlqAt = DateTime.UtcNow,
    LastAttemptAt = DateTime.UtcNow.AddSeconds(-10),
    CorrelationId = "corr-8675309",
    CausationId = "command-98765",
    Metadata = JsonSerializer.Serialize(new { Priority = "high", Source = "api-gateway" })
};

// Reviewed dead letter
var reviewedDeadLetter = new DeadLetter
{
    OutboxMessageId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa"),
    IdempotencyKey = "order-created-12345",
    AggregateId = "order-12345",
    AggregateType = "Order",
    EventType = EventType.OrderCreated,
    EventData = JsonSerializer.Serialize(new { OrderId = "order-12345", Amount = 99.99m }),
    EventTypeName = typeof(OrderCreatedEvent).FullName,
    Topic = "orders",
    PartitionKey = "order-12345",
    TotalAttempts = 5,
    ErrorMessage = "Failed to process order: inventory check timeout",
    ErrorStackTrace = "at OrderService.ProcessOrder() in OrderService.cs:line 42\n...",
    OriginalCreatedAt = DateTime.UtcNow.AddMinutes(-30),
    MovedToDlqAt = DateTime.UtcNow,
    LastAttemptAt = DateTime.UtcNow.AddSeconds(-10),
    CorrelationId = "corr-8675309",
    CausationId = "command-98765",
    Metadata = JsonSerializer.Serialize(new { Priority = "high", Source = "api-gateway" }),
    IsReviewed = true
};
```

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