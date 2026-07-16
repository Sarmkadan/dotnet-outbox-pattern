# dotnet-outbox-pattern

## SerializationHelperTests

The `SerializationHelperTests` class provides comprehensive unit tests for the `SerializationHelper` class, verifying JSON serialization and deserialization behavior with camelCase property naming, null value omission, and validation utilities. These tests ensure reliable serialization of domain objects, statistics, and health metrics for storage and transmission.

### Example Usage

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;

class Program
{
  static void Main()
  {
    // Serialize domain objects with camelCase property names
    var stats = new OutboxStatistics
    {
      TotalMessages = 100,
      PublishedMessages = 90,
      FailedMessages = 10
    };
    
    string json = SerializationHelper.Serialize(stats);
    Console.WriteLine(json);
    // Output: {"totalMessages":100,"publishedMessages":90,"failedMessages":10}
    
    // Deserialize back to object
    var deserialized = SerializationHelper.Deserialize<OutboxStatistics>(json);
    Console.WriteLine($"Total: {deserialized.TotalMessages}");
    
    // Serialize health metrics (nulls omitted)
    var metrics = new HealthMetrics
    {
      IsHealthy = false,
      ConsecutiveFailures = 3,
      ErrorMessage = "Connection timeout"
    };
    
    string metricsJson = SerializationHelper.Serialize(metrics);
    Console.WriteLine(metricsJson);
    
    // Pretty print for debugging
    string prettyJson = SerializationHelper.SerializePretty(stats);
    Console.WriteLine(prettyJson);
    
    // Validate JSON
    bool isValid = SerializationHelper.IsValidJson(json);
    Console.WriteLine($"Valid JSON: {isValid}");
  }
}
```

Transactional outbox pattern for .NET: persist domain changes and outgoing messages in the 
same SQL Server transaction, then let a background processor deliver them with retries, 
idempotency keys and a dead-letter queue.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full picture: component breakdown, 
write/read pipeline, locking and retry semantics, design decisions with their trade-offs, 
extension points and known limitations. Short version: `IOutboxService` writes messages to 
the outbox table, the `OutboxProcessor` background service polls in batches (with optional 
idle backoff), pushes them through your `IMessagePublisher` implementation, and parks 
exhausted messages in a dead-letter table with a review/requeue API.

## OutboxMessageTests

The `OutboxMessageTests` class provides comprehensive unit tests for the `OutboxMessage` domain model, verifying its core logic, state transitions, and validation rules. These tests ensure reliable handling of message publishing attempts, locking, and retry policies.

### Example Usage

```csharp
using DotnetOutboxPattern.Domain;

// Creating and validating a message
var message = new OutboxMessage
{
    Id = Guid.NewGuid(),
    IdempotencyKey = "key-001",
    AggregateId = "agg-1",
    AggregateType = "Order",
    EventType = EventType.Created,
    EventData = "{\"orderId\":\"1\"}",
    EventTypeName = "OrderCreatedEvent",
    Topic = "orders.created"
};

message.Validate();

// Simulating state transitions
message.Lock(TimeSpan.FromMinutes(5));
message.RecordFailure("Connection failed");
if (message.CanRetry())
{
    // Retry logic...
}
message.MarkAsPublished();
```

## StringHelperTests

The `StringHelperTests` class provides a set of unit tests for the string helper methods, including hash computation, email validation, string truncation, and string formatting. These tests verify the correctness of various string operations, ensuring that the helper methods behave as expected.

### Example Usage

```csharp
using DotnetOutboxPattern.Tests;

class Program
{
    static void Main()
    {
        var tests = new StringHelperTests();

        // Test hash computation
        tests.ComputeSha256Hash_ReturnsExpectedHash();

        // Test email validation
        tests.IsValidEmail_ReturnsExpectedResult();

        // Test string truncation
        tests.Truncate_ReturnsExpectedString();

        // Test string formatting
        tests.ToSlug_ReturnsSlugifiedString();
        tests.ToKebabCase_ReturnsKebabCaseString();
    }
}
```

## EnumsTests

The `EnumsTests` class verifies that the enum types used throughout the library have the correct numeric values, string representations, and expected behaviours. It ensures that any changes to the enums are caught early by checking their underlying values and `ToString()` output.

### Example Usage

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Tests;

class Program
{
    static void Main()
    {
        var tests = new EnumsTests();

        // Verify enum numeric values
        tests.OutboxMessageState_Values_MatchExpected();
        tests.EventType_Values_MatchExpected();
        tests.DeliveryGuarantee_Values_MatchExpected();
        tests.RetryPolicyType_Values_MatchExpected();

        // Verify ToString() representations
        tests.OutboxMessageState_ToString_ReturnsCorrectString(OutboxMessageState.Pending, "Pending");
        tests.EventType_ToString_ReturnsCorrectString(EventType.Created, "Created");
        tests.DeliveryGuarantee_ToString_ReturnsCorrectString(DeliveryGuarantee.AtLeastOnce, "AtLeastOnce");
        tests.RetryPolicyType_ToString_ReturnsCorrectString(RetryPolicyType.NoRetry, "NoRetry");

        // Additional enum behaviour checks
        tests.OutboxMessageState_HasExpectedDescriptionAttributes();
        tests.EventType_HasExpectedValues();
    }
}
```

