// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Service interface for managing webhook subscriptions and deliveries
/// Allows external systems to subscribe to outbox events
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Registers a new webhook subscription
    /// </summary>
    Task<dynamic> RegisterWebhookAsync(string url, List<string> events);

    /// <summary>
    /// Gets a specific webhook by ID
    /// </summary>
    Task<dynamic?> GetWebhookAsync(Guid id);

    /// <summary>
    /// Gets all registered webhooks, optionally filtered by active status
    /// </summary>
    Task<List<dynamic>> GetWebhooksAsync(bool? active = null);

    /// <summary>
    /// Deletes a webhook subscription
    /// </summary>
    Task<bool> DeleteWebhookAsync(Guid id);

    /// <summary>
    /// Gets delivery history for a webhook
    /// </summary>
    Task<List<dynamic>> GetDeliveriesAsync(Guid webhookId, int limit);

    /// <summary>
    /// Tests a webhook subscription by sending a test payload
    /// </summary>
    Task<dynamic?> TestWebhookAsync(Guid id);
}

/// <summary>
/// Default implementation of webhook service
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly ILogger<WebhookService> _logger;
    private readonly Dictionary<Guid, dynamic> _webhooks = new(); // In-memory storage for demo

    public WebhookService(ILogger<WebhookService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<dynamic> RegisterWebhookAsync(string url, List<string> events)
    {
        var id = Guid.NewGuid();
        var webhook = new
        {
            Id = id,
            Url = url,
            Events = events,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastDeliveryAt = (DateTime?)null,
            SuccessfulDeliveries = 0,
            FailedDeliveries = 0
        };

        _webhooks[id] = webhook;
        _logger.LogInformation("Webhook registered: {WebhookId} for URL: {Url}", id, url);

        return webhook;
    }

    public async Task<dynamic?> GetWebhookAsync(Guid id)
    {
        return _webhooks.TryGetValue(id, out var webhook) ? webhook : null;
    }

    public async Task<List<dynamic>> GetWebhooksAsync(bool? active = null)
    {
        var webhooks = _webhooks.Values.ToList();

        if (active.HasValue)
            webhooks = webhooks.Where(w => w.IsActive == active.Value).ToList();

        return webhooks;
    }

    public async Task<bool> DeleteWebhookAsync(Guid id)
    {
        return _webhooks.Remove(id);
    }

    public async Task<List<dynamic>> GetDeliveriesAsync(Guid webhookId, int limit)
    {
        // Mock delivery history
        return new List<dynamic>();
    }

    public async Task<dynamic?> TestWebhookAsync(Guid id)
    {
        var webhook = await GetWebhookAsync(id);
        if (webhook == null)
            return null;

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var testPayload = new { test = true, timestamp = DateTime.UtcNow };
            var json = System.Text.Json.JsonSerializer.Serialize(testPayload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(webhook.Url, content);
            stopwatch.Stop();

            return new
            {
                IsSuccessful = response.IsSuccessStatusCode,
                HttpStatusCode = (int)response.StatusCode,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ResponseBody = await response.Content.ReadAsStringAsync(),
                ErrorMessage = (string?)null,
                TestedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook: {WebhookId}", id);

            return new
            {
                IsSuccessful = false,
                HttpStatusCode = 0,
                DurationMs = 0,
                ResponseBody = (string?)null,
                ErrorMessage = ex.Message,
                TestedAt = DateTime.UtcNow
            };
        }
    }
}
