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

## RabbitMqMessagePublisher

The `RabbitMqMessagePublisher` class implements the `IMessagePublisher` interface to publish outbox messages to RabbitMQ. It provides reliable message delivery with configurable exchange and routing key strategies, supporting at-least-once delivery semantics through persistent message properties. The publisher integrates with the .NET dependency injection system and can be configured for production use with RabbitMQ.Client connections.

### Example Usage

```csharp
// Register RabbitMQ publisher in Program.cs
builder.Services.AddMessagePublisher<RabbitMqMessagePublisher>();
builder.Services.AddSingleton(sp => new ConnectionFactory { HostName = "localhost" });

// In a background service or application component
public class OrderEventProcessor
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<OrderEventProcessor> _logger;

    public OrderEventProcessor(IMessagePublisher publisher, ILogger<OrderEventProcessor> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ProcessOrderEventAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken)
    {
        var outboxMessage = new OutboxMessage
        {
            IdempotencyKey = $"order-created-{orderEvent.OrderId}",
            AggregateId = orderEvent.OrderId,
            AggregateType = "Order",
            EventType = EventType.OrderCreated,
            EventData = JsonSerializer.Serialize(orderEvent),
            EventTypeName = typeof(OrderCreatedEvent).FullName,
            Topic = "orders-exchange",
            PartitionKey = orderEvent.OrderId,
            DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce,
            CorrelationId = orderEvent.CorrelationId,
            CausationId = orderEvent.EventId.ToString()
        };

        await _publisher.PublishAsync(outboxMessage, cancellationToken);
        _logger.LogInformation("Order event published to RabbitMQ: {OrderId}", orderEvent.OrderId);
    }
}

// Simple console application example
public static class RabbitMqPublisherExample
{
    public static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        services.AddMessagePublisher<RabbitMqMessagePublisher>();
        services.AddSingleton(sp => new ConnectionFactory { HostName = "localhost", DispatchConsumersAsync = true });

        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        var logger = serviceProvider.GetRequiredService<ILogger<RabbitMqPublisherExample>>();

        var message = new OutboxMessage
        {
            IdempotencyKey = "test-message-123",
            AggregateId = "test-aggregate",
            AggregateType = "Test",
            EventType = EventType.Generic,
            EventData = JsonSerializer.Serialize(new { Test = "data", Timestamp = DateTime.UtcNow }),
            EventTypeName = "TestEvent",
            Topic = "test-exchange",
            PartitionKey = "test-key",
            DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce
        };

        await publisher.PublishAsync(message, CancellationToken.None);
        logger.LogInformation("Message published successfully");
    }
}
```

## BasicEventPublishingExample

The `BasicEventPublishingExample` demonstrates the fundamental pattern for publishing domain events using the transactional outbox. This example shows how to create a domain event, register a user service, and publish events atomically with your domain changes. The outbox pattern ensures reliable event delivery even if the application crashes after saving domain state but before publishing events.

### Example Usage

```csharp
// Create and register services
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());
services.AddOutboxPattern("your-connection-string");
services.AddMessagePublisher<RabbitMqMessagePublisher>();
services.AddScoped<UserService>();

var serviceProvider = services.BuildServiceProvider();
var userService = serviceProvider.GetRequiredService<UserService>();

// Register a user and publish the event atomically
await userService.RegisterUserAsync(
    userId: "USER-123",
    email: "alice@example.com", 
    fullName: "Alice Johnson"
);

// The UserRegisteredEvent is now stored in the OutboxMessages table
// and will be published to RabbitMQ by the background processor
```

## HealthCheckService

The `HealthCheckService` is a background service that continuously monitors the health of the outbox pattern system. It periodically checks message processing rates, failure patterns, and resource usage to detect issues like high failure rates, stuck messages, or dead letter accumulation. When issues are detected, it raises alerts that can be retrieved via `GetActiveAlerts()` for monitoring and alerting purposes.

The service runs in the background and caches health status for quick access by other components or health check endpoints.

### Example Usage

```csharp
// Register the health check service in Program.cs
builder.Services.AddHostedService<HealthCheckService>();
builder.Services.AddSingleton<HealthCheckOptions>(options => new HealthCheckOptions
{
    CheckIntervalMs = 300000, // 5 minutes
    HighFailureRateThreshold = 0.15, // 15% failure rate
    StuckMessageThreshold = 200, // 200 stuck messages
    DeadLetterThreshold = 100 // 100 dead letters
});

// In a background service or application component
public class MonitoringService : BackgroundService
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(HealthCheckService healthCheckService, ILogger<MonitoringService> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Check for active alerts
            var alerts = _healthCheckService.GetActiveAlerts();
            
            if (alerts.Count > 0)
            {
                _logger.LogWarning("System has {AlertCount} active alerts:", alerts.Count);
                foreach (var alert in alerts)
                {
                    _logger.LogWarning("- [{Type}] {Message} (Raised: {RaisedAt})", 
                        alert.Type, alert.Message, alert.RaisedAt);
                }
            }
            else
            {
                _logger.LogInformation("System is healthy - no active alerts");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

// Example: Configure alert thresholds based on environment
var healthCheckOptions = new HealthCheckOptions
{
    CheckIntervalMs = Environment.GetEnvironmentVariable("HEALTH_CHECK_INTERVAL_MINUTES") switch
    {
        var interval when int.TryParse(interval, out var minutes) => minutes * 60 * 1000,
        _ => 300000 // 5 minutes default
    },
    HighFailureRateThreshold = Environment.GetEnvironmentVariable("FAILURE_RATE_THRESHOLD") switch
    {
        var rate when double.TryParse(rate, out var threshold) => threshold,
        _ => 0.10 // 10% default
    },
    StuckMessageThreshold = int.TryParse(Environment.GetEnvironmentVariable("STUCK_MESSAGE_THRESHOLD"), out var stuckThreshold) 
        ? stuckThreshold
        : 100, // 100 messages default
    DeadLetterThreshold = int.TryParse(Environment.GetEnvironmentVariable("DEAD_LETTER_THRESHOLD"), out var dlqThreshold)
        ? dlqThreshold
        : 50 // 50 messages default
};

builder.Services.AddSingleton(healthCheckOptions);
```

## RateLimitingMiddleware

The `RateLimitingMiddleware` class implements a sliding window token bucket rate limiting algorithm to protect your API from abuse and ensure fair resource allocation across clients. It tracks request rates per client identifier (IP address or API key) and rejects requests that exceed configured limits, returning appropriate HTTP 429 responses with retry-after headers.

The middleware uses a concurrent dictionary to track rate limits per client with automatic cleanup of expired entries to prevent memory leaks. Rate limiting headers (`X-RateLimit-Limit` and `X-RateLimit-Remaining`) are added to responses to enable client-side monitoring and retry logic.

### Example Usage

```csharp
// Configure rate limiting in Program.cs with custom options
builder.Services.AddRateLimiting(options =>
{
    options.RequestsPerWindow = 500;  // Allow 500 requests per window
    options.WindowSeconds = 30;        // 30-second sliding window
});

// Register the middleware in the pipeline
app.UseRateLimiting();

// Example: Register with default options (1000 requests per 60 seconds)
app.UseRateLimiting();

// Example: Custom configuration for a specific endpoint
app.Map("/api/orders", orderApp =>
{
    orderApp.UseRateLimiting(new RateLimitingOptions
    {
        RequestsPerWindow = 100,   // More restrictive for order endpoints
        WindowSeconds = 60
    });
    
    orderApp.MapGet("/", () => "Order API");
});

// Example: Using API key authentication with rate limiting
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
    {
        // Validate API key
        if (IsValidApiKey(apiKey))
        {
            await next();
        }
        else
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API key");
        }
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("API key required");
    }
});

app.UseRateLimiting();
```

## IdempotencyKeyGenerator

The `IdempotencyKeyGenerator` class provides deterministic methods for generating idempotency keys across various message types and scenarios. Idempotency keys ensure exactly-once message processing by creating consistent, unique identifiers that prevent duplicate processing of the same logical operation. This is critical for distributed systems where message delivery may be unreliable or retries may occur.

