#nullable enable

using DotnetOutboxPattern.Utilities;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for the StringHelper class.
/// </summary>
public sealed class StringHelperTests
{
    [Theory]
    [InlineData("hello")]
    public void ComputeSha256Hash_ReturnsExpectedHash(string input)
    {
        /// <summary>
        /// Verifies that the ComputeSha256Hash method returns a non-empty hash for a given input string.
        /// </summary>
        /// <param name="input">The input string to compute the hash for.</param>
        var hash = StringHelper.ComputeSha256Hash(input);
        hash.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_ReturnsExpectedResult(string? email, bool expected)
    {
        /// <summary>
        /// Verifies that the IsValidEmail method returns the expected result for a given email address.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <param name="expected">The expected result of the validation.</param>
        StringHelper.IsValidEmail(email).Should().Be(expected);
    }

    [Theory]
    [InlineData("hello world", 5, "he...")]
    [InlineData("hello", 10, "hello")]
    [InlineData(null, 5, "")]
    public void Truncate_ReturnsExpectedString(string? input, int maxLength, string expected)
    {
        /// <summary>
        /// Verifies that the Truncate method returns the expected truncated string for a given input string and maximum length.
        /// </summary>
        /// <param name="input">The input string to truncate.</param>
        /// <param name="maxLength">The maximum length of the output string.</param>
        /// <param name="expected">The expected truncated string.</param>
        StringHelper.Truncate(input, maxLength).Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("This is a test!", "this-is-a-test")]
    public void ToSlug_ReturnsSlugifiedString(string input, string expected)
    {
        /// <summary>
        /// Verifies that the ToSlug method returns the expected slugified string for a given input string.
        /// </summary>
        /// <param name="input">The input string to slugify.</param>
        /// <param name="expected">The expected slugified string.</param>
        StringHelper.ToSlug(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("PascalCaseString", "pascal-case-string")]
    [InlineData("Already-Kebab-Case", "already-kebab-case")]
    public void ToKebabCase_ReturnsKebabCaseString(string input, string expected)
    {
        /// <summary>
        /// Verifies that the ToKebabCase method returns the expected kebab-case string for a given input string.
        /// </summary>
        /// <param name="input">The input string to convert to kebab-case.</param>
        /// <param name="expected">The expected kebab-case string.</param>
        StringHelper.ToKebabCase(input).Should().Be(expected);
    }
}
