// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Frequently Asked Questions

## General Questions

### Q: What is the outbox pattern and why do I need it?

**A:** The outbox pattern ensures reliable message publishing in distributed systems. It solves the "exactly-once delivery" problem by atomically storing messages with your domain data, then having a background process publish them. If your app crashes before publishing, the message is still safely stored and will be published later.

**Use cases:**
- Microservices communication
- Event-driven architectures
- CQRS with event sourcing
- Any system where message loss is unacceptable

### Q: How is this different from Saga pattern?

**A:** 
- **Outbox Pattern:** Focuses on reliable message *publishing*. Ensures a message gets delivered to a broker.
- **Saga Pattern:** Focuses on *orchestrating* distributed transactions. Ensures a multi-step process completes or rolls back.

They're complementary - use the outbox pattern within your sagas to reliably publish saga events.

### Q: Do I need a distributed transaction manager?

**A:** No. The outbox pattern eliminates the need for XA/2PC transactions across databases. Everything is stored in a single SQL Server transaction, making it simpler and faster.

### Q: Is this library production-ready?

**A:** Yes. It includes:
- Comprehensive error handling
- Configurable retry policies
- Dead letter queue for failed messages
- Health monitoring
- Production deployment guides
- Real-world examples

### Q: What's the performance impact?

**A:** Minimal. The overhead is:
- One additional INSERT to OutboxMessages table
- Background processor polls every 5 seconds (configurable)
- No impact on request latency

Typical throughput: 1,000+ messages/second on a single instance.

## Configuration Questions

### Q: How do I change how often messages are processed?

**A:** Configure `DelayBetweenBatches` in appsettings.json:

```json
{
  "Outbox": {
    "DelayBetweenBatches": 2000  // Process every 2 seconds instead of 5
  }
}
```

Lower values = lower latency but more database load.

### Q: What's the difference between retry policies?

**A:**
- **ExponentialBackoff** (recommended): 5s → 10s → 20s → 40s → 80s
  - Best for transient failures
  - Reduces load on failing services

- **LinearBackoff**: 5s → 10s → 15s → 20s → 25s
  - Simpler behavior
  - More predictable

- **FixedDelay**: 30s → 30s → 30s → 30s → 30s
  - For consistent timing
  - Use when you know failure duration

### Q: How many retries is too many?

**A:** The default is 5 retries. This provides:
- ~150 seconds total with exponential backoff
- Enough time for most transient issues to resolve
- Reasonable balance with business SLAs

Increase for:
- Slow external services (try 7-10)
- Higher tolerance for delays

Decrease for:
- Fast recovery requirements (try 3)
- Limited DLQ tolerance

### Q: Should I enable PreservePartitionOrdering?

**A:** Almost always yes. It ensures:
- Messages for the same customer/aggregate are processed in order
- Prevents out-of-order event effects

Disable only if:
- You have no causal dependencies between messages
- You need maximum parallel throughput and have idempotent deduplication

## Usage Questions

### Q: How do I generate idempotency keys?

**A:** Use consistent, deterministic patterns:

```csharp
// Entity creation
$"entity-{entityType.ToLower()}-{entityId}"

// State transitions
$"aggregate-{aggregateId}-{eventName}-{timestamp.Ticks}"

// External API calls
$"webhook-{webhookId}-attempt-{attemptNumber}"
```

Key principle: **Same input = same key = idempotent**

### Q: What if I forget to set an idempotency key?

**A:** The message is still published, but:
- Duplicate calls will create duplicate messages
- Subscribers must still implement idempotent handling

Best practice: Always use idempotency keys when publishing from web requests.

### Q: How do I test without a message broker?

**A:** Use the `DefaultMessagePublisher` which logs messages:

```csharp
// In test configuration
builder.Services.AddMessagePublisher<DefaultMessagePublisher>();

// Or create a mock
var mockPublisher = new Mock<IMessagePublisher>();
builder.Services.AddSingleton(mockPublisher.Object);

// Verify messages
mockPublisher.Verify(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()));
```

### Q: Can I publish multiple events in one transaction?

**A:** Yes. All inserts in the same DbContext.SaveChangesAsync() are atomic:

```csharp
var evt1 = new OrderCreatedEvent { /* ... */ };
var evt2 = new InventoryReservedEvent { /* ... */ };

await outboxService.PublishEventAsync(evt1, "orders");
await outboxService.PublishEventAsync(evt2, "inventory");

// Both stored atomically
await dbContext.SaveChangesAsync();
```

### Q: What happens if SaveChangesAsync() is called twice?

**A:** Each call is separate:
- First call: Publishes evt1, evt2
- Second call: Would try to publish evt3 (if it exists)

Use idempotency keys to prevent duplicates across retries.

## Dead Letter Queue Questions

### Q: When do messages go to the Dead Letter Queue?

**A:** After `MaxRetries` (default 5) consecutive failures:
1. Message fails
2. Retry count incremented
3. Next retry scheduled
4. After attempt 5 fails → Moved to DeadLetters table
5. State set to "MovedToDlq"

### Q: How do I handle a dead lettered message?

**A:** Three options:

1. **Fix upstream issue and requeue:**
   ```csharp
   await dlService.RequeueAsync(dlId, "Database connection issue resolved");
   ```

2. **Mark as reviewed and move on:**
   ```csharp
   await dlService.ReviewAsync(dlId, "Unable to fix, subscriber removed");
   ```

3. **Investigate and take manual action:**
   ```csharp
   var deadLetter = await dlService.GetByIdAsync(dlId);
   Console.WriteLine($"Message: {deadLetter.OriginalMessage}");
   Console.WriteLine($"Error: {deadLetter.LastError}");
   // Manually publish to different system, etc.
   ```

