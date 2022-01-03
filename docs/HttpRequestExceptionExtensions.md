# HttpRequestExceptionExtensions

Provides extension methods for `HttpRequestException` to simplify HTTP status code inspection and error message formatting.

## API

### `public static int GetStatusCode(HttpRequestException exception)`

Extracts the HTTP status code from the exception.

- **Parameters**
  - `exception`: The `HttpRequestException` instance to inspect.
- **Return value**
  - The HTTP status code as an integer.
- **Exceptions**
  - Throws `ArgumentNullException` if `exception` is `null`.
  - Throws `InvalidOperationException` if the exception does not contain a valid status code.

---

### `public static bool IsClientError(HttpRequestException exception)`

Determines whether the HTTP status code indicates a client error (4xx).

- **Parameters**
  - `exception`: The `HttpRequestException` instance to inspect.
- **Return value**
  - `true` if the status code is in the 4xx range; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `exception` is `null`.

---

### `public static bool IsServerError(HttpRequestException exception)`

Determines whether the HTTP status code indicates a server error (5xx).

- **Parameters**
  - `exception`: The `HttpRequestException` instance to inspect.
  - **Return value**
  - `true` if the status code is in the 5xx range; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `exception` is `null`.

---
### `public static string ToErrorMessage(HttpRequestException exception)`

Formats a human-readable error message from the exception, including the status code and reason phrase.

- **Parameters**
  - `exception`: The `HttpRequestException` instance to inspect.
- **Return value**
  - A formatted error message string.
- **Exceptions**
  - Throws `ArgumentNullException` if `exception` is `null`.

## Usage

```csharp
using System;
using System.Net;
using System.Net.Http;
using DotNetOutboxPattern.Extensions;

public class Example
{
    public static void Main()
    {
        try
        {
            // Simulate an HTTP call that fails with 404
            throw new HttpRequestException("Resource not found", null, HttpStatusCode.NotFound);
        }
        catch (HttpRequestException ex)
        {
            if (HttpRequestExceptionExtensions.IsClientError(ex))
            {
                Console.WriteLine(HttpRequestExceptionExtensions.ToErrorMessage(ex));
            }
        }
    }
}
```

```csharp
using System;
using System.Net;
using System.Net.Http;
using DotNetOutboxPattern.Extensions;

public class Example
{
    public static void Main()
    {
        try
        {
            // Simulate an HTTP call that fails with 500
            throw new HttpRequestException("Internal server error", null, HttpStatusCode.InternalServerError);
        }
        catch (HttpRequestException ex)
        {
            if (HttpRequestExceptionExtensions.IsServerError(ex))
            {
                Console.WriteLine($"Server error detected: {HttpRequestExceptionExtensions.GetStatusCode(ex)}");
            }
        }
    }
}
```

## Notes

- All methods validate the input `exception` parameter and throw `ArgumentNullException` if `null`.
- `GetStatusCode` may throw `InvalidOperationException` if the exception lacks a status code, which can occur if the exception was constructed without one.
- The methods are thread-safe as they do not modify shared state and only read from the exception object.
- Status code ranges are validated strictly (4xx for client errors, 5xx for server errors); no special handling is provided for uncommon or non-standard codes.
