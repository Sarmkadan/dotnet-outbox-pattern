#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;

namespace DotnetOutboxPattern.Middleware;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for
/// PerformanceMonitoringMiddleware and related types for configuration persistence,
/// telemetry export, and inter-process communication scenarios.
/// </summary>
public static class PerformanceMonitoringMiddlewareJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions _jsonOptionsIndented = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the PerformanceMonitoringMiddleware instance to a JSON string.
    /// Note: Only the Monitor property can be meaningfully serialized as the middleware
    /// requires runtime dependencies (RequestDelegate, ILogger) that cannot be deserialized.
    /// </summary>
    /// <param name="value">The middleware instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>A JSON string representing the middleware instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static string ToJson(this PerformanceMonitoringMiddleware value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented ? _jsonOptionsIndented : _jsonOptions;
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Serializes the PerformanceMonitor instance to a JSON string.
    /// </summary>
    /// <param name="monitor">The performance monitor to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>A JSON string representing the performance monitor</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="monitor"/> is null</exception>
    public static string ToJson(this PerformanceMonitor monitor, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        var options = indented ? _jsonOptionsIndented : _jsonOptions;
        return JsonSerializer.Serialize(monitor, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a PerformanceMonitor instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>A PerformanceMonitor instance, or null if JSON is empty or whitespace</returns>
    /// <exception cref="JsonException">Thrown when JSON is invalid or cannot be deserialized</exception>
    public static PerformanceMonitor? FromJsonToMonitor(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PerformanceMonitor>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonException("Failed to deserialize PerformanceMonitor from JSON", ex);
        }
    }

    /// <summary>
    /// Deserializes a JSON string to a PerformanceMonitoringMiddleware instance.
    /// Note: The middleware requires runtime dependencies (RequestDelegate, ILogger, PerformanceMonitor)
    /// so this method is primarily useful for deserializing the Monitor property.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>A PerformanceMonitoringMiddleware instance, or null if JSON is empty or whitespace</returns>
    /// <exception cref="JsonException">Thrown when JSON is invalid or cannot be deserialized</exception>
    public static PerformanceMonitoringMiddleware? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PerformanceMonitoringMiddleware>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonException("Failed to deserialize PerformanceMonitoringMiddleware from JSON", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a PerformanceMonitor instance.
    /// Safely handles malformed JSON and returns false instead of throwing.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="monitor">Receives the deserialized monitor if successful</param>
    /// <returns>True if deserialization succeeded; false otherwise</returns>
    public static bool TryFromJsonToMonitor(string json, out PerformanceMonitor? monitor)
    {
        monitor = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            monitor = JsonSerializer.Deserialize<PerformanceMonitor>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a PerformanceMonitoringMiddleware instance.
    /// Safely handles malformed JSON and returns false instead of throwing.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">Receives the deserialized instance if successful</param>
    /// <returns>True if deserialization succeeded; false otherwise</returns>
    public static bool TryFromJson(string json, out PerformanceMonitoringMiddleware? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<PerformanceMonitoringMiddleware>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}