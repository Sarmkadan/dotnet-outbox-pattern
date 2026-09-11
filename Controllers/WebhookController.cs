#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotnetOutboxPattern.Integration;
using DotnetOutboxPattern.Dtos;
using DotnetOutboxPattern.Services;

namespace DotnetOutboxPattern.Controllers;

/// <summary>
/// Manages webhook subscriptions and handles incoming webhook deliveries
/// External systems can subscribe to outbox events via webhooks
/// </summary>
[ApiController]
[Route("api/webhooks")]
public sealed class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly IWebhookHandler _webhookHandler;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookService webhookService,
        IWebhookHandler webhookHandler,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new webhook subscription - external systems use this to subscribe to events
    /// </summary>
    /// <remarks>
    /// POST /api/webhooks/subscriptions
    /// </remarks>
    /// <param name="request">The webhook registration request containing URL and events to subscribe to.</param>
    /// <response code="201">Returns the created webhook subscription</response>
    /// <response code="400">If the request is null or URL is not well-formed</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpPost("subscriptions")]
    [ProducesResponseType(typeof(WebhookSubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubscribeAsync([FromBody] RegisterWebhookRequest request)
    {
        if (request is null || !Uri.IsWellFormedUriString(request.Url, UriKind.Absolute))
            return BadRequest(new ErrorResponse { Message = "Valid webhook URL is required" });

        try
        {
            var subscription = await _webhookService.RegisterWebhookAsync(request.Url, request.Events);

            _logger.LogInformation("Webhook subscription registered: {SubscriptionId}", (object)subscription.Id);

            return CreatedAtAction(
                nameof(GetSubscriptionAsync),
                new { id = subscription.Id },
                new WebhookSubscriptionDto(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error registering webhook" });
        }
    }

    /// <summary>
    /// Gets details of a specific webhook subscription
    /// </summary>
    /// <remarks>
    /// GET /api/webhooks/subscriptions/{id}
    /// </remarks>
    /// <param name="id">The unique identifier of the webhook subscription.</param>
    /// <response code="200">Returns the webhook subscription details</response>
    /// <response code="404">If the webhook subscription is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("subscriptions/{id:guid}")]
    [ProducesResponseType(typeof(WebhookSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionAsync(Guid id)
    {
        try
        {
            var subscription = await _webhookService.GetWebhookAsync(id);

            if (subscription is null)
                return NotFound();

            return Ok(new WebhookSubscriptionDto(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving webhook" });
        }
    }

    /// <summary>
    /// Lists all registered webhook subscriptions
    /// </summary>
    /// <remarks>
    /// GET /api/webhooks/subscriptions
    /// </remarks>
    /// <param name="active">Optional filter to return only active or inactive subscriptions</param>
    /// <response code="200">Returns the list of webhook subscriptions</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(List<WebhookSubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSubscriptionsAsync([FromQuery] bool? active = null)
    {
        try
        {
            var subscriptions = await _webhookService.GetWebhooksAsync(active);
            return Ok(subscriptions.Select(s => new WebhookSubscriptionDto(s)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing webhooks");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error listing webhooks" });
        }
    }

    /// <summary>
    /// Deletes a webhook subscription - stops sending events to that endpoint
    /// </summary>
    [HttpDelete("subscriptions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubscriptionAsync(Guid id)
    {
        try
        {
            var success = await _webhookService.DeleteWebhookAsync(id);

            if (!success)
                return NotFound();

            _logger.LogInformation("Webhook subscription deleted: {SubscriptionId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error deleting webhook" });
        }
    }

    /// <summary>
    /// Gets delivery history for a webhook subscription - shows past deliveries and failures
    /// </summary>
    /// <remarks>
    /// GET /api/webhooks/subscriptions/{id}/deliveries
    /// </remarks>
    /// <param name="id">The unique identifier of the webhook subscription.</param>
    /// <param name="limit">The maximum number of deliveries to return (default 100).</param>
    /// <response code="200">Returns the list of webhook deliveries</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("subscriptions/{id:guid}/deliveries")]
    [ProducesResponseType(typeof(List<WebhookDeliveryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveriesAsync(Guid id, [FromQuery] int limit = 100)
    {
        try
        {
            var deliveries = await _webhookService.GetDeliveriesAsync(id, limit);
            return Ok(deliveries.Select(d => new WebhookDeliveryDto(d)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deliveries");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving deliveries" });
        }
    }

    /// <summary>
    /// Webhook delivery endpoint - external systems use this path to receive events
    /// Requires signature verification for security
    /// </summary>
    [HttpPost("deliver/{subscriptionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeliverAsync(Guid subscriptionId, [FromBody] dynamic payload)
    {
        try
        {
            var signature = HttpContext.Request.Headers["X-Webhook-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook delivery attempt without signature");
                return Unauthorized();
            }

            var success = await _webhookHandler.HandleDeliveryAsync(subscriptionId, payload, signature);

            if (!success)
                return BadRequest(new ErrorResponse { Message = "Invalid signature" });

            return Ok(new { status = "delivered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook delivery");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error processing webhook" });
        }
    }

    /// <summary>
    /// Tests a webhook subscription by sending a test payload
    /// Useful for validating webhook configurations
    /// </summary>
    /// <remarks>
    /// POST /api/webhooks/subscriptions/{id}/test
    /// </remarks>
    /// <param name="id">The unique identifier of the webhook subscription to test.</param>
    /// <response code="200">Returns the result of the webhook test</response>
    /// <response code="404">If the webhook subscription is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpPost("subscriptions/{id:guid}/test")]
    [ProducesResponseType(typeof(WebhookTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestWebhookAsync(Guid id)
    {
        try
        {
            var result = await _webhookService.TestWebhookAsync(id);

            if (result is null)
                return NotFound();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error testing webhook" });
        }
    }
}
