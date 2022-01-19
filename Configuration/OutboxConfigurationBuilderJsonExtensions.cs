using System;
using System.Text.Json;

namespace DotnetOutboxPattern.Configuration
{
	/// <summary>
	/// JSON (de)serialization helpers for <see cref="OutboxConfigurationBuilder"/>.
	/// </summary>
	public static class OutboxConfigurationBuilderJsonExtensions
	{
		// Cached options: camelCase naming, no indentation by default.
		private static readonly JsonSerializerOptions _options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};

		/// <summary>
		/// Serializes the <see cref="OutboxConfigurationBuilder"/> instance to JSON.
		/// </summary>
		/// <param name="value">The builder instance to serialize.</param>
		/// <param name="indented">If true, the output JSON will be indented.</param>
		/// <returns>A JSON string representing the builder.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
		public static string ToJson(this OutboxConfigurationBuilder value, bool indented = false)
		{
			ArgumentNullException.ThrowIfNull(value);

			// Use the cached options, optionally overriding indentation.
			var options = indented
				? new JsonSerializerOptions(_options) { WriteIndented = true }
				: _options;

			return JsonSerializer.Serialize(value, options);
		}

		/// <summary>
		/// Deserializes a JSON string into an <see cref="OutboxConfigurationBuilder"/> instance.
		/// </summary>
		/// <param name="json">The JSON representation of the builder.</param>
		/// <returns>The deserialized builder, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
		/// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized into a <see cref="OutboxConfigurationBuilder"/>.</exception>
		public static OutboxConfigurationBuilder? FromJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return null;
			}

			return JsonSerializer.Deserialize<OutboxConfigurationBuilder>(json, _options);
		}

		/// <summary>
		/// Attempts to deserialize a JSON string into an <see cref="OutboxConfigurationBuilder"/> instance.
		/// </summary>
		/// <param name="json">The JSON representation of the builder.</param>
		/// <param name="value">When the method returns, contains the deserialized builder if successful; otherwise <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if deserialization succeeded; <see langword="false"/> if a <see cref="JsonException"/> was thrown.</returns>
		public static bool TryFromJson(string json, out OutboxConfigurationBuilder? value)
			=> (value = FromJson(json)) is not null;
	}
}