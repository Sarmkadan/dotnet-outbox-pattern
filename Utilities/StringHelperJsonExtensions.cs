#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Provides System.Text.Json serialization/deserialization extensions for StringHelper
/// </summary>
public static class StringHelperJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	/// <summary>
	/// Serializes a StringHelper operation result to JSON
	/// </summary>
	/// <param name="value">The StringHelper operation result to serialize</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability</param>
	/// <returns>A JSON string representation</returns>
	/// <exception cref="ArgumentNullException">Thrown when value is null</exception>
	public static string ToJson(this string value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonOptions)
			{
				WriteIndented = true
			}
			: _jsonOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string back into a string
	/// </summary>
	/// <param name="json">The JSON string to deserialize</param>
	/// <returns>A string populated from the JSON data</returns>
	/// <exception cref="ArgumentException">Thrown when json is null or empty</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized</exception>
	public static string? FromJson(string json)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		return JsonSerializer.Deserialize<string>(json, _jsonOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string into a string
	/// </summary>
	/// <param name="json">The JSON string to deserialize</param>
	/// <param name="value">Receives the deserialized string if successful</param>
	/// <returns>True if deserialization succeeded; otherwise, false</returns>
	public static bool TryFromJson(string json, out string? value)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		try
		{
			value = JsonSerializer.Deserialize<string>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}