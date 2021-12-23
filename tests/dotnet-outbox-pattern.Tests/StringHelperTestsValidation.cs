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
    public static IReadOnlyList<string> Validate(string? input)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(input))
        {
            problems.Add("Input string is null, empty, or whitespace");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that an email string for IsValidEmail is valid
    /// </summary>
    /// <param name="email">The email string to validate</param>
    /// <param name="expectedResult">The expected boolean result</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    public static IReadOnlyList<string> Validate(string? email, bool expectedResult)
    {
        var problems = new List<string>();

        if (email is not null && string.IsNullOrWhiteSpace(email))
        {
            problems.Add("Email string is empty or whitespace");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that truncation parameters are valid
    /// </summary>
    /// <param name="input">The input string to truncate</param>
    /// <param name="maxLength">The maximum length</param>
    /// <param name="expected">The expected result</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    public static IReadOnlyList<string> Validate(string? input, int maxLength, string expected)
    {
        var problems = new List<string>();

        if (input is not null && string.IsNullOrWhiteSpace(input))
        {
            problems.Add("Input string is empty or whitespace");
        }

        if (maxLength <= 0)
        {
            problems.Add("maxLength must be positive");
        }

        if (expected is null)
        {
            problems.Add("Expected result is null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a string input for ToSlug or ToKebabCase is valid
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <param name="expected">The expected result</param>
    /// <returns>A list of human-readable validation problems, or empty if valid</returns>
    public static IReadOnlyList<string> Validate(string? input, string expected)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(input))
        {
            problems.Add("Input string is null, empty, or whitespace");
        }

        if (expected is null)
        {
            problems.Add("Expected result is null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the input is valid for ComputeSha256Hash
    /// </summary>
    /// <param name="input">The input string to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    public static bool IsValid(string? input)
    {
        return Validate(input).Count == 0;
    }

    /// <summary>
    /// Determines whether the email and expected result are valid for IsValidEmail
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <param name="expectedResult">The expected boolean result</param>
    /// <returns>True if valid; otherwise, false</returns>
    public static bool IsValid(string? email, bool expectedResult)
    {
        return Validate(email, expectedResult).Count == 0;
    }

    /// <summary>
    /// Determines whether the truncation parameters are valid
    /// </summary>
    /// <param name="input">The input string to check</param>
    /// <param name="maxLength">The maximum length</param>
    /// <param name="expected">The expected result</param>
    /// <returns>True if valid; otherwise, false</returns>
    public static bool IsValid(string? input, int maxLength, string expected)
    {
        return Validate(input, maxLength, expected).Count == 0;
    }

    /// <summary>
    /// Determines whether the input and expected result are valid for ToSlug or ToKebabCase
    /// </summary>
    /// <param name="input">The input string to check</param>
    /// <param name="expected">The expected result</param>
    /// <returns>True if valid; otherwise, false</returns>
    public static bool IsValid(string? input, string expected)
    {
        return Validate(input, expected).Count == 0;
    }

    /// <summary>
    /// Ensures that the input is valid for ComputeSha256Hash
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <exception cref="ArgumentException">Thrown if input is invalid</exception>
    public static void EnsureValid(string? input)
    {
        var problems = Validate(input);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Input is invalid for ComputeSha256Hash:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the email and expected result are valid for IsValidEmail
    /// </summary>
    /// <param name="email">The email to validate</param>
    /// <param name="expectedResult">The expected boolean result</param>
    /// <exception cref="ArgumentException">Thrown if parameters are invalid</exception>
    public static void EnsureValid(string? email, bool expectedResult)
    {
        var problems = Validate(email, expectedResult);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Parameters are invalid for IsValidEmail:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the truncation parameters are valid
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <param name="maxLength">The maximum length</param>
    /// <param name="expected">The expected result</param>
    /// <exception cref="ArgumentException">Thrown if parameters are invalid</exception>
    public static void EnsureValid(string? input, int maxLength, string expected)
    {
        var problems = Validate(input, maxLength, expected);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Truncation parameters are invalid:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", problems)}");
        }
    }

    /// <summary>
    /// Ensures that the input and expected result are valid for ToSlug or ToKebabCase
    /// </summary>
    /// <param name="input">The input string to validate</param>
    /// <param name="expected">The expected result</param>
    /// <exception cref="ArgumentException">Thrown if parameters are invalid</exception>
    public static void EnsureValid(string? input, string expected)
    {
        var problems = Validate(input, expected);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Parameters are invalid for ToSlug/ToKebabCase:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", problems)}");
        }
    }
}