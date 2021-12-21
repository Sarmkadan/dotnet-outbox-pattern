using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetOutboxPattern.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="MessagePublishingServiceBenchmarks"/> instances
/// </summary>
public static class MessagePublishingServiceBenchmarksValidation
{
    /// <summary>
    /// Validates the specified <see cref="MessagePublishingServiceBenchmarks"/> instance
    /// </summary>
    /// <param name="value">The benchmarks instance to validate</param>
    /// <returns>An enumerable of validation errors, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this MessagePublishingServiceBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Check if disposed
        try
        {
            if (value is IDisposable disposable && disposable is MessagePublishingServiceBenchmarks benchmarks)
            {
                // Attempt to check disposed state by checking if service provider is disposed
                // This is a heuristic since we can't directly access the private field
                if (benchmarks.GetType().GetField("_serviceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(benchmarks) is null)
                {
                    errors.Add("The benchmarks instance has been disposed.");
                }
            }
        }
        catch
        {
            errors.Add("Failed to check disposed state of the benchmarks instance.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="MessagePublishingServiceBenchmarks"/> instance is valid
    /// </summary>
    /// <param name="value">The benchmarks instance to check</param>
    /// <returns>True if the instance is valid; otherwise, false</returns>
    public static bool IsValid(this MessagePublishingServiceBenchmarks value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="MessagePublishingServiceBenchmarks"/> instance is valid
    /// </summary>
    /// <param name="value">The benchmarks instance to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when the benchmarks instance is not valid, containing the validation errors</exception>
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