## ConstantsTests

The `ConstantsTests` class provides unit tests for the various constant values and configuration parameters used throughout the outbox pattern implementation. It verifies that default values are sensible, standard topics are correctly defined, log property names are consistent, error codes are comprehensive, and HTTP headers follow conventions. These tests ensure that the library's constants remain stable and predictable.

### Example Usage

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Tests;

class Program
{
    static void Main()
    {
        var tests = new ConstantsTests();

        // Verify default constant values
        tests.OutboxConstants_DefaultValues_MatchExpected();
        tests.OutboxConstants_ValidationConstants_AreReasonable();

        // Verify standard topics
        tests.StandardTopics_ContainsExpectedTopicNames();

        // Verify log property names
        tests.LogProperties_ContainsExpectedPropertyNames();

        // Verify error codes
        tests.ErrorCodes_ContainsExpectedErrorCodes();

        // Verify HTTP headers
        tests.HttpHeaders_ContainsExpectedHeaderNames();
    }
}
```

## BatchProcessingServiceTests

The `BatchProcessingServiceTests` class provides comprehensive unit tests for the `BatchProcessingService` that verify batch processing behavior, chunking logic, error handling, and metrics tracking. These tests ensure reliable processing of outbox messages in configurable batch sizes with proper guard clauses and cancellation support.

### Example Usage

```csharp
using System.Threading.Tasks;
using DotnetOutboxPattern.Tests;
using Microsoft.Extensions.Logging;
using Moq;

class Program
{
    static async Task Main()
    {
        // Setup mocks
        var publishingServiceMock = new Mock<IMessagePublishingService>();
        var loggerMock = new Mock<ILogger<BatchProcessingService>>();

        // Create test instance with default options
        var test = new BatchProcessingServiceTests();

        // Guard clause tests
        test.Constructor_WithNullPublishingService_ThrowsArgumentNullException();
        test.Constructor_WithNullOptions_ThrowsArgumentNullException();

        // Basic batch processing scenarios
        await test.ProcessInChunksAsync_WithDefaultSize_DividesIntoChunks();
        await test.ProcessInChunksAsync_WithCustomTotal_RespectsCustomSize();
        await test.ProcessInChunksAsync_WithSingleMessage_CreatesOneChunk();

        // Error handling and metrics
        await test.ProcessInChunksAsync_WhenServiceThrows_CatchesAndReturnsFailure();
        await test.ProcessInChunksAsync_TracksCumulativeMetrics();
        await test.ProcessInChunksAsync_SetsDurationCorrectly();

        // Scheduled message processing
        await test.ProcessScheduledInChunksAsync_DelegatesToPublishingService();
    }
}
```

## BatchProcessingModelsTests

The `BatchProcessingModelsTests` class provides comprehensive unit tests for the batch processing models and DTOs used throughout the outbox pattern implementation. These tests verify the default values, property initialization, and calculation logic for `BatchProcessingOptions`, `BatchChunkResult`, and `BatchProcessingSummary` classes, ensuring reliable batch processing configuration and metrics tracking.

### Example Usage

```csharp
using System;
using DotnetOutboxPattern.Domain;

