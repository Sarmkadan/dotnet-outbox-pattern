// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Formatters;

/// <summary>
/// Formatter for exporting outbox messages to CSV format
/// Useful for reporting and data analysis
/// </summary>
public class CsvFormatter : IDataFormatter
{
    public string FormatName => "csv";
    public string ContentType => "text/csv";

    /// <summary>
    /// Formats a list of messages as CSV with headers
    /// </summary>
    public string Format(List<OutboxMessage> messages)
    {
        var sb = new StringBuilder();

        // Write CSV headers
        sb.AppendLine("MessageId,IdempotencyKey,AggregateId,AggregateType,EventType,Topic,State,PublishAttempts,MaxAttempts,CreatedAt,PublishedAt,ErrorMessage");

        // Write data rows
        foreach (var message in messages)
        {
            sb.AppendLine(FormatRow(message));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a single message as a CSV row
    /// </summary>
    private string FormatRow(OutboxMessage message)
    {
        var columns = new[]
        {
            EscapeCsv(message.Id.ToString()),
            EscapeCsv(message.IdempotencyKey),
            EscapeCsv(message.AggregateId),
            EscapeCsv(message.AggregateType),
            EscapeCsv(message.EventTypeName),
            EscapeCsv(message.Topic),
            EscapeCsv(message.State.ToString()),
            message.PublishAttempts.ToString(),
            message.MaxPublishAttempts.ToString(),
            message.CreatedAt.ToString("o"),
            message.PublishedAt?.ToString("o") ?? "",
            EscapeCsv(message.ErrorMessage ?? "")
        };

        return string.Join(",", columns);
    }

    /// <summary>
    /// Escapes CSV special characters by wrapping in quotes if needed
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}

/// <summary>
/// Interface for data formatters
/// </summary>
public interface IDataFormatter
{
    string FormatName { get; }
    string ContentType { get; }
    string Format(List<OutboxMessage> messages);
}
