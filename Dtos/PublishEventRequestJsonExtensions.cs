#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Text.Json;

namespace DotnetOutboxPattern.Dtos;

/// <summary>
/// Provides extension methods for serializing and deserializing <see cref="PublishEventRequest"/> to/from JSON
/// </summary>
public static class PublishEventRequestJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="PublishEventRequest"/> to a JSON string
    /// </summary>
    /// <param name="value">The request to serialize</param>
    /// <param name="indented">Whether to indent the JSON output for readability</param>
    /// <returns>A JSON string representation of the request</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static string ToJson(this PublishEventRequest value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="PublishEventRequest"/> instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>A deserialized <see cref="PublishEventRequest"/> instance, or null if the JSON is empty or whitespace</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized</exception>
    public static PublishEventRequest? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<PublishEventRequest>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="PublishEventRequest"/> instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">Receives the deserialized value if successful</param>
    /// <returns>True if deserialization succeeded; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null</exception>
    public static bool TryFromJson(string json, out PublishEventRequest? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        value = default;

        return !string.IsNullOrWhiteSpace(json) && TryDeserialize(json, out value);
    }

    private static bool TryDeserialize(string json, out PublishEventRequest? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<PublishEventRequest>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}
