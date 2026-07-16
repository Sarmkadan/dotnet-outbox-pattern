
## OutboxMessageAdditionalTests

The `OutboxMessageAdditionalTests` class provides comprehensive unit tests for the `OutboxMessage` domain model, extending the basic validation and state management tests with additional scenarios for message validation, state transitions, locking behavior, retry logic, and failure handling. These tests ensure robust handling of outbox messages throughout their lifecycle, including proper validation of required fields, state transitions during publishing, error recording, and lock management.

### Example Usage

```csharp
using System;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Tests;

class Program
{
    static void Main()
    {
        // Create a valid outbox message
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "order-123-created",
            AggregateId = "order-123",
            AggregateType = "Order",
            EventType = EventType.Created,
            EventData = "{\"orderId\":\"123\",\"amount\":99.99,\"customerId\":\"cust-456\"}",
            EventTypeName = "OrderCreatedEvent",
            Topic = "orders.created",
            MaxPublishAttempts = 5,
            PartitionKey = "order-123",
            DeliveryGuarantee = DeliveryGuarantee.AtLeastOnce,
            Priority = 5,
            CorrelationId = "corr-789",
            CausationId = "caus-abc",
            Metadata = "{\"source\":\"web-api\",\"userId\":\"user-789\"}"
        };

        // Validate the message - should not throw
        message.Validate();
        Console.WriteLine("Message validated successfully");

        // Test default values are set during validation
        var minimalMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "key-1",
            AggregateId = "agg-1",
            AggregateType = "Type",
            EventType = EventType.Created,
            EventData = "data",
            EventTypeName = "Event",
            Topic = "topic",
            MaxPublishAttempts = 3
        };

        minimalMessage.Validate();
        Console.WriteLine($"Default state: {minimalMessage.State}");
        Console.WriteLine($"Default attempts: {minimalMessage.PublishAttempts}");
        Console.WriteLine($"Default guarantee: {minimalMessage.DeliveryGuarantee}");

        // Test validation of required fields
        try
        {
            var invalidMessage = new OutboxMessage { Id = Guid.NewGuid() };
            invalidMessage.Validate();
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation caught: {ex.Message}");
        }

        // Test state transitions during publishing
        message.State = OutboxMessageState.Pending;
        message.MarkAsPublished();
        Console.WriteLine($"After MarkAsPublished: {message.State}");
        Console.WriteLine($"Published at: {message.PublishedAt}");

        // Test failure handling
        message.State = OutboxMessageState.Pending;
        message.RecordFailure("Connection timeout to message broker");
        Console.WriteLine($"After RecordFailure: Error = {message.ErrorMessage}");
        Console.WriteLine($"State after failure: {message.State}");
        Console.WriteLine($"Is locked: {message.IsLocked}");

        // Test lock management
        message.IsLocked = false;
        message.Lock(TimeSpan.FromMinutes(5));
        Console.WriteLine($"After Lock: IsLocked = {message.IsLocked}, State = {message.State}");

        // Test retry logic
        message.PublishAttempts = 2;
        message.MaxPublishAttempts = 5;
        Console.WriteLine($"Can retry: {message.CanRetry()}");

        // Test reaching max attempts
        message.PublishAttempts = 5;
        message.RecordFailure("Final attempt failed");
        Console.WriteLine($"After max attempts: State = {message.State}");

        // Test scheduled messages
        var scheduledMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "scheduled-key",
            AggregateId = "agg-2",
            AggregateType = "Order",
            EventType = EventType.Created,
            EventData = "{\"orderId\":\"456\"}",
            EventTypeName = "OrderCreatedEvent",
            Topic = "orders.created",
            MaxPublishAttempts = 3,
            ScheduledFor = DateTime.UtcNow.AddHours(1)
        };

        Console.WriteLine($"Scheduled for: {scheduledMessage.ScheduledFor}");
    }
}
```