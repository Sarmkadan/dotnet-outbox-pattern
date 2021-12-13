#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Provides validation helpers for <see cref="OutboxProcessorOptions"/> configuration
/// </summary>
public static class OutboxProcessorOptionsValidation
{
    /// <summary>
    /// Determines whether the configuration options are valid.
    /// </summary>
    /// <param name="value">The options to validate</param>
    /// <returns>True if valid, otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static bool IsValid(this OutboxProcessorOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Delegate to the existing extension method's validation logic
        try
        {
            value.Validate();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates the configuration options and throws an exception if invalid.
    /// </summary>
    /// <param name="value">The options to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static void EnsureValid(this OutboxProcessorOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.Validate();
    }
}