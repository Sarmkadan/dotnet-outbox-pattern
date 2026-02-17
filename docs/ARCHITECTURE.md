// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Architecture Guide

This document explains the architectural design of the Outbox Pattern implementation.

## High-Level Design

The outbox pattern solves the distributed transaction problem with these key principles:

1. **Atomic Persistence**: Domain data and messages are saved together
2. **Eventual Publication**: A background process publishes messages asynchronously
3. **Reliable Delivery**: Failed publishes are retried with exponential backoff
4. **Dead Letter Queue**: Undeliverable messages are moved for manual review
5. **Idempotent Processing**: Duplicate messages don't cause issues at the subscriber

## Component Architecture

### Layer 1: API Layer (Controllers)

```csharp
Controllers/
├── OutboxMessageController.cs    // Event publishing API
├── DeadLetterController.cs       // DLQ management
├── MetricsController.cs          // Statistics and monitoring
└── ExportController.cs           // Data export (CSV, JSON, XML)
```

**Responsibilities:**
- Handle HTTP requests
- Validate input
- Delegate to service layer
- Return responses

**Examples:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class OutboxMessageController : ControllerBase
{
    private readonly IOutboxService _outboxService;

    [HttpPost("events")]
    public async Task<IActionResult> PublishEvent(PublishEventRequest request)
    {
        // Request validation → Service call → Response
        var message = await _outboxService.PublishEventAsync(
            request.Event, request.Topic, request.PartitionKey);
        return Ok(message);
    }
}
```

### Layer 2: Service Layer (Business Logic)

```csharp
Services/
├── OutboxService.cs                    // Main publishing API
├── MessagePublishingService.cs         // Retry logic and delivery
├── DeadLetterService.cs                // DLQ operations
├── IMessageSearchService.cs            // Query operations
└── IMetricsService.cs                  // Statistics
```

**IOutboxService Responsibilities:**
- Accept domain events
- Store in outbox table
- Apply idempotency keys
- Return message ID to caller

**IMessagePublishingService Responsibilities:**
- Poll pending messages
- Invoke IMessagePublisher
- Handle retry logic
- Move to DLQ on max retries
- Update message state

**IDeadLetterService Responsibilities:**
- Retrieve failed messages
- Mark as reviewed
- Requeue for retry
- Provide search/filtering

### Layer 3: Data Access Layer (Repositories)

```csharp
Data/
├── OutboxRepository.cs          // Outbox CRUD
├── DeadLetterRepository.cs      // DLQ operations
└── OutboxDbContext.cs           // Entity Framework context
```

**Key Operations:**
```csharp
// Outbox Repository
GetPendingMessages(int batchSize, bool preserveOrdering)
MarkAsPublished(Guid messageId)
MarkAsLockedForProcessing(Guid messageId, TimeSpan duration)
MoveToDlq(Guid messageId, string error)
UpdateRetryCount(Guid messageId)

// Dead Letter Repository
GetUnreviewed()
MarkAsReviewed(Guid deadLetterId, string notes)
CreateFromOutboxMessage(OutboxMessage message, string error)
Requeue(Guid deadLetterId, string reason)
```

### Layer 4: Infrastructure

```csharp
Infrastructure/
├── OutboxProcessor.cs           // Hosted service (background task)
├── DefaultMessagePublisher.cs   // Abstract message broker
├── RetryPolicyHelper.cs         // Exponential/linear backoff
└── SerializationHelper.cs       // JSON serialization
```

**OutboxProcessor** is an `IHostedService`:
- Starts with the application
- Runs background message processing loop
- Respects configuration for batch size and delay
- Handles exceptions gracefully

**IMessagePublisher** is your extensibility point:
- Implement for RabbitMQ, Azure Service Bus, Kafka, etc.
- Called by OutboxProcessor for each pending message
- Exception behavior determines retry vs. DLQ

### Layer 5: Domain

```csharp
Domain/
├── OutboxMessage.cs             // Core entity
├── DeadLetter.cs                // Failed message entity
├── Enums.cs                      // State, retry policies
├── Events.cs                     // Domain event hierarchy
└── Models.cs                     // Statistics, DTOs
```

## Data Flow Sequences

### Publishing a Message

```
1. Domain Event Created
   └─ OrderCreatedEvent { OrderId, Amount, ... }

2. IOutboxService.PublishEventAsync()
   ├─ Serialize event to JSON
   ├─ Create OutboxMessage entity
   │  ├─ State = Pending
   │  ├─ CreatedAt = now
   │  ├─ IdempotencyKey (if provided)
   │  └─ PartitionKey (for ordering)
   └─ Insert into OutboxMessages table

3. Return to caller
   └─ MessageId to track

4. Transaction commits
   └─ Message is now persisted
```

### Processing Pending Messages

```
Background Loop (OutboxProcessor):

