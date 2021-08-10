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
}