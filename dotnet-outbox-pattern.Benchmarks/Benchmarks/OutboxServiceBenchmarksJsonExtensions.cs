using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetOutboxPattern.Benchmarks;

/// <summary>
/// System.Text.Json serialization extensions for <see cref="OutboxServiceBenchmarks"/>
/// </summary>
public static class OutboxServiceBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes <see cref="OutboxServiceBenchmarks"/> to JSON string
    /// </summary>
    /// <param name="value">The benchmark instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation</returns>
    public static string ToJson(this OutboxServiceBenchmarks value, bool indented = false)
    {
        if (value == null)
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
    /// Deserializes JSON string to <see cref="OutboxServiceBenchmarks"/> instance
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized benchmark instance or null if JSON is invalid</returns>
    public static OutboxServiceBenchmarks? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OutboxServiceBenchmarks>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize JSON string to <see cref="OutboxServiceBenchmarks"/> instance
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter containing the deserialized instance or null</param>
    /// <returns>True if deserialization succeeded, false otherwise</returns>
    public static bool TryFromJson(string json, out OutboxServiceBenchmarks? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<OutboxServiceBenchmarks>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
