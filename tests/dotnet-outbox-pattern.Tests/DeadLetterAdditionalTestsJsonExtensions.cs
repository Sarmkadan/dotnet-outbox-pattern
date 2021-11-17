#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Text.Json;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// System.Text.Json serialization extensions for DeadLetter type
/// </summary>
public static class DeadLetterAdditionalTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a DeadLetter instance to JSON string
    /// </summary>
    /// <param name="value">DeadLetter instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation</returns>
    public static string ToJson(this DeadLetter value, bool indented = false)
    {
        if (value is null)
        {
            return "null";
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
    /// Deserializes a DeadLetter instance from JSON string
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized DeadLetter instance or null if JSON is null or empty</returns>
    public static DeadLetter? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return null;
        }

        return JsonSerializer.Deserialize<DeadLetter>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a DeadLetter instance from JSON string
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter for the deserialized DeadLetter</param>
    /// <returns>True if deserialization succeeded, false otherwise</returns>
    public static bool TryFromJson(string json, out DeadLetter? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<DeadLetter>(json, _jsonSerializerOptions) ?? null;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}