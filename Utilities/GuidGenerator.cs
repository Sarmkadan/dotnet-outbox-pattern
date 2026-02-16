// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Utility for generating and working with GUIDs
/// Provides convenience methods for GUID operations
/// </summary>
public static class GuidGenerator
{
    /// <summary>
    /// Generates a new GUID
    /// </summary>
    public static Guid NewGuid() => Guid.NewGuid();

    /// <summary>
    /// Generates a GUID from a string using MD5 hashing (deterministic)
    /// Useful for idempotency keys and consistent identifiers
    /// </summary>
    public static Guid FromString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return Guid.Empty;

        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }

    /// <summary>
    /// Generates a GUID from multiple string components
    /// Useful for creating composite identifiers
    /// </summary>
    public static Guid FromComponents(params string[] components)
    {
        var combined = string.Concat(components);
        return FromString(combined);
    }

    /// <summary>
    /// Checks if a string is a valid GUID
    /// </summary>
    public static bool IsValid(string? value)
    {
        return Guid.TryParse(value, out _);
    }

    /// <summary>
    /// Parses a string to GUID, returning a default if invalid
    /// </summary>
    public static Guid Parse(string? value, Guid defaultValue = default)
    {
        return Guid.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Creates a sequential GUID for better database index performance
    /// Note: Only works on SQL Server with NEWSEQUENTIALID()
    /// </summary>
    public static Guid NewSequentialId()
    {
        // Implementation using algorithm similar to NEWSEQUENTIALID()
        var guid = Guid.NewGuid();
        var buffer = guid.ToByteArray();
        var now = DateTime.UtcNow;
        var ticks = new byte[6];
        BitConverter.GetBytes(now.Ticks).CopyTo(ticks, 0);

        // Rearrange bytes to create sequential pattern
        Array.Copy(ticks, 0, buffer, 10, 6);

        return new Guid(buffer);
    }

    /// <summary>
    /// Generates a correlation ID (GUID in lowercase format)
    /// Used for tracing related messages across systems
    /// </summary>
    public static string GenerateCorrelationId() => NewGuid().ToString("n").ToLower();

    /// <summary>
    /// Generates a request ID (GUID in short format)
    /// </summary>
    public static string GenerateRequestId() => NewGuid().ToString("n").Substring(0, 16);

    /// <summary>
    /// Generates an idempotency key combining timestamp and random component
    /// </summary>
    public static string GenerateIdempotencyKey(string prefix = "")
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var random = NewGuid().ToString("n").Substring(0, 8);
        return string.IsNullOrEmpty(prefix) ? $"{timestamp}-{random}" : $"{prefix}-{timestamp}-{random}";
    }
}
