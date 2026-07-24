#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Exception thrown when attempting to deserialize an outbox message with an unrecognized or disallowed type.
/// </summary>
public sealed class OutboxTypeResolutionException : Exception
{
    /// <summary>
    /// Gets the message type name that failed to resolve.
    /// </summary>
    public string MessageType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxTypeResolutionException"/> class.
    /// </summary>
    /// <param name="messageType">The message type name that failed to resolve.</param>
    public OutboxTypeResolutionException(string messageType)
        : base($"Message type '{messageType}' is not registered or allowed for deserialization.")
    {
        MessageType = messageType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxTypeResolutionException"/> class with a custom message.
    /// </summary>
    /// <param name="messageType">The message type name that failed to resolve.</param>
    /// <param name="message">The error message.</param>
    public OutboxTypeResolutionException(string messageType, string message)
        : base(message)
    {
        MessageType = messageType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxTypeResolutionException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="messageType">The message type name that failed to resolve.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OutboxTypeResolutionException(string messageType, string message, Exception innerException)
        : base(message, innerException)
    {
        MessageType = messageType;
    }
}

/// <summary>
/// Exception thrown when attempting to deserialize an outbox message payload that exceeds configured limits.
/// </summary>
public sealed class OutboxDeserializationLimitExceededException : Exception
{
    /// <summary>
    /// Gets the limit that was exceeded.
    /// </summary>
    public string LimitName { get; }

    /// <summary>
    /// Gets the limit value that was exceeded.
    /// </summary>
    public int LimitValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxDeserializationLimitExceededException"/> class.
    /// </summary>
    /// <param name="limitName">The name of the limit that was exceeded.</param>
    /// <param name="limitValue">The value of the limit that was exceeded.</param>
    public OutboxDeserializationLimitExceededException(string limitName, int limitValue)
        : base($"Outbox deserialization limit exceeded: {limitName} = {limitValue}")
    {
        LimitName = limitName;
        LimitValue = limitValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxDeserializationLimitExceededException"/> class with a custom message.
    /// </summary>
    /// <param name="limitName">The name of the limit that was exceeded.</param>
    /// <param name="limitValue">The value of the limit that was exceeded.</param>
    /// <param name="message">The error message.</param>
    public OutboxDeserializationLimitExceededException(string limitName, int limitValue, string message)
        : base(message)
    {
        LimitName = limitName;
        LimitValue = limitValue;
    }
}

/// <summary>
/// Default implementation of IOutboxSerializer using System.Text.Json.
/// </summary>
public sealed class SystemTextJsonOutboxSerializer : IOutboxSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonOutboxSerializer"/> class with secure default options.
    /// </summary>
    public SystemTextJsonOutboxSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            MaxDepth = 100
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonOutboxSerializer"/> class with custom options.
    /// </summary>
    /// <param name="options">The JSON serializer options to use. Must not be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public SystemTextJsonOutboxSerializer(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Ensure security-relevant options are set
        if (_options.MaxDepth <= 0)
        {
            _options.MaxDepth = 100;
        }
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

        // Security validation: Ensure the resolved type is safe to deserialize
        if (!IsTypeSafeForDeserialization(clrType))
        {
            return OutboxDeserializationResult.Failed(
                $"type '{clrType.FullName}' is not safe for deserialization",
                envelope.MessageType,
                envelope.SchemaVersion);
        }

        try
        {
            var value = JsonSerializer.Deserialize(envelope.Payload, clrType, _options);
            return OutboxDeserializationResult.Ok(value, envelope.MessageType, envelope.SchemaVersion);
        }
        catch (JsonException ex) when (ex.Message.Contains("maximum depth") || ex.Message.Contains("exceeded"))
        {
            return OutboxDeserializationResult.Failed(
                $"payload exceeds maximum nesting depth: {ex.Message}",
                envelope.MessageType,
                envelope.SchemaVersion);
        }
        catch (JsonException ex)
        {
            return OutboxDeserializationResult.Failed(
                $"payload does not match type '{envelope.MessageType}': {ex.Message}",
                envelope.MessageType,
                envelope.SchemaVersion);
        }
    }

    /// <summary>
    /// Determines whether the specified type is safe for deserialization from untrusted sources.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if the type is safe; otherwise <see langword="false"/>.</returns>
    private static bool IsTypeSafeForDeserialization(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // Allow abstract types and interfaces - they'll be handled by the deserializer
        if (type.IsAbstract || type.IsInterface)
        {
            return true;
        }

        // Allow primitive types and their nullable variants
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(Uri))
        {
            return true;
        }

        // Allow common collection types
        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
        {
            return true;
        }

        // Allow types with [JsonSerializable] or other serialization attributes that indicate controlled deserialization
        // This is a basic check - in a real application, you might want to maintain an explicit allow-list
        var jsonSerializableAttr = type.GetCustomAttributes(typeof(JsonSerializableAttribute), inherit: false).Any();
        if (jsonSerializableAttr)
        {
            return true;
        }

        // Allow types from the current assembly or known safe assemblies
        var assembly = type.Assembly;
        var assemblyName = assembly.GetName().Name;
        if (assemblyName?.StartsWith("DotnetOutboxPattern", StringComparison.Ordinal) == true)
        {
            return true;
        }

        // For all other types, be conservative and require explicit registration
        // This prevents type confusion attacks where an attacker could specify a malicious type
        return false;
    }
}