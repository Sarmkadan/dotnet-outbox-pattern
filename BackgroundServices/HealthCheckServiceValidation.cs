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
    /// Ensures a HealthAlert instance is valid, throwing ArgumentException if not
    /// </summary>
    /// <param name="value">The HealthAlert to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ArgumentException">Thrown if value is not valid</exception>
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
    /// Validates a HealthCheckOptions instance
    /// </summary>
    /// <param name="value">The HealthCheckOptions to validate</param>
    /// <returns>List of validation problems; empty list if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static IReadOnlyList<string> Validate(this HealthCheckOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.CheckIntervalMs <= 0)
        {
            errors.Add("HealthCheckOptions.CheckIntervalMs must be positive");
        }

        if (value.HighFailureRateThreshold < 0 || value.HighFailureRateThreshold > 1.0)
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
    /// Validates a HealthCheckOptions instance
    /// </summary>
    /// <param name="value">The HealthCheckOptions to validate</param>
    /// <returns>True if valid; false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    public static bool IsValid(this HealthCheckOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures a HealthCheckOptions instance is valid, throwing ArgumentException if not
    /// </summary>
    /// <param name="value">The HealthCheckOptions to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null</exception>
    /// <exception cref="ArgumentException">Thrown if value is not valid</exception>
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