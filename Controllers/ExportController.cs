// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotnetOutboxPattern.Services;
using DotnetOutboxPattern.Dtos;

namespace DotnetOutboxPattern.Controllers;

/// <summary>
/// API controller for exporting outbox messages in various formats
/// Supports JSON, CSV, and XML output formats with filtering options
/// </summary>
[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IExportService exportService,
        ILogger<ExportController> logger)
    {
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports messages in the specified format
    /// Returns the export as a downloadable file
    /// </summary>
    [HttpPost("messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportMessagesAsync([FromBody] ExportRequest request)
    {
        if (request == null)
            return BadRequest(new ErrorResponse { Message = "Export request is required" });

        try
        {
            var supportedFormats = _exportService.GetSupportedFormats();
            if (!supportedFormats.Contains(request.Format.ToLower()))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = $"Unsupported format: {request.Format}. Supported: {string.Join(", ", supportedFormats)}"
                });
            }

            _logger.LogInformation(
                "Exporting messages in {Format} format (StartDate: {StartDate}, EndDate: {EndDate})",
                request.Format, request.StartDate, request.EndDate);

            var result = await _exportService.ExportAsync(request);

            var filename = $"outbox_messages_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{result.Format}";
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(result.Content);

            return File(
                contentBytes,
                result.ContentType,
                filename);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid export format requested");
            return BadRequest(new ErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting messages");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error exporting messages" });
        }
    }

    /// <summary>
    /// Gets supported export formats
    /// </summary>
    [HttpGet("formats")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public IActionResult GetSupportedFormatsAsync()
    {
        try
        {
            var formats = _exportService.GetSupportedFormats();
            return Ok(formats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supported formats");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving supported formats" });
        }
    }

    /// <summary>
    /// Gets export format details
    /// </summary>
    [HttpGet("formats/{format}")]
    [ProducesResponseType(typeof(ExportFormatInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetFormatDetailsAsync(string format)
    {
        try
        {
            var info = GetFormatInfo(format.ToLower());

            if (info == null)
                return NotFound();

            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving format details");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving format details" });
        }
    }

    /// <summary>
    /// Gets information about available export options and limitations
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ExportInfo), StatusCodes.Status200OK)]
    public IActionResult GetExportInfoAsync()
    {
        return Ok(new ExportInfo
        {
            MaxMessagesPerExport = 100000,
            SupportedFormats = _exportService.GetSupportedFormats(),
            DefaultFormat = "json",
            FilterableFields = new[] { "AggregateId", "AggregateType", "Topic", "State", "CreatedAt" }
        });
    }

    private ExportFormatInfo? GetFormatInfo(string format)
    {
        return format switch
        {
            "json" => new ExportFormatInfo
            {
                Format = "json",
                ContentType = "application/json",
                Extension = ".json",
                Description = "JSON format - preserves all message details including event data"
            },
            "csv" => new ExportFormatInfo
            {
                Format = "csv",
                ContentType = "text/csv",
                Extension = ".csv",
                Description = "CSV format - suitable for Excel and data analysis tools"
            },
            "xml" => new ExportFormatInfo
            {
                Format = "xml",
                ContentType = "application/xml",
                Extension = ".xml",
                Description = "XML format - suitable for enterprise systems"
            },
            _ => null
        };
    }
}

/// <summary>
/// Information about an export format
/// </summary>
public class ExportFormatInfo
{
    public string Format { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// General export service information
/// </summary>
public class ExportInfo
{
    public int MaxMessagesPerExport { get; set; }
    public List<string> SupportedFormats { get; set; } = new();
    public string DefaultFormat { get; set; } = string.Empty;
    public string[] FilterableFields { get; set; } = Array.Empty<string>();
}