class Program
{
    static void Main()
    {
        // Configure batch processing options with default values
        var defaultOptions = new BatchProcessingOptions();
        Console.WriteLine($"Default batch size: {defaultOptions.TotalBatchSize}");
        Console.WriteLine($"Default chunk size: {defaultOptions.ChunkSize}");
        Console.WriteLine($"Default parallel chunks: {defaultOptions.MaxParallelChunks}");

        // Configure custom batch processing options
        var customOptions = new BatchProcessingOptions
        {
            TotalBatchSize = 5000,
            ChunkSize = 200,
            MaxParallelChunks = 4,
            EnableParallelChunks = true,
            DelayBetweenChunksMs = 100,
            StopOnChunkFailure = true
        };

        // Create batch chunk result with default constructor
        var chunkResult = new BatchChunkResult();
        Console.WriteLine($"Chunk {chunkResult.ChunkIndex} processed: {chunkResult.Success}");

        // Create batch chunk result with custom values
        var startTime = DateTime.UtcNow;
        var completedTime = startTime.AddSeconds(2);
        var result = new BatchChunkResult
        {
            ChunkIndex = 5,
            Success = true,
            ProcessedCount = 25,
            FailedCount = 2,
            ErrorMessage = "Some messages failed",
            StartedAt = startTime,
            CompletedAt = completedTime
        };
        Console.WriteLine($"Chunk {result.ChunkIndex} duration: {result.Duration.TotalSeconds:F2}s");

        // Create batch processing summary with default constructor
        var summary = new BatchProcessingSummary();
        Console.WriteLine($"Total chunks: {summary.TotalChunks}");

        // Create batch processing summary with accumulated results
        var processingSummary = new BatchProcessingSummary
        {
            Success = false,
            TotalProcessed = 80,
            TotalFailed = 5,
            TotalChunks = 2,
            SuccessfulChunks = 1,
            FailedChunks = 1,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddSeconds(5)
        };
        
        // Accumulate chunk results
        processingSummary.Accumulate(new BatchChunkResult
        {
            ChunkIndex = 1,
            Success = true,
            ProcessedCount = 50,
            FailedCount = 0,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddSeconds(1)
        });
        
        processingSummary.Accumulate(new BatchChunkResult
        {
            ChunkIndex = 2,
            Success = false,
            ProcessedCount = 30,
            FailedCount = 5,
            ErrorMessage = "Connection timeout",
            StartedAt = DateTime.UtcNow.AddSeconds(2),
            CompletedAt = DateTime.UtcNow.AddSeconds(4)
        });
        
        Console.WriteLine($"Processing completed in {processingSummary.Duration.TotalSeconds:F2}s");
        Console.WriteLine($"Total: {processingSummary.TotalProcessed}, Failed: {processingSummary.TotalFailed}");
    }
}
```

## DefaultMessagePublisherTests

The `DefaultMessagePublisherTests` class contains unit tests that verify the behavior of the `DefaultMessagePublisher` implementation. It checks constructor guard clauses, successful publishing, logging of message details and event types, cancellation handling, and publishing of multiple messages. The class also validates the `MessagePublisherFactory`'s ability to create a functional logging publisher.

### Example Usage

```csharp
using System;
using System.Threading.Tasks;
using DotnetOutboxPattern.Tests;
using Microsoft.Extensions.Logging;
using Moq;

class Program
{
    static async Task Main()
    {
        // Instantiate the test class (its constructor sets up a mock logger and the SUT)
        var publisherTests = new DefaultMessagePublisherTests();

        // Guard‑clause test for the constructor
        publisherTests.Constructor_WithNullLogger_ThrowsArgumentNullException();

        // Basic publishing scenarios
        await publisherTests.PublishAsync_WithValidMessage_CompletesSuccessfully();
        await publisherTests.PublishAsync_WithValidMessage_LogsMessageDetails();
        await publisherTests.PublishAsync_WithNullMessage_DoesNotThrow();
        await publisherTests.PublishAsync_RespectsCancellationToken();

        // Multiple messages and event‑type logging
        await publisherTests.PublishAsync_MultipleMessages_PublishesEach();
        await publisherTests.PublishAsync_LogsEventType();

        // Factory tests for a logging publisher
        var factoryTests = new MessagePublisherFactoryTests();
        factoryTests.CreateLoggingPublisher_ReturnsValidPublisher();
        await factoryTests.LoggingPublisher_PublishesMessage();
    }
}
```

## OutboxServiceTests

The `OutboxServiceTests` class provides comprehensive unit tests for the `OutboxService` class, which manages outbox message processing, event publishing, retry mechanisms, and message retrieval operations. These tests verify proper parameter validation, idempotency handling, state transitions, and repository delegation for all service operations.

### Example Usage

```csharp
using System;
using System.Threading.Tasks;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using DotnetOutboxPattern.Tests;
using Microsoft.Extensions.Logging;
using Moq;

