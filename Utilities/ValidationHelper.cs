#nullable enable
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
    /// Throws an ArgumentException if value is null or empty.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    public static void ValidateNotEmpty(string? value, string paramName)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(paramName);

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
    }

    /// <summary>
    /// Throws an ArgumentNullException if value is null.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    public static void ValidateNotNull<T>(T? value, string paramName) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(paramName);

        if (value is null)
            throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Throws an ArgumentException if value is not positive.
    /// </summary>
    /// <param name="value">The integer value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    public static void ValidatePositive(int value, string paramName)
    {
        ArgumentNullException.ThrowIfNull(paramName);

        if (value <= 0)
            throw new ArgumentException($"{paramName} must be positive", paramName);
    }

    /// <summary>
    /// Throws an ArgumentException if value is not in valid range.
    /// </summary>
    /// <param name="value">The integer value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    public static void ValidateRange(int value, int min, int max, string paramName)
    {
        ArgumentNullException.ThrowIfNull(paramName);

        if (value < min || value > max)
            throw new ArgumentException($"{paramName} must be between {min} and {max}", paramName);
    }

    /// <summary>
    /// Throws an ArgumentException if value is not within the specified length.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="minLength">The minimum allowed length.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    public static void ValidateLength(string? value, int minLength, int maxLength, string paramName)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(paramName);

        if (string.IsNullOrEmpty(value) || value.Length < minLength || value.Length > maxLength)
            throw new ArgumentException(
                $"{paramName} length must be between {minLength} and {maxLength}",
                paramName);
    }

    /// <summary>
    /// Validates that a collection contains at least one item matching predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="predicate">The predicate to test each element against.</param>
    /// <param name="errorMessage">The error message to throw if validation fails.</param>
    public static void ValidateAny<T>(IEnumerable<T> collection, Func<T, bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorMessage);

        if (!collection.Any(predicate))
            throw new ArgumentException(errorMessage);
    }

    /// <summary>
    /// Validates that all items in a collection match a predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="predicate">The predicate to test each element against.</param>
    /// <param name="errorMessage">The error message to throw if validation fails.</param>
    public static void ValidateAll<T>(IEnumerable<T> collection, Func<T, bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorMessage);

        if (!collection.All(predicate))
            throw new ArgumentException(errorMessage);
    }

    /// <summary>
    /// Validates that two values are equal.
    /// </summary>
    /// <typeparam name="T">The type of the values to compare.</typeparam>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    public static void ValidateEqual<T>(T expected, T actual, string paramName) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(paramName);

        if (!expected.Equals(actual))
            throw new ArgumentException($"{paramName} value mismatch", paramName);
    }

    /// <summary>
    /// Validates that a value matches a specific pattern/condition.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="errorMessage">The error message to throw if validation fails.</param>
    public static void ValidateCondition(bool condition, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

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
public sealed class ValidationContext<T>
{
    private readonly T _value;
    private readonly List<string> _errors = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationContext{T}"/> class.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    public ValidationContext(T value)
    {
        _value = value;
    }

    /// <summary>
    /// Validates that the value is not null.
    /// </summary>
    /// <param name="fieldName">The name of the field being validated.</param>
    /// <returns>The validation context for chaining.</returns>
    public ValidationContext<T> NotNull(string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (_value is null)
            _errors.Add($"{fieldName} cannot be null");
        return this;
    }

    public ValidationContext<T> NotEmpty(Func<T, string?> accessor, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(fieldName);

        var value = accessor(_value);
        if (string.IsNullOrWhiteSpace(value))
            _errors.Add($"{fieldName} cannot be empty");
        return this;
    }

    public ValidationContext<T> MinLength(Func<T, string?> accessor, int minLength, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(fieldName);

        var value = accessor(_value);
        if (value?.Length < minLength)
            _errors.Add($"{fieldName} must be at least {minLength} characters");
        return this;
    }

    public ValidationContext<T> MaxLength(Func<T, string?> accessor, int maxLength, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(fieldName);

        var value = accessor(_value);
        if (value?.Length > maxLength)
            _errors.Add($"{fieldName} cannot exceed {maxLength} characters");
        return this;
    }

    public ValidationContext<T> Condition(bool condition, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

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
