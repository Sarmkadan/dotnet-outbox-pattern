// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Service for sending notifications about outbox events
/// Supports multiple notification channels (in-memory, email, Slack, etc.)
/// </summary>
public interface INotificationService
{
    Task SendAsync(Notification notification);
    Task SendToChannelAsync(Notification notification, string channel);
}

/// <summary>
/// Represents a notification to be sent
/// </summary>
public class Notification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> Channels { get; set; } = new() { "in-memory" };
}

/// <summary>
/// Severity levels for notifications
/// </summary>
public enum NotificationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

/// <summary>
/// Default implementation of notification service
/// </summary>
public class NotificationService : INotificationService
{
    private readonly Dictionary<string, INotificationChannel> _channels = new();
    private readonly ILogger<NotificationService> _logger;
    private readonly List<Notification> _notifications = new(); // In-memory storage

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Register default channels
        _channels["in-memory"] = new InMemoryNotificationChannel(_notifications);
        _channels["console"] = new ConsoleNotificationChannel(_logger);
        _channels["file"] = new FileNotificationChannel(_logger);
    }

    public async Task SendAsync(Notification notification)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        // Send to all configured channels
        var tasks = notification.Channels
            .Select(channel => SendToChannelAsync(notification, channel))
            .ToList();

        await Task.WhenAll(tasks);
    }

    public async Task SendToChannelAsync(Notification notification, string channel)
    {
        if (!_channels.TryGetValue(channel, out var handler))
        {
            _logger.LogWarning("Unknown notification channel: {Channel}", channel);
            return;
        }

        try
        {
            await handler.SendAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to channel {Channel}", channel);
        }
    }

    public List<Notification> GetRecentNotifications(int count = 100)
    {
        lock (_notifications)
        {
            return _notifications
                .TakeLast(count)
                .ToList();
        }
    }
}

/// <summary>
/// Interface for notification channels
/// </summary>
public interface INotificationChannel
{
    Task SendAsync(Notification notification);
}

/// <summary>
/// In-memory notification channel - stores notifications in memory
/// </summary>
public class InMemoryNotificationChannel : INotificationChannel
{
    private readonly List<Notification> _notifications;

    public InMemoryNotificationChannel(List<Notification> notifications)
    {
        _notifications = notifications;
    }

    public async Task SendAsync(Notification notification)
    {
        lock (_notifications)
        {
            _notifications.Add(notification);

            // Keep only recent notifications
            if (_notifications.Count > 1000)
            {
                _notifications.RemoveRange(0, _notifications.Count - 1000);
            }
        }
    }
}

/// <summary>
/// Console notification channel - outputs to console
/// </summary>
public class ConsoleNotificationChannel : INotificationChannel
{
    private readonly ILogger<ConsoleNotificationChannel> _logger;

    public ConsoleNotificationChannel(ILogger<ConsoleNotificationChannel> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(Notification notification)
    {
        var levelText = notification.Severity switch
        {
            NotificationSeverity.Critical => "🔴 CRITICAL",
            NotificationSeverity.Error => "🟠 ERROR",
            NotificationSeverity.Warning => "🟡 WARNING",
            _ => "🔵 INFO"
        };

        _logger.LogInformation(
            "{Level} [{Title}] {Message}",
            levelText, notification.Title, notification.Message);

        await Task.CompletedTask;
    }
}

/// <summary>
/// File notification channel - writes to log file
/// </summary>
public class FileNotificationChannel : INotificationChannel
{
    private readonly ILogger<FileNotificationChannel> _logger;
    private readonly string _notificationFile = "logs/notifications.log";

    public FileNotificationChannel(ILogger<FileNotificationChannel> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(Notification notification)
    {
        try
        {
            var logLine = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{notification.Severity}] {notification.Title}: {notification.Message}";

            var directory = Path.GetDirectoryName(_notificationFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            await File.AppendAllTextAsync(_notificationFile, logLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing notification to file");
        }
    }
}
