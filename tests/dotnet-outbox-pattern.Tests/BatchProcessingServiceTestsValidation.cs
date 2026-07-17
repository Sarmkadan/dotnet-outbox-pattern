#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides validation helpers for <see cref="BatchProcessingOptions"/> instances.
/// </summary>
public static class BatchProcessingServiceTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="BatchProcessingOptions"/> instance.
    /// </summary>
    /// <param name="value">The options instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this BatchProcessingOptions? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.TotalBatchSize < 0)
        {
            problems.Add($"{nameof(value.TotalBatchSize)} must be non-negative, but was {value.TotalBatchSize}.");
        }

        if (value.ChunkSize < 1)
        {
            problems.Add($"{nameof(value.ChunkSize)} must be at least 1, but was {value.ChunkSize}.");
        }

        if (value.MaxParallelChunks < 1)
        {
            problems.Add($"{nameof(value.MaxParallelChunks)} must be at least 1, but was {value.MaxParallelChunks}.");
        }

        if (value.DelayBetweenChunksMs < 0)
        {
            problems.Add($"{nameof(value.DelayBetweenChunksMs)} must be non-negative, but was {value.DelayBetweenChunksMs}.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BatchProcessingOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this BatchProcessingOptions? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="BatchProcessingOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid.</exception>
    public static void EnsureValid(this BatchProcessingOptions? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"BatchProcessingOptions instance is not valid. Problems: {string.Join(", ", problems)}");
        }
    }
}