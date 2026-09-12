#nullable enable
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
public sealed class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IExportService exportService,
        ILogger<ExportController> logger)
    {
        ArgumentNullException.ThrowIfNull(exportService);
        ArgumentNullException.ThrowIfNull(logger);

        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Exports messages in the specified format
    /// </summary>
    /// <remarks>
    /// POST /api/export/messages
    /// </remarks>
    /// <param name="request">The export request containing format and date range.</param>
    /// <returns>The export as a downloadable file.</returns>
    /// <response code="200">Returns the export file</response>
    /// <response code="400">If the request is null or the format is unsupported</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpPost("messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportMessagesAsync([FromBody] ExportRequest request)
    {
        if (request is null)
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
    /// <remarks>
    /// GET /api/export/formats
    /// </remarks>
    /// <returns>A list of supported export formats.</returns>
    /// <response code="200">Returns the list of supported formats</response>
    /// <response code="500">If an unexpected error occurs</response>
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
    /// <remarks>
    /// GET /api/export/formats/{format}
    /// </remarks>
    /// <param name="format">The format to get details for (json, csv, xml).</param>
    /// <returns>The export format details.</returns>
    /// <response code="200">Returns the format details</response>
    /// <response code="404">If the format is not found</response>
    /// <response code="500">If an unexpected error occurs</response>
    [HttpGet("formats/{format}")]
    [ProducesResponseType(typeof(ExportFormatInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetFormatDetailsAsync(string format)
    {
        try
        {
            var info = GetFormatInfo(format.ToLower());

            if (info is null)
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
    /// <remarks>
    /// GET /api/export/info
    /// </remarks>
    /// <returns>Information about export options and limitations.</returns>
    /// <response code="200">Returns the export information</response>
    /// <response code="500">If an unexpected error occurs</response>
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
public sealed class ExportFormatInfo
{
    public string Format { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// General export service information
/// </summary>
public sealed class ExportInfo
{
    public int MaxMessagesPerExport { get; set; }
    public List<string> SupportedFormats { get; set; } = new();
    public string DefaultFormat { get; set; } = string.Empty;
    public string[] FilterableFields { get; set; } = Array.Empty<string>();
}
