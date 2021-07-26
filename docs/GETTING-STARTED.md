// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Getting Started with .NET Outbox Pattern

This guide walks you through setting up and using the outbox pattern in your .NET application.

## Prerequisites

- .NET 10.0 SDK or later
- SQL Server 2019+ (LocalDB, Express, Standard)
- Git (for cloning the repository)
- Visual Studio Code or Visual Studio 2022 (optional but recommended)

## Installation Steps

### Step 1: Clone the Repository

```bash
git clone https://github.com/sarmkadan/dotnet-outbox-pattern.git
cd dotnet-outbox-pattern
```

### Step 2: Restore Dependencies

```bash
dotnet restore
```

### Step 3: Configure Database Connection

Open `appsettings.json` and update the connection string:

**For SQL Server LocalDB:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OutboxPattern;Integrated Security=true;Encrypt=false;"
}
```

**For SQL Server Express (local):**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=OutboxPattern;Integrated Security=true;Encrypt=false;"
}
```

**For Azure SQL Database:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your-server.database.windows.net;Database=OutboxPattern;User Id=your-username;Password=your-password;Encrypt=true;Connection Timeout=30;"
}
```

### Step 4: Create and Migrate the Database

```bash
# Add and apply migrations
dotnet ef database update

# Verify the database was created
# Check SQL Server Object Explorer in Visual Studio or use SQL Server Management Studio
```

### Step 5: Run the Application

```bash
dotnet run
```

You should see output like:

```
Starting Outbox Pattern application
Initializing database
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started
```

### Step 6: Access the API

- **Swagger Documentation:** https://localhost:5001/swagger
- **Health Check:** https://localhost:5001/health
- **API Base URL:** https://localhost:5001/api

## Your First Event

### Step 1: Define an Event

Create a new file `Domain/Events/OrderCreatedEvent.cs`:

```csharp
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

### Step 2: Publish an Event

Using the Swagger UI:

1. Navigate to https://localhost:5001/swagger
2. Click on `POST /api/outbox/events`
3. Click "Try it out"
4. Enter the request body:

```json
{
  "event": {
    "eventType": "order.created",
    "version": 1,
    "orderId": "ORD-123",
    "customerId": "CUST-456",
    "amount": 99.99,
    "createdAt": "2024-01-15T10:00:00Z"
  },
  "topic": "orders.created",
  "partitionKey": "CUST-456",
  "idempotencyKey": "order-ORD-123-create"
}
```

5. Click "Execute"
6. You should see a response with the message ID

### Step 3: Verify the Message

Using the Swagger UI:

1. Click on `GET /api/outbox/statistics`
2. Click "Try it out" and execute
3. You should see:
   - `pendingCount`: 1
   - `publishedCount`: 0 (or higher if processor already ran)

The outbox processor runs every 5 seconds by default. After a few seconds, check statistics again - you should see the pending count decrease.

## Running with Docker

### Prerequisites

- Docker Desktop installed
- Docker daemon running

### Quick Start

```bash
# Start SQL Server and the application
docker-compose up

# The API is available at http://localhost:5001
# Swagger UI: http://localhost:5001/swagger
```

### Stopping the Services

```bash
# Stop and remove containers
docker-compose down

# Stop but keep data
docker-compose stop

# Resume
docker-compose start
```

## Project Structure

After cloning, you'll see this structure:

```
dotnet-outbox-pattern/
├── Domain/                 # Domain models and events
│   ├── Enums.cs           # State enums, retry policies
│   ├── OutboxMessage.cs   # Core entity
│   ├── DeadLetter.cs      # Failed message entity
│   ├── Events.cs          # Domain event hierarchy
│   └── Models.cs          # Supporting models
│
├── Data/                   # Data access layer
│   ├── OutboxDbContext.cs # Entity Framework context
│   ├── OutboxRepository.cs
│   └── DeadLetterRepository.cs
│
├── Services/               # Business logic
│   ├── OutboxService.cs
│   ├── MessagePublishingService.cs
│   └── DeadLetterService.cs
│
├── Infrastructure/         # Supporting services
│   ├── DefaultMessagePublisher.cs
│   ├── OutboxProcessor.cs
│   └── SerializationHelper.cs
│
├── Controllers/            # REST API endpoints
├── Configuration/          # Dependency injection setup
├── Program.cs              # Application entry point
├── appsettings.json        # Configuration
└── DotnetOutboxPattern.csproj

examples/                   # Complete working examples
docs/                       # Documentation files
tests/                      # Unit and integration tests (optional)
```

