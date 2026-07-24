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
    /// <remarks>
    /// This method provides a centralized, shared implementation of exponential backoff calculation
    /// used by both <see cref="RetryHelper"/> and <see cref="OutboxBackoffExtensions"/> to ensure consistent behavior
    /// across the codebase.
    /// </remarks>
    /// <param name="baseDelayMs">
    /// The initial delay in milliseconds. Must be non‑negative.
    /// Negative values will throw <see cref="ArgumentOutOfRangeException"/>.
    /// </param>
    /// <param name="maxDelayMs">
    /// The maximum allowed delay in milliseconds. Must be greater than or equal to <paramref name="baseDelayMs"/>.
    /// </param>
    /// <param name="multiplier">
    /// Growth factor applied on each retry attempt. Must be at least 1.0.
    /// </param>
    /// <param name="attempt">
    /// The retry attempt number used in the exponential calculation.
    /// This parameter is 1‑based: attempt 1 uses baseDelay × multiplier¹,
    /// attempt 2 uses baseDelay × multiplier², etc.
    /// Zero or negative values are treated as 0 (returns base delay).
    /// </param>
    /// <returns>The calculated delay in milliseconds, never exceeding <paramref name="maxDelayMs"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="baseDelayMs"/> is negative.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDelayMs"/> is less than <paramref name="baseDelayMs"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="multiplier"/> is less than 1.0.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="attempt"/> is negative.</exception>
    public static double ComputeExponentialDelay(int baseDelayMs, int maxDelayMs, double multiplier, int attempt)
    {
        // Guard clauses - validate inputs before normalization
        if (baseDelayMs < 0)
            throw new ArgumentOutOfRangeException(nameof(baseDelayMs), baseDelayMs, "baseDelayMs must be non-negative.");

        if (maxDelayMs < baseDelayMs)
            throw new ArgumentOutOfRangeException(nameof(maxDelayMs), maxDelayMs, "maxDelayMs must be greater than or equal to baseDelayMs.");

        if (multiplier < 1.0)
            throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "multiplier must be at least 1.0.");

        if (attempt < 0)
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "attempt must be non-negative.");

        // Normalise inputs - handle edge cases defensively
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
