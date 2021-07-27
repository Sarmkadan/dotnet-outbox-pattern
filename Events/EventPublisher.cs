// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Events;

/// <summary>
/// Event publisher for internal pub-sub system
/// Allows components to subscribe to system events without tight coupling
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event) where T : class;
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : class;
}

/// <summary>
/// In-process event publisher implementation
/// Uses delegate pattern for simple pub-sub within same process
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _lock = new();
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<T>(T @event) where T : class
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        lock (_lock)
        {
            var eventType = typeof(T);

            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                _logger.LogDebug("No subscribers for event type {EventType}", eventType.Name);
                return;
            }

            _logger.LogInformation(
                "Publishing event {EventType} to {SubscriberCount} subscribers",
                eventType.Name, handlers.Count);

            foreach (var handler in handlers.ToList())
            {
                try
                {
                    if (handler is Func<T, Task> asyncHandler)
                    {
                        await asyncHandler(@event);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error in event handler for {EventType}",
                        eventType.Name);
                }
            }
        }
    }

    public IDisposable Subscribe<T>(Func<T, Task> handler) where T : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            var eventType = typeof(T);

            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }

            _subscribers[eventType].Add(handler);

            _logger.LogInformation(
                "Subscriber added for event type {EventType}. Total subscribers: {Count}",
                eventType.Name, _subscribers[eventType].Count);
        }

        // Return a disposable that removes the handler when disposed
        return new EventSubscription<T>(this, handler);
    }

    internal void Unsubscribe<T>(Func<T, Task> handler) where T : class
    {
        lock (_lock)
        {
            var eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);

                if (handlers.Count == 0)
                {
                    _subscribers.Remove(eventType);
                }

                _logger.LogInformation(
                    "Subscriber removed for event type {EventType}",
                    eventType.Name);
            }
        }
    }

    private class EventSubscription<T> : IDisposable where T : class
    {
        private readonly EventPublisher _publisher;
        private readonly Func<T, Task> _handler;

        public EventSubscription(EventPublisher publisher, Func<T, Task> handler)
        {
            _publisher = publisher;
            _handler = handler;
        }

        public void Dispose()
        {
            _publisher.Unsubscribe(_handler);
        }
    }
}

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event fired when a message is published successfully
/// </summary>
public class MessagePublishedEvent : DomainEvent
{
    public Guid MessageId { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public int PublishAttempts { get; set; }
}

/// <summary>
/// Event fired when a message publishing fails
/// </summary>
public class MessagePublishFailedEvent : DomainEvent
{
    public Guid MessageId { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int PublishAttempts { get; set; }
}

/// <summary>
/// Event fired when a message is moved to dead letter queue
/// </summary>
public class MessageMovedToDeadLetterEvent : DomainEvent
{
    public Guid MessageId { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