1. Wait DelayBetweenBatches
2. Query pending messages (BatchSize limit)
3. Order by:
   ├─ PartitionKey (group related messages)
   └─ CreatedAt (maintain FIFO within partition)

4. For each message:
   a) Lock for processing
      └─ Update State = Processing, LockedAt = now
   
   b) Call IMessagePublisher.PublishAsync()
      ├─ On success:
      │  ├─ Update State = Published
      │  ├─ Set PublishedAt = now
      │  └─ Release lock
      │
      └─ On failure:
         ├─ If RetryCount < MaxRetries:
         │  ├─ Increment RetryCount
         │  ├─ Calculate next retry time based on policy
         │  ├─ Release lock
         │  └─ Wait until retry time
         │
         └─ If RetryCount == MaxRetries:
            ├─ Create DeadLetter record
            ├─ Move OutboxMessage to DLQ
            ├─ Set State = MovedToDlq
            └─ Log error for investigation

5. Repeat
```

## Data Model

### OutboxMessage Table

```sql
CREATE TABLE OutboxMessages (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    AggregateId NVARCHAR(255) NOT NULL,
    Topic NVARCHAR(255) NOT NULL,
    EventData NVARCHAR(MAX) NOT NULL,
    EventType NVARCHAR(100),
    
    State INT NOT NULL,  -- 0=Pending, 1=Processing, 2=Published, 3=Failed
    RetryCount INT DEFAULT 0,
    IdempotencyKey NVARCHAR(255) UNIQUE,
    PartitionKey NVARCHAR(255),
    
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2,
    PublishedAt DATETIME2,
    LockedAt DATETIME2,
    NextRetryAt DATETIME2,
    
    INDEX idx_state_created (State, CreatedAt),
    INDEX idx_partition (PartitionKey, CreatedAt),
    INDEX idx_idempotency (IdempotencyKey)
);
```

### DeadLetters Table

```sql
CREATE TABLE DeadLetters (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OriginalMessageId UNIQUEIDENTIFIER,
    OriginalMessage NVARCHAR(MAX),
    
    LastError NVARCHAR(MAX),
    FailureCount INT,
    
    ReviewedAt DATETIME2,
    ReviewedBy NVARCHAR(255),
    ReviewNotes NVARCHAR(MAX),
    
    CreatedAt DATETIME2 NOT NULL,
    
    INDEX idx_reviewed (ReviewedAt)
);
```

## Concurrency & Locking

### Optimistic Locking (Version-based)

Not currently used, but extensible design allows adding:
- Prevents lost updates to messages
- Based on `RowVersion` column

### Pessimistic Locking (Row-level)

Current implementation uses database-level locks:

```csharp
// In OutboxProcessor
await repository.MarkAsLockedForProcessing(messageId, lockDuration: 5minutes);

// SQL Generated:
// UPDATE OutboxMessages 
// SET State = 1 (Processing), LockedAt = GETUTCDATE()
// WHERE Id = @id AND LockedAt IS NULL

// Ensures only one processor processes the message
// Lock expires after 5 minutes (default configurable)
```

### Idempotency

Duplicate messages are detected by:

```csharp
// Unique constraint on IdempotencyKey
var existing = await repository.GetByIdempotencyKeyAsync(key);
if (existing != null)
{
    return existing;  // Return existing message, don't create duplicate
}
```

## Retry Policies

### ExponentialBackoff (Recommended)

```
Attempt 1: 5 seconds
Attempt 2: 10 seconds (5 * 2)
Attempt 3: 20 seconds (10 * 2)
Attempt 4: 40 seconds
Attempt 5: 80 seconds
After Attempt 5: Move to DLQ
```

**Algorithm:**
```csharp
NextRetryDelay = Math.Min(
    InitialDelay * Math.Pow(2, attemptNumber),
    MaxDelay
);
```

### LinearBackoff

```
Attempt 1: 5 seconds
Attempt 2: 10 seconds (5 + 5)
Attempt 3: 15 seconds (10 + 5)
Attempt 4: 20 seconds (15 + 5)
Attempt 5: 25 seconds
After Attempt 5: Move to DLQ
```

### FixedDelay

```
Attempt 1-5: 30 seconds each
After Attempt 5: Move to DLQ
```

## Scalability Considerations

### Horizontal Scaling

Multiple instances can run simultaneously:

```
Instance 1 ──┐
Instance 2 ──┼─→ SQL Server (OutboxMessages)
Instance 3 ──┘
             ↓
         Message Broker
