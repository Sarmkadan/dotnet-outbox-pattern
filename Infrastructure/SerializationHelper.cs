#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetOutboxPattern.Exceptions;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Helper class for JSON serialization and deserialization
/// Provides consistent serialization settings across the application
/// </summary>
public static class SerializationHelper
{
    /// <summary>
    /// JSON serializer options configured for the outbox pattern
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new GuidConverter(),
            new DateTimeConverter()
        }
    };

    /// <summary>
    /// Pretty-printed JSON options for debugging and logging
    /// </summary>
    public static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new GuidConverter(),
            new DateTimeConverter()
        }
    };

    /// <summary>
    /// Serializes an object to JSON
    /// </summary>
    public static string Serialize<T>(T obj) where T : class
    {
        try
        {
            return JsonSerializer.Serialize(obj, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new SerializationException($"Failed to serialize object of type {typeof(T).Name}", typeof(T).Name, ex);
        }
        catch (Exception ex)
        {
            throw new SerializationException($"Serialization error for type {typeof(T).Name}", typeof(T).Name, ex);
        }
    }

    /// <summary>
    /// Deserializes JSON to an object
    /// </summary>
    public static T Deserialize<T>(string json) where T : class
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
            if (result is null)
                throw new SerializationException($"Deserialized null for type {typeof(T).Name}", typeof(T).Name);

            return result;
        }
        catch (JsonException ex)
        {
            throw new SerializationException($"Invalid JSON for type {typeof(T).Name}", typeof(T).Name, ex);
        }
        catch (SerializationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SerializationException($"Deserialization error for type {typeof(T).Name}", typeof(T).Name, ex);
        }
    }

    /// <summary>
    /// Deserializes JSON to a dynamic object
    /// </summary>
    public static object? DeserializeDynamic(string json, Type targetType)
    {
        try
        {
            return JsonSerializer.Deserialize(json, targetType, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new SerializationException($"Invalid JSON for type {targetType.Name}", targetType.Name, ex);
        }
        catch (Exception ex)
        {
            throw new SerializationException($"Deserialization error for type {targetType.Name}", targetType.Name, ex);
        }
    }

    /// <summary>
    /// Gets pretty-printed JSON for debugging
    /// </summary>
    public static string SerializePretty<T>(T obj) where T : class
    {
        try
        {
            return JsonSerializer.Serialize(obj, PrettyOptions);
        }
        catch (Exception ex)
        {
            throw new SerializationException($"Failed to serialize object of type {typeof(T).Name}", typeof(T).Name, ex);
        }
    }

    /// <summary>
    /// Validates if a string is valid JSON
    /// </summary>
    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Custom JSON converter for Guid
    /// </summary>
    private class GuidConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Guid.Parse(reader.GetString() ?? throw new InvalidOperationException("GUID cannot be null"));
        }

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// Custom JSON converter for DateTime
    /// </summary>
    private class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (string.IsNullOrEmpty(str))
                throw new InvalidOperationException("DateTime cannot be null");

            if (DateTime.TryParseExact(str, "O", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt;

            // Machine-facing payload: never let the ambient culture decide how it is read.
            return DateTime.Parse(str, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("O"));
        }
    }
}
