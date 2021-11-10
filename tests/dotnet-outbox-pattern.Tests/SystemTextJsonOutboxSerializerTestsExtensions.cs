#nullable enable

using DotnetOutboxPattern.Services;
using FluentAssertions;
using System.Text.Json;

namespace DotnetOutboxPattern.Tests;

public static class SystemTextJsonOutboxSerializerTestsExtensions
{
    /// <summary>
    /// Creates a new serializer instance with custom JSON options for testing specific serialization scenarios.
    /// </summary>
    /// <param name="options">Custom JSON serializer options to use</param>
    /// <returns>A new SystemTextJsonOutboxSerializer instance</returns>
    public static SystemTextJsonOutboxSerializer WithCustomOptions(this SystemTextJsonOutboxSerializerTests _, JsonSerializerOptions options)
    {
        return new SystemTextJsonOutboxSerializer(options);
    }

    /// <summary>
    /// Creates a new serializer instance with camelCase naming policy for testing.
    /// </summary>
    /// <returns>A new SystemTextJsonOutboxSerializer instance with camelCase naming</returns>
    public static SystemTextJsonOutboxSerializer WithCamelCaseOptions(this SystemTextJsonOutboxSerializerTests _)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        return new SystemTextJsonOutboxSerializer(options);
    }

    /// <summary>
    /// Creates a new serializer instance with indented output for testing formatted JSON.
    /// </summary>
    /// <returns>A new SystemTextJsonOutboxSerializer instance with indented output</returns>
    public static SystemTextJsonOutboxSerializer WithIndentedOptions(this SystemTextJsonOutboxSerializerTests _)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        return new SystemTextJsonOutboxSerializer(options);
    }

    /// <summary>
    /// Asserts that the serialized JSON contains the expected property name.
    /// </summary>
    /// <typeparam name="T">Type of the object being serialized</typeparam>
    /// <param name="serializer">The serializer instance</param>
    /// <param name="value">The value to serialize</param>
    /// <param name="expectedPropertyName">The property name that should appear in the JSON</param>
    public static void ShouldContainPropertyName<T>(this SystemTextJsonOutboxSerializerTests _, SystemTextJsonOutboxSerializer serializer, T value, string expectedPropertyName)
    {
        var json = serializer.Serialize(value);
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain(expectedPropertyName);
    }

    /// <summary>
    /// Asserts that the serialized JSON can be deserialized back to the original type without data loss.
    /// </summary>
    /// <typeparam name="T">Type of the object</typeparam>
    /// <param name="serializer">The serializer instance</param>
    /// <param name="value">The value to serialize and round-trip</param>
    public static void ShouldRoundTrip<T>(this SystemTextJsonOutboxSerializerTests _, SystemTextJsonOutboxSerializer serializer, T value)
    {
        var json = serializer.Serialize(value);
        var deserialized = serializer.Deserialize<T>(json);

        deserialized.Should().NotBeNull();
        deserialized.Should().BeEquivalentTo(value);
    }

    /// <summary>
    /// Asserts that the serialized JSON matches the expected JSON string exactly.
    /// </summary>
    /// <typeparam name="T">Type of the object being serialized</typeparam>
    /// <param name="serializer">The serializer instance</param>
    /// <param name="value">The value to serialize</param>
    /// <param name="expectedJson">The expected JSON string</param>
    public static void ShouldSerializeTo<T>(this SystemTextJsonOutboxSerializerTests _, SystemTextJsonOutboxSerializer serializer, T value, string expectedJson)
    {
        var json = serializer.Serialize(value);
        json.Should().Be(expectedJson);
    }
}