#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DotnetOutboxPattern.Controllers;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for MetricsController
/// Enables JSON serialization of controller instances and deserialization back to controller state
/// </summary>
public static class MetricsControllerJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    /// <summary>
    /// Serializes the MetricsController instance to a JSON string
    /// </summary>
    /// <param name="value">The MetricsController instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>A JSON string representation of the controller</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static string ToJson(this MetricsController value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a MetricsController instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>A MetricsController instance populated from JSON, or null if deserialization fails</returns>
    /// <exception cref="ArgumentException">Thrown when json is null or empty</exception>
    public static MetricsController? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<MetricsController>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a MetricsController instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">Output parameter containing the deserialized controller or null</param>
    /// <returns>True if deserialization succeeds; false if it fails (exception caught internally)</returns>
    /// <exception cref="ArgumentException">Thrown when json is null or empty</exception>
    public static bool TryFromJson(string json, out MetricsController? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<MetricsController>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}