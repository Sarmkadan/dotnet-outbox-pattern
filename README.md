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

## IExternalApiClient

The `IExternalApiClient` interface defines a contract for making external API calls as part of the outbox message publishing process. It's used when outbox events need to trigger actions in external systems that aren't handled by the message broker. The interface provides two methods: one for generic API calls returning an `ApiCallResult` object, and a generic version that deserializes the response into a specified type.

### Example Usage

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetOutboxPattern.Integration;
using Microsoft.Extensions.Logging;
using Moq;

class Program
{
    static async Task Main()
    {
        // Create a mock logger
        var loggerMock = new Mock<ILogger<ExternalApiClient>>();
        var httpClientMock = new Mock<ResilientHttpClient>(loggerMock.Object);
        
        // Create the external API client
        var apiClient = new ExternalApiClient(httpClientMock.Object, loggerMock.Object);
        
        // Create a sample payload
        var orderCreatedPayload = new {
            OrderId = "12345",
            CustomerId = "cust-67890",
            Amount = 99.99m,
            Items = new[] {
                new { ProductId = "prod-1", Quantity = 2, Price = 49.99m },
                new { ProductId = "prod-2", Quantity = 1, Price = 0.00m }
            }
        };
        
        // Make a generic API call
        var result = await apiClient.CallAsync(
            "https://api.example.com/orders",
            orderCreatedPayload,
            new Dictionary<string, string> { { "Authorization", "Bearer token123" } }
        );
        
        Console.WriteLine($"API Call Success: {result.IsSuccess}");
        Console.WriteLine($"Status Code: {result.StatusCode}");
        Console.WriteLine($"Duration: {result.DurationMs}ms");
        Console.WriteLine($"Response: {result.ResponseBody}");
        
        // Make a typed API call (deserialize response to OrderResponse)
        var orderResponse = await apiClient.CallAsync<OrderResponse>(
            "https://api.example.com/orders/12345",
            new { } // empty payload for GET request
        );
        
        if (orderResponse != null)
        {
            Console.WriteLine($"Order ID: {orderResponse.OrderId}");
            Console.WriteLine($"Order Amount: {orderResponse.Amount}");
        }
        
        // Handle API errors
        var errorResult = await apiClient.CallAsync(
            "https://api.example.com/invalid-endpoint",
            new { }
        );
        
        if (!errorResult.IsSuccess)
        {
            Console.WriteLine($"API Error: {errorResult.ErrorMessage}");
        }
    }
}

public class OrderResponse
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## IIntegrationEventPublisher

The `IIntegrationEventPublisher` interface serves as the main entry point for publishing integration events to various external systems and channels. It acts as a bridge between the outbox pattern's internal message storage and external consumers such as webhooks, external APIs, or in-process event handlers. The publisher maintains a registry of event publishers for different channels and provides methods to publish events either to all registered channels or to a specific channel.

### Key Features
- Register publishers for specific event types and channels
- Publish events to all configured integration channels
- Publish events to specific channels by name
- Automatic error handling and logging for each publishing attempt

### Example Usage

```csharp
using System;
using System.Threading.Tasks;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Events;
using DotnetOutboxPattern.Integration;
using Microsoft.Extensions.Logging;
using Moq;

class Program
{
    static async Task Main()
    {
        // Create mock logger
        var loggerMock = new Mock<ILogger<IntegrationEventPublisher>>();
        
        // Create the main integration event publisher
        var publisher = new IntegrationEventPublisher(loggerMock.Object);
        
        // Create a sample domain event
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = "order-123",
            CustomerId = "customer-456",
            Amount = 99.99m,
            Items = new[]
            {
                new OrderItem { ProductId = "prod-1", Quantity = 2, Price = 49.99m },
                new OrderItem { ProductId = "prod-2", Quantity = 1, Price = 0.00m }
            }
        };
        
        // Register webhook publisher for OrderCreatedEvent
        var webhookHandlerMock = new Mock<IWebhookHandler>();
        var webhookPublisher = new WebhookIntegrationEventPublisher(webhookHandlerMock.Object, loggerMock.Object);
        publisher.RegisterPublisher<OrderCreatedEvent>("webhooks", webhookPublisher);
        
        // Register external API publisher for OrderCreatedEvent
        var apiClientMock = new Mock<IExternalApiClient>();
        var apiPublisher = new ExternalApiIntegrationEventPublisher(
            apiClientMock.Object, 
            "https://api.example.com/orders", 
            loggerMock.Object
        );
        publisher.RegisterPublisher<OrderCreatedEvent>("external-api", apiPublisher);
        
        // Register in-process publisher for OrderCreatedEvent
        var eventPublisherMock = new Mock<IEventPublisher>();
        var inProcessPublisher = new InProcessIntegrationEventPublisher(eventPublisherMock.Object, loggerMock.Object);
        publisher.RegisterPublisher<OrderCreatedEvent>("in-process", inProcessPublisher);
        
        // Publish to all registered channels (webhooks, external-api, in-process)
        await publisher.PublishAsync(orderCreatedEvent);
        Console.WriteLine("Event published to all channels");
        
        // Publish to a specific channel only
        await publisher.PublishToChannelAsync(orderCreatedEvent, "webhooks");
        Console.WriteLine("Event published to webhooks channel only");
        
        // Publish different event type to different channels
        var paymentProcessedEvent = new PaymentProcessedEvent
        {
            PaymentId = "payment-789",
            OrderId = "order-123",
            Amount = 99.99m,
            Status = PaymentStatus.Completed
        };
        
        // Register publisher for PaymentProcessedEvent
        var paymentPublisher = new InProcessIntegrationEventPublisher(eventPublisherMock.Object, loggerMock.Object);
        publisher.RegisterPublisher<PaymentProcessedEvent>("payments", paymentPublisher);
        
        await publisher.PublishAsync(paymentProcessedEvent);
        Console.WriteLine("Payment event published to payments channel");
    }
}

public class OrderCreatedEvent : DomainEvent
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public OrderItem[] Items { get; set; }
}

public class OrderItem
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class PaymentProcessedEvent : DomainEvent
{
    public string PaymentId { get; set; }
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
}

public enum PaymentStatus { Pending, Completed, Failed }
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
