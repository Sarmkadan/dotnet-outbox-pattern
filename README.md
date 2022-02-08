// existing content ...

## DeadLetterAdditionalTests

The `DeadLetterAdditionalTests` class provides additional comprehensive tests for the `DeadLetter` domain model. It verifies the correctness of various methods, including copying properties from an `OutboxMessage`, marking a dead letter as reviewed or requeued, and initializing a dead letter with all properties.

### Example Usage

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Tests;
using System;

class Program
{
    static void Main()
    {
        var tests = new DeadLetterAdditionalTests();

        // Test copying properties from an OutboxMessage
        tests.FromOutboxMessage_WithAllProperties_CopiesCorrectly();

        // Test marking a dead letter as reviewed
        tests.MarkAsReviewed_WithEmptyNotes_SetsReviewedProperties();

        // Test marking a dead letter as requeued
        tests.MarkAsRequeued_WithEmptyReason_SetsRequeueProperties();
    }
}
```

## ModelsTests

The `ModelsTests` class provides a comprehensive set of unit tests for the domain models and DTOs used in the outbox message processing system. These tests verify the correctness of model initialization, property validation, and business logic across key types such as `OutboxProcessingResult`, `OutboxProcessorConfig`, `OutboxStatistics`, `PublishingOptions`, and `HealthMetrics`.

### Example Usage

```csharp
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Tests;
using System;

class Program
{
    static void Main()
    {
        var tests = new ModelsTests();

        // Test OutboxProcessingResult functionality
        tests.OutboxProcessingResult_DefaultConstructor_InitializesCollections();
        tests.OutboxProcessingResult_Duration_ReturnsCorrectTimeSpan();

        // Test OutboxProcessorConfig functionality
        tests.OutboxProcessorConfig_DefaultValues_AreCorrect();
        tests.OutboxProcessorConfig_CustomValues_AreApplied();

        // Test OutboxStatistics functionality
        tests.OutboxStatistics_DefaultValues_AreCorrect();
        tests.OutboxStatistics_SuccessRate_CalculatesCorrectly();
        tests.OutboxStatistics_SuccessRate_WithZeroTotal_ReturnsZero();

        // Test PublishingOptions functionality
        tests.PublishingOptions_DefaultValues_AreCorrect();
        tests.PublishingOptions_CustomValues_AreApplied();

        // Test HealthMetrics functionality
        tests.HealthMetrics_DefaultValues_AreCorrect();
        tests.HealthMetrics_UpdateProperties_WorksCorrectly();
    }
}
```

## MessageContextTests

The `MessageContextTests` class provides a set of unit tests for the `MessageContext` class, which is responsible for managing activities and events in the outbox message processing service. These tests verify the correctness of various methods, including getting or creating correlation and causation IDs, starting activities, recording events, and disposing of scopes.

### Example Usage

```csharp
using DotnetOutboxPattern.Tests;

