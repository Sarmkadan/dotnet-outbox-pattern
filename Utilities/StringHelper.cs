#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Helper utilities for string operations - validation, hashing, formatting
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Generates a secure hash of the input string using SHA256
    /// Useful for checksums and signatures
    /// </summary>
    public static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Validates that a string matches a specific format/pattern
    /// Returns true if matches, false otherwise
    /// </summary>
    public static bool IsValidFormat(string? value, string pattern)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        try
        {
            return Regex.IsMatch(value, pattern);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a string is a valid UUID/GUID format
    /// </summary>
    public static bool IsValidGuid(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return Guid.TryParse(value, out _);
    }

    /// <summary>
    /// Checks if a string is a valid email address
    /// </summary>
    public static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            return addr.Address == value;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Truncates a string to a maximum length with ellipsis if needed
    /// </summary>
    public static string Truncate(string? value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        return value.Substring(0, Math.Max(0, maxLength - suffix.Length)) + suffix;
    }

    /// <summary>
    /// Sanitizes a string for use in JSON by escaping special characters
    /// </summary>
    public static string SanitizeForJson(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Converts a string to a URL-safe slug
    /// Removes special characters and converts to lowercase
    /// </summary>
    public static string ToSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var slug = value.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");
        slug = slug.Trim('-');
        slug = Regex.Replace(slug, @"-+", "-");

        return slug;
    }

    /// <summary>
    /// Converts PascalCase string to kebab-case
    /// Used for converting enum names to lowercase identifiers
    /// </summary>
    public static string ToKebabCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var result = Regex.Replace(value, "(?<!^)(?=[A-Z])", "-");
        return result.ToLowerInvariant();
    }

    /// <summary>
    /// Generates a random alphanumeric string of specified length
    /// Useful for generating tokens and temporary identifiers
    /// </summary>
    public static string GenerateRandomString(int length = 32)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Checks if a string is null or whitespace - more readable than string.IsNullOrWhiteSpace
    /// </summary>
    public static bool IsEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Joins multiple strings into a single string with separator
    /// Filters out null/empty values automatically
    /// </summary>
    public static string JoinNonEmpty(string separator, params string?[] values)
    {
        var nonEmpty = values.Where(v => !IsEmpty(v)).ToList();
        return string.Join(separator, nonEmpty);
    }

    /// <summary>
    /// Extracts a substring between two delimiters
    /// Returns empty string if delimiters not found
    /// </summary>
    public static string ExtractBetween(string? value, string? start, string? end)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
            return string.Empty;

        var startIndex = value.IndexOf(start);
        if (startIndex < 0)
            return string.Empty;

        startIndex += start.Length;
        var endIndex = value.IndexOf(end, startIndex);
        if (endIndex < 0)
            return string.Empty;

        return value.Substring(startIndex, endIndex - startIndex);
    }
}
