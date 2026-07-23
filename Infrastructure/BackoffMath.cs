#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Centralised back‑off calculation logic shared between <see cref="OutboxBackoffExtensions"/>
/// and <see cref="RetryHelper"/>. The implementation mirrors the original exponential
/// back‑off algorithm while protecting against overflow by capping the exponent at 32
/// and clamping the result to the supplied maximum delay.
/// </summary>
public static class BackoffMath
{
    /// <summary>
    /// Computes the exponential back‑off delay in milliseconds.
    /// </summary>
    /// <param name="baseDelayMs">The initial delay in milliseconds; negative values are treated as zero.</param>
    /// <param name="maxDelayMs">The maximum allowed delay; must be greater than or equal to <paramref name="baseDelayMs"/>.</param>
    /// <param name="multiplier">Growth factor; must be at least 1.0.</param>
    /// <param name="attempt">The retry attempt count (1‑based). Zero or negative attempts are treated as zero.</param>
    /// <returns>The calculated delay in milliseconds, never exceeding <paramref name="maxDelayMs"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="baseDelayMs"/> is negative.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDelayMs"/> is less than <paramref name="baseDelayMs"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="multiplier"/> is less than 1.0.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="attempt"/> is negative.</exception>
    public static double ComputeExponentialDelay(int baseDelayMs, int maxDelayMs, double multiplier, int attempt)
    {
        // Guard clauses
        ArgumentOutOfRangeException.ThrowIfNegative(baseDelayMs);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelayMs, baseDelayMs);
        ArgumentOutOfRangeException.ThrowIfLessThan(multiplier, 1.0);
        ArgumentOutOfRangeException.ThrowIfNegative(attempt);

        // Normalise inputs
        var baseDelay = Math.Max(0, baseDelayMs);
        var safeMultiplier = Math.Max(1.0, multiplier);
        var safeAttempt = Math.Max(0, attempt);

        // Clamp the exponent to avoid double overflow (32 is sufficient for 64‑bit double)
        var cappedExponent = Math.Min(safeAttempt, 32);
        var scaled = baseDelay * Math.Pow(safeMultiplier, cappedExponent);

        // Ensure we never exceed the configured ceiling
        var ceiling = Math.Max(maxDelayMs, baseDelay);
        var clamped = Math.Min(scaled, ceiling);

        return clamped;
    }
}
