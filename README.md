// existing content ...

## StringHelperTests

The `StringHelperTests` class provides a set of unit tests for the string helper methods, including hash computation, email validation, string truncation, and string formatting. These tests verify the correctness of various string operations, ensuring that the helper methods behave as expected. 

### Example Usage

```csharp
using DotnetOutboxPattern.Tests;

class Program
{
    static void Main()
    {
        var tests = new StringHelperTests();

        // Test hash computation
        tests.ComputeSha256Hash_ReturnsExpectedHash();

        // Test email validation
        tests.IsValidEmail_ReturnsExpectedResult();

        // Test string truncation
        tests.Truncate_ReturnsExpectedString();

        // Test string formatting
        tests.ToSlug_ReturnsSlugifiedString();
        tests.ToKebabCase_ReturnsKebabCaseString();
    }
}
```

// existing content ...
