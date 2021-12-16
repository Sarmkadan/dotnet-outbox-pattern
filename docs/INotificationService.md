# INotificationService

The `INotificationService` interface defines a contract for creating, configuring, and sending notifications across various channels. It provides access to notification content, severity, metadata, timestamps, and channel‑specific implementations, allowing consumers to compose a notification and deliver it synchronously or asynchronously.

## API

### Title
- **Purpose:** Gets or sets the title text of the notification.  
- **Type:** `string`  
- **Exceptions:** None.

### Message
- **Purpose:** Gets or sets the body message of the notification.  
- **Type:** `string`  
- **Exceptions:** None.

### Severity
- **Purpose:** Gets or sets the severity level of the notification.  
- **Type:** `NotificationSeverity`  
- **Exceptions:** None.

### Metadata
- **Purpose:** Gets or sets optional key‑value pairs associated with the notification.  
- **Type:** `Dictionary<string, string>?`  
- **Exceptions:** None.

### CreatedAt
- **Purpose:** Gets the UTC timestamp indicating when the notification was instantiated.  
- **Type:** `DateTime`  
- **Exceptions:** None.

### Channels
- **Purpose:** Gets or sets the collection of channel identifiers that the notification should be delivered through.  
- **Type:** `List<string>`  
- **Exceptions:** None.

### NotificationService
- **Purpose:** Provides access to the default concrete implementation of `INotificationService`.  
- **Type:** `NotificationService`  
- **Exceptions:** None.

### SendAsync
- **Purpose:** Asynchronously sends the notification using the configuration defined by the properties of this instance.  
- **Parameters:** None.  
- **Return:** `Task` representing the asynchronous operation.  
- **Exceptions:** Implementations may throw exceptions such as `OperationCanceledException` if cancellation is requested, or `InvalidOperationException` if required data is missing.

### SendToChannelAsync
- **Purpose:** Asynchronously sends the notification to a specific channel (channel selection logic is implementation‑defined).  
- **Parameters:** None.  
- **Return:** `Task` representing the asynchronous operation.  
- **Exceptions:** Implementations may throw exceptions such as `ArgumentException` if the target channel cannot be resolved, or `OperationCanceledException`.

### GetRecentNotifications
- **Purpose:** Retrieves a list of recently sent notifications from the underlying store.  
- **Parameters:** None.  
- **Return:** `List<Notification>` containing the most recent notifications.  
- **Exceptions:** Implementations may throw exceptions such as `InvalidOperationException` if the store is unavailable.

### InMemoryNotificationChannel
- **Purpose:** Gets the in‑memory channel instance used for temporary notification storage.  
- **Type:** `InMemoryNotificationChannel`  
- **Exceptions:** None.

### SendAsync
- **Purpose:** Asynchronously sends the notification via the in‑memory channel.  
- **Parameters:** None.  
- **Return:** `Task` representing the asynchronous operation.  
- **Exceptions:** Implementations may throw exceptions such as `OperationCanceledException`.

### ConsoleNotificationChannel
- **Purpose:** Gets the console output channel instance.  
- **Type:** `ConsoleNotificationChannel`  
- **Exceptions:** None.

### SendAsync
- **Purpose:** Asynchronously sends the notification via the console channel.  
- **Parameters:** None.  
- **Return:** `Task` representing the asynchronous operation.  
- **Exceptions:** Implementations may throw exceptions such as `IOException` if console output fails, or `OperationCanceledException`.

### FileNotificationChannel
- **Purpose:** Gets the file‑based channel instance used to persist notifications to disk.  
- **Type:** `FileNotificationChannel`  
- **Exceptions:** None.

### SendAsync
- **Purpose:** Asynchronously sends the notification via the file channel (e.g., appending to a log file).  
- **Parameters:** None.  
- **Return:** `Task` representing the asynchronous operation.  
- **Exceptions:** Implementations may throw exceptions such as `IOException`, `UnauthorizedAccessException`, or `OperationCanceledException`.

## Usage

### Example 1: Sending a notification through all configured channels
```csharp
using System.Threading.Tasks;
using System.Collections.Generic;
using DotNetOutboxPattern; // adjust namespace as needed

public class ExampleSender
{
    private readonly INotificationService _notificationService;

    public ExampleSender(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task NotifyUserAsync()
    {
        _notificationService.Title = "Account Update";
        _notificationService.Message = "Your email address has been changed.";
        _notificationService.Severity = NotificationSeverity.Info;
        _notificationService.Metadata = new Dictionary<string, string>
        {
            ["userId"] = "12345",
            ["timestamp"] = DateTime.UtcNow.ToString("o")
        };
        _notificationService.Channels = new List<string> { "email", "sms" };

        await _notificationService.SendAsync();
    }
}
```

### Example 2: Sending a notification only to the console channel and retrieving recent notifications
```csharp
using System.Threading.Tasks;
using DotNetOutboxPattern;

public class ConsoleDemo
{
    private readonly INotificationService _service;

    public ConsoleDemo(INotificationService service)
    {
        _service = service;
    }

    public async Task DemoAsync()
    {
        // Use the console channel directly
        var consoleChan = _service.ConsoleNotificationChannel;
        consoleChan.Title = "Console Alert";
        consoleChan.Message = "This is a test.";
        consoleChan.Severity = NotificationSeverity.Warning;
        await consoleChan.SendAsync(); // sends via console

        // Retrieve recent notifications from the service
        List<Notification> recent = _service.GetRecentNotifications();
        foreach (var n in recent)
        {
            System.Console.WriteLine($"{n.CreatedAt:u} - {n.Title}: {n.Message}");
        }
    }
}
```

## Notes
- The interface does not enforce thread‑safety; concrete implementations should document their own concurrency guarantees.
- Setting `Channels` to `null` or an empty list may cause `SendAsync` to throw an implementation‑defined exception.
- The `Metadata` property accepts a nullable dictionary; when `null` is assigned, implementations may treat it as having no metadata.
- `GetRecentNotifications` returns a snapshot; modifications to the returned list do not affect the internal store.
- The channel‑specific `SendAsync` members are intended to be invoked on the channel instances obtained via the corresponding channel properties (e.g., `InMemoryNotificationChannel`, `ConsoleNotificationChannel`, `FileNotificationChannel`). Calling them on the service instance directly may result in undefined behavior.
- Implementations should consider cancellation tokens; although the signatures shown do not include a `CancellationToken` parameter, overloads that accept one may exist in concrete types but are not part of this interface.
- The `NotificationService` property returns a default concrete implementation; replacing it may affect the behavior of other members that rely on the internal state.
