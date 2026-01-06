// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Validation helper utilities - provides fluent validation and common validation patterns
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Throws an ArgumentException if value is null or empty
    /// </summary>
    public static void ValidateNotEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
    }

    /// <summary>
    /// Throws an ArgumentNullException if value is null
    /// </summary>
    public static void ValidateNotNull<T>(T? value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Throws an ArgumentException if value is not positive
    /// </summary>
    public static void ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentException($"{paramName} must be positive", paramName);
    }

    /// <summary>
    /// Throws an ArgumentException if value is not in valid range
    /// </summary>
    public static void ValidateRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
            throw new ArgumentException($"{paramName} must be between {min} and {max}", paramName);
    }

    /// <summary>
    /// Throws an ArgumentException if value is not within the specified length
    /// </summary>
    public static void ValidateLength(string? value, int minLength, int maxLength, string paramName)
    {
        if (string.IsNullOrEmpty(value) || value.Length < minLength || value.Length > maxLength)
            throw new ArgumentException(
                $"{paramName} length must be between {minLength} and {maxLength}",
                paramName);
    }

    /// <summary>
    /// Validates that a collection contains at least one item matching predicate
    /// </summary>
    public static void ValidateAny<T>(IEnumerable<T> collection, Func<T, bool> predicate, string errorMessage)
    {
        if (!collection.Any(predicate))
            throw new ArgumentException(errorMessage);
    }

    /// <summary>
    /// Validates that all items in a collection match a predicate
    /// </summary>
    public static void ValidateAll<T>(IEnumerable<T> collection, Func<T, bool> predicate, string errorMessage)
    {
        if (!collection.All(predicate))
            throw new ArgumentException(errorMessage);
    }

    /// <summary>
    /// Validates that two values are equal
    /// </summary>
    public static void ValidateEqual<T>(T expected, T actual, string paramName) where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
            throw new ArgumentException($"{paramName} value mismatch", paramName);
    }

    /// <summary>
    /// Validates that a value matches a specific pattern/condition
    /// </summary>
    public static void ValidateCondition(bool condition, string errorMessage)
    {
        if (!condition)
            throw new ArgumentException(errorMessage);
    }

    /// <summary>
    /// Creates a validation context for fluent validation
    /// </summary>
    public static ValidationContext<T> Validate<T>(T value)
    {
        return new ValidationContext<T>(value);
    }
}

/// <summary>
/// Fluent validation context for chaining validations
/// </summary>
public class ValidationContext<T>
{
    private readonly T _value;
    private readonly List<string> _errors = new();

    public ValidationContext(T value)
    {
        _value = value;
    }

    public ValidationContext<T> NotNull(string fieldName)
    {
        if (_value == null)
            _errors.Add($"{fieldName} cannot be null");
        return this;
    }

    public ValidationContext<T> NotEmpty(Func<T, string?> accessor, string fieldName)
    {
        var value = accessor(_value);
        if (string.IsNullOrWhiteSpace(value))
            _errors.Add($"{fieldName} cannot be empty");
        return this;
    }

    public ValidationContext<T> MinLength(Func<T, string?> accessor, int minLength, string fieldName)
    {
        var value = accessor(_value);
        if (value?.Length < minLength)
            _errors.Add($"{fieldName} must be at least {minLength} characters");
        return this;
    }

    public ValidationContext<T> MaxLength(Func<T, string?> accessor, int maxLength, string fieldName)
    {
        var value = accessor(_value);
        if (value?.Length > maxLength)
            _errors.Add($"{fieldName} cannot exceed {maxLength} characters");
        return this;
    }

    public ValidationContext<T> Condition(bool condition, string errorMessage)
    {
        if (!condition)
            _errors.Add(errorMessage);
        return this;
    }

    public void ThrowIfInvalid()
    {
        if (_errors.Count > 0)
            throw new ArgumentException(string.Join("; ", _errors));
    }

    public bool IsValid => _errors.Count == 0;

    public IReadOnlyList<string> Errors => _errors.AsReadOnly();
}
