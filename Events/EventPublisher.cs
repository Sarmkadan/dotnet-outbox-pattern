#nullable enable
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
    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    Task PublishAsync<T>(T @event) where T : class;
    /// <summary>
    /// Subscribes to events of type T.
    /// </summary>
    /// <typeparam name="T">The type of the event to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when an event is published.</param>
    /// <returns>An IDisposable that can be used to unsubscribe.</returns>
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : class;
}

/// <summary>
/// In-process event publisher implementation
/// Uses delegate pattern for simple pub-sub within same process
/// </summary>
public sealed class EventPublisher : IEventPublisher
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _lock = new();
    private readonly ILogger<EventPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventPublisher"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    public async Task PublishAsync<T>(T @event) where T : class
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(T);
        List<Delegate>? handlersCopy = null;

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                _logger.LogDebug("No subscribers for event type {EventType}", eventType.Name);
                return;
            }

            _logger.LogInformation(
                "Publishing event {EventType} to {SubscriberCount} subscribers",
                eventType.Name, handlers.Count);

            handlersCopy = handlers.ToList();
        }

        foreach (var handler in handlersCopy)
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

    /// <summary>
    /// Subscribes to events of type T.
    /// </summary>
    /// <typeparam name="T">The type of the event to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when an event is published.</param>
    /// <returns>An IDisposable that can be used to unsubscribe.</returns>
    public IDisposable Subscribe<T>(Func<T, Task> handler) where T : class
    {
        if (handler is null)
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

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscription{T}"/> class.
        /// </summary>
        /// <param name="publisher">The publisher that created this subscription.</param>
        /// <param name="handler">The handler to be unsubscribed when this subscription is disposed.</param>
        public EventSubscription(EventPublisher publisher, Func<T, Task> handler)
        {
            _publisher = publisher;
            _handler = handler;
        }

        /// <summary>
        /// Unsubscribes the handler from the publisher.
        /// </summary>
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
    /// <summary>
    /// Gets the unique identifier for the event.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
    /// <summary>
    /// Gets the date and time when the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event fired when a message is published successfully
/// </summary>
public sealed class MessagePublishedEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the identifier of the message that was published.
    /// </summary>
    public Guid MessageId { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the aggregate associated with the message.
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the number of attempts made to publish the message.
    /// </summary>
    public int PublishAttempts { get; set; }
}

/// <summary>
/// Event fired when a message publishing fails
/// </summary>
public sealed class MessagePublishFailedEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the identifier of the message that failed to publish.
    /// </summary>
    public Guid MessageId { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the aggregate associated with the message.
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the error message associated with the publishing failure.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the number of attempts made to publish the message.
    /// </summary>
    public int PublishAttempts { get; set; }
}

/// <summary>
/// Event fired when a message is moved to dead letter queue
/// </summary>
public sealed class MessageMovedToDeadLetterEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the identifier of the message that was moved to the dead letter queue.
    /// </summary>
    public Guid MessageId { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the aggregate associated with the message.
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the reason why the message was moved to the dead letter queue.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}