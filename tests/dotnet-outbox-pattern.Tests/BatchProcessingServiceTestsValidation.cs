#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides validation helpers for <see cref="BatchProcessingServiceTests"/> instances.
/// </summary>
public static class BatchProcessingServiceTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="BatchProcessingServiceTests"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this BatchProcessingServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Constructor parameters validation
        // Note: The actual validation happens in the constructor, but we can check for null references
        // that would have been passed to the constructor

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BatchProcessingServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this BatchProcessingServiceTests? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="BatchProcessingServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
    public static void EnsureValid(this BatchProcessingServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"BatchProcessingServiceTests instance is not valid. Problems: {string.Join(", ", problems)}");
        }
    }
}