class Program
{
    static async Task Main()
    {
        // Setup mocks
        var repositoryMock = new Mock<IOutboxRepository>();
        var loggerMock = new Mock<ILogger<OutboxService>>();
        var serializerMock = new Mock<IOutboxSerializer>();

        // Create service instance
        var service = new OutboxService(repositoryMock.Object, loggerMock.Object, serializerMock.Object);

        // Guard clause tests
        service.Constructor_WithNullRepository_ThrowsArgumentNullException();
        service.Constructor_WithNullLogger_ThrowsArgumentNullException();

        // Event publishing scenarios
        var domainEvent = new EntityCreatedEvent { EntityId = "order-123", EntityType = "Order" };
        var publishable = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "orders.created",
            DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce,
            MaxAttempts = 3
        };

        var publishedMessage = await service.PublishEventAsync(publishable);

        // Message retrieval
        var messageId = publishedMessage.Id;
        var retrievedMessage = await service.GetMessageAsync(messageId);

        // Statistics
        var statistics = await service.GetStatisticsAsync();

        // Retrieval by various criteria
        var allMessages = await service.GetAllMessagesAsync();
        var topicMessages = await service.GetMessagesByTopicAsync("orders.created");
        var aggregateMessages = await service.GetMessagesByAggregateAsync("order-123");
        var stateMessages = await service.GetMessagesByStateAsync(OutboxMessageState.Pending);

        // Retry failed message
        var retryResult = await service.RetryFailedMessageAsync(messageId);

        // Retrieval by date range
        var dateRangeMessages = await service.GetMessagesByDateRangeAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow
        );
    }
}
```

## EventsTests

The `EventsTests` class provides comprehensive unit tests for the domain event hierarchy and related event models in the outbox pattern implementation. These tests verify proper initialization, property setting, and validation of various event types including domain events, entity lifecycle events, custom events, notification events, and publishable events. The test suite ensures that all event models correctly handle optional properties like correlation IDs, causation IDs, user IDs, and payload data structures.

### Example Usage

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Tests;

class Program
{
  static void Main()
  {
    // Test default constructor initializes required properties
    var domainEvent = new TestDomainEvent();
    Console.WriteLine($"Event ID: {domainEvent.EventId}");
    Console.WriteLine($"Occurred At: {domainEvent.OccurredAt}");

    // Test setting optional properties
    var domainEventWithProps = new TestDomainEvent
    {
      CorrelationId = "corr-123",
      CausationId = "caus-456",
      UserId = "user-789"
    };
    Console.WriteLine($"Correlation ID: {domainEventWithProps.CorrelationId}");
    Console.WriteLine($"Causation ID: {domainEventWithProps.CausationId}");
    Console.WriteLine($"User ID: {domainEventWithProps.UserId}");

    // Test entity lifecycle events
    var createdEvent = new EntityCreatedEvent
    {
      EntityId = "order-123",
      EntityType = "Order",
      EntityData = new Dictionary<string, object>
      {
        { "id", "order-123" },
        { "amount", 100.50 },
        { "customerId", "cust-456" }
      }
    };
    Console.WriteLine($"Created Event - Entity: {createdEvent.EntityType} {createdEvent.EntityId}");

    var updatedEvent = new EntityUpdatedEvent
    {
      EntityId = "order-123",
      EntityType = "Order",
      OldData = new Dictionary<string, object> { { "status", "pending" } },
      NewData = new Dictionary<string, object> { { "status", "completed" } },
      ChangedProperties = new List<string> { "status" }
    };
    Console.WriteLine($"Updated Event - Changes: {string.Join(", ", updatedEvent.ChangedProperties)}");

    var deletedEvent = new EntityDeletedEvent
    {
      EntityId = "order-123",
      EntityType = "Order",
      DeletedData = new Dictionary<string, object>
      {
        { "id", "order-123" },
        { "amount", 100.50 },
        { "status", "completed" }
      }
    };
    Console.WriteLine($"Deleted Event - Data preserved: {deletedEvent.DeletedData.Count} items");

    // Test custom domain event
    var customEvent = new CustomDomainEvent
    {
      EventName = "OrderStatusChanged",
      AggregateId = "order-123",
      AggregateType = "Order",
      Payload = new Dictionary<string, object>
      {
        { "oldStatus", "pending" },
        { "newStatus", "completed" },
        { "orderId", "order-123" }
      }
    };
    Console.WriteLine($"Custom Event - {customEvent.EventName}: {customEvent.AggregateId}");

    // Test notification event
    var notificationEvent = new NotificationEvent
    {
      NotificationType = "Email",
      RecipientId = "user-789",
      Subject = "Order Confirmation",
      Body = "Your order has been confirmed",
      IsCritical = true,
      ActionUrl = "https://example.com/orders/123"
    };
    Console.WriteLine($"Notification - {notificationEvent.NotificationType}: {notificationEvent.Subject}");

    // Test publishable event for outbox integration
    var publishableEvent = new PublishableEvent
    {
      Event = new TestDomainEvent(),
      Topic = "orders.created",
      PartitionKey = "order-123",
      MaxAttempts = 3,
      DeliveryGuarantee = DeliveryGuarantee.ExactlyOnce
    };
    Console.WriteLine($"Publishable Event - Topic: {publishableEvent.Topic}, Max Attempts: {publishableEvent.MaxAttempts}");

    // Test default constructor for publishable event
    var defaultPublishable = new PublishableEvent();
    Console.WriteLine($"Default Publishable - Max Attempts: {defaultPublishable.MaxAttempts}, Delivery: {defaultPublishable.DeliveryGuarantee}");
  }
}
```

