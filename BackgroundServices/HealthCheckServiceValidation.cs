#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetOutboxPattern.BackgroundServices;

/// <summary>
/// Provides validation helpers for health check related types
/// </summary>
public static class HealthCheckServiceValidation
{
    /// <summary>
    /// Validates a HealthAlert instance
    /// </summary>
    /// <param name="value">The HealthAlert to validate</param>
    /// <returns>List of validation problems; empty list if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this HealthAlert value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value.Type))
        {
            errors.Add("HealthAlert.Type cannot be null or whitespace");
        }

        if (string.IsNullOrWhiteSpace(value.Message))
        {
            errors.Add("HealthAlert.Message cannot be null or whitespace");
        }

        if (value.RaisedAt == default)
        {
            errors.Add("HealthAlert.RaisedAt cannot be default(DateTime)");
        }
        else if (value.RaisedAt > DateTime.UtcNow.AddMinutes(1))
        {
            errors.Add("HealthAlert.RaisedAt cannot be in the future");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a HealthAlert instance
    /// </summary>
    /// <param name="value">The HealthAlert to validate</param>
    /// <returns>True if valid; false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static bool IsValid(this HealthAlert value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures a <see cref="HealthAlert"/> instance is valid, throwing <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The <see cref="HealthAlert"/> to validate</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid</exception>
    public static void EnsureValid(this HealthAlert value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"HealthAlert validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Validates a <see cref="HealthCheckOptions"/> instance
    /// </summary>
    /// <param name="value">The <see cref="HealthCheckOptions"/> to validate</param>
    /// <returns>List of validation problems; empty list if valid</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this HealthCheckOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.CheckIntervalMs <= 0)
        {
            errors.Add("HealthCheckOptions.CheckIntervalMs must be positive");
        }
        else if (value.CheckIntervalMs > TimeSpan.FromHours(1).TotalMilliseconds)
        {
            errors.Add("HealthCheckOptions.CheckIntervalMs cannot exceed 1 hour");
        }

        if (value.HighFailureRateThreshold is < 0 or > 1.0)
        {
            errors.Add("HealthCheckOptions.HighFailureRateThreshold must be between 0 and 1.0");
        }

        if (value.StuckMessageThreshold < 0)
        {
            errors.Add("HealthCheckOptions.StuckMessageThreshold must be non-negative");
        }

        if (value.DeadLetterThreshold < 0)
        {
            errors.Add("HealthCheckOptions.DeadLetterThreshold must be non-negative");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a <see cref="HealthCheckOptions"/> instance
    /// </summary>
    /// <param name="value">The <see cref="HealthCheckOptions"/> to validate</param>
    /// <returns>True if valid; false otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
    public static bool IsValid(this HealthCheckOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures a <see cref="HealthCheckOptions"/> instance is valid, throwing <see cref="ArgumentException"/> if not
    /// </summary>
    /// <param name="value">The <see cref="HealthCheckOptions"/> to validate</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid</exception>
    public static void EnsureValid(this HealthCheckOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"HealthCheckOptions validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}