### Q: How long are dead letters kept?

**A:** Forever (unless you delete them). Recommendations:
- Archive old dead letters (>30 days) to historical table
- Implement retention policy in background job
- Review all unreviewed monthly

### Q: Can I move a message back to the outbox?

**A:** Yes, via requeue:

```csharp
// This creates a new OutboxMessage with the same event data
await dlService.RequeueAsync(deadLetterId, "Retry after fix");

// The original message remains in DeadLetters (for audit)
```

## Message Broker Integration

### Q: What message brokers do you support?

**A:** Out of the box: any broker via custom `IMessagePublisher`. Examples included:
- RabbitMQ
- Azure Service Bus
- Kafka
- AWS SNS/SQS

### Q: Do I have to implement my own publisher?

**A:** For production, yes. The `DefaultMessagePublisher` only logs. Implement `IMessagePublisher`:

```csharp
public class MyPublisher : IMessagePublisher
{
    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        // Your broker logic
    }
}
```

Then register: `builder.Services.AddMessagePublisher<MyPublisher>();`

### Q: What if the message broker is down?

**A:** Messages fail to publish and:
1. Retry with backoff
2. After max retries → DLQ
3. Once broker recovers → Requeue for retry

No messages are lost.

### Q: Can I publish to multiple brokers?

**A:** Yes, create a composite publisher:

```csharp
public class CompositePublisher : IMessagePublisher
{
    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        await _rabbitmq.PublishAsync(message, ct);
        await _serviceBus.PublishAsync(message, ct);
        // Publish to both
    }
}
```

### Q: What's the maximum message size?

**A:** Limited by:
- `NVARCHAR(MAX)` in SQL Server = 2GB (practical limit ~4MB)
- Broker limits (RabbitMQ: 256MB, Kafka: configurable)

For large messages:
1. Store in blob storage, put URL in message
2. Compress with gzip
3. Use binary serialization (MessagePack, Protobuf)

## Performance & Scaling

### Q: How many messages can this handle?

**A:** Depends on:
- Single instance: 1,000-5,000 msg/sec
- Multiple instances: 10,000+ msg/sec (scales linearly)
- Limited by database and broker throughput

### Q: Should I use a read replica for metrics?

**A:** Yes, for production:
```csharp
var readReplicaConnection = "...";  // Read-only replica
var metricsRepository = new OutboxRepository(readReplicaConnection);
```

Prevents metrics queries from blocking processing.

### Q: What's the memory footprint?

**A:** Minimal:
- Application: ~100MB base
- Per batch: ~1MB per 100 messages
- No in-memory queues (uses database)

### Q: Do I need to scale the database separately?

**A:** Scale database when:
- CPU > 80% sustained
- Disk I/O at capacity
- Lock wait times increasing

SQL Server recommendations:
- SSD storage (not spinning disk)
- 8+ cores for multi-instance deployments
- Monitor with: `sp_who2`, `sp_whoisactive`

### Q: Can I run multiple processor instances?

**A:** Yes, recommended for HA. They coordinate via database locks:

```
Instance 1 ──┐
Instance 2 ──┼─→ SQL Server (uses locks to coordinate)
Instance 3 ──┘
```

Only one instance processes each message (due to pessimistic locking).

## Troubleshooting

### Q: Messages accumulate in the database

**Check:**
1. Is processor enabled? `Outbox:ProcessorEnabled = true`
2. Is processor running? Check logs for "Processing batch"
3. Is broker accessible? Test connectivity
4. Are there exceptions? Check `logs/outbox-*.txt`

### Q: High CPU usage

**Causes:**
- `BatchSize` too large (try reducing to 50-100)
- `DelayBetweenBatches` too small (try 5000+)
- Slow database queries

### Q: Database locks and timeouts

**Solutions:**
1. Increase `LockDurationSeconds` (default 300)
2. Reduce `BatchSize`
3. Add database indexes (auto-created by migrations)
4. Review slow queries in SQL Server profiler

### Q: "Idempotency Key Already Exists"

**Cause:** Publishing same event twice with same idempotency key

**Action:**
1. Check application logic for retry loops
2. If intentional, verify subscriber is idempotent
3. Old key? Reuse OK, subscriber deduplicates

### Q: "Execution Timeout Expired"

**Solutions:**
1. Check database is responsive
2. Reduce batch size
3. Increase timeout: `Connection Timeout=60` in connection string
4. Check for long-running locks

## Best Practices

### Q: What's the recommended deployment model?

**A:** 
- 2-3 instances behind load balancer
- Database: SQL Server Standard (or Azure SQL)
- Monitoring: ELK stack or Application Insights
- Backup: Daily snapshots

### Q: Should I archive old messages?

**A:** Yes, implement:
```csharp
// Move published messages older than 90 days to archive table
var cutoff = DateTime.UtcNow.AddDays(-90);
var oldMessages = await dbContext.OutboxMessages
    .Where(m => m.State == OutboxMessageState.Published && m.PublishedAt < cutoff)
    .ToListAsync();

await archiveContext.ArchivedMessages.AddRangeAsync(oldMessages);
await archiveContext.SaveChangesAsync();

await dbContext.OutboxMessages.Where(m => oldMessages.Contains(m)).DeleteAsync();
```

### Q: What monitoring should I set up?

**A:**
- Pending message count (alert if > 1000)
- DLQ count (alert if > 100)
- Processing latency (track p50, p95, p99)
- Publisher error rate (alert if > 1%)
- Database connection pool usage

