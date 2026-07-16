// README.md
# OutboxMessageAdditionalTests

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

## ExportServiceTests

The `ExportServiceTests` class provides comprehensive unit tests for the `ExportService` class, validating functionality related to data export operations, format support, and error handling. These tests ensure robust handling of export operations throughout their lifecycle, including proper validation of required fields, state transitions during publishing, error recording, and lock management.

### Example Usage

```csharp
using System;
using DotnetOutboxPattern.Dtos;
using DotnetOutboxPattern.Services;
using DotnetOutboxPattern.Tests;

class Program
{
    static void Main()
    {
        // Create a valid export request
        var request = new ExportRequest { Format = "json" };

        // Test constructor with null outbox service
        var exportService = new ExportServiceTests();
        exportService.Constructor_WithNullOutboxService_ThrowsArgumentNullException();

        // Test constructor with null formatters
        exportService.Constructor_WithNullFormatters_ThrowsArgumentNullException();

        // Test get supported formats
        var formats = exportService.GetSupportedFormats_ReturnsRegisteredFormats();
        Console.WriteLine($"Supported formats: {string.Join(", ", formats)}");

        // Test export with JSON format
        var result = exportService.ExportAsync_WithJsonFormat_UsesJsonFormatter(request);
        Console.WriteLine($"Export result: {result.Format}, {result.ContentType}");

        // Test export with CSV format
        result = exportService.ExportAsync_WithCsvFormat_UsesCsvFormatter(request);
        Console.WriteLine($"Export result: {result.Format}, {result.ContentType}");

        // Test export with unsupported format
        try
        {
            result = exportService.ExportAsync_WithUnsupportedFormat_ThrowsInvalidOperationException(request);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Export error: {ex.Message}");
        }

        // Test export with lowercase format
        result = exportService.ExportAsync_WithLowercaseFormat_IsFormatCaseInsensitive(request);
        Console.WriteLine($"Export result: {result.Format}, {result.ContentType}");

        // Test export with content size
        result = exportService.ExportAsync_SetsContentSizeCorrectly(request);
        Console.WriteLine($"Export result: {result.ContentLength}");

        // Test export with exported at timestamp
        result = exportService.ExportAsync_SetsExportedAtTimestamp(request);
        Console.WriteLine($"Export result: {result.ExportedAt}");

        // Test export with formatter exception
        try
        {
            result = exportService.ExportAsync_WhenFormatterThrows_PropagatesException(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Export error: {ex.Message}");
        }

        // Test export to file
        var filePath = exportService.ExportToFileAsync_CreatesExportDirectory(request);
        Console.WriteLine($"Export file path: {filePath}");

        // Test get supported formats with no formatters
        var emptyFormats = exportService.GetSupportedFormats_ReturnsEmptyList_WhenNoFormatters();
        Console.WriteLine($"Supported formats: {string.Join(", ", emptyFormats)}");

        // Test export with empty message list
        result = exportService.ExportAsync_WithEmptyMessageList_ReturnsValidResult(request);
        Console.WriteLine($"Export result: {result.MessageCount}");
    }
}
```

## NotificationServiceTests

The `NotificationServiceTests` class provides comprehensive unit tests for the `NotificationService` class, validating functionality related to notification sending, channel management, error handling, and notification retrieval. These tests ensure robust handling of notifications throughout their lifecycle, including proper validation of required parameters, channel routing, error logging, and recent notification tracking.

### Example Usage

```csharp
using System;
using System.Collections.Generic;
using DotnetOutboxPattern.Services;
using DotnetOutboxPattern.Tests;
using Microsoft.Extensions.Logging;
using Moq;

class Program
{
    static void Main()
    {
        // Create a mock logger
        var loggerMock = new Mock<ILogger<NotificationService>>();
        var notificationService = new NotificationService(loggerMock.Object);

        // Test constructor with null logger
        try
        {
            var invalidService = new NotificationService(null!);
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine($"Constructor validation: {ex.Message}");
        }

        // Create a valid notification
        var notification = new Notification
        {
            Title = "System Alert",
            Message = "Database connection established",
            Severity = NotificationSeverity.Info,
            Channels = new List<string> { "in-memory", "console" }
        };

        // Send notification to all channels
        notificationService.SendAsync_WithValidNotification_SendsToAllChannels();
        Console.WriteLine("Notification sent successfully");

        // Test sending to specific known channel
        notificationService.SendToChannelAsync_WithKnownChannel_DelegatesToHandler();
        Console.WriteLine("Channel delegation tested");

        // Test sending to unknown channel (should log warning)
        notificationService.SendToChannelAsync_WithUnknownChannel_LogsWarningAndReturns();
        Console.WriteLine("Unknown channel handling tested");

        // Test error handling when channel throws
        notificationService.SendToChannelAsync_WhenHandlerThrows_LogsErrorAndContinues();
        Console.WriteLine("Error handling tested");

        // Get recent notifications
        var recentNotifications = notificationService.GetRecentNotifications(10);
        Console.WriteLine($"Recent notifications count: {recentNotifications.Count}");

        // Get default recent notifications (last 100)
        var defaultRecent = notificationService.GetRecentNotifications_WithDefaultCount_ReturnsLast100();
        Console.WriteLine($"Default recent notifications count: {defaultRecent.Count}");
    }
}
```
