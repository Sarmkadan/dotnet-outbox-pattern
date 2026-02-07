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
public class OutboxMessageController : ControllerBase
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
    [HttpPost("events")]
    [ProducesResponseType(typeof(OutboxMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishEventAsync([FromBody] PublishableEvent request)
    {
        if (request == null)
            return BadRequest(new ErrorResponse { Message = "Request body cannot be empty" });

        try
        {
            _logger.LogInformation(
                "Publishing event for aggregate {AggregateId} of type {AggregateType}",
                request.AggregateId, request.AggregateType);

            var message = await _outboxService.PublishEventAsync(request);

            return CreatedAtAction(
                nameof(GetMessageByIdAsync),
                new { id = message.Id },
                new OutboxMessageDto(message));
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
    [HttpGet("messages/{id:guid}")]
    [ProducesResponseType(typeof(OutboxMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageByIdAsync(Guid id)
    {
        try
        {
            var message = await _outboxService.GetMessageAsync(id);

            if (message == null)
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
    /// Retrieves messages by aggregate ID - returns all events for a specific entity
    /// Useful for auditing and understanding entity history
    /// </summary>
    [HttpGet("messages/aggregate/{aggregateId}")]
    [ProducesResponseType(typeof(List<OutboxMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessagesByAggregateAsync(string aggregateId, [FromQuery] int? limit = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                return BadRequest(new ErrorResponse { Message = "AggregateId is required" });

            var messages = await _outboxService.GetMessagesByAggregateAsync(aggregateId, limit ?? 50);
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
    /// Retrieves pending outbox messages with pagination - shows which events are waiting to be published
    /// </summary>
    [HttpGet("messages")]
    [ProducesResponseType(typeof(PaginatedResponse<OutboxMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingMessagesAsync(
        [FromQuery] OutboxMessageState? state = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 500)
                return BadRequest(new ErrorResponse { Message = "Invalid pagination parameters" });

            var messages = await _outboxService.GetPendingMessagesAsync(
                state: state,
                page: page,
                pageSize: pageSize);

            var result = new PaginatedResponse<OutboxMessageDto>
            {
                Page = page,
                PageSize = pageSize,
                Items = messages.Select(m => new OutboxMessageDto(m)).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending messages");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving messages" });
        }
    }

    /// <summary>
    /// Manually retries a failed message - useful for operational recovery
    /// </summary>
    [HttpPost("messages/{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryMessageAsync(Guid id)
    {
        try
        {
            var success = await _outboxService.RetryMessageAsync(id);

            if (!success)
            {
                _logger.LogWarning("Message not found for retry: {MessageId}", id);
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
    /// Archives published messages - moves them from active processing to archive
    /// Improves performance by reducing the active message set
    /// </summary>
    [HttpPost("messages/archive")]
    [ProducesResponseType(typeof(ArchiveResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ArchivePublishedMessagesAsync([FromQuery] int daysOld = 30)
    {
        try
        {
            if (daysOld < 1 || daysOld > 365)
                return BadRequest(new ErrorResponse { Message = "daysOld must be between 1 and 365" });

            var result = await _outboxService.ArchivePublishedMessagesAsync(daysOld);

            _logger.LogInformation("Archived {Count} messages", result.ArchivedCount);
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
