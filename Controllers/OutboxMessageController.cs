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
            _logger.LogInformation("Publishing event to topic {Topic}", request.Topic);

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
    /// Retrieves messages filtered by aggregate ID
    /// </summary>
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
