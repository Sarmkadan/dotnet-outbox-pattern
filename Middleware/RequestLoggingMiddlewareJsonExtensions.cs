#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetOutboxPattern.Middleware;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for RequestLoggingMiddleware.
/// Enables serialization to JSON strings and deserialization from JSON strings.
/// </summary>
public static class RequestLoggingMiddlewareJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes the RequestLoggingMiddleware instance to a JSON string.
    /// </summary>
    /// <param name="value">The middleware instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the middleware instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this RequestLoggingMiddleware value, bool indented = false) =>
        value is null
            ? throw new ArgumentNullException(nameof(value))
            : JsonSerializer.Serialize(value, GetJsonSerializerOptions(indented));

    /// <summary>
    /// Deserializes a JSON string to a RequestLoggingMiddleware instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized RequestLoggingMiddleware instance, or <see langword="null"/> if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/> or empty.</exception>
    public static RequestLoggingMiddleware? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<RequestLoggingMiddleware>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a RequestLoggingMiddleware instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Output parameter containing the deserialized instance, or <see langword="null"/> if deserialization fails.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/> or empty.</exception>
    public static bool TryFromJson(string json, [NotNullWhen(true)] out RequestLoggingMiddleware? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<RequestLoggingMiddleware>(json, _jsonSerializerOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Gets JsonSerializerOptions configured for the specified formatting.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>Configured JsonSerializerOptions.</returns>
    private static JsonSerializerOptions GetJsonSerializerOptions(bool indented) =>
        indented
            ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
            : _jsonSerializerOptions;
}