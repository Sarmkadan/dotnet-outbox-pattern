#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Default implementation of IOutboxSerializer using System.Text.Json.
/// </summary>
public sealed class SystemTextJsonOutboxSerializer : IOutboxSerializer
{
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonOutboxSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public SystemTextJsonOutboxSerializer(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Serialize<T>(T value)
    {
        if (value is null)
        {
            return "null";
        }
        return JsonSerializer.Serialize(value, _options);
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(json, _options);
    }

    /// <inheritdoc />
    public object? Deserialize(string json, Type type)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return null;
        }
        return JsonSerializer.Deserialize(json, type, _options);
    }
}