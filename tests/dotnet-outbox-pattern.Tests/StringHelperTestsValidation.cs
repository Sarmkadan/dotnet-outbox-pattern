#nullable enable

using DotnetOutboxPattern.Utilities;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Validation helpers for StringHelper method parameters used in StringHelperTests
/// Provides validation for test data validation
/// </summary>
public static class StringHelperTestsValidation
{
    /// <summary>
    /// Validates that a string input for ComputeSha256Hash is valid
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if input is null</exception>
    public static IReadOnlyList<string> Validate(string? input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return [];
    }

    /// <summary>
    /// Validates that an email string for IsValidEmail is valid
    /// </summary>
    /// <param name="email">The email string to validate</param>
    /// <param name="expectedResult">The expected boolean result</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if email is null</exception>
    public static IReadOnlyList<string> Validate(string? email, bool expectedResult)
    {
        ArgumentNullException.ThrowIfNull(email);

        return [];
    }

    /// <summary>
    /// Validates that truncation parameters are valid
    /// </summary>
    /// <param name="input">The input string to truncate</param>
    /// <param name="maxLength">The maximum length</param>
    /// <param name="expected">The expected result</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if input or expected is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxLength is not positive</exception>
    public static IReadOnlyList<string> Validate(string? input, int maxLength, string expected)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxLength, 0);

        return [];
    }

    /// <summary>
    /// Validates that a string input for ToSlug or ToKebabCase is valid
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <param name="expected">The expected result</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if input or expected is null</exception>
    public static IReadOnlyList<string> Validate(string? input, string expected)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(expected);

        return [];
    }

    /// <summary>
    /// Determines whether the input is valid for ComputeSha256Hash
    /// </summary>
    /// <param name="input">The input string to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if input is null</exception>
    public static bool IsValid(string? input)
        => Validate(input).Count == 0;

    /// <summary>
    /// Determines whether the email and expected result are valid for IsValidEmail
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <param name="expectedResult">The expected boolean result</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if email is null</exception>
    public static bool IsValid(string? email, bool expectedResult)
        => Validate(email, expectedResult).Count == 0;

    /// <summary>
    /// Determines whether the truncation parameters are valid
    /// </summary>
    /// <param name="input">The input string to check</param>
    /// <param name="maxLength">The maximum length</param>
    /// <param name="expected">The expected result</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if input or expected is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxLength is not positive</exception>
    public static bool IsValid(string? input, int maxLength, string expected)
        => Validate(input, maxLength, expected).Count == 0;

    /// <summary>
    /// Determines whether the input and expected result are valid for ToSlug or ToKebabCase
    /// </summary>
    /// <param name="input">The input string to check</param>
    /// <param name="expected">The expected result</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if input or expected is null</exception>
    public static bool IsValid(string? input, string expected)
        => Validate(input, expected).Count == 0;

    /// <summary>
    /// Ensures that the input is valid for ComputeSha256Hash
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if input is null</exception>
    public static void EnsureValid(string? input)
    {
        _ = Validate(input);
    }

    /// <summary>
    /// Ensures that the email and expected result are valid for IsValidEmail
    /// </summary>
    /// <param name="email">The email to validate</param>
    /// <param name="expectedResult">The expected boolean result</param>
    /// <exception cref="ArgumentNullException">Thrown if email is null</exception>
    public static void EnsureValid(string? email, bool expectedResult)
    {
        _ = Validate(email, expectedResult);
    }

    /// <summary>
    /// Ensures that the truncation parameters are valid
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <param name="maxLength">The maximum length</param>
    /// <param name="expected">The expected result</param>
    /// <exception cref="ArgumentNullException">Thrown if input or expected is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxLength is not positive</exception>
    public static void EnsureValid(string? input, int maxLength, string expected)
    {
        _ = Validate(input, maxLength, expected);
    }

    /// <summary>
    /// Ensures that the input and expected result are valid for ToSlug or ToKebabCase
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <param name="expected">The expected result</param>
    /// <exception cref="ArgumentNullException">Thrown if input or expected is null</exception>
    public static void EnsureValid(string? input, string expected)
    {
        _ = Validate(input, expected);
    }
}