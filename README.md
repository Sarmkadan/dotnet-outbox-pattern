# .NET Outbox Pattern

A production-ready implementation of the **transactional outbox pattern** for .NET 10, providing guaranteed message delivery, deduplication, ordering, and dead letter handling.

## Overview

The outbox pattern is a proven architecture for ensuring reliable message publishing in distributed systems. This implementation guarantees:

- **Guaranteed Delivery**: Messages are persisted before publishing
- **Deduplication**: Idempotency keys prevent duplicate processing
- **Ordering**: Partition keys maintain message order within aggregates
- **Dead Letter Handling**: Failed messages are moved to a review queue
- **Distributed Processing**: Background processor handles message publishing
- **Lock Management**: Prevents concurrent processing of the same message

## Features

### Core Capabilities
- Transactional outbox message storage
- Configurable retry policies (Fixed, Linear, Exponential backoff)
- Partition-based ordering for causally dependent messages
- Dead letter queue (DLQ) for failed messages with operator review
- Health metrics and statistics
- Message archival and cleanup

### Domain Models
- **OutboxMessage**: Core entity for reliable message publishing
- **DeadLetter**: Failed messages awaiting operator review
- **Domain Events**: Strongly-typed event hierarchy
- **PublishableEvent**: Configuration wrapper for publishing

### Services
- **IOutboxService**: Main API for publishing events
- **IMessagePublishingService**: Message processing and delivery
- **IDeadLetterService**: Dead letter queue management
- **IMessagePublisher**: Extensible interface for message brokers

### Infrastructure
- Entity Framework Core with SQL Server support
- Configurable background processor
- JSON serialization helpers
- Custom exception hierarchy
- Serilog integration for structured logging

## Project Structure

```
dotnet-outbox-pattern/
├── Domain/                    # Domain models and events
│   ├── Enums.cs              # OutboxMessageState, EventType, etc.
│   ├── OutboxMessage.cs      # Core outbox entity
│   ├── DeadLetter.cs         # Dead letter entity
│   ├── Events.cs             # Domain event hierarchy
│   └── Models.cs             # Supporting models and statistics
├── Data/                      # Data access layer
│   ├── OutboxDbContext.cs    # EF Core DbContext
│   ├── OutboxRepository.cs   # Outbox CRUD and queries
│   └── DeadLetterRepository.cs # Dead letter operations
├── Services/                  # Business logic
│   ├── OutboxService.cs      # Event publishing API
│   ├── MessagePublishingService.cs # Message processing
│   └── DeadLetterService.cs  # DLQ management
├── Configuration/             # Dependency injection
│   └── ServiceCollectionExtensions.cs
├── Infrastructure/            # Supporting utilities
│   ├── DefaultMessagePublisher.cs # Message broker interface
│   ├── SerializationHelper.cs    # JSON serialization
│   └── OutboxProcessor.cs        # Background service
├── Exceptions/                # Custom exception types
│   └── OutboxExceptions.cs
├── Program.cs                 # Application entry point
├── appsettings.json          # Configuration
└── DotnetOutboxPattern.csproj # Project file
```

## Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB or full edition)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/sarmkadan/dotnet-outbox-pattern.git
cd dotnet-outbox-pattern
```

2. Update connection string in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your-server;Database=OutboxPattern;Trusted_Connection=true;"
}
```

3. Create and migrate the database:
```bash
dotnet ef database update
```

4. Run the application:
```bash
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger documentation at `/swagger`.

## Usage

### Publishing Events

```csharp
var outboxService = serviceProvider.GetRequiredService<IOutboxService>();

// Create and publish an event
var customerEvent = new EntityCreatedEvent
{
    EntityId = "CUST-123",
    EntityType = "Customer",
    EntityData = new Dictionary<string, object>
    {
        { "Name", "John Doe" },
        { "Email", "john@example.com" }
    }
};

var message = await outboxService.PublishEventAsync(
    customerEvent,
    topic: "customer.events",
    partitionKey: "CUST-123");
```

### Custom Message Publisher

Replace `DefaultMessagePublisher` with your broker implementation:

```csharp
public class RabbitMqPublisher : IMessagePublisher
{
    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var channel = _connection.CreateModel();
        await channel.BasicPublishAsync(
            exchange: message.Topic,
            routingKey: message.Topic,
            body: Encoding.UTF8.GetBytes(message.EventData),
            cancellationToken: cancellationToken);
    }
}

// Register in Program.cs
builder.Services.AddMessagePublisher<RabbitMqPublisher>();
```

### Managing Dead Letters

```csharp
var dlService = serviceProvider.GetRequiredService<IDeadLetterService>();

// Get unreviewed dead letters
var unreviewed = await dlService.GetUnreviewedAsync();

// Review a dead letter
await dlService.ReviewAsync(deadLetterId, "Checked with team - retry recommended");

// Requeue for retry
await dlService.RequeueAsync(deadLetterId, "Fixed upstream issue");
```

## API Endpoints

### Outbox Operations
- `GET /api/outbox/statistics` - Get outbox statistics
- `GET /api/outbox/messages/{messageId}` - Get message details
- `POST /api/outbox/events` - Publish an event
- `GET /health` - Health check

### Dead Letter Queue
- `GET /api/deadletters/unreviewed` - Get unreviewed dead letters
- `PUT /api/deadletters/{deadLetterId}/review` - Mark as reviewed
- `PUT /api/deadletters/{deadLetterId}/requeue` - Requeue for retry

## Configuration

Key configuration options in `appsettings.json`:

```json
"Outbox": {
  "ProcessorEnabled": true,
  "BatchSize": 100,
  "DelayBetweenBatches": 5000,
  "MaxRetries": 5,
  "RetryPolicy": "ExponentialBackoff",
  "DeliveryGuarantee": "AtLeastOnce",
  "PublishTimeoutSeconds": 30
}
```

## Performance Considerations

1. **Batch Processing**: Adjust `BatchSize` based on message volume
2. **Partition Ordering**: Use partition keys to maintain order for related events
3. **Lock Duration**: Set appropriately for message processing time
4. **Database Indexes**: Leverages indexes on State, IdempotencyKey, AggregateId, Topic
5. **Archive Strategy**: Regularly archive published messages to maintain performance

## Testing

Use the in-memory `DefaultMessagePublisher` or the Logging publisher for testing:

```csharp
var testPublisher = MessagePublisherFactory.CreateLoggingPublisher(logger);
```

## Thread Safety

- All repository operations are thread-safe
- Lock management prevents concurrent processing
- Message locking uses distributed locking at the database level
- Background processor uses scoped services per batch

## License

MIT License - See LICENSE file for details.

## Author

**Vladyslav Zaiets** - CTO & Software Architect
- Website: https://sarmkadan.com
- Email: rutova2@gmail.com

## Contributing

Contributions are welcome! Please follow the code style and include tests for new features.

## Support

For issues, questions, or feature requests, please open an issue on GitHub.
