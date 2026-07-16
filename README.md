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

## OutboxMessage

The `OutboxMessage` class represents a message stored in the transactional outbox, providing reliable delivery guarantees with ordering and deduplication. It captures essential metadata and state information for processing and retry logic.

### Example Usage

```csharp
// Create a new outbox message
var outboxMessage = new OutboxMessage
{
    IdempotencyKey = "order-created-12345",
    AggregateId = "order-12345",
    AggregateType = "Order",
    EventType = EventType.OrderCreated,
    EventData = JsonSerializer.Serialize(new { OrderId = "order-12345", Amount = 99.99m }),
    EventTypeName = typeof(OrderCreatedEvent).FullName,
    Topic = "orders",
    PartitionKey = "order-12345",
    DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce,
    CorrelationId = "corr-12345",
    CausationId = "command-12345"
};

outboxMessage.Validate();

// Simulate publishing the message
outboxMessage.MarkAsPublished();

// Record a processing failure
outboxMessage.RecordFailure("Failed to publish message", "at OutboxService.Publish() in OutboxService.cs:line 42");

// Check if the message can be retried
if (outboxMessage.CanRetry())
{
    Console.WriteLine("Message can be retried");
}
else
{
    Console.WriteLine("Message cannot be retried");
}
```

## OutboxMetricsCollector

The `OutboxMetricsCollector` class provides comprehensive metrics collection and monitoring capabilities for the outbox pattern implementation. It tracks publishing metrics, monitors queue health, implements health checks, and enables observability through detailed reporting and alerting systems.

The collector aggregates statistics from the outbox service and dead letter queue, providing real-time insights into system health, throughput, and failure rates. It includes built-in health checks for container orchestrators, alerting thresholds for proactive monitoring, and detailed metrics reports for dashboards and observability platforms.

### Example Usage

```csharp
// Register services in Program.cs
builder.Services.AddScoped<OutboxMetricsCollector>();
builder.Services.AddScoped<OutboxAlertingService>();
builder.Services.AddScoped<OutboxHealthCheck>();
builder.Services.AddScoped<MetricsCollectionJob>();

// Configure health checks
builder.Services.AddHealthChecks()
    .AddCheck<OutboxHealthCheck>("outbox");

// Schedule metrics collection job
builder.Services.AddHostedService<MetricsCollectionBackgroundService>();

// Example: Inject and use in a background service
public class MetricsCollectionBackgroundService : BackgroundService
{
    private readonly OutboxMetricsCollector _metricsCollector;
    private readonly OutboxAlertingService _alertingService;
    private readonly ILogger<MetricsCollectionBackgroundService> _logger;

    public MetricsCollectionBackgroundService(
        OutboxMetricsCollector metricsCollector,
        OutboxAlertingService alertingService,
        ILogger<MetricsCollectionBackgroundService> logger)
    {
        _metricsCollector = metricsCollector;
        _alertingService = alertingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Collecting outbox metrics");
                
                // Collect and log metrics
                await _metricsCollector.CollectMetricsAsync();
                
                // Check alert conditions
                await _alertingService.CheckAndAlertAsync();
                
                // Get detailed metrics report
                var detailedReport = await _metricsCollector.GetDetailedMetricsAsync();
                _logger.LogInformation(detailedReport);
                
                // Check health status
                var (statusCode, message) = await _metricsCollector.HealthCheck.CheckHealthAsync();
                _logger.LogInformation("Health check: {StatusCode} - {Message}", statusCode, message);
                
                // Check readiness
                var isReady = await _metricsCollector.HealthCheck.IsReadyAsync();
                _logger.LogInformation("System ready: {IsReady}", isReady);
                
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Metrics collection failed");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}

// Example: Configure alerting thresholds
var alertingService = new OutboxAlertingService(outboxService, logger);
alertingService.Thresholds.MaxPendingMessages = 2000;
alertingService.Thresholds.MaxDlqMessages = 200;
alertingService.Thresholds.MaxFailureRate = 0.10; // 10% failure rate allowed
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