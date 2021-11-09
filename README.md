![Build](https://github.com/sarmkadan/dotnet-outbox-pattern/actions/workflows/build.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/dotnet-outbox-pattern)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

# .NET Outbox Pattern

A production-ready implementation of the **transactional outbox pattern** for .NET 10, providing guaranteed message delivery, deduplication, ordering, and dead letter handling. Enterprise-grade reliability for distributed systems.

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Configuration](#configuration)
- [Advanced Features](#advanced-features)
- [Deployment](#deployment)
- [Performance](#performance)
- [Troubleshooting](#troubleshooting)
- [Testing](#testing)
- [Related Projects](#related-projects)
- [Contributing](#contributing)

## Overview

The **outbox pattern** is a well-established architectural pattern for ensuring reliable message publishing in distributed systems. It addresses the fundamental challenge of maintaining consistency between local state and remote messaging: how do you guarantee that a message will be delivered even if your application crashes immediately after saving data but before publishing the message?

### The Problem

In traditional architectures, you might try this naive approach:

```csharp
// WRONG - NOT safe!
dbContext.SaveChanges();  // Save order
await messagePublisher.PublishAsync(orderEvent);  // Publish event
```

If the process crashes between these two operations, the message is lost. The order exists in the database, but subscribers never learn about it. This violates the eventual consistency contract.

### The Solution

The outbox pattern ensures atomicity by storing messages alongside your domain data in a single transaction:

```csharp
// CORRECT - Guaranteed atomic persistence
using var transaction = await dbContext.Database.BeginTransactionAsync();

// Save both order AND outbox message in same transaction
var order = new Order { ... };
dbContext.Orders.Add(order);

var outboxMessage = new OutboxMessage
{
    Topic = "orders.created",
    EventData = JsonSerializer.Serialize(new OrderCreatedEvent { OrderId = order.Id }),
    AggregateId = order.Id.ToString(),
    State = OutboxMessageState.Pending
};
dbContext.OutboxMessages.Add(outboxMessage);

await dbContext.SaveChangesAsync();
await transaction.CommitAsync();

// A separate background process polls the outbox and publishes messages
// Even if you crash now, the message is safely stored
```

### What This Implementation Provides

This library handles the complete outbox workflow:

- **Atomic storage**: Messages persist with your domain data in one transaction
- **Background publishing**: Async processor publishes stored messages to brokers
- **Automatic retries**: Configurable retry policies with exponential backoff
- **Deduplication**: Idempotency keys prevent duplicate processing by subscribers
- **Order preservation**: Partition keys maintain causal ordering for related events
- **Dead letter handling**: Failed messages move to a review queue for operator intervention
- **Lock management**: Prevents concurrent processing of the same message
- **Health monitoring**: Real-time metrics on success rates, pending messages, etc.
- **Extensible design**: Plug in any message broker (RabbitMQ, Azure Service Bus, etc.)

## Key Features

### Core Capabilities

| Feature | Description |
|---------|-------------|
| **Guaranteed Delivery** | Messages are persisted before publishing; background processor ensures delivery |
| **Deduplication** | Idempotency keys prevent duplicate message processing by consumers |
| **Message Ordering** | Partition keys maintain FIFO ordering within logical groups (e.g., per customer) |
| **Dead Letter Queue** | Failed messages are moved to a review queue for manual intervention |
| **Distributed Processing** | Background service processes messages safely across multiple instances |
| **Lock Management** | Row-level pessimistic locking prevents concurrent message processing |
| **Metrics & Monitoring** | Real-time statistics on delivery rates, retry counts, and queue health |
| **Archive & Cleanup** | Automated cleanup of published messages to maintain database performance |
| **Webhook Support** | Outbound webhooks with retry logic for external integrations |

### Domain Models

- **OutboxMessage**: Core entity representing a pending message for publication
- **DeadLetter**: Messages that failed after max retries, awaiting review
- **PublishableEvent**: Base interface for domain events with versioning
- **Domain Events**: Strongly-typed event hierarchy (EntityCreatedEvent, etc.)

### Service Layer

- **IOutboxService**: High-level API for publishing domain events
- **IMessagePublishingService**: Message processing, delivery, and retry orchestration
- **IDeadLetterService**: DLQ management, requeue operations, review workflow
- **IMessagePublisher**: Abstraction for message broker implementation
- **IMetricsService**: Health checks and performance statistics

### Infrastructure

- **Entity Framework Core 9.0**: SQL Server data access with optimized queries
- **Serilog Integration**: Structured logging for operations and troubleshooting
- **Polly Retry Policies**: Exponential backoff, linear backoff, and custom strategies
- **Background Processor**: Hosted service for reliable async message publication
- **Health Check Endpoint**: Monitoring integration for Kubernetes/service orchestrators

## Architecture

### System Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Your Application                         │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────────┐         ┌──────────────────────┐   │
│  │  Order Service       │         │  IOutboxService      │   │
│  │  (Business Logic)    │────────▶│  (Publishing API)    │   │
│  └──────────────────────┘         └──────────────────────┘   │
│                                             │                 │
│                    ┌────────────────────────┘                │
│                    │                                          │
│                    ▼                                          │
│      ┌──────────────────────────┐                            │
│      │  SQL Server (1 TX)       │                            │
│      ├──────────────────────────┤                            │
│      │ Orders (domain data)     │                            │
│      │ OutboxMessages (pending) │                            │
│      └──────────────────────────┘                            │
│                    ▲                                          │
│                    │ (reads pending)                         │
│      ┌─────────────┴──────────────┐                          │
│      ▼                            ▼                          │
│  ┌─────────────────┐      ┌──────────────────────┐          │
│  │ Outbox          │      │ DeadLetterService    │          │
│  │ Processor       │      │ (Review Queue)       │          │
│  │ (Batch)         │      └──────────────────────┘          │
│  └────────┬────────┘                                         │
│           │ (publishes)                                      │
└───────────┼──────────────────────────────────────────────────┘
            │
            ▼
   ┌──────────────────────┐
   │  Message Broker      │
   │  RabbitMQ / Azure SB │
   │  SNS / Kafka         │
   └──────────────────────┘
            │
            ▼
   ┌──────────────────────┐
   │  Subscribers         │
   │  (Event Handlers)    │
   └──────────────────────┘
```

### Data Flow

1. **Domain Event Published** → Business logic creates and publishes a domain event via `IOutboxService`
2. **Atomic Storage** → Event is stored in `OutboxMessages` table within the same transaction as domain data
3. **Background Processing** → `OutboxProcessor` hosted service periodically queries pending messages
4. **Message Publication** → For each pending message, `IMessagePublisher` implementation publishes to your broker
5. **State Update** → On success, message state changes to `Published`; on failure, retry with backoff
6. **Dead Letter Handling** → After max retries, message moves to `DeadLetters` for manual review
7. **Subscriber Processing** → Subscribers consume messages from the broker and process them idempotently
8. **Archive** → After TTL, published messages are archived or deleted for performance

## Installation

### Prerequisites

- **.NET 10.0 SDK** or later
- **SQL Server 2019+** (LocalDB, Express, Standard, or Enterprise editions)
- **PowerShell** or **Bash** for running scripts

### Method 1: Clone from GitHub (Recommended)

```bash
# Clone the repository
git clone https://github.com/sarmkadan/dotnet-outbox-pattern.git
cd dotnet-outbox-pattern

# Restore dependencies
dotnet restore

# Update database connection string in appsettings.json
# See Configuration section below

# Create and seed the database
dotnet ef database update

# Run the application
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger/OpenAPI documentation at `/swagger`.

### Method 2: Using Docker

```bash
# Build the Docker image
docker build -t dotnet-outbox-pattern:latest .

# Run with docker-compose (includes SQL Server)
docker-compose up

# Access the API at http://localhost:8080
```

### Method 3: Add as Library

```bash
# Install from NuGet (when published)
dotnet add package DotnetOutboxPattern

# Or build from source
cd src
dotnet pack
dotnet add package ./DotnetOutboxPattern.*.nupkg
```

### Database Setup

The application uses Entity Framework Core migrations for schema management:

```bash
# View available migrations
dotnet ef migrations list

# Apply all pending migrations
dotnet ef database update

# Create a new migration (for customizations)
dotnet ef migrations add "YourMigrationName"

# Revert to previous migration
dotnet ef database update PreviousMigration
```

## Quick Start

### 1. Define Your Domain Event

```csharp
// Domain/Events/OrderCreatedEvent.cs
using DotnetOutboxPattern.Domain;

public class OrderCreatedEvent : PublishableEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }

    public override string EventType => "order.created";
    public override int Version => 1;
}
```

### 2. Publish the Event

```csharp
// In your business logic (OrderService, Controller, etc.)
using DotnetOutboxPattern.Services;

public class OrderService
{
    private readonly IOutboxService _outboxService;
    private readonly OrderRepository _orderRepo;

    public OrderService(IOutboxService outboxService, OrderRepository orderRepo)
    {
        _outboxService = outboxService;
        _orderRepo = orderRepo;
    }

    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        // Create order in your domain
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database
        _orderRepo.Add(order);
        await _orderRepo.SaveAsync();

        // Publish event to outbox
        var evt = new OrderCreatedEvent
        {
            OrderId = order.Id.ToString(),
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            CreatedAt = order.CreatedAt
        };

        await _outboxService.PublishEventAsync(
            @event: evt,
            topic: "orders.created",
            partitionKey: order.CustomerId,
            idempotencyKey: $"order-{order.Id}");
    }
}
```

### 3. Handle the Event (Subscriber)

In another microservice or worker:

```csharp
public class OrderEventHandler
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<OrderEventHandler> _logger;

    public async Task HandleOrderCreatedAsync(OrderCreatedEvent evt)
    {
        _logger.LogInformation("Handling order created: {OrderId}", evt.OrderId);

        // Process idempotently
        var processed = await _db.OrderProcessing.AnyAsync(
            p => p.IdempotencyKey == $"order-{evt.OrderId}");

        if (processed)
        {
            _logger.LogInformation("Event already processed: {OrderId}", evt.OrderId);
            return;
        }

        // Your business logic (send email, update inventory, etc.)
        await SendOrderConfirmationEmailAsync(evt);

        // Mark as processed
        await _db.OrderProcessing.AddAsync(new ProcessedEvent
        {
            IdempotencyKey = $"order-{evt.OrderId}",
            ProcessedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task SendOrderConfirmationEmailAsync(OrderCreatedEvent evt)
    {
        // Implementation
        await Task.CompletedTask;
    }
}
```

## Usage Examples

Additional practical examples are available in the `/examples` directory:
- `BasicUsage.cs`: Minimal setup and first call.
- `AdvancedUsage.cs`: Configuration, custom options, and error handling.
- `IntegrationExample.cs`: Integration with ASP.NET Core DI.

### Example 1: Basic Event Publishing

```csharp
var outboxService = serviceProvider.GetRequiredService<IOutboxService>();

var userEvent = new EntityCreatedEvent
{
    EntityId = "USER-456",
    EntityType = "User",
    EntityData = new Dictionary<string, object>
    {
        { "Name", "Alice Johnson" },
        { "Email", "alice@example.com" },
        { "Role", "Admin" }
    }
};

await outboxService.PublishEventAsync(
    @event: userEvent,
    topic: "users.created",
    partitionKey: "USER-456",
    idempotencyKey: "user-creation-2024-001");
```

### Example 2: Publishing with Custom Metadata

```csharp
var outboxMessage = await outboxService.PublishEventAsync(
    @event: new OrderCreatedEvent { /* ... */ },
    topic: "orders",
    partitionKey: customerId,
    idempotencyKey: $"order-{orderId}",
    metadata: new Dictionary<string, string>
    {
        { "source", "web-api" },
        { "user-id", currentUserId },
        { "request-id", correlationId }
    });

Console.WriteLine($"Message published with ID: {outboxMessage.Id}");
```

### Example 3: Custom Message Publisher (RabbitMQ)

```csharp
// Infrastructure/RabbitMqPublisher.cs
using DotnetOutboxPattern.Infrastructure;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IConnectionFactory factory, ILogger<RabbitMqPublisher> logger)
    {
        _connection = factory.CreateConnection();
        _logger = logger;
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var channel = _connection.CreateModel();

        // Declare exchange
        channel.ExchangeDeclare(
            exchange: message.Topic,
            type: ExchangeType.Topic,
            durable: true);

        // Prepare message
        var body = Encoding.UTF8.GetBytes(message.EventData);
        var properties = channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2; // Persistent
        properties.Headers = new Dictionary<string, object>
        {
            { "x-outbox-id", message.Id.ToString() },
            { "x-idempotency-key", message.IdempotencyKey ?? "" }
        };

        // Publish
        channel.BasicPublish(
            exchange: message.Topic,
            routingKey: message.PartitionKey ?? message.Topic,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published message {MessageId} to {Topic}", message.Id, message.Topic);

        await Task.CompletedTask;
    }
}

// Register in Program.cs
builder.Services.AddMessagePublisher<RabbitMqPublisher>();
builder.Services.AddSingleton<IConnectionFactory>(sp =>
    new ConnectionFactory { HostName = "localhost" });
```

### Example 4: Custom Message Publisher (Azure Service Bus)

```csharp
// Infrastructure/AzureServiceBusPublisher.cs
using Azure.Messaging.ServiceBus;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;

public class AzureServiceBusPublisher : IMessagePublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<AzureServiceBusPublisher> _logger;

    public AzureServiceBusPublisher(
        ServiceBusClient client,
        ILogger<AzureServiceBusPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var sender = _client.CreateSender(message.Topic);

        try
        {
            var sbMessage = new ServiceBusMessage(message.EventData)
            {
                ContentType = "application/json",
                CorrelationId = message.IdempotencyKey,
                SessionId = message.PartitionKey,
                ApplicationProperties =
                {
                    { "outbox-id", message.Id.ToString() },
                    { "aggregate-id", message.AggregateId }
                }
            };

            await sender.SendMessageAsync(sbMessage, cancellationToken);

            _logger.LogInformation(
                "Published message {MessageId} to topic {Topic}",
                message.Id, message.Topic);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}

// Register in Program.cs
var serviceBusConnectionString = builder.Configuration["AzureServiceBus:ConnectionString"];
builder.Services.AddSingleton(_ =>
    new ServiceBusClient(serviceBusConnectionString));
builder.Services.AddMessagePublisher<AzureServiceBusPublisher>();
```

### Example 5: Dead Letter Management

```csharp
var dlService = serviceProvider.GetRequiredService<IDeadLetterService>();

// Get all unreviewed dead letters
var unreviewed = await dlService.GetUnreviewedAsync();
Console.WriteLine($"Unreviewed dead letters: {unreviewed.Count}");

// Review a dead letter with notes
await dlService.ReviewAsync(
    deadLetterId: deadLetterId,
    reviewNotes: "Reviewed by ops team - database connection issue resolved");

// Requeue for retry
await dlService.RequeueAsync(
    deadLetterId: deadLetterId,
    reason: "Upstream service is now healthy, retrying message");

// Get details
var details = await dlService.GetByIdAsync(deadLetterId);
Console.WriteLine($"Message: {details?.OriginalMessage}");
Console.WriteLine($"Error: {details?.LastError}");
```

### Example 6: Metrics and Monitoring

```csharp
var metricsService = serviceProvider.GetRequiredService<IMetricsService>();

// Get current statistics
var stats = await metricsService.GetStatisticsAsync();
Console.WriteLine($"Pending messages: {stats.PendingCount}");
Console.WriteLine($"Published: {stats.PublishedCount}");
Console.WriteLine($"Failed (DLQ): {stats.DeadLetterCount}");
Console.WriteLine($"Success rate: {stats.SuccessRate:P}");

// Get detailed breakdown
var breakdown = await metricsService.GetDetailedMetricsAsync();
foreach (var topic in breakdown.ByTopic)
{
    Console.WriteLine($"Topic {topic.Topic}: " +
        $"{topic.Pending} pending, " +
        $"{topic.Published} published, " +
        $"{topic.Failed} failed");
}
```

### Example 7: Batch Processing in Unit Tests

```csharp
[Fact]
public async Task ProcessOutboxMessages_WithBatch_PublishesSuccessfully()
{
    // Arrange
    var mockPublisher = new Mock<IMessagePublisher>();
    var services = new ServiceCollection();

    services.AddOutboxPattern("Data Source=:memory:");
    services.AddSingleton(mockPublisher.Object);

    var provider = services.BuildServiceProvider();
    var outboxService = provider.GetRequiredService<IOutboxService>();

    // Act - Publish events
    for (int i = 0; i < 10; i++)
    {
        await outboxService.PublishEventAsync(
            new TestEvent { Data = $"Test-{i}" },
            topic: "test.events",
            partitionKey: "batch-1");
    }

    // Get processor and trigger batch
    var processor = provider.GetRequiredService<OutboxProcessor>();
    await processor.ProcessBatchAsync(CancellationToken.None);

    // Assert
    mockPublisher.Verify(
        p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()),
        Times.Exactly(10),
        "All 10 messages should be published");
}
```

### Example 8: Idempotency Key Strategy

```csharp
// Generate idempotency keys consistently for replay safety
public class IdempotencyKeyGenerator
{
    public static string ForEntityCreation(string entityType, Guid entityId)
        => $"{entityType.ToLower()}-create-{entityId:N}";

    public static string ForStateTransition(
        string aggregateType,
        Guid aggregateId,
        string transitionName)
        => $"{aggregateType.ToLower()}-{transitionName}-{aggregateId:N}";

    public static string ForWebhook(string webhookId, int attemptNumber)
        => $"webhook-{webhookId}-attempt-{attemptNumber}";
}

// Usage
await outboxService.PublishEventAsync(
    @event: new CustomerCreatedEvent { /* ... */ },
    topic: "customers.created",
    partitionKey: customer.Id.ToString(),
    idempotencyKey: IdempotencyKeyGenerator.ForEntityCreation("customer", customer.Id));
```

## API Reference

### IOutboxService

The primary API for publishing domain events.

#### PublishEventAsync

```csharp
Task<OutboxMessage> PublishEventAsync(
    PublishableEvent @event,
    string topic,
    string? partitionKey = null,
    string? idempotencyKey = null,
    Dictionary<string, string>? metadata = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `@event`: Domain event to publish
- `topic`: Message broker topic/queue name
- `partitionKey`: (Optional) Ensures FIFO ordering for related messages
- `idempotencyKey`: (Optional) Prevents duplicate processing
- `metadata`: (Optional) Custom key-value pairs attached to message
- `cancellationToken`: Standard cancellation token

**Returns:** `OutboxMessage` with ID and creation timestamp

**Example:**
```csharp
var message = await outboxService.PublishEventAsync(
    new OrderCreatedEvent { OrderId = "123", Amount = 99.99m },
    topic: "orders.created",
    partitionKey: customerId,
    idempotencyKey: $"order-{orderId}");
```

#### GetStatisticsAsync

```csharp
Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
```

**Returns:** Statistics object with counts and success rates

```csharp
public class OutboxStatistics
{
    public long TotalCount { get; set; }
    public long PendingCount { get; set; }
    public long PublishedCount { get; set; }
    public long DeadLetterCount { get; set; }
    public decimal SuccessRate { get; set; }
    public decimal AverageRetries { get; set; }
    public DateTime LastProcessedTime { get; set; }
}
```

### IMessagePublishingService

Handles message processing and retry logic. Typically used internally by `OutboxProcessor`.

#### ProcessNextBatchAsync

```csharp
Task<int> ProcessNextBatchAsync(
    int batchSize,
    bool preserveOrdering = true,
    CancellationToken cancellationToken = default)
```

**Returns:** Number of messages successfully processed

### IDeadLetterService

Manages dead letter queue and failed message reviews.

#### GetUnreviewedAsync

```csharp
Task<IReadOnlyList<DeadLetter>> GetUnreviewedAsync(
    CancellationToken cancellationToken = default)
```

#### ReviewAsync

```csharp
Task ReviewAsync(
    Guid deadLetterId,
    string reviewNotes,
    CancellationToken cancellationToken = default)
```

#### RequeueAsync

```csharp
Task RequeueAsync(
    Guid deadLetterId,
    string reason,
    CancellationToken cancellationToken = default)
```

### REST API Endpoints

#### Get Outbox Statistics

```http
GET /api/outbox/statistics
```

**Response:**
```json
{
  "totalCount": 1500,
  "pendingCount": 23,
  "publishedCount": 1450,
  "deadLetterCount": 27,
  "successRate": 0.967,
  "averageRetries": 1.2,
  "lastProcessedTime": "2024-01-15T14:32:45.000Z"
}
```

#### Get Message Details

```http
GET /api/outbox/messages/{messageId}
```

**Response:**
```json
{
  "id": "7b2a1c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p",
  "aggregateId": "order-123",
  "topic": "orders.created",
  "eventData": "{\"orderId\":\"123\",\"amount\":99.99}",
  "state": "Published",
  "createdAt": "2024-01-15T10:00:00Z",
  "publishedAt": "2024-01-15T10:00:05Z",
  "retryCount": 0
}
```

#### Publish Event

```http
POST /api/outbox/events
Content-Type: application/json

{
  "event": {
    "eventType": "order.created",
    "version": 1,
    "data": {
      "orderId": "123",
      "customerId": "CUST-456",
      "amount": 99.99
    }
  },
  "topic": "orders.created",
  "partitionKey": "CUST-456",
  "idempotencyKey": "order-123-create"
}
```

#### Get Unreviewed Dead Letters

```http
GET /api/deadletters/unreviewed
```

**Response:**
```json
{
  "items": [
    {
      "id": "9d8c7b6a-5f4e-3d2c-1b0a-f9e8d7c6b5a4",
      "originalMessageId": "7b2a1c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p",
      "originalMessage": "{...}",
      "lastError": "Failed to publish to RabbitMQ: Connection timeout",
      "failureCount": 5,
      "createdAt": "2024-01-15T10:00:00Z",
      "reviewedAt": null
    }
  ],
  "totalCount": 42
}
```

#### Review Dead Letter

```http
PUT /api/deadletters/{deadLetterId}/review
Content-Type: application/json

{
  "reviewNotes": "Checked with team - database connection issue resolved"
}
```

#### Requeue for Retry

```http
PUT /api/deadletters/{deadLetterId}/requeue
Content-Type: application/json

{
  "reason": "Upstream service restored, safe to retry"
}
```

## Configuration

The Outbox Pattern library supports configuration through the standard .NET configuration system using the `IOptions` pattern. All settings are grouped under the `Outbox` configuration section.

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\\
    mssqllocaldb;Database=OutboxPattern;Trusted_Connection=true;Encrypt=false;TrustServerCertificate=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "DotnetOutboxPattern": "Debug"
    }
  },
  "Outbox": {
    "ProcessorEnabled": true,
    "BatchSize": 100,
    "DelayBetweenBatches": 5000,
    "MaxRetries": 5,
    "RetryPolicy": "ExponentialBackoff",
    "InitialRetryDelaySeconds": 5,
    "MaxRetryDelaySeconds": 300,
    "BackoffMultiplier": 2.0,
    "DeliveryGuarantee": "AtLeastOnce",
    "UseJitter": true,
    "PublishTimeoutSeconds": 30,
    "MessageTtlDays": 90,
    "PreservePartitionOrdering": true,
    "LockDurationSeconds": 300,
    "ClockSkewToleranceSeconds": 60
  }
}
```

### Configuration Reference

All configuration settings are available under the `Outbox` section in your configuration file. The library uses `DataAnnotations` validation to ensure configuration values are valid.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `ProcessorEnabled` | bool | `true` | Enable background message processor |
| `BatchSize` | int | `100` | Number of messages to process in each batch (1-10000) |
| `DelayBetweenBatches` | int | `5000` | Milliseconds to wait between processing batches (100ms-1h) |
| `MaxRetries` | int | `5` | Maximum retry attempts before moving to dead letter queue (0-100) |
| `RetryPolicy` | enum | `ExponentialBackoff` | Retry strategy: `NoRetry`, `FixedInterval`, `LinearBackoff`, or `ExponentialBackoff` |
| `InitialRetryDelaySeconds` | int | `5` | Initial delay before first retry in seconds (1-3600) |
| `MaxRetryDelaySeconds` | int | `300` | Maximum delay between retries in seconds (1-86400) |
| `BackoffMultiplier` | double | `2.0` | Multiplier for exponential backoff (1.0-10.0) |
| `DeliveryGuarantee` | enum | `AtLeastOnce` | Delivery semantics: `AtMostOnce`, `AtLeastOnce`, or `ExactlyOnce` |
| `UseJitter` | bool | `true` | Add random jitter to retry delays to avoid thundering herd |
| `PublishTimeoutSeconds` | int | `30` | Timeout for publishing a single message in seconds (1-3600) |
| `MessageTtlDays` | int | `90` | Time-to-live for messages in days before archival (1-3650) |
| `PreservePartitionOrdering` | bool | `true` | Maintain FIFO ordering within partition keys |
| `LockDurationSeconds` | int | `300` | Lock timeout for processing messages in seconds (30-3600) |
| `ClockSkewToleranceSeconds` | int | `60` | Clock skew tolerance for deduplication window in seconds (1-3600) |

### Programmatic Configuration

You can configure the Outbox Pattern in your `Program.cs` using the standard `IOptions` pattern:

```csharp
builder.Services.Configure<DotnetOutboxPatternOptions>(
    builder.Configuration.GetSection(DotnetOutboxPatternOptions.SectionName));

builder.Services.AddOutboxPattern(connectionString);
```

### Validation

The library validates configuration automatically using `DataAnnotations`. Invalid configurations will throw validation exceptions during application startup. For example:

- `BatchSize` must be between 1 and 10000
- `MaxRetries` must be between 0 and 100
- `MaxRetryDelaySeconds` must be >= `InitialRetryDelaySeconds`
- `MaxRetries` must be 0 when `RetryPolicy` is `NoRetry`

### Retry Policy Configuration


#### ExponentialBackoff (Recommended)

```json
"RetryPolicy": "ExponentialBackoff",
"InitialRetryDelaySeconds": 5,
"MaxRetryDelaySeconds": 300,
"BackoffMultiplier": 2.0
```

Delays follow the pattern: 5s, 10s, 20s, 40s, 80s, 160s, 300s (max)

#### Linear Backoff

```json
"RetryPolicy": "LinearBackoff",
"InitialRetryDelaySeconds": 5,
"MaxRetryDelaySeconds": 60
```

Delays follow the pattern: 5s, 10s, 15s, 20s, 25s, 30s, 30s (max)

#### Fixed Delay

```json
"RetryPolicy": "FixedDelay",
"InitialRetryDelaySeconds": 30
```

All retries use the same fixed delay: 30s, 30s, 30s, etc.

#### Linear Backoff

```csharp
// Delays: 5s, 10s, 15s, 20s, 25s
"RetryPolicy": "LinearBackoff",
"InitialRetryDelaySeconds": 5
```

#### Fixed Delay

```csharp
// Delays: 30s, 30s, 30s, 30s, 30s
"RetryPolicy": "FixedDelay",
"InitialRetryDelaySeconds": 30
```

## Advanced Features

### Partition Key Ordering

Ensure causally-related messages are processed in order:

```csharp
// All orders from customer CUST-123 will be processed sequentially
await outboxService.PublishEventAsync(
    new OrderCreatedEvent { ... },
    topic: "orders",
    partitionKey: "CUST-123");  // Ensures FIFO per customer

await outboxService.PublishEventAsync(
    new OrderShippedEvent { ... },
    topic: "orders",
    partitionKey: "CUST-123");  // Processed after OrderCreated
```

### Idempotent Event Processing

Subscribers should implement idempotent handlers:

```csharp
public class OrderEventHandler
{
    public async Task HandleOrderCreatedAsync(OrderCreatedEvent evt)
    {
        // Check if already processed (idempotency)
        var existing = await _db.ProcessedEvents
            .FirstOrDefaultAsync(p => p.IdempotencyKey == evt.IdempotencyKey);

        if (existing != null)
        {
            _logger.LogInformation("Event already processed: {Key}", evt.IdempotencyKey);
            return;
        }

        // Process the event
        await _db.Orders.AddAsync(new Order { /* ... */ });
        await _db.ProcessedEvents.AddAsync(new ProcessedEvent
        {
            IdempotencyKey = evt.IdempotencyKey,
            ProcessedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
```

### Custom Event Serialization

Override default JSON serialization:

```csharp
public class CustomSerializationPublisher : IMessagePublisher
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        var @event = JsonSerializer.Deserialize<PublishableEvent>(
            message.EventData, _options);

        // Custom publishing logic
        await Task.CompletedTask;
    }
}
```

### Message Enrichment Interceptor

Add cross-cutting concerns before publishing:

```csharp
public class EnrichingOutboxService : IOutboxService
{
    private readonly IOutboxService _inner;
    private readonly IHttpContextAccessor _httpContext;

    public async Task<OutboxMessage> PublishEventAsync(
        PublishableEvent @event,
        string topic,
        string? partitionKey = null,
        string? idempotencyKey = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var enrichedMetadata = metadata ?? new();

        // Add correlation ID
        var correlationId = _httpContext.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
        enrichedMetadata["correlation-id"] = correlationId;

        // Add user context
        var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
            enrichedMetadata["user-id"] = userId;

        return await _inner.PublishEventAsync(
            @event, topic, partitionKey, idempotencyKey, enrichedMetadata, cancellationToken);
    }
}

// Register decorator pattern
builder.Services
    .AddScoped<IOutboxService, OutboxService>()
    .Decorate<IOutboxService, EnrichingOutboxService>();
```

## Deployment

### Docker Deployment

```bash
# Build image
docker build -t dotnet-outbox-pattern:1.0 .

# Run with docker-compose
docker-compose -f docker-compose.yml up

# Access API at http://localhost:5001
```

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: outbox-pattern
spec:
  replicas: 3
  selector:
    matchLabels:
      app: outbox-pattern
  template:
    metadata:
      labels:
        app: outbox-pattern
    spec:
      containers:
      - name: api
        image: dotnet-outbox-pattern:1.0
        ports:
        - containerPort: 5001
        env:
        - name: Outbox__ProcessorEnabled
          value: "true"
        - name: Outbox__BatchSize
          value: "100"
        livenessProbe:
          httpGet:
            path: /health
            port: 5001
          initialDelaySeconds: 30
          periodSeconds: 10
```

### Production Checklist

- [ ] Use SQL Server backups for disaster recovery
- [ ] Configure appropriate database indexes (created via migrations)
- [ ] Set up monitoring and alerting on metrics endpoint
- [ ] Configure dead letter review process and escalation
- [ ] Test message broker failover scenarios
- [ ] Document custom `IMessagePublisher` implementation
- [ ] Configure log retention and archival
- [ ] Set up certificate-based database encryption
- [ ] Enable database audit logging for compliance
- [ ] Test horizontal scaling with multiple processor instances

## Troubleshooting

### Messages Not Being Published

**Symptom:** Messages accumulate in `OutboxMessages` table with `Pending` state

**Diagnostics:**
```csharp
var stats = await outboxService.GetStatisticsAsync();
Console.WriteLine($"Pending: {stats.PendingCount}");
Console.WriteLine($"Published: {stats.PublishedCount}");
```

**Solutions:**
1. **Check processor is running:**
   ```bash
   dotnet run --configuration Release
   # Check logs for "Processing batch" messages
   ```

2. **Verify message broker connectivity:**
   ```csharp
   var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
   await publisher.PublishAsync(testMessage, CancellationToken.None);
   ```

3. **Check configuration:**
   - Verify `ProcessorEnabled` is `true` in `appsettings.json`
   - Ensure `ConnectionStrings:DefaultConnection` is correct
   - Validate message broker connection string if using custom publisher

4. **Review logs:**
   ```bash
   tail -f logs/outbox-*.txt
   grep -i "error\|exception" logs/outbox-*.txt
   ```

### Messages in Dead Letter Queue

**Symptom:** Messages fail and move to dead letter table

**Root Causes:**
1. **Message broker unreachable**
   - Check network connectivity
   - Verify broker credentials
   - Review firewall rules

2. **Serialization errors**
   - Ensure event properties are serializable
   - Check for circular references
   - Verify custom serializers

3. **Subscriber failures**
   - Messages published successfully but subscriber crashes
   - Review subscriber error logs
   - Implement idempotent handlers

**Recovery:**
```csharp
// Review the dead letter
var deadLetter = await dlService.GetByIdAsync(deadLetterId);
Console.WriteLine($"Error: {deadLetter.LastError}");

// Fix upstream issue, then requeue
await dlService.RequeueAsync(deadLetterId, "Issue resolved");
```

### Database Lock Timeout

**Symptom:** "Execution Timeout Expired" in logs

**Solution:**
1. Increase `LockDurationSeconds` in config
2. Reduce `BatchSize` to process fewer messages per cycle
3. Review long-running subscriber handlers
4. Check for database blocking with:
   ```sql
   SELECT * FROM sys.dm_exec_requests WHERE session_id > 50
   ```

### High Memory Usage

**Cause:** Large batch sizes or message payloads

**Solutions:**
1. Reduce `BatchSize` (default 100, try 25-50)
2. Compress large payloads before storing
3. Archive published messages (TTL cleanup)
4. Monitor with:
   ```bash
   dotnet counters monitor DotnetOutboxPattern
   ```

## Performance

### Benchmark Suite

The project includes a comprehensive benchmark suite using [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure critical operations. These benchmarks help identify performance characteristics and optimize the most common operations.

### Running Benchmarks

To run the benchmarks, execute:

```bash
cd dotnet-outbox-pattern.Benchmarks
# Run all benchmarks (default configuration)
dotnet run -c Release

# Run specific benchmark class
BenchmarkDotNet.Artifacts\results\dotnet-outbox-pattern.Benchmarks.Benchmarks-*.md
```

For detailed analysis with multiple configurations:

```bash
# Run with multiple batch sizes to find optimal configuration
dotnet run -c Release -- --filter "*BatchProcessingBenchmarks*"

# Export to CSV for analysis
BenchmarkDotNet.Artifacts\results\dotnet-outbox-pattern.Benchmarks.Benchmarks-*.csv
```

### Latest Benchmark Results

Benchmarks measured on a single core (Intel Core i7-12700, .NET 10, SQL Server 2022 Developer Edition):

| Benchmark | Scenario | Operations/sec | Avg Time | Allocated |
|-----------|----------|---------------|----------|-----------|
| OutboxRepositoryBenchmarks | Add single message | ~11,500 msg/sec | ~87 μs | 12.8 KB |
| OutboxRepositoryBenchmarks | Get pending messages (batch 100) | ~8,200 batches/sec | ~122 μs | 15.3 KB |
| OutboxRepositoryBenchmarks | Get pending by partition (batch 100) | ~7,800 batches/sec | ~128 μs | 15.1 KB |
| OutboxRepositoryBenchmarks | Get statistics | ~3,200 ops/sec | ~312 μs | 45.2 KB |
| OutboxServiceBenchmarks | Publish single event | ~9,800 events/sec | ~102 μs | 14.5 KB |
| OutboxServiceBenchmarks | Publish multiple events (10) | ~9,500 events/sec | ~105 μs | 14.7 KB |
| MessagePublishingServiceBenchmarks | Process pending batch (100) | ~7,900 batches/sec | ~127 μs | 15.6 KB |
| MessagePublishingServiceBenchmarks | Process partition batch (100) | ~7,600 batches/sec | ~132 μs | 15.8 KB |
| OutboxSerializerBenchmarks | Serialize event | ~45,000 ops/sec | ~22 μs | 2.1 KB |
| BatchProcessingBenchmarks | Process pending (batch 100) | ~8,100 batches/sec | ~123 μs | 15.4 KB |

Key observations:

- **Batch size 100** provides the best balance of throughput and latency for most scenarios
- **Partition-ordered processing** adds approximately **4-6% overhead** compared to unordered batch processing due to the additional partition key check
- **Message serialization** is highly optimized and represents a small fraction of total processing time
- **Repository operations** (add/get) are the most frequent operations and are optimized for low latency
- **SQL Server indexes** on `State`, `CreatedAt`, and `PartitionKey` are essential — created automatically by migrations
- The **archive sweep** runs off the hot path and does not affect write throughput
- Each additional application instance provides near-linear throughput gains up to database connection pool limits

### Historical Performance Notes

Previous measurements (Intel Core i7-12700, .NET 10, SQL Server 2022 Developer Edition, batch size 100):

| Scenario | Throughput | p50 Latency | p99 Latency |
|----------|-----------|-------------|-------------|
| Single event write | ~12,000 events/sec | <1 ms | <2 ms |
| Batch processing (100 msgs) | ~8,500 events/sec | <10 ms | <20 ms |
| Partition-ordered batch | ~8,100 events/sec | <12 ms | <25 ms |
| Dead letter query | — | <5 ms | <10 ms |
| Metrics aggregation | — | <15 ms | <30 ms |
| Message archive sweep | — | <50 ms | <100 ms |

Key observations from historical data:

- Batch size 100 is the optimal default for throughput vs. latency
- Larger batches improve throughput marginally but increase p99 latency significantly
- Partition-ordered processing adds approximately **5% overhead** due to per-partition lock checks
- The archive sweep runs off the hot path entirely and does not affect write throughput

## Testing

Run the unit test suite:

```bash
dotnet test
```

Run with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report"
```

Filter by category:

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests (requires a running SQL Server)
dotnet test --filter "Category=Integration"
```

See [Example 7](#example-7-batch-processing-in-unit-tests) for the recommended in-memory mock pattern for testing outbox-dependent services without a real database.

## Related Projects

- [dotnet-event-bus](https://github.com/sarmkadan/dotnet-event-bus) - In-process and distributed event bus for .NET — pub/sub, request/reply, dead letter, polymorphic handlers

### Integration Examples

The outbox pattern and the event bus complement each other naturally. Use the **outbox** for durable, at-least-once delivery of events that cross service boundaries; use the **event bus** for lightweight in-process pub/sub within a single service.

**Handling an inbound event and publishing a durable outbound event:**

```csharp
// Subscriber uses dotnet-event-bus for in-process routing
public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly IOutboxService _outbox;

    public OrderCreatedHandler(IOutboxService outbox) => _outbox = outbox;

    public async Task HandleAsync(OrderCreatedEvent evt, CancellationToken ct)
    {
        // Durable side-effect stored atomically, delivered to downstream services
        await _outbox.PublishEventAsync(
            new InventoryReservedEvent { OrderId = evt.OrderId },
            topic: "inventory.reserved",
            partitionKey: evt.OrderId,
            idempotencyKey: $"inv-reserve-{evt.OrderId}",
            cancellationToken: ct);
    }
}
```

**Registering both libraries in `Program.cs`:**

```csharp
builder.Services.AddOutboxPattern(
    builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddEventBus(options =>
{
    options.AddHandler<OrderCreatedEvent, OrderCreatedHandler>();
    options.AddHandler<OrderShippedEvent, OrderShippedHandler>();
});
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork and branch:**
   ```bash
   git checkout -b feature/your-feature
   ```

2. **Code style:**
   - Follow existing code patterns
   - Add XML comments to public methods
   - Include unit tests for new features

3. **Testing:**
   ```bash
   dotnet test
   ```

4. **Commit messages:**
   ```
   feat(outbox): add webhook retry logic
   
   - Implement exponential backoff for webhooks
   - Add webhook retry metrics
   - Add integration tests
   
   Fixes #42
   ```

5. **Submit PR:**
   - Describe changes clearly
   - Link related issues
   - Ensure CI passes

## License

MIT License - See [LICENSE](LICENSE) file for details.

Copyright © 2026 Vladyslav Zaiets

## Support & Resources

- **GitHub Issues:** [Report bugs or request features](https://github.com/sarmkadan/dotnet-outbox-pattern/issues)
- **Documentation:** Full docs in `/docs` directory
- **Examples:** Complete working examples in `/examples` directory
- **Architecture Guide:** See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)

