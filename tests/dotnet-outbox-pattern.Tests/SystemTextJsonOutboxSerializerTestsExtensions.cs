#nullable enable

using DotnetOutboxPattern.Services;
using FluentAssertions;
using System.Text.Json;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides extension methods for testing <see cref="SystemTextJsonOutboxSerializer"/> instances.
/// </summary>
public static class SystemTextJsonOutboxSerializerTestsExtensions
{
	/// <summary>
	/// Creates a new serializer instance with custom JSON options for testing specific serialization scenarios.
	/// </summary>
	/// <param name="_">The test instance (unused discard parameter)</param>
	/// <param name="options">Custom JSON serializer options to use. Cannot be null.</param>
	/// <returns>A new <see cref="SystemTextJsonOutboxSerializer"/> instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
	public static SystemTextJsonOutboxSerializer WithCustomOptions(this SystemTextJsonOutboxSerializerTests _, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		return new SystemTextJsonOutboxSerializer(options);
	}

	/// <summary>
	/// Creates a new serializer instance with camelCase naming policy for testing.
	/// </summary>
	/// <param name="_">The test instance (unused discard parameter)</param>
	/// <returns>A new <see cref="SystemTextJsonOutboxSerializer"/> instance with camelCase naming.</returns>
	public static SystemTextJsonOutboxSerializer WithCamelCaseOptions(this SystemTextJsonOutboxSerializerTests _)
		=> new SystemTextJsonOutboxSerializer(new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		});

	/// <summary>
	/// Creates a new serializer instance with indented output for testing formatted JSON.
	/// </summary>
	/// <param name="_">The test instance (unused discard parameter)</param>
	/// <returns>A new <see cref="SystemTextJsonOutboxSerializer"/> instance with indented output.</returns>
	public static SystemTextJsonOutboxSerializer WithIndentedOptions(this SystemTextJsonOutboxSerializerTests _)
		=> new SystemTextJsonOutboxSerializer(new JsonSerializerOptions
		{
			WriteIndented = true
		});

	/// <summary>
	/// Asserts that the serialized JSON contains the expected property name.
	/// </summary>
	/// <typeparam name="T">Type of the object being serialized.</typeparam>
	/// <param name="_">The test instance (unused discard parameter)</param>
	/// <param name="serializer">The serializer instance. Cannot be null.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="expectedPropertyName">The property name that should appear in the JSON. Cannot be null or empty.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/>, <paramref name="value"/>, or <paramref name="expectedPropertyName"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="expectedPropertyName"/> is empty or whitespace.</exception>
	public static void ShouldContainPropertyName<T>(this SystemTextJsonOutboxSerializerTests _, SystemTextJsonOutboxSerializer serializer, T value, string expectedPropertyName)
	{
		ArgumentNullException.ThrowIfNull(serializer);
		ArgumentNullException.ThrowIfNull(expectedPropertyName);
		ArgumentException.ThrowIfNullOrEmpty(expectedPropertyName);

		var json = serializer.Serialize(value);
		json.Should().NotBeNullOrEmpty();
		json.Should().Contain(expectedPropertyName);
	}

	/// <summary>
	/// Asserts that the serialized JSON can be deserialized back to the original type without data loss.
	/// </summary>
	/// <typeparam name="T">Type of the object.</typeparam>
	/// <param name="_">The test instance (unused discard parameter)</param>
	/// <param name="serializer">The serializer instance. Cannot be null.</param>
	/// <param name="value">The value to serialize and round-trip.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/> is <see langword="null"/>.</exception>
	public static void ShouldRoundTrip<T>(this SystemTextJsonOutboxSerializerTests _, SystemTextJsonOutboxSerializer serializer, T value)
	{
		ArgumentNullException.ThrowIfNull(serializer);

		var json = serializer.Serialize(value);
		var deserialized = serializer.Deserialize<T>(json);

		deserialized.Should().NotBeNull();
		deserialized.Should().BeEquivalentTo(value);
	}

	/// <summary>
	/// Asserts that the serialized JSON matches the expected JSON string exactly.
	/// </summary>
	/// <typeparam name="T">Type of the object being serialized.</typeparam>
	/// <param name="_">The test instance (unused discard parameter)</param>
	/// <param name="serializer">The serializer instance. Cannot be null.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="expectedJson">The expected JSON string. Cannot be null or empty.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/>, <paramref name="value"/>, or <paramref name="expectedJson"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="expectedJson"/> is empty or whitespace.</exception>
	public static void ShouldSerializeTo<T>(this SystemTextJsonOutboxSerializerTests _, SystemTextJsonOutboxSerializer serializer, T value, string expectedJson)
	{
		ArgumentNullException.ThrowIfNull(serializer);
		ArgumentNullException.ThrowIfNull(expectedJson);
		ArgumentException.ThrowIfNullOrEmpty(expectedJson);

		var json = serializer.Serialize(value);
		json.Should().Be(expectedJson);
	}
}