class Program
{
    static void Main()
    {
        var tests = new MessageContextTests();

        // Run individual test methods
        tests.GetOrCreateCorrelationId_ReturnsValidGuidString();
        tests.GetOrCreateCausationId_WithActivity_ReturnsCurrentActivityId();
        tests.StartActivity_WithMessage_SetsCorrectTags();
        tests.RecordEvent_AddsEventToActivity();
        tests.RecordException_SetsExceptionTags();

        // Run all test methods
        tests.Run();
    }
}
```

## BatchProcessingBenchmarks

The `BatchProcessingBenchmarks` class provides a set of performance benchmarks for the outbox message processing service. It sets up an in-memory database, pre-loads a batch of messages, and measures the time taken to process pending messages or process a specific partition. The benchmarks are intended to help developers understand the throughput and latency characteristics of the outbox implementation under realistic workloads.

### Example

```csharp
using DotnetOutboxPattern.Benchmarks;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var benchmarks = new BatchProcessingBenchmarks { BatchSize = 100 };

        // Prepare the benchmark environment
        benchmarks.Setup();

        // Run the benchmark methods
        await benchmarks.ProcessPendingMessages();
        await benchmarks.ProcessPartitionMessages();

        // Clean up resources
        benchmarks.Cleanup();
        benchmarks.Dispose();
    }
}
```

## OutboxRepositoryBenchmarks

The `OutboxRepositoryBenchmarks` class provides performance benchmarks for the outbox repository operations, measuring the efficiency of message persistence and retrieval operations. It sets up a SQL Server database, initializes the outbox schema, and benchmarks common repository methods such as adding messages, retrieving pending messages in batches, checking message statistics, and counting pending messages.

### Example

```csharp
using DotnetOutboxPattern.Benchmarks;
using DotnetOutboxPattern.Domain;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var benchmarks = new OutboxRepositoryBenchmarks();

        // Prepare the benchmark environment
        benchmarks.Setup();

        // Add a single outbox message
        await benchmarks.AddSingleMessage();

        // Retrieve pending messages in batches of 100
        await benchmarks.GetPendingMessages_Batch100();

        // Retrieve pending messages for a specific partition
        await benchmarks.GetPendingMessagesByPartition_Batch100();

        // Get statistics about pending messages
        await benchmarks.GetStatistics();

        // Get count of pending messages
        await benchmarks.GetPendingCount();

        // Clean up resources
        benchmarks.Cleanup();
        benchmarks.Dispose();
    }
}
```

## OutboxServiceBenchmarks

The `OutboxServiceBenchmarks` class provides performance benchmarks for the outbox service operations, measuring the efficiency of message publishing and retrieval operations. It sets up a SQL Server database with the outbox schema and benchmarks common service methods such as publishing single events, publishing multiple events sequentially, retrieving message statistics, and fetching individual messages by ID.

### Example

```csharp
using DotnetOutboxPattern.Benchmarks;
using DotnetOutboxPattern.Domain;
using System;
using System.Threading.Tasks;

class Program
{
  static async Task Main()
  {
    var benchmarks = new OutboxServiceBenchmarks();

    // Prepare the benchmark environment
    benchmarks.Setup();

    // Publish a single domain event to the outbox
    await benchmarks.PublishSingleEvent();

    // Publish multiple domain events sequentially
    await benchmarks.PublishMultipleEvents_Sequential();

    // Get statistics about pending messages
    await benchmarks.GetStatistics();

    // Retrieve a specific message by its ID
    await benchmarks.GetMessageById();

    // Clean up resources
    benchmarks.Cleanup();
    benchmarks.Dispose();
  }
}
```
```
## IntegrationTestFixture

The `IntegrationTestFixture` class provides a reusable test fixture that sets up an in-memory integration test environment for the Outbox Pattern application. It creates a WebApplicationFactory with an in-memory SQLite database and provides HTTP client access to test the application's API endpoints and services. The fixture manages the lifecycle of the test environment, including proper disposal of resources.

### Example Usage

```csharp
using DotnetOutboxPattern.Tests;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Create the integration test fixture
        var fixture = new IntegrationTestFixture();

        try
        {
            // Initialize the fixture to create the HTTP client
            await fixture.InitializeAsync();

            // Use the fixture in your tests
            var response = await fixture.Client.GetAsync("/health");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Application is healthy!");
            }

            // Create a service scope for resolving scoped services
            using var scope = fixture.CreateScope();
            var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

            // Publish an event through the service
            var publishEvent = new PublishableEvent
            {
                Event = new EntityCreatedEvent { EntityId = "test-1", EntityType = "Order" },
                Topic = "orders.created",
                MaxAttempts = 3
            };

            var message = await outboxService.PublishEventAsync(publishEvent);
            Console.WriteLine($"Published message with ID: {message.Id}");
        }
        finally
        {
            // Dispose the fixture to clean up resources
            await fixture.DisposeAsync();
        }
    }
}
```
