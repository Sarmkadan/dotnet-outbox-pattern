#nullable enable

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Version-tolerant envelope wrapped around an outbox message payload. Storing the message type
/// name and a schema version alongside the raw JSON payload lets a message survive a CLR-level
/// rename or property refactor of the type it was originally serialized from: the stored
/// <see cref="MessageType"/> is resolved through an <see cref="IOutboxTypeResolver"/> rather than
/// relying on the payload matching whatever the current CLR type happens to look like.
/// </summary>
/// <param name="MessageType">The stable, stored name of the message type (resolved via <see cref="IOutboxTypeResolver"/>).</param>
/// <param name="SchemaVersion">The schema version the payload was written with.</param>
/// <param name="Payload">The raw JSON payload for the message body.</param>
public sealed record OutboxEnvelope(string MessageType, int SchemaVersion, string Payload);

/// <summary>
/// Outcome of attempting to deserialize an <see cref="OutboxEnvelope"/>. Deserialization never
/// throws - a malformed envelope, an unresolvable <see cref="MessageType"/>, or a payload that no
/// longer matches the resolved CLR type is reported as a failed result so the caller can route the
/// message to the dead letter queue instead of crashing the dispatch loop.
/// </summary>
public sealed class OutboxDeserializationResult
{
    /// <summary>
    /// Gets a value indicating whether deserialization succeeded.
    /// </summary>
    public bool Success { get; private init; }

    /// <summary>
    /// Gets the deserialized value when <see cref="Success"/> is <see langword="true"/>; otherwise <see langword="null"/>.
    /// </summary>
    public object? Value { get; private init; }

    /// <summary>
    /// Gets the stored message type name read from the envelope, when available.
    /// </summary>
    public string? MessageType { get; private init; }

    /// <summary>
    /// Gets the schema version read from the envelope, when available.
    /// </summary>
    public int SchemaVersion { get; private init; }

    /// <summary>
    /// Gets a human-readable failure reason when <see cref="Success"/> is <see langword="false"/>; otherwise <see langword="null"/>.
    /// </summary>
    public string? Error { get; private init; }

    /// <summary>
    /// Creates a successful deserialization result.
    /// </summary>
    /// <param name="value">The deserialized value.</param>
    /// <param name="messageType">The stored message type name.</param>
    /// <param name="schemaVersion">The schema version the payload was written with.</param>
    /// <returns>A successful <see cref="OutboxDeserializationResult"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="messageType"/> is null or empty.</exception>
    public static OutboxDeserializationResult Ok(object? value, string messageType, int schemaVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageType);

        return new OutboxDeserializationResult
        {
            Success = true,
            Value = value,
            MessageType = messageType,
            SchemaVersion = schemaVersion
        };
    }

    /// <summary>
    /// Creates a failed deserialization result.
    /// </summary>
    /// <param name="error">A human-readable description of why deserialization failed.</param>
    /// <param name="messageType">The stored message type name, if it could be read from the envelope.</param>
    /// <param name="schemaVersion">The schema version, if it could be read from the envelope.</param>
    /// <returns>A failed <see cref="OutboxDeserializationResult"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="error"/> is null or empty.</exception>
    public static OutboxDeserializationResult Failed(string error, string? messageType = null, int schemaVersion = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(error);

        return new OutboxDeserializationResult
        {
            Success = false,
            Error = error,
            MessageType = messageType,
            SchemaVersion = schemaVersion
        };
    }
}
