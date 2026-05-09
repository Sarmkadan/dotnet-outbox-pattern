// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using DotnetOutboxPattern.Services;

namespace DotnetOutboxPattern.Integration;

/// <summary>
/// Interface for handling webhook deliveries and verification
/// Ensures webhook security through signature validation
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Handles an incoming webhook delivery with signature verification
    /// </summary>
    Task<bool> HandleDeliveryAsync(Guid subscriptionId, dynamic payload, string signature);

    /// <summary>
    /// Publishes an event to all subscribed webhooks
    /// </summary>
    Task PublishToWebhooksAsync(string eventType, dynamic eventData);

    /// <summary>
    /// Verifies the webhook signature using HMAC-SHA256
    /// </summary>
    bool VerifySignature(string payload, string signature, string secret);
}

/// <summary>
/// Default implementation of webhook handler
/// </summary>
public class WebhookHandler : IWebhookHandler
{
    private readonly ILogger<WebhookHandler> _logger;
    private readonly IWebhookService _webhookService;
    private readonly string _webhookSecret = "your-secret-key";

    public WebhookHandler(IWebhookService webhookService, ILogger<WebhookHandler> logger)
    {
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HandleDeliveryAsync(Guid subscriptionId, dynamic payload, string signature)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);

            if (!VerifySignature(json, signature, _webhookSecret))
            {
                _logger.LogWarning("Invalid webhook signature for subscription: {SubscriptionId}", subscriptionId);
                return false;
            }

            _logger.LogInformation("Webhook delivery handled successfully: {SubscriptionId}", subscriptionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook delivery: {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task PublishToWebhooksAsync(string eventType, dynamic eventData)
    {
        try
        {
            var webhooks = await _webhookService.GetWebhooksAsync(active: true);
            var json = System.Text.Json.JsonSerializer.Serialize(eventData);

            var tasks = new List<Task>();

            foreach (var webhook in webhooks)
            {
                if (webhook.Events.Contains(eventType))
                {
                    tasks.Add(DeliverWebhookAsync(webhook, eventData));
                }
            }

            await Task.WhenAll(tasks);
            _logger.LogInformation("Published event to {Count} webhooks", webhooks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to webhooks");
        }
    }

    public bool VerifySignature(string payload, string signature, string secret)
    {
        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expectedSignature = Convert.ToBase64String(hash);

            return signature == expectedSignature;
        }
        catch
        {
            return false;
        }
    }

    private async Task DeliverWebhookAsync(dynamic webhook, dynamic eventData)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(eventData);
            var signature = ComputeSignature(json);

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("X-Webhook-Signature", signature);

            var response = await httpClient.PostAsync(webhook.Url, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webhook delivery failed: {StatusCode}",
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering webhook");
        }
    }

    private string ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
