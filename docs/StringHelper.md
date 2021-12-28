# StringHelper

Utility class providing common string manipulation and validation helpers used throughout the dotnet-outbox-pattern project.

## API

### ComputeSha256Hash
```csharp
public static string ComputeSha256Hash(string input)
```
- **Purpose**: Returns the SHA‑256 hash of the supplied string as a lowercase hexadecimal string.  
- **Parameters**:  
  - `input`: The string to hash. Must not be `null`.  
- **Return value**: A 64‑character hexadecimal string representing the hash.  
- **Exceptions**:  
  - `ArgumentNullException` if `input` is `null`.

### IsValidFormat
```csharp
public static bool IsValidFormat(string input, string pattern)
```
- **Purpose**: Determines whether `input` matches the regular expression supplied in `pattern`.  
- **Parameters**:  
  - `input`: The string to test. May be `null`; a `null` input yields `false`.  
  - `pattern`: A .NET regular expression pattern. Must not be `null`.  
- **Return value**: `true` if `input` matches `pattern`; otherwise `false`.  
- **Exceptions**:  
  - `ArgumentNullException` if `pattern` is `null`.  
  - `ArgumentException` if `pattern` is an invalid regular expression.

### IsValidGuid
```csharp
public static bool IsValidGuid(string input)
```
- **Purpose**: Checks whether `input` can be parsed as a `System.Guid`.  
- **Parameters**:  
  - `input`: The string to test. May be `null`; a `null` input yields `false`.  
- **Return value**: `true` if `input` is a valid GUID representation; otherwise `false`.  
- **Exceptions**: None.

### IsValidEmail
```csharp
public static bool IsValidEmail(string input)
```
- **Purpose**: Validates that `input` conforms to a typical email address format.  
- **Parameters**:  
  - `input`: The string to test. May be `null`; a `null` input yields `false`.  
- **Return value**: `true` if `input looks like a valid email address; otherwise `false`.  
- **Exceptions**: None.

### Truncate
```csharp
public static string Truncate(string input, int maxLength, string suffix = "...")
```
- **Purpose**: Returns `input` shortened to `maxLength` characters, appending `suffix` when truncation occurs.  
- **Parameters**:  
  - `input`: The string to truncate. May be `null`; returns `null`.  
  - `maxLength`: Desired maximum length of the returned string, excluding the suffix. Must be non‑negative.  
  - `suffix`: Optional string appended when `input` exceeds `maxLength`. Defaults to `"..."`.  
- **Return value**: The truncated string, or the original string if its length ≤ `maxLength`.  
- **Exceptions**:  
  - `ArgumentOutOfRangeException` if `maxLength` is negative.

### SanitizeForJson
```csharp
public static string SanitizeForJson(string input)
```
- **Purpose**: Escapes characters in `input` so that the result can be safely placed inside a JSON string literal.  
- **Parameters**:  
  - `input`: The string to sanitize. May be `null`; returns `null`.  
- **Return value**: A new string with JSON‑escaped characters (e.g., `\"`, `\\`, `\/`).  
- **Exceptions**: None.

### ToSlug
```csharp
public static string ToSlug(string input)
```
- **Purpose**: Converts `input` to a URL‑friendly slug: lowercased, spaces and special characters replaced with hyphens, consecutive hyphens collapsed, and leading/trailing hyphens removed.  
- **Parameters**:  
  - `input`: The string to convert. May be `null`; returns `null`.  
- **Return value**: A slug string suitable for use in URLs.  
- **Exceptions**: None.

### ToKebabCase
```csharp
public static string ToKebabCase(string input)
```
- **Purpose**: Transforms `input` into kebab‑case (all lowercase, words separated by hyphens).  
- **Parameters**:  
  - `input`: The string to convert. May be `null`; returns `null`.  
- **Return value**: A kebab‑cased string.  
- **Exceptions**: None.

