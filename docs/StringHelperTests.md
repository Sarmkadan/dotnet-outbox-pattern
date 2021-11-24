# StringHelperTests

Unit test class that verifies the behavior of `StringHelper` utility methods. Each test validates input/output transformations, edge cases, and expected exceptions to ensure consistent behavior across common string manipulation scenarios.

## API

### `void ComputeSha256Hash_ReturnsExpectedHash()`

Validates that `StringHelper.ComputeSha256Hash` produces the correct SHA-256 hash for given inputs. The test confirms that identical inputs yield identical hashes and that the output length is always 64 hexadecimal characters.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: Does not throw under test conditions.

---

### `void IsValidEmail_ReturnsExpectedResult()`

Ensures that `StringHelper.IsValidEmail` correctly identifies valid and invalid email formats according to RFC 5322 standards. The test covers common patterns including local-parts, domains, subdomains, and special characters.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: Does not throw under test conditions.

---
### `void Truncate_ReturnsExpectedString()`

Confirms that `StringHelper.Truncate` returns a substring of the specified length and appends an ellipsis when truncation occurs. The test validates behavior for inputs shorter than, equal to, and longer than the maximum length.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: Does not throw under test conditions.

---
### `void ToSlug_ReturnsSlugifiedString()`

Verifies that `StringHelper.ToSlug` converts input strings into URL-friendly slugs by replacing spaces and special characters with hyphens, converting to lowercase, and removing consecutive separators.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: Does not throw under test conditions.

---
### `void ToKebabCase_ReturnsKebabCaseString()`

Checks that `StringHelper.ToKebabCase` transforms PascalCase, camelCase, and space-separated strings into kebab-case format by inserting hyphens between words and converting to lowercase.

- **Parameters**: None.
- **Return value**: None.
- **Throws**: Does not throw under test conditions.

---

## Usage