## MessagePublishingServiceTests

The `MessagePublishingServiceTests` class provides comprehensive unit tests for the `MessagePublishingService` that verify message publishing behavior, batch processing, retry mechanisms, locking semantics, and dead-letter queue handling. These tests ensure reliable message delivery with proper error handling, idempotency checks, and state management for outbox messages.

### Example Usage

```csharp
using System;
using System.Threading.Tasks;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using DotnetOutboxPattern.Tests;
using Microsoft.Extensions.Logging;
using Moq;

class Program
{
    static async Task Main()
    {
        // Setup mocks
        var outboxRepositoryMock = new Mock<IOutboxRepository>();
        var publisherMock = new Mock<IMessagePublisher>();
        var loggerMock = new Mock<ILogger<MessagePublishingService>>();
        var batchServiceMock = new Mock<IBatchProcessingService>();

        // Create test instance
        var publishingTests = new MessagePublishingServiceTests(
            outboxRepositoryMock.Object,
            publisherMock.Object,
            loggerMock.Object,
            batchServiceMock.Object
        );

        // Guard clause tests
        publishingTests.Constructor_WithNullOutboxRepository_ThrowsArgumentNullException();
        publishingTests.Constructor_WithNullPublisher_ThrowsArgumentNullException();

        // Basic message processing scenarios
        await publishingTests.ProcessPendingMessagesAsync_WithEmptyBatch_ReturnsZeroProcessed();
        await publishingTests.ProcessPendingMessagesAsync_WithSingleMessage_PublishesAndMarksPublished();

        // Error handling and retry logic
        await publishingTests.ProcessPendingMessagesAsync_WhenPublisherThrows_RecordsFailureAndContinues();
        await publishingTests.ProcessSingleMessageAsync_WhenMessageLocked_ReturnsFalse();
        await publishingTests.ProcessSingleMessageAsync_WhenMessageCanRetry_AttemptsPublish();
        await publishingTests.ProcessSingleMessageAsync_WhenReachedMaxRetries_MovesToDlq();

        // Scheduled message processing
        await publishingTests.ProcessScheduledMessagesAsync_WithFutureSchedule_DoesNotProcess();
        await publishingTests.ProcessScheduledMessagesAsync_WithPastSchedule_ProcessesMessages();

        // Lock management
        await publishingTests.ReleaseLockAsync_UnlocksExpiredMessage();
        await publishingTests.ReleaseLockAsync_WhenMessageNotFound_DoesNotThrow();
        await publishingTests.ReleaseLockAsync_WhenMessageNotLocked_DoesNotUpdate();

        // State-based processing rules
        await publishingTests.ProcessPendingMessagesAsync_WhenMessageIsLocked_DoesNotProcess();
        await publishingTests.ProcessPendingMessagesAsync_WhenMessageIsScheduled_DoesNotProcess();
        await publishingTests.ProcessSingleMessageAsync_WhenMessageIsNull_ReturnsFalse();
        await publishingTests.ProcessSingleMessageAsync_WhenMessageIsLocked_ReturnsFalse();
        await publishingTests.ProcessSingleMessageAsync_WhenMessageIsScheduled_ReturnsFalse();

        // Bulk error scenarios
        await publishingTests.ProcessPendingMessagesAsync_WhenPublisherThrowsForAllMessages_RecordsAllFailures();

        // Create a sample message for testing
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid().ToString(),
            AggregateId = "order-123",
            AggregateType = "Order",
            EventType = EventType.Created,
            EventData = "{\"orderId\":\"123\"}",
            EventTypeName = "OrderCreatedEvent",
            Topic = "orders.created",
            State = OutboxMessageState.Pending,
            CreatedAt = DateTime.UtcNow,
            LockedAt = null,
            LockedUntil = null,
            FailedAttempts = 0,
            MaxAttempts = 3,
            DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce,
            ScheduledAt = null
        };

        // Test single message processing
        var result = await publishingTests.ProcessSingleMessageAsync_WhenMessageCanRetry_AttemptsPublish(message);
        Console.WriteLine($"Message processing result: {result}");

        // Test lock expiration
        var lockedMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid().ToString(),
            State = OutboxMessageState.Pending,
            LockedAt = DateTime.UtcNow.AddMinutes(-10),
            LockedUntil = DateTime.UtcNow.AddMinutes(-5) // Expired lock
        };

        var unlocked = await publishingTests.ReleaseLockAsync_UnlocksExpiredMessage(lockedMessage.Id);
        Console.WriteLine($"Lock released: {unlocked}");
    }
}
```