The generator produces keys that are:
- **Deterministic**: Same input parameters always produce the same key
- **Unique**: Different events generate different keys
- **Collision-free**: Designed to avoid false duplicates
- **Readable**: Human-readable format for debugging and logging

### Example Usage

```csharp
// Generate an idempotency key for entity creation
var creationKey = IdempotencyKeyGenerator.ForEntityCreation("order", Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa"));
Console.WriteLine($"Creation key: {creationKey}");
// Output: creation key: order-create-3fa85f6457174562b3fc2c963f66afa

// Generate a key for state transition
var transitionKey = IdempotencyKeyGenerator.ForStateTransition(
    "order", 
    Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa"), 
    "confirm");
Console.WriteLine($"Transition key: {transitionKey}");
// Output: transition key: order-confirm-3fa85f6457174562b3fc2c963f66afa

// Generate a key for webhook retry attempts
var webhookKey = IdempotencyKeyGenerator.ForWebhookAttempt("webhook-123", 3);
Console.WriteLine($"Webhook key: {webhookKey}");
// Output: webhook key: webhook-webhook-123-attempt-3

// Generate a timestamped key for events with temporal ordering
var timestampKey = IdempotencyKeyGenerator.ForTimestampedEvent(
    "payment.processed", 
    Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa"),
    DateTime.UtcNow);
Console.WriteLine($"Timestamp key: {timestampKey}");

// Generate a key for external API calls
var apiKey = IdempotencyKeyGenerator.ForExternalApiCall(
    "stripe", 
    "req_1234567890");
Console.WriteLine($"API key: {apiKey}");
// Output: api key: api-stripe-req_1234567890
```

## DeadLetterHandlingExample

The `DeadLetterHandlingExample` class demonstrates comprehensive dead letter queue (DLQ) management for handling failed message processing in distributed systems. It provides tools for monitoring unreviewed dead letters, investigating failed messages, implementing manual review workflows, and automated recovery strategies for transient errors. This example is essential for maintaining system reliability when messages repeatedly fail processing.


The example includes:
- **DeadLetterMonitor**: Periodically checks for unreviewed dead letters and logs them
- **AutomatedDeadLetterRecovery**: Implements recovery strategies for transient errors like timeouts and connection issues
- **DeadLetterHealthCheck**: Health check for monitoring/alerting on DLQ state

### Example Usage

```csharp
// Setup services in Program.cs
var services = new ServiceCollection();
services.AddLogging();
services.AddOutboxPattern(connectionString);
services.AddScoped<DeadLetterMonitor>();
services.AddScoped<AutomatedDeadLetterRecovery>();
services.AddScoped<DeadLetterHealthCheck>();

// Register health checks
services.AddHealthChecks()
    .AddCheck<DeadLetterHealthCheck>("dead_letter_queue");

// Example: Monitor dead letters from a background service
public class DeadLetterMonitoringService : BackgroundService
{
    private readonly DeadLetterMonitor _monitor;
    private readonly ILogger<DeadLetterMonitoringService> _logger;

    public DeadLetterMonitoringService(DeadLetterMonitor monitor, ILogger<DeadLetterMonitoringService> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Checking for unreviewed dead letters...");
            await _monitor.MonitorDeadLettersAsync();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

// Example: Investigate and manually review a dead letter
var monitor = new DeadLetterMonitor(deadLetterService, logger);
var investigationResult = await monitor.InvestigateDeadLetterAsync(deadLetterId);
Console.WriteLine(investigationResult);

await monitor.ReviewDeadLetterAsync(deadLetterId, "Investigated - issue appears to be transient");

// Example: Requeue a dead letter after fixing the underlying issue
await monitor.RequeueDeadLetterAsync(deadLetterId, "Fixed database connection timeout issue");

// Example: Run automated recovery periodically
await new AutomatedDeadLetterRecovery(deadLetterService, logger)
    .AttemptAutomaticRecoveryAsync();

// Example: Check DLQ health status
var (isHealthy, message) = await new DeadLetterHealthCheck(deadLetterService)
    .CheckHealthAsync();

if (!isHealthy)
{
    _logger.LogWarning("DLQ health check failed: {Message}", message);
}
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