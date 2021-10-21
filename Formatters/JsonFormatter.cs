// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Formatters;

/// <summary>
/// Formatter for exporting outbox messages to JSON format
/// Preserves all message details including event data
/// </summary>
public class JsonFormatter : IDataFormatter
{
    public string FormatName => "json";
    public string ContentType => "application/json";

    /// <summary>
    /// Formats messages as a JSON array with proper indentation
    /// </summary>
    public string Format(List<OutboxMessage> messages)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var data = new
        {
            Messages = messages.Select(m => new
            {
                m.Id,
                m.IdempotencyKey,
                m.AggregateId,
                m.AggregateType,
                m.EventTypeName,
                m.Topic,
                State = m.State.ToString(),
                m.PublishAttempts,
                m.MaxPublishAttempts,
                m.CreatedAt,
                m.PublishedAt,
                m.ErrorMessage,
                m.PartitionKey,
                m.CorrelationId,
                m.CausationId,
                EventData = ParseJsonSafe(m.EventData),
                Metadata = ParseJsonSafe(m.Metadata)
            }).ToList(),
            ExportedAt = DateTime.UtcNow,
            Count = messages.Count
        };

        return JsonSerializer.Serialize(data, options);
    }

    /// <summary>
    /// Safely parses JSON strings, returning parsed object or raw string if invalid
    /// </summary>
    private static object? ParseJsonSafe(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<object>(json);
        }
        catch
        {
            return json;
        }
    }
}