### GenerateRandomString
```csharp
public static string GenerateRandomString(int length, string allowedChars = null)
```
- **Purpose**: Produces a cryptographically‑strong random string of the specified length using the supplied character set.  
- **Parameters**:  
  - `length`: Desired length of the result. Must be positive.  
  - `allowedChars`: Optional set of characters to draw from. If `null`, defaults to alphanumeric (`A-Z`, `a-z`, `0-9`).  
- **Return value**: A random string of length `length`.  
- **Exceptions**:  
  - `ArgumentOutOfRangeException` if `length` is less than 1.  
  - `ArgumentException` if `allowedChars` is provided and empty.

### IsEmpty
```csharp
public static bool IsEmpty(string input)
```
- **Purpose**: Determines whether `input` is `null`, empty (`""`), or consists only of white‑space characters.  
- **Parameters**:  
  - `input`: The string to test.  
- **Return value**: `true` if `input` is null, empty, or whitespace; otherwise `false`.  
- **Exceptions**: None.

### JoinNonEmpty
```csharp
public static string JoinNonEmpty(string separator, params string[] values)
```
- **Purpose**: Concatenates the non‑null, non‑empty elements of `values` using `separator`.  
- **Parameters**:  
  - `separator`: The string placed between elements. May be `null`; treated as empty string.  
  - `values`: Variable list of strings to join. Null entries are ignored.  
- **Return value**: A single string containing all non‑empty values separated by `separator`. Returns an empty string if no values qualify.  
- **Exceptions**: None.

### ExtractBetween
```csharp
public static string ExtractBetween(string input, string startMarker, string endMarker)
```
- **Purpose**: Returns the substring that appears between the first occurrence of `startMarker` and the first occurrence of `endMarker` that follows it.  
- **Parameters**:  
  - `input`: The string to search. May be `null`; returns `null`.  
  - `startMarker`: The string that marks the beginning of the desired excerpt. Must not be `null` or empty.  
  - `endMarker`: The string that marks the end of the desired excerpt. Must not be `null` or empty.  
- **Return value**: The substring between the markers, or an empty string if the markers are found adjacent. Returns `null` if either marker is not found.  
- **Exceptions**:  
  - `ArgumentException` if `startMarker` or `endMarker` is `null` or empty.

## Usage

```csharp
using static MyProject.Helpers.StringHelper;

// Validate and hash a user‑provided password before storage.
string password = Console.ReadLine();
if (!IsEmpty(password) && IsValidFormat(password, @"^.{8,}$"))
{
    string hash = ComputeSha256Hash(password);
    StoreHashInDatabase(hash);
}
else
{
    Console.WriteLine("Password must be at least 8 characters.");
}
```

```csharp
// Build a slug for a blog post title and generate a random identifier.
string title = "  Announcing the .NET Outbox Pattern!  ";
string slug = ToSlug(title);                     // "announcing-the-net-outbox-pattern"
string id    = GenerateRandomString(8);          // e.g., "a3F9zQ2L"
string url   = $"/posts/{slug}/{id}";
```

## Notes

- All methods are **static** and contain no mutable state; therefore they are thread‑safe and can be called concurrently from multiple threads without additional synchronization.  
- Null handling follows a consistent pattern: methods that require a non‑null argument throw `ArgumentNullException`; methods that merely inspect the string treat `null` as an invalid or empty value and return a sensible default (`false` for validators, `null` for pass‑through methods).  
- `Truncate` respects the `suffix` length; if `maxLength` is less than the suffix length, the method will still return the suffix (potentially exceeding `maxLength`) – callers should ensure `maxLength` is sufficient for the desired output.  
- `GenerateRandomString` uses `RandomNumberGenerator` internally, providing cryptographic suitability; supplying a custom `allowedChars` empty string will raise an exception.  
- Regular expression patterns passed to `IsValidFormat` are not cached; if the same pattern is used repeatedly, consider pre‑compiling it with `RegexOptions.Compiled` for performance.  
- Culture‑insensitive comparisons are used where applicable (e.g., `ToSlug`, `ToKebabCase`) to ensure deterministic output across different environments.
