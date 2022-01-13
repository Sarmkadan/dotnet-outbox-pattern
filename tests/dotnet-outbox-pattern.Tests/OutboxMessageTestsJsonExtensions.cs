#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="OutboxMessageTests"/>.
/// </summary>
public static class OutboxMessageTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes an <see cref="OutboxMessageTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="OutboxMessageTests"/> instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the <see cref="OutboxMessageTests"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this OutboxMessageTests value, bool indented = false)
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
    /// Deserializes a JSON string to an <see cref="OutboxMessageTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An <see cref="OutboxMessageTests"/> instance, or <see langword="null"/> if the JSON is <see langword="null"/>, whitespace, or empty.</returns>
    public static OutboxMessageTests? FromJson(string json) => string.IsNullOrWhiteSpace(json)
        ? null
        : JsonSerializer.Deserialize<OutboxMessageTests>(json, _jsonSerializerOptions);

    /// <summary>
    /// Attempts to deserialize a JSON string to an <see cref="OutboxMessageTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized <see cref="OutboxMessageTests"/> instance if deserialization succeeds.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool TryFromJson(string json, out OutboxMessageTests? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<OutboxMessageTests>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}