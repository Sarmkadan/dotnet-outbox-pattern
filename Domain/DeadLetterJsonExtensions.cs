#nullable enable

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Provides JSON serialization and deserialization extensions for DeadLetter type.
/// </summary>
public static class DeadLetterJsonExtensions
{
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = false,
    };

    /// <summary>
    /// Serializes a DeadLetter to JSON string.
    /// </summary>
    /// <param name="value">The DeadLetter to serialize.</param>
    /// <param name="indented">Whether to format with indentation.</param>
    /// <returns>JSON string representation of the DeadLetter.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this DeadLetter value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new System.Text.Json.JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return System.Text.Json.JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a DeadLetter from JSON string.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <returns>The deserialized DeadLetter, or <see langword="null"/> if <paramref name="json"/> is <see langword="null"/>, empty, or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is <see langword="null"/>.</exception>
    public static DeadLetter? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return System.Text.Json.JsonSerializer.Deserialize<DeadLetter>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a DeadLetter from JSON string.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <param name="value">Output parameter for the deserialized DeadLetter.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryFromJson(string? json, out DeadLetter? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = System.Text.Json.JsonSerializer.Deserialize<DeadLetter>(json, _jsonOptions);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}