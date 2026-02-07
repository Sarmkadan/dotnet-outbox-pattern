// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotnetOutboxPattern.Services;
using DotnetOutboxPattern.Dtos;

namespace DotnetOutboxPattern.Controllers;

/// <summary>
/// API controller for managing dead letter queue operations
/// Provides endpoints for reviewing, requeuing, and analyzing failed messages
/// </summary>
[ApiController]
[Route("api/deadletters")]
public class DeadLetterController : ControllerBase
{
    private readonly IDeadLetterService _dlService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DeadLetterController> _logger;

    public DeadLetterController(
        IDeadLetterService dlService,
        INotificationService notificationService,
        ILogger<DeadLetterController> logger)
    {
        _dlService = dlService ?? throw new ArgumentNullException(nameof(dlService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets unreviewed dead letters - messages awaiting operator review
    /// </summary>
    [HttpGet("unreviewed")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreviewedAsync([FromQuery] int limit = 100)
    {
        try
        {
            var deadLetters = await _dlService.GetUnreviewedAsync();
            return Ok(deadLetters.Take(limit).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unreviewed dead letters");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving dead letters" });
        }
    }

    /// <summary>
    /// Gets dead letters with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeadLettersAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 500)
                return BadRequest(new ErrorResponse { Message = "Invalid pagination parameters" });

            var deadLetters = await _dlService.GetAllAsync();

            var result = new PaginatedResponse<object>
            {
                Page = page,
                PageSize = pageSize,
                Items = deadLetters.Skip((page - 1) * pageSize).Take(pageSize).Cast<object>().ToList(),
                TotalItems = deadLetters.Count
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letters");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving dead letters" });
        }
    }

    /// <summary>
    /// Gets a specific dead letter entry with full details
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeadLetterAsync(Guid id)
    {
        try
        {
            var deadLetter = await _dlService.GetByIdAsync(id);

            if (deadLetter == null)
                return NotFound();

            return Ok(deadLetter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter {DeadLetterId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving dead letter" });
        }
    }

    /// <summary>
    /// Reviews a dead letter - marks it as reviewed with notes
    /// </summary>
    [HttpPut("{id:guid}/review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewAsync(Guid id, [FromBody] ReviewDeadLetterRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Notes))
            return BadRequest(new ErrorResponse { Message = "Notes are required" });

        try
        {
            var success = await _dlService.ReviewAsync(id, request.Notes);

            if (!success)
            {
                _logger.LogWarning("Dead letter not found for review: {DeadLetterId}", id);
                return NotFound();
            }

            _logger.LogInformation("Dead letter reviewed: {DeadLetterId}", id);

            // Send notification
            await _notificationService.SendAsync(new Notification
            {
                Title = "Dead Letter Reviewed",
                Message = $"Dead letter {id} has been reviewed",
                Severity = NotificationSeverity.Info
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing dead letter {DeadLetterId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error reviewing dead letter" });
        }
    }

    /// <summary>
    /// Requeues a dead letter for retry - moves it back to pending
    /// </summary>
    [HttpPost("{id:guid}/requeue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequeueAsync(Guid id, [FromBody] RequeueDeadLetterRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Reason))
            return BadRequest(new ErrorResponse { Message = "Reason is required" });

        try
        {
            var success = await _dlService.RequeueAsync(id, request.Reason);

            if (!success)
            {
                _logger.LogWarning("Dead letter not found for requeue: {DeadLetterId}", id);
                return NotFound();
            }

            _logger.LogInformation("Dead letter requeued: {DeadLetterId}", id);

            // Send notification
            await _notificationService.SendAsync(new Notification
            {
                Title = "Dead Letter Requeued",
                Message = $"Dead letter {id} has been requeued for retry: {request.Reason}",
                Severity = NotificationSeverity.Warning
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requeuing dead letter {DeadLetterId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error requeuing dead letter" });
        }
    }

    /// <summary>
    /// Gets statistics about the dead letter queue
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(DeadLetterStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatisticsAsync()
    {
        try
        {
            var stats = await _dlService.GetStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter statistics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving statistics" });
        }
    }

    /// <summary>
    /// Permanently deletes a dead letter entry
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        try
        {
            var success = await _dlService.DeleteAsync(id);

            if (!success)
                return NotFound();

            _logger.LogInformation("Dead letter deleted: {DeadLetterId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dead letter");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error deleting dead letter" });
        }
    }

    /// <summary>
    /// Exports dead letters in specified format
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAsync([FromBody] ExportRequest request)
    {
        try
        {
            var format = request.Format.ToLower();
            var deadLetters = await _dlService.GetAllAsync();

            // In a real implementation, this would use formatters
            var content = format switch
            {
                "json" => System.Text.Json.JsonSerializer.Serialize(deadLetters, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                "csv" => ExportAsCSV(deadLetters),
                _ => ExportAsCSV(deadLetters)
            };

            var filename = $"deadletters_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format}";
            return File(System.Text.Encoding.UTF8.GetBytes(content), "application/octet-stream", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dead letters");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error exporting dead letters" });
        }
    }

    private string ExportAsCSV(List<dynamic> deadLetters)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,MessageId,AggregateId,ErrorMessage,CreatedAt,ReviewedAt,Status");

        foreach (var dl in deadLetters.Take(1000))
        {
            sb.AppendLine($"{dl.Id},{dl.MessageId},{dl.AggregateId},\"{dl.ErrorMessage}\",{dl.CreatedAt},{dl.ReviewedAt},{dl.Status}");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Dead letter queue statistics
/// </summary>
public class DeadLetterStatistics
{
    public int TotalDeadLetters { get; set; }
    public int UnreviewedCount { get; set; }
    public int ReviewedCount { get; set; }
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public DateTime? OldestDeadLetter { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
