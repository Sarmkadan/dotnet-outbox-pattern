# dotnet-outbox-pattern

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

// existing content ...
