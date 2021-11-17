#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// System.Text.Json serialization extensions for EventsTests type
/// </summary>
public static class EventsTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the EventsTests instance to a JSON string
    /// </summary>
    /// <param name="value">The EventsTests instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation</returns>
    public static string ToJson(this EventsTests value, bool indented = false)
    {
        if (value is null)
        {
            return "{}";
        }

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes an EventsTests instance from a JSON string
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized EventsTests instance, or null if JSON is invalid</returns>
    public static EventsTests? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<EventsTests>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize an EventsTests instance from a JSON string
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter containing the deserialized instance, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeds, false otherwise</returns>
    public static bool TryFromJson(string json, out EventsTests? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<EventsTests>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}