## Configuration

Key settings in `appsettings.json`:

```json
{
  "Outbox": {
    "ProcessorEnabled": true,      // Enable background processing
    "BatchSize": 100,              // Messages per batch
    "DelayBetweenBatches": 5000,   // Milliseconds
    "MaxRetries": 5,               // Before moving to DLQ
    "RetryPolicy": "ExponentialBackoff",
    "DeliveryGuarantee": "AtLeastOnce"
  }
}
```

## Common Tasks

### Viewing Messages in the Database

```bash
# Using SQL command line
sqlcmd -S localhost\SQLEXPRESS -d OutboxPattern -Q "SELECT TOP 10 Id, Topic, State, CreatedAt FROM OutboxMessages ORDER BY CreatedAt DESC"
```

Or using Visual Studio's SQL Server Object Explorer:
1. View → SQL Server Object Explorer
2. Connect to local database
3. Browse `OutboxMessages` and `DeadLetters` tables

### Checking Logs

```bash
# View real-time logs
tail -f logs/outbox-*.txt

# Search for errors
grep -i error logs/outbox-*.txt

# Search for a specific message ID
grep "7b2a1c3d-4e5f" logs/outbox-*.txt
```

### Processing Messages Manually

If the processor is disabled, you can trigger processing via:

```bash
# Using HTTP
curl -X POST https://localhost:5001/api/outbox/process

# Or in C#
var processor = serviceProvider.GetRequiredService<OutboxProcessor>();
await processor.ProcessBatchAsync(CancellationToken.None);
```

### Handling Dead Letters

```bash
# Get unreviewed dead letters
curl https://localhost:5001/api/deadletters/unreviewed

# Review one
curl -X PUT https://localhost:5001/api/deadletters/{deadLetterId}/review \
  -H "Content-Type: application/json" \
  -d '{"reviewNotes":"Database issue resolved"}'

# Requeue for retry
curl -X PUT https://localhost:5001/api/deadletters/{deadLetterId}/requeue \
  -H "Content-Type: application/json" \
  -d '{"reason":"Upstream service is back online"}'
```

## Creating a Custom Message Publisher

The `DefaultMessagePublisher` just logs messages. To actually publish to a broker, create a custom implementation:

```csharp
// Infrastructure/MyMessagePublisher.cs
using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Domain;

public class MyMessagePublisher : IMessagePublisher
{
    private readonly ILogger<MyMessagePublisher> _logger;

    public MyMessagePublisher(ILogger<MyMessagePublisher> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Publishing message {MessageId} to {Topic}",
                message.Id, message.Topic);

            // Your publishing logic here
            await MyBroker.SendAsync(message.Topic, message.EventData);

            _logger.LogInformation("Message published successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {MessageId}", message.Id);
            throw;
        }
    }
}

// Register in Program.cs
builder.Services.AddMessagePublisher<MyMessagePublisher>();
```

## Next Steps

1. **Read the Architecture Guide:** [docs/ARCHITECTURE.md](ARCHITECTURE.md)
2. **Explore Examples:** Check the `/examples` directory
3. **API Reference:** See [docs/API-REFERENCE.md](API-REFERENCE.md)
4. **Deployment:** Read [docs/DEPLOYMENT.md](DEPLOYMENT.md)

## Troubleshooting

### "Execution Timeout Expired"
- Database is overloaded
- Increase timeout in connection string: `Connection Timeout=60;`
- Reduce `BatchSize` in config

### "Could not find a part of the path"
- Database initialization failed
- Verify `appsettings.json` connection string
- Check SQL Server is running: `sqlcmd -?`

### "The specified named connection 'DefaultConnection' not found"
- Check `appsettings.json` spelling
- Ensure correct JSON format (no trailing commas)

## Support

- **Issues:** https://github.com/sarmkadan/dotnet-outbox-pattern/issues
- **Discussions:** https://github.com/sarmkadan/dotnet-outbox-pattern/discussions
- **Documentation:** See `/docs` folder

