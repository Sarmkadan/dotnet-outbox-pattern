#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Dtos;
using DotnetOutboxPattern.Formatters;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Service for exporting outbox messages in various formats
/// Supports JSON, CSV, and XML export formats
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports messages in the specified format
    /// </summary>
    Task<ExportResult> ExportAsync(ExportRequest request);

    /// <summary>
    /// Exports messages to a file
    /// </summary>
    Task<string> ExportToFileAsync(ExportRequest request);

    /// <summary>
    /// Gets supported export formats
    /// </summary>
    List<string> GetSupportedFormats();
}

/// <summary>
/// Result of an export operation
/// </summary>
public sealed class ExportResult
{
    public string Content { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public long ContentSizeBytes { get; set; }
}

/// <summary>
/// Default implementation of export service
/// </summary>
public sealed class ExportService : IExportService
{
    private readonly IOutboxService _outboxService;
    private readonly IEnumerable<IDataFormatter> _formatters;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        IOutboxService outboxService,
        IEnumerable<IDataFormatter> formatters,
        ILogger<ExportService> logger)
    {
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
        _formatters = formatters ?? throw new ArgumentNullException(nameof(formatters));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExportResult> ExportAsync(ExportRequest request)
    {
        try
        {
            var format = request.Format.ToLower();
            var formatter = _formatters.FirstOrDefault(f => f.FormatName == format);

            if (formatter is null)
                throw new InvalidOperationException($"Unsupported export format: {format}");

            // Get messages based on filters
            var messages = await GetMessagesToExportAsync(request);

            _logger.LogInformation(
                "Exporting {Count} messages in {Format} format",
                messages.Count, format);

            var content = formatter.Format(messages);
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);

            return new ExportResult
            {
                Content = content,
                Format = format,
                ContentType = formatter.ContentType,
                MessageCount = messages.Count,
                ContentSizeBytes = contentBytes.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting messages");
            throw;
        }
    }

    public async Task<string> ExportToFileAsync(ExportRequest request)
    {
        try
        {
            var result = await ExportAsync(request);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var filename = $"outbox_export_{timestamp}.{result.Format}";
            var filepath = Path.Combine("exports", filename);

            var directory = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            await File.WriteAllTextAsync(filepath, result.Content);

            _logger.LogInformation(
                "Messages exported to file: {FilePath} ({SizeBytes} bytes)",
                filepath, result.ContentSizeBytes);

            return filepath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting messages to file");
            throw;
        }
    }

    public List<string> GetSupportedFormats()
    {
        return _formatters.Select(f => f.FormatName).ToList();
    }

    private async Task<List<Domain.OutboxMessage>> GetMessagesToExportAsync(ExportRequest request)
    {
        // Get all messages - in a real implementation, would apply date/status filters
        var messages = await _outboxService.GetAllMessagesAsync();

        if (request.StartDate.HasValue)
            messages = messages.Where(m => m.CreatedAt >= request.StartDate.Value).ToList();

        if (request.EndDate.HasValue)
            messages = messages.Where(m => m.CreatedAt <= request.EndDate.Value).ToList();

        if (!string.IsNullOrEmpty(request.Status))
            messages = messages.Where(m => m.State.ToString() == request.Status).ToList();

        return messages;
    }
}
