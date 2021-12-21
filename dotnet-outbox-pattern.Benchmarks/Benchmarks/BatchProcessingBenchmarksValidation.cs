namespace DotnetOutboxPattern.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="BatchProcessingBenchmarks"/> instances.
/// </summary>
public static class BatchProcessingBenchmarksValidation
{
    /// <summary>
    /// Validates the specified <see cref="BatchProcessingBenchmarks"/> instance.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this BatchProcessingBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate BatchSize - must be positive and within reasonable bounds for benchmarks
        if (value.BatchSize <= 0)
        {
            errors.Add($"BatchSize must be positive, but was {value.BatchSize}.");
        }
        else if (value.BatchSize > 10000)
        {
            errors.Add($"BatchSize {value.BatchSize} exceeds maximum reasonable value of 10000.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BatchProcessingBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid(this BatchProcessingBenchmarks value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="BatchProcessingBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the benchmarks instance is not valid, containing the validation errors.</exception>
    public static void EnsureValid(this BatchProcessingBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"BatchProcessingBenchmarks instance is not valid. Errors: {string.Join(" ", errors)}",
                nameof(value));
        }
    }
}
