#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotnetOutboxPattern.Services;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Dtos;

namespace DotnetOutboxPattern.Controllers;

/// <summary>
/// API controller for managing outbox messages - the primary interface for publishing events
/// </summary>
[ApiController]
[Route("api/outbox")]
public sealed class OutboxMessageController : ControllerBase
{
    private readonly IOutboxService _outboxService;
    private readonly ILogger<OutboxMessageController> _logger;

    public OutboxMessageController(
        IOutboxService outboxService,
        ILogger<OutboxMessageController> logger)
    {
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes a new event to the outbox. The event is persisted and published asynchronously.
    /// </summary>
    /// <remarks>
    /// POST /api/outbox/events
    /// </remarks>
    /// <param name="request">The event to publish.</param>
    /// <response code="201">Returns the created outbox message.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPost("events")]
    [ProducesResponseType(typeof(OutboxMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishEventAsync([FromBody] PublishableEvent request)
    {
        if (request is null)
            return BadRequest(new ErrorResponse { Message = "Request body cannot be empty" });

        try
        {
            _logger.LogInformation("Publishing event to topic {Topic}", request.Topic);

            var message = await _outboxService.PublishEventAsync(request);

            // Build the location explicitly rather than through CreatedAtAction: this
            // controller's attribute-routed actions do not resolve reliably through the
            // MVC link generator here, and the message ID is all that is needed to build
            // the canonical URL for GetMessageByIdAsync.
            return Created($"/api/outbox/messages/{message.Id}", new OutboxMessageDto(message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid event publication request");
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error publishing event" });
        }
    }

    /// <summary>
    /// Retrieves a specific outbox message by ID with full details including event data and status
    /// </summary>
    /// <remarks>
    /// GET /api/outbox/messages/{id}
    /// </remarks>
    /// <param name="id">The unique identifier of the outbox message.</param>
    /// <response code="200">Returns the outbox message.</response>
    /// <response code="404">If the outbox message is not found.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("messages/{id:guid}")]
    [ProducesResponseType(typeof(OutboxMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageByIdAsync(Guid id)
    {
        try
        {
            var message = await _outboxService.GetMessageAsync(id);

            if (message is null)
            {
                _logger.LogWarning("Message not found: {MessageId}", id);
                return NotFound();
            }

            return Ok(new OutboxMessageDto(message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving message {MessageId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving message" });
        }
    }

    /// <summary>
    /// Retrieves messages filtered by aggregate ID
    /// </summary>
    /// <remarks>
    /// GET /api/outbox/messages/aggregate/{aggregateId}
    /// </remarks>
    /// <param name="aggregateId">The aggregate identifier to filter messages by.</param>
    /// <param name="limit">Maximum number of messages to return (optional, defaults to 50).</param>
    /// <response code="200">Returns the list of outbox messages for the aggregate.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("messages/aggregate/{aggregateId}")]
    [ProducesResponseType(typeof(List<OutboxMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessagesByAggregateAsync(string aggregateId, [FromQuery] int? limit = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                return BadRequest(new ErrorResponse { Message = "AggregateId is required" });

            var all = await _outboxService.GetAllMessagesAsync();
            var messages = all
                .Where(m => m.AggregateId == aggregateId)
                .Take(limit ?? 50)
                .ToList();

            return Ok(messages.Select(m => new OutboxMessageDto(m)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for aggregate {AggregateId}", aggregateId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving messages" });
        }
    }

    /// <summary>
    /// Retrieves outbox messages with optional state filter and pagination
    /// </summary>
    /// <remarks>
    /// GET /api/outbox/messages
    /// </remarks>
    /// <param name="state">Filter messages by state (optional).</param>
    /// <param name="page">The page number to retrieve (defaults to 1).</param>
    /// <param name="pageSize">The number of items per page (defaults to 50, max 500).</param>
    /// <response code="200">Returns the paginated list of outbox messages.</response>
    /// <response code="400">If pagination parameters are invalid.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("messages")]
    [ProducesResponseType(typeof(PaginatedResponse<OutboxMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessagesAsync(
        [FromQuery] OutboxMessageState? state = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 500)
                return BadRequest(new ErrorResponse { Message = "Invalid pagination parameters" });

            var all = await _outboxService.GetAllMessagesAsync();

            var filtered = state.HasValue
                ? all.Where(m => m.State == state.Value).ToList()
                : all;

            var paged = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new PaginatedResponse<OutboxMessageDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = filtered.Count(),
                Items = paged.Select(m => new OutboxMessageDto(m)).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving messages" });
        }
    }

    /// <summary>
    /// Manually retries a failed message - useful for operational recovery
    /// </summary>
    /// <remarks>
    /// POST /api/outbox/messages/{id}/retry
    /// </remarks>
    /// <param name="id">The unique identifier of the outbox message to retry.</param>
    /// <response code="204">If the message retry was initiated successfully.</response>
    /// <response code="404">If the message is not found or not eligible for retry.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPost("messages/{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryMessageAsync(Guid id)
    {
        try
        {
            var success = await _outboxService.RetryFailedMessageAsync(id);

            if (!success)
            {
                _logger.LogWarning("Message not found or not eligible for retry: {MessageId}", id);
                return NotFound();
            }

            _logger.LogInformation("Message retry initiated: {MessageId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying message {MessageId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrying message" });
        }
    }

    /// <summary>
    /// Archives published messages older than the specified number of days
    /// </summary>
    /// <remarks>
    /// POST /api/outbox/messages/archive
    /// </remarks>
    /// <param name="daysOld">The age in days for messages to be archived (must be between 1 and 365).</param>
    /// <response code="200">Returns the result of the archive operation.</response>
    /// <response code="400">If the daysOld parameter is outside the valid range.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPost("messages/archive")]
    [ProducesResponseType(typeof(ArchiveResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ArchivePublishedMessagesAsync([FromQuery] int daysOld = 30)
    {
        try
        {
            if (daysOld < 1 || daysOld > 365)
                return BadRequest(new ErrorResponse { Message = "daysOld must be between 1 and 365" });

            var olderThan = DateTime.UtcNow.AddDays(-daysOld);
            await _outboxService.ArchiveOldMessagesAsync(olderThan);

            var result = new ArchiveResult { Status = "Success" };

            _logger.LogInformation("Archived messages older than {DaysOld} days", daysOld);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving messages");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error archiving messages" });
        }
    }

    /// <summary>
    /// Gets detailed statistics about the outbox - provides operational insights
    /// </summary>
    /// <remarks>
    /// GET /api/outbox/statistics
    /// </remarks>
    /// <response code="200">Returns the outbox statistics.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(OutboxStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatisticsAsync()
    {
        try
        {
            var stats = await _outboxService.GetStatisticsAsync();
            return Ok(new OutboxStatisticsDto(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving statistics" });
        }
    }
}