## DeadLetterServiceTests

The `DeadLetterServiceTests` class provides comprehensive unit tests for the `DeadLetterService` that verify dead-letter queue management, message review workflows, health checks, and requeue operations. These tests ensure proper handling of messages that cannot be delivered, including validation of constructor parameters, error conditions, and state transitions between dead-letter and pending states.

### Example Usage

```csharp
using System;
using System.Threading.Tasks;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Tests;
using Microsoft.Extensions.Logging;
using Moq;

class Program
{
    static async Task Main()
    {
        // Setup mocks
        var dlRepositoryMock = new Mock<IDeadLetterRepository>();
        var outboxRepositoryMock = new Mock<IOutboxRepository>();
        var loggerMock = new Mock<ILogger<DeadLetterServiceTests>>();

        // Create test instance
        var deadLetterTests = new DeadLetterServiceTests(
            dlRepositoryMock.Object,
            outboxRepositoryMock.Object,
            loggerMock.Object
        );

        // Guard clause tests
        deadLetterTests.Constructor_WithNullDlRepository_ThrowsArgumentNullException();
        deadLetterTests.Constructor_WithNullOutboxRepository_ThrowsArgumentNullException();
        deadLetterTests.Constructor_WithNullLogger_ThrowsArgumentNullException();

        // Dead letter operations
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "dlq-key-001",
            AggregateId = "order-456",
            AggregateType = "Order",
            EventType = EventType.Created,
            EventData = "{\"orderId\":\"456\"}",
            EventTypeName = "OrderCreatedEvent",
            Topic = "orders.created",
            State = OutboxMessageState.Failed,
            FailedAttempts = 5,
            MaxAttempts = 3
        };

        // Move message to dead letter queue
        var dlqMessage = await deadLetterTests.MoveToDlqAsync_WithValidMessage_AddsDeadLetterAndReturnsIt(message);

        // Review dead letter message
        await deadLetterTests.ReviewAsync_WhenFound_MarksAsReviewedAndPersistsUpdate(dlqMessage.Id);

        // Check health status
        var health = await deadLetterTests.GetHealthAsync_WithUnreviewedMessages_ReturnsUnhealthyWithCount();
        Console.WriteLine($"Health status: {health.Status}, Unreviewed count: {health.UnreviewedCount}");

        // Requeue reviewed message
        var requeued = await deadLetterTests.RequeueAsync_WhenOriginalMessageExists_ResetsToPendingAndMarksRequeued(dlqMessage.Id);

        // Get unreviewed messages
        var unreviewed = await deadLetterTests.GetUnreviewedAsync();
        Console.WriteLine($"Found {unreviewed.Count} unreviewed messages");

        // Get by topic
        var topicMessages = await deadLetterTests.GetByTopicAsync("orders.created");
        Console.WriteLine($"Found {topicMessages.Count} messages for topic");

        // Delete dead letter
        await deadLetterTests.DeleteAsync(dlqMessage.Id);
    }
}
```