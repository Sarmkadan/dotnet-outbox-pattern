#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using System.Text;

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// String extension methods for common operations
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts the string to title case (first letter of each word uppercase, rest lowercase)
    /// </summary>
    public static string ToTitleCase(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
    }

    /// <summary>
    /// Reverses the string
    /// </summary>
    public static string Reverse(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        char[] chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>
    /// Removes all whitespace characters from the string
    /// </summary>
    public static string RemoveWhitespace(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    /// <summary>
    /// Checks if the string contains only digits
    /// </summary>
    public static bool IsDigitsOnly(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return input.All(char.IsDigit);
    }

    /// <summary>
    /// Checks if the string contains only letters
    /// </summary>
    public static bool IsLettersOnly(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return input.All(char.IsLetter);
    }

    /// <summary>
    /// Truncates the string to a specified length without adding suffix
    /// </summary>
    public static string TruncatePlain(this string? input, int length)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input.Length <= length ? input : input[..length];
    }
}