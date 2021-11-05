#nullable enable

using DotnetOutboxPattern.Utilities;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

public sealed class StringHelperTests
{
    [Theory]
    [InlineData("hello")]
    public void ComputeSha256Hash_ReturnsExpectedHash(string input)
    {
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
        StringHelper.IsValidEmail(email).Should().Be(expected);
    }

    [Theory]
    [InlineData("hello world", 5, "he...")]
    [InlineData("hello", 10, "hello")]
    [InlineData(null, 5, "")]
    public void Truncate_ReturnsExpectedString(string? input, int maxLength, string expected)
    {
        StringHelper.Truncate(input, maxLength).Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("This is a test!", "this-is-a-test")]
    public void ToSlug_ReturnsSlugifiedString(string input, string expected)
    {
        StringHelper.ToSlug(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("PascalCaseString", "pascal-case-string")]
    [InlineData("Already-Kebab-Case", "already-kebab-case")]
    public void ToKebabCase_ReturnsKebabCaseString(string input, string expected)
    {
        StringHelper.ToKebabCase(input).Should().Be(expected);
    }
}
