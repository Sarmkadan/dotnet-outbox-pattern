#nullable enable

using System.Text.Json;
using DotnetOutboxPattern.Services;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extension methods for <see cref="Notification"/> instances used in test scenarios.
/// </summary>
public static class NotificationServiceTestsJsonExtensions
{
    /// <summary>
    /// The default <see cref="JsonSerializerOptions"/> used for serialization and deserialization.
    /// Uses camelCase property naming and no indentation by default.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a <see cref="Notification"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="Notification"/> instance to serialize.</param>
    /// <param name="indented">If <c>true</c>, the JSON will be formatted with indentation for readability.</param>
    /// <returns>A JSON string representation of the <see cref="Notification"/>.</returns>
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
    /// Deserializes a JSON string into a <see cref="Notification"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="Notification"/> instance, or <c>null</c> if deserialization fails.</returns>
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
    /// Attempts to deserialize a JSON string into a <see cref="Notification"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">When this method returns, contains the deserialized <see cref="Notification"/> if successful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
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
