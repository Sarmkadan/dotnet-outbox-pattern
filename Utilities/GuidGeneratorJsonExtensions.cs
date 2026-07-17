#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Provides System.Text.Json serialization extensions for Guid operations
/// </summary>
public static class GuidGeneratorJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes a Guid to JSON string
    /// </summary>
    /// <param name="guid">The Guid to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="guid"/> is not a valid Guid</exception>
    public static string ToJson(this Guid guid, bool indented = false)
        => JsonSerializer.Serialize(guid, indented ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions);

    /// <summary>
    /// Deserializes a JSON string to a Guid
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized Guid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace</exception>
    /// <exception cref="JsonException">Thrown when JSON is invalid</exception>
    public static Guid FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<Guid>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a Guid
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter for the deserialized value</param>
    /// <returns>True if deserialization succeeded; false otherwise</returns>
    public static bool TryFromJson(string json, out Guid value)
    {
        try
        {
            value = FromJson(json);
            return true;
        }
        catch (JsonException)
        {
            value = Guid.Empty;
            return false;
        }
    }
}