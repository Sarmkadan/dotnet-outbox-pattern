using System;
using System.Collections.Generic;

namespace DotnetOutboxPattern.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="MessagePublishingServiceBenchmarks"/> instances
/// </summary>
public static class MessagePublishingServiceBenchmarksValidation
{
    /// <summary>
    /// Validates the specified <see cref="MessagePublishingServiceBenchmarks"/> instance.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <returns>An enumerable of validation errors, or empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this MessagePublishingServiceBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();


        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="MessagePublishingServiceBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this MessagePublishingServiceBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="MessagePublishingServiceBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the benchmarks instance is not valid, containing the validation errors.</exception>
    public static void EnsureValid(this MessagePublishingServiceBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"The MessagePublishingServiceBenchmarks instance is not valid. Validation errors: {string.Join("; ", errors)}");
        }
    }
}