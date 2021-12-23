#nullable enable

using System.Reflection;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides validation helpers for <see cref="RetryPolicyHelperTests"/> instances.
/// Validates that all required test methods exist and can be invoked.
/// </summary>
public static class RetryPolicyHelperTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="RetryPolicyHelperTests"/> instance.
    /// Ensures all required test methods exist and are accessible.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>An enumerable of validation messages; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this RetryPolicyHelperTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        var testType = typeof(RetryPolicyHelperTests);

        // Validate CalculateDelay_WithZeroAttempt_ThrowsArgumentException
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_WithZeroAttempt_ThrowsArgumentException),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_WithZeroAttempt_ThrowsArgumentException");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_WithZeroAttempt_ThrowsArgumentException method");
        }

        // Validate CalculateDelay_WithNegativeAttempt_ThrowsArgumentException
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_WithNegativeAttempt_ThrowsArgumentException),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_WithNegativeAttempt_ThrowsArgumentException");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_WithNegativeAttempt_ThrowsArgumentException method");
        }

        // Validate CalculateDelay_WithNoRetryPolicy_ReturnsZero
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_WithNoRetryPolicy_ReturnsZero),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_WithNoRetryPolicy_ReturnsZero");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_WithNoRetryPolicy_ReturnsZero method");
        }

        // Validate CalculateDelay_WithFixedIntervalPolicy_ReturnsSameDelayEachAttempt
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_WithFixedIntervalPolicy_ReturnsSameDelayEachAttempt),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_WithFixedIntervalPolicy_ReturnsSameDelayEachAttempt");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_WithFixedIntervalPolicy_ReturnsSameDelayEachAttempt method");
        }

        // Validate CalculateDelay_WithLinearBackoffPolicy_IncreasesProperly
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_WithLinearBackoffPolicy_IncreasesProperly),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_WithLinearBackoffPolicy_IncreasesProperly");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_WithLinearBackoffPolicy_IncreasesProperly method");
        }

        // Validate CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_WithExponentialBackoffPolicy_ExponentiallyIncreases method");
        }

        // Validate CalculateDelay_RespectMaxDelayLimit
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_RespectMaxDelayLimit),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_RespectMaxDelayLimit");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_RespectMaxDelayLimit method");
        }

        // Validate CalculateDelay_WithJitterEnabled_AddsRandomness
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_WithJitterEnabled_AddsRandomness),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_WithJitterEnabled_AddsRandomness");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_WithJitterEnabled_AddsRandomness method");
        }

        // Validate CalculateDelay_WithJitterDisabled_ProducesSameDelay
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_WithJitterDisabled_ProducesSameDelay),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_WithJitterDisabled_ProducesSameDelay");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_WithJitterDisabled_ProducesSameDelay method");
        }

        // Validate CalculateDelay_ReturnsMinimumOneSecond
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateDelay_ReturnsMinimumOneSecond),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateDelay_ReturnsMinimumOneSecond");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateDelay_ReturnsMinimumOneSecond method");
        }

        // Validate CalculateStatistics_ReturnsProperValues
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateStatistics_ReturnsProperValues),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateStatistics_ReturnsProperValues");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateStatistics_ReturnsProperValues method");
        }

        // Validate CalculateStatistics_WithOneAttempt_HasZeroRetries
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateStatistics_WithOneAttempt_HasZeroRetries),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateStatistics_WithOneAttempt_HasZeroRetries");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateStatistics_WithOneAttempt_HasZeroRetries method");
        }

        // Validate CalculateStatistics_CalculatesAverageCorrectly
        try
        {
            var method = testType.GetMethod(
                nameof(RetryPolicyHelperTests.CalculateStatistics_CalculatesAverageCorrectly),
                BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                errors.Add("Missing method: CalculateStatistics_CalculatesAverageCorrectly");
            }
        }
        catch
        {
            errors.Add("Failed to validate CalculateStatistics_CalculatesAverageCorrectly method");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="RetryPolicyHelperTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this RetryPolicyHelperTests value)
    {
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