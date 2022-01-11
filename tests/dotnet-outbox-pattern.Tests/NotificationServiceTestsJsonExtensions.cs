#nullable enable

using System.Text.Json;
using DotnetOutboxPattern.Services;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Extension methods for serializing and deserializing notification test data to/from JSON.
/// </summary>
public static class NotificationServiceTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a <see cref="Notification"/> instance to JSON.
    /// </summary>
    /// <param name="value">The notification to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>The JSON string representation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this Notification value, bool indented = false)
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
    /// Deserializes a JSON string to a <see cref="Notification"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized notification, or <c>null</c> if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c>, empty, or whitespace.</exception>
    public static Notification? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<Notification>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="Notification"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized notification if successful.</param>
    /// <returns><c>true</c> if deserialization succeeds; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c>, empty, or whitespace.</exception>
    public static bool TryFromJson(string json, out Notification? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<Notification>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}