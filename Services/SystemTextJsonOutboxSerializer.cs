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
        try
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <inheritdoc />
    public object? Deserialize(string json, Type type)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize(json, type, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string SerializeEnvelope<T>(T value, int schemaVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(value);

        var messageType = typeof(T).FullName ?? typeof(T).Name;
        var payload = JsonSerializer.Serialize(value, _options);
        var envelope = new OutboxEnvelope(messageType, schemaVersion, payload);
        return JsonSerializer.Serialize(envelope, _options);
    }

    /// <inheritdoc />
    public OutboxDeserializationResult DeserializeEnvelope(string envelopeJson, IOutboxTypeResolver resolver)
    {
        ArgumentException.ThrowIfNullOrEmpty(envelopeJson);
        ArgumentNullException.ThrowIfNull(resolver);

        OutboxEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<OutboxEnvelope>(envelopeJson, _options);
        }
        catch (JsonException ex)
        {
            return OutboxDeserializationResult.Failed($"envelope is not valid json: {ex.Message}");
        }

        if (envelope is null || string.IsNullOrWhiteSpace(envelope.MessageType))
        {
            return OutboxDeserializationResult.Failed("envelope is missing a message type");
        }

        if (!resolver.TryResolve(envelope.MessageType, out var clrType) || clrType is null)
        {
            return OutboxDeserializationResult.Failed(
                $"no type is registered for message type '{envelope.MessageType}'",
                envelope.MessageType,
                envelope.SchemaVersion);
        }

        try
        {
            var value = JsonSerializer.Deserialize(envelope.Payload, clrType, _options);
            return OutboxDeserializationResult.Ok(value, envelope.MessageType, envelope.SchemaVersion);
        }
        catch (JsonException ex)
        {
            return OutboxDeserializationResult.Failed(
                $"payload does not match type '{envelope.MessageType}': {ex.Message}",
                envelope.MessageType,
                envelope.SchemaVersion);
        }
    }
}