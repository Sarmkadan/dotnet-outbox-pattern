#nullable enable

using System.Reflection;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides validation helpers for <see cref="RetryPolicyHelperTests"/> instances.
/// Validates that all required test methods exist and have the correct signatures.
/// </summary>
public static class RetryPolicyHelperTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="RetryPolicyHelperTests"/> instance.
    /// Ensures all required test methods exist and have the correct signatures.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>An enumerable of validation messages; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this RetryPolicyHelperTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();
        var testType = typeof(RetryPolicyHelperTests);
        ArgumentNullException.ThrowIfNull(testType);

        // Validate CalculateDelay_WithZeroAttempt_ThrowsArgumentException
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_WithZeroAttempt_ThrowsArgumentException), out var method))
            errors.Add("Missing method: CalculateDelay_WithZeroAttempt_ThrowsArgumentException");

        // Validate CalculateDelay_WithNegativeAttempt_ThrowsArgumentException
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_WithNegativeAttempt_ThrowsArgumentException), out method))
            errors.Add("Missing method: CalculateDelay_WithNegativeAttempt_ThrowsArgumentException");

        // Validate CalculateDelay_WithNoRetryPolicy_ReturnsZero
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_WithNoRetryPolicy_ReturnsZero), out method))
            errors.Add("Missing method: CalculateDelay_WithNoRetryPolicy_ReturnsZero");

        // Validate CalculateDelay_WithFixedIntervalPolicy_ReturnsSameDelayEachAttempt
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_WithFixedIntervalPolicy_ReturnsSameDelayEachAttempt), out method))
            errors.Add("Missing method: CalculateDelay_WithFixedIntervalPolicy_ReturnsSameDelayEachAttempt");

        // Validate CalculateDelay_WithLinearBackoffPolicy_IncreasesProperly
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_WithLinearBackoffPolicy_IncreasesProperly), out method))
            errors.Add("Missing method: CalculateDelay_WithLinearBackoffPolicy_IncreasesProperly");

        // Validate CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases), out method))
            errors.Add("Missing method: CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases");

        // Validate CalculateDelay_RespectMaxDelayLimit
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_RespectMaxDelayLimit), out method))
            errors.Add("Missing method: CalculateDelay_RespectMaxDelayLimit");

        // Validate CalculateDelay_WithJitterEnabled_AddsRandomness
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_WithJitterEnabled_AddsRandomness), out method))
            errors.Add("Missing method: CalculateDelay_WithJitterEnabled_AddsRandomness");

        // Validate CalculateDelay_WithJitterDisabled_ProducesSameDelay
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_WithJitterDisabled_ProducesSameDelay), out method))
            errors.Add("Missing method: CalculateDelay_WithJitterDisabled_ProducesSameDelay");

        // Validate CalculateDelay_ReturnsMinimumOneSecond
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateDelay_ReturnsMinimumOneSecond), out method))
            errors.Add("Missing method: CalculateDelay_ReturnsMinimumOneSecond");

        // Validate CalculateStatistics_ReturnsProperValues
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateStatistics_ReturnsProperValues), out method))
            errors.Add("Missing method: CalculateStatistics_ReturnsProperValues");

        // Validate CalculateStatistics_WithOneAttempt_HasZeroRetries
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateStatistics_WithOneAttempt_HasZeroRetries), out method))
            errors.Add("Missing method: CalculateStatistics_WithOneAttempt_HasZeroRetries");

        // Validate CalculateStatistics_CalculatesAverageCorrectly
        if (!TryGetTestMethod(testType, nameof(RetryPolicyHelperTests.CalculateStatistics_CalculatesAverageCorrectly), out method))
            errors.Add("Missing method: CalculateStatistics_CalculatesAverageCorrectly");

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Helper method to safely retrieve a test method by name.
    /// </summary>
    /// <param name="type">The type to search for the method.</param>
    /// <param name="methodName">The name of the method to find.</param>
    /// <param name="method">Output parameter for the found method.</param>
    /// <returns><see langword="true"/> if the method was found; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetTestMethod(Type type, string methodName, out MethodInfo? method)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(methodName);

        method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        return method is not null;
    }

    /// <summary>
    /// Determines whether the specified <see cref="RetryPolicyHelperTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this RetryPolicyHelperTests value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="RetryPolicyHelperTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is not valid.</exception>
    public static void EnsureValid(this RetryPolicyHelperTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"RetryPolicyHelperTests instance is not valid. Problems:\n{string.Join("\n", errors)}");
        }
    }
}