#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides validation helpers for <see cref="SystemTextJsonOutboxSerializerTests"/> instances.
/// </summary>
public static class SystemTextJsonOutboxSerializerTestsValidation
{
    /// <summary>
    /// Validates that the specified <see cref="SystemTextJsonOutboxSerializerTests"/> instance is not null.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns><see langword="true"/> if the instance is valid (not null); otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this SystemTextJsonOutboxSerializerTests? value)
    {
        return value is not null;
    }

    /// <summary>
    /// Ensures that the specified <see cref="SystemTextJsonOutboxSerializerTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static void EnsureValid(this SystemTextJsonOutboxSerializerTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    /// <summary>
    /// Validates that the specified <see cref="SystemTextJsonOutboxSerializerTests"/> instance is not null and returns a list of validation errors.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>An empty list if the instance is valid; otherwise, a list containing validation messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this SystemTextJsonOutboxSerializerTests? value)
    {
        return value is null
            ? throw new ArgumentNullException(nameof(value))
            : Array.Empty<string>();
    }
}