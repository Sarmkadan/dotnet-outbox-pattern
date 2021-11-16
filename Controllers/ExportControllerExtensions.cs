#nullable enable

using Microsoft.AspNetCore.Mvc;
using DotnetOutboxPattern.Dtos;
using System.Text.Json;

namespace DotnetOutboxPattern.Controllers;

/// <summary>
/// Extension methods for ExportController providing additional functionality
/// </summary>
public static class ExportControllerExtensions
{
    /// <summary>
    /// Creates a simplified export request with default values for the specified format
    /// </summary>
    /// <param name="controller">The ExportController instance</param>
    /// <param name="format">The export format (json, csv, xml)</param>
    /// <returns>ExportRequest configured with default values</returns>
    public static ExportRequest CreateExportRequest(this ExportController controller, string format)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Format cannot be null or empty", nameof(format));
        }

        return new ExportRequest
        {
            Format = format.ToLower(),
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow,
            Status = null,
            IncludeEventData = false
        };
    }

    /// <summary>
    /// Exports messages with simplified parameters and returns the file directly
    /// </summary>
    /// <param name="controller">The ExportController instance</param>
    /// <param name="format">The export format (json, csv, xml)</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>IActionResult with the exported file</returns>
    public static async Task<IActionResult> ExportMessagesAsync(
        this ExportController controller,
        string format,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Format cannot be null or empty", nameof(format));
        }

        var request = controller.CreateExportRequest(format);

        if (startDate.HasValue)
        {
            request.StartDate = startDate.Value;
        }

        if (endDate.HasValue)
        {
            request.EndDate = endDate.Value;
        }

        return await controller.ExportMessagesAsync(request);
    }

    /// <summary>
    /// Gets export format details with simplified parameter
    /// </summary>
    /// <param name="controller">The ExportController instance</param>
    /// <param name="format">The export format to get details for</param>
    /// <returns>IActionResult with format details or 404 if not found</returns>
    public static IActionResult GetFormatDetails(this ExportController controller, string format)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Format cannot be null or empty", nameof(format));
        }

        return controller.GetFormatDetailsAsync(format);
    }

    /// <summary>
    /// Gets export information including supported formats and limitations
    /// </summary>
    /// <param name="controller">The ExportController instance</param>
    /// <returns>ExportInfo object with configuration details</returns>
    public static IActionResult GetExportInfo(this ExportController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return controller.GetExportInfoAsync();
    }

    /// <summary>
    /// Checks if a format is supported by calling GetSupportedFormatsAsync
    /// </summary>
    /// <param name="controller">The ExportController instance</param>
    /// <param name="format">The format to check</param>
    /// <returns>True if the format is supported, false otherwise</returns>
    public static bool IsFormatSupported(this ExportController controller, string format)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (string.IsNullOrWhiteSpace(format))
        {
            return false;
        }

        var result = controller.GetSupportedFormatsAsync();
        if (result is OkObjectResult okResult)
        {
            if (okResult.Value is List<string> formats)
            {
                return formats.Contains(format.ToLower());
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a JSON-serialized string containing all supported formats
    /// </summary>
    /// <param name="controller">The ExportController instance</param>
    /// <returns>JSON string of supported formats</returns>
    public static string GetSupportedFormatsJson(this ExportController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var result = controller.GetSupportedFormatsAsync();
        if (result is OkObjectResult okResult)
        {
            if (okResult.Value is List<string> formats)
            {
                return JsonSerializer.Serialize(formats);
            }
        }

        return "[]";
    }

    /// <summary>
    /// Exports messages in JSON format with default parameters
    /// </summary>
    /// <param name="controller">The ExportController instance</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="includeEventData">Whether to include event data in export</param>
    /// <returns>IActionResult with the exported JSON file</returns>
    public static async Task<IActionResult> ExportJsonAsync(
        this ExportController controller,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeEventData = false)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var request = new ExportRequest
        {
            Format = "json",
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-7),
            EndDate = endDate ?? DateTime.UtcNow,
            Status = null,
            IncludeEventData = includeEventData
        };

        return await controller.ExportMessagesAsync(request);
    }

    /// <summary>
    /// Exports messages in CSV format with default parameters
    /// </summary>
    /// <param name="controller">The ExportController instance</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>IActionResult with the exported CSV file</returns>
    public static async Task<IActionResult> ExportCsvAsync(
        this ExportController controller,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var request = new ExportRequest
        {
            Format = "csv",
            StartDate = startDate ?? DateTime.UtcNow.AddDays(-7),
            EndDate = endDate ?? DateTime.UtcNow,
            Status = null,
            IncludeEventData = false
        };

        return await controller.ExportMessagesAsync(request);
    }
}