```

**Safety mechanisms:**
- Row-level locking prevents duplicate publishing
- `LockedAt` timestamp prevents indefinite locks
- `NextRetryAt` ensures ordered retry attempts

**Configuration for scale:**
- Increase `BatchSize` for high throughput (100-500)
- Reduce `DelayBetweenBatches` for lower latency (1000-5000ms)
- Monitor database connection pool exhaustion

### Vertical Scaling

Single instance handling high volume:

```csharp
"Outbox": {
    "BatchSize": 500,              // Process more per cycle
    "DelayBetweenBatches": 1000,   // More frequent cycles
    "ProcessorEnabled": true
}
```

**Considerations:**
- Memory usage grows with batch size
- Database lock timeouts may increase
- Message broker rate limits become bottleneck

## Delivery Guarantees

### At-Least-Once (Default)

- Message is published one or more times
- Retried on failure
- **Use when:** Subscribers handle duplicates idempotently

```csharp
"DeliveryGuarantee": "AtLeastOnce"
```

**Example:** OrderCreated event where subscriber checks order ID before processing

### At-Most-Once

- Message published zero or one time
- Not retried on failure
- **Use when:** Exactly-once isn't critical

```csharp
"DeliveryGuarantee": "AtMostOnce"
```

**Example:** Analytics events where some loss is acceptable

### Exactly-Once (Recommended with idempotency)

- Achieved through:
  1. Idempotency keys at publisher
  2. Idempotent handlers at subscriber
  3. Exactly-once message broker (e.g., Kafka with idempotent producer)

```csharp
// Publisher: Use consistent idempotency keys
await outboxService.PublishEventAsync(
    evt,
    topic: "orders",
    idempotencyKey: $"order-{order.Id}");  // Same key = same message

// Subscriber: Check if already processed
if (await _db.ProcessedEvents.AnyAsync(e => e.Key == idempotencyKey))
    return;  // Already handled
```

## Extension Points

### 1. Custom IMessagePublisher

```csharp
public class KafkaPublisher : IMessagePublisher
{
    public async Task PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        // Publish to Kafka
    }
}

// Register
builder.Services.AddMessagePublisher<KafkaPublisher>();
```

### 2. Custom Serialization

```csharp
public class CustomSerializationPublisher : IMessagePublisher
{
    // Use Protocol Buffers, MessagePack, etc.
}
```

### 3. Event Enrichment Decorator

```csharp
public class EnrichingOutboxService : IOutboxService
{
    // Add correlation IDs, user context, etc.
}
```

### 4. Dead Letter Processing

```csharp
public class DlqProcessor : IHostedService
{
    // Custom logic to handle/recover failed messages
}
```

## Testing Strategy

### Unit Tests

Test individual components in isolation:

```csharp
[Fact]
public async Task PublishEventAsync_CreatesOutboxMessage()
{
    // Arrange
    var mockRepository = new Mock<IOutboxRepository>();
    var service = new OutboxService(mockRepository.Object, ...);

    // Act
    await service.PublishEventAsync(evt, "topic");

    // Assert
    mockRepository.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>()));
}
```

### Integration Tests

Test with real database:

```csharp
[Fact]
public async Task ProcessorPublishesAllPendingMessages()
{
    // Arrange - Use in-memory SQL Server
    var options = new DbContextOptionsBuilder<OutboxDbContext>()
        .UseSqlServer("connection-string")
        .Options;

    using var context = new OutboxDbContext(options);
    await context.Database.EnsureCreatedAsync();

    // Act - Publish and process
    var outboxService = new OutboxService(context, ...);
    await outboxService.PublishEventAsync(evt, "topic");

    var processor = new OutboxProcessor(...);
    await processor.ProcessBatchAsync(CancellationToken.None);

    // Assert
    var published = await context.OutboxMessages
        .Where(m => m.State == OutboxMessageState.Published)
        .CountAsync();
    Assert.Equal(1, published);
}
```

### Performance Tests

Monitor throughput and latency:

```csharp
[Fact]
public async Task ProcessorHandles10KMessages()
{
    // Publish 10K messages
    for (int i = 0; i < 10000; i++)
    {
        await outboxService.PublishEventAsync(evt, "topic");
    }

    // Measure processing time
    var stopwatch = Stopwatch.StartNew();
    await processor.ProcessBatchAsync(CancellationToken.None);
    stopwatch.Stop();

    // Assert < 30 seconds for 10K messages
    Assert.True(stopwatch.ElapsedMilliseconds < 30000);
}
```

## Monitoring & Observability

### Key Metrics

- **Pending count:** Unprocessed messages
- **Published rate:** Messages/second
- **Retry rate:** Failure percentage
- **DLQ count:** Messages awaiting review
- **Processing latency:** Time from creation to publication

### Logging

Structured logging with Serilog:

```csharp
Log.Information(
    "Published message {MessageId} to topic {Topic} after {Retries} retries",
    messageId, topic, retryCount);

Log.Warning(
    "Message {MessageId} moved to DLQ. Error: {Error}",
    messageId, lastError);
```

### Health Checks

Custom health check for orchestrators:

```csharp
app.MapGet("/health", async (IOutboxService service) =>
{
    var stats = await service.GetStatisticsAsync();
    
    if (stats.DlqCount > 100)
        return Results.StatusCode(503);  // Unhealthy
    
    return Results.Ok(new { status = "healthy", stats });
});
```

