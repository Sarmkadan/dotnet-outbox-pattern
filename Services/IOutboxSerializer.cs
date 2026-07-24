#nullable enable

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Defines an interface for pluggable outbox message serialization.
/// </summary>
public interface IOutboxSerializer
{
    /// <summary>
    /// Serializes an object to a string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    string Serialize<T>(T value);

    /// <summary>
    /// Deserializes a string to an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The target type for deserialization.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An object of the specified type.</returns>
    T? Deserialize<T>(string json);

    /// <summary>
    /// Deserializes a string to an object of the specified type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="type">The target type for deserialization.</param>
    /// <returns>An object of the specified type.</returns>
    object? Deserialize(string json, Type type);

    /// <summary>
    /// Serializes a value into a version-tolerant envelope that records the message type name and
    /// a schema version alongside the JSON payload, so the payload can still be resolved and
    /// deserialized after the originating CLR type is renamed, moved, or has its members refactored.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="schemaVersion">The schema version to record for the payload. Defaults to 1.</param>
    /// <returns>A JSON string representing the <see cref="OutboxEnvelope"/> wrapping the value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    string SerializeEnvelope<T>(T value, int schemaVersion = 1);

    /// <summary>
    /// Attempts to deserialize a version-tolerant envelope, resolving its stored message type name
    /// through <paramref name="resolver"/> before deserializing the payload. Never throws on
    /// malformed input, an unresolvable type, or a payload/type mismatch - failures are reported
    /// through the returned result so the caller can route the message to the dead letter queue
    /// instead of crashing the dispatch loop.
    /// </summary>
    /// <param name="envelopeJson">The envelope JSON, as produced by <see cref="SerializeEnvelope{T}"/>.</param>
    /// <param name="resolver">The resolver used to map the stored message type name to a CLR type.</param>
    /// <returns>An <see cref="OutboxDeserializationResult"/> describing the outcome.</returns>
    /// <exception cref="ArgumentException"><paramref name="envelopeJson"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <see langword="null"/>.</exception>
    OutboxDeserializationResult DeserializeEnvelope(string envelopeJson, IOutboxTypeResolver resolver);
}