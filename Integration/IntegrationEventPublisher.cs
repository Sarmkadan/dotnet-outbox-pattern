#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Events;

namespace DotnetOutboxPattern.Integration;

/// <summary>
/// Publisher for integration events - bridges the outbox system with external systems
/// Handles publishing to webhooks, external APIs, and event buses
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an event to all configured integration channels
    /// </summary>
    Task PublishAsync<T>(T @event) where T : class;

    /// <summary>
    /// Publishes an event to a specific integration channel
    /// </summary>
    Task PublishToChannelAsync<T>(T @event, string channel) where T : class;

    /// <summary>
    /// Registers a publisher for a specific event type
    /// </summary>
    void RegisterPublisher<T>(string channel, IIntegrationEventPublisherChannel<T> publisher) where T : class;
}

/// <summary>
/// Interface for integration event publisher channels
/// </summary>
public interface IIntegrationEventPublisherChannel<in T> where T : class
{
    Task PublishAsync(T @event);
}

/// <summary>
/// Default implementation of integration event publisher
/// </summary>
public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly Dictionary<Type, Dictionary<string, object>> _publishers = new();
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(ILogger<IntegrationEventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<T>(T @event) where T : class
    {
        var eventType = typeof(T);

        if (!_publishers.TryGetValue(eventType, out var channelPublishers))
        {
            _logger.LogDebug("No publishers registered for event type {EventType}", eventType.Name);
            return;
        }

        var tasks = channelPublishers.Values
            .Select(publisher => PublishToPublisherAsync(publisher, @event))
            .ToList();

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Published {EventType} to {ChannelCount} integration channels",
            eventType.Name, channelPublishers.Count);
    }

    public async Task PublishToChannelAsync<T>(T @event, string channel) where T : class
    {
        var eventType = typeof(T);

        if (!_publishers.TryGetValue(eventType, out var channelPublishers) ||
            !channelPublishers.TryGetValue(channel, out var publisher))
        {
            _logger.LogWarning(
                "No publisher found for event type {EventType} and channel {Channel}",
                eventType.Name, channel);
            return;
        }

        await PublishToPublisherAsync(publisher, @event);

        _logger.LogInformation(
            "Published {EventType} to channel {Channel}",
            eventType.Name, channel);
    }

    public void RegisterPublisher<T>(string channel, IIntegrationEventPublisherChannel<T> publisher) where T : class
    {
        if (publisher is null)
            throw new ArgumentNullException(nameof(publisher));

        var eventType = typeof(T);

        if (!_publishers.ContainsKey(eventType))
        {
            _publishers[eventType] = new Dictionary<string, object>();
        }

        _publishers[eventType][channel] = publisher;

        _logger.LogInformation(
            "Registered publisher for event type {EventType} on channel {Channel}",
            eventType.Name, channel);
    }

    private async Task PublishToPublisherAsync<T>(object publisher, T @event) where T : class
    {
        try
        {
            var method = publisher.GetType().GetMethod("PublishAsync");

            if (method is not null)
            {
                var task = method.Invoke(publisher, new object[] { @event }) as Task;

                if (task is not null)
                {
                    await task;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to channel");
        }
    }
}

/// <summary>
/// Webhook integration event publisher - publishes events to webhooks
/// </summary>
public sealed class WebhookIntegrationEventPublisher : IIntegrationEventPublisherChannel<DomainEvent>
{
    private readonly IWebhookHandler _webhookHandler;
    private readonly ILogger<WebhookIntegrationEventPublisher> _logger;

    public WebhookIntegrationEventPublisher(
        IWebhookHandler webhookHandler,
        ILogger<WebhookIntegrationEventPublisher> logger)
    {
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(DomainEvent @event)
    {
        try
        {
            var eventType = @event.GetType().Name;
            await _webhookHandler.PublishToWebhooksAsync(eventType, @event);

            _logger.LogInformation("Event published to webhooks: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to webhooks");
        }
    }
}

/// <summary>
/// External API integration event publisher - calls external APIs on events
/// </summary>
public sealed class ExternalApiIntegrationEventPublisher : IIntegrationEventPublisherChannel<DomainEvent>
{
    private readonly IExternalApiClient _apiClient;
    private readonly string _apiUrl;
    private readonly ILogger<ExternalApiIntegrationEventPublisher> _logger;

    public ExternalApiIntegrationEventPublisher(
        IExternalApiClient apiClient,
        string apiUrl,
        ILogger<ExternalApiIntegrationEventPublisher> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _apiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(DomainEvent @event)
    {
        try
        {
            var result = await _apiClient.CallAsync(_apiUrl, @event);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Event published to external API: {EventType}", @event.GetType().Name);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to publish event to external API: {EventType}. Status: {StatusCode}",
                    @event.GetType().Name, result.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to external API");
        }
    }
}

/// <summary>
/// In-process event publisher - publishes events to internal subscribers
/// </summary>
public sealed class InProcessIntegrationEventPublisher : IIntegrationEventPublisherChannel<DomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<InProcessIntegrationEventPublisher> _logger;

    public InProcessIntegrationEventPublisher(
        IEventPublisher eventPublisher,
        ILogger<InProcessIntegrationEventPublisher> logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(DomainEvent @event)
    {
        try
        {
            await _eventPublisher.PublishAsync(@event);
            _logger.LogInformation("Event published in-process: {EventType}", @event.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event in-process");
        }
    }
}
