# NotificationService

## Purpose
Service for sending notifications about outbox events. Supports multiple notification channels (in-memory, console, file).

## Public Methods

### `Task SendAsync(Notification notification)`
Sends a notification to all configured channels for the notification.

**Parameters:**
- `notification`: The notification to send

**Returns:** A task representing the asynchronous operation

### `Task SendToChannelAsync(Notification notification, string channel)`
Sends a notification to a specific channel.

**Parameters:**
- `notification`: The notification to send
- `channel`: The name of the channel to send to (e.g., "in-memory", "console", "file")

**Returns:** A task representing the asynchronous operation

### `List<Notification> GetRecentNotifications(int count = 100)`
Retrieves recent notifications from the in-memory storage.

**Parameters:**
- `count`: Maximum number of notifications to return (default: 100)

**Returns:** List of recent notifications

## Usage Example

```csharp
// Create notification service (typically via dependency injection)
var notificationService = new NotificationService(logger);

// Create a notification
var notification = new Notification
{
    Title = "Order Processed",
    Message = "Order #12345 has been successfully processed",
    Severity = NotificationSeverity.Info,
    Channels = new List<string> { "console", "file" }
};

// Send the notification
await notificationService.SendAsync(notification);

// Or send to a specific channel
await notificationService.SendToChannelAsync(notification, "in-memory");

// Retrieve recent notifications
var recent = notificationService.GetRecentNotifications(10);
```

## Notification Channels
The service includes three built-in channels:
- **in-memory**: Stores notifications in memory (accessible via `GetRecentNotifications`)
- **console**: Outputs notifications to the console with severity-based formatting
- **file**: Writes notifications to a log file (`logs/notifications.log`)

## Notification Properties
- `Title`: Notification title
- `Message`: Notification message body
- `Severity`: Notification severity level (Info, Warning, Error, Critical)
- `Metadata`: Optional key-value pairs for additional data
- `CreatedAt`: Timestamp when notification was created
- `Channels`: List of channel names to send the notification to (defaults to ["in-memory"])