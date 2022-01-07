# ErrorResponseExtensions
The `ErrorResponseExtensions` class provides a set of extension methods for working with `ErrorResponse` objects, allowing for easy addition of context, timestamps, and trace IDs, as well as conversion to JSON and log formats. These methods enable developers to enhance error responses with additional information and transform them into various formats for logging, debugging, or client-side error handling.

## API
* `public static ErrorResponse WithContext(this ErrorResponse errorResponse, object context)`: Adds context to an `ErrorResponse` object. The `context` parameter is an object that provides additional information about the error. Returns the modified `ErrorResponse` object.
* `public static ErrorResponse WithTimestamp(this ErrorResponse errorResponse)`: Adds a timestamp to an `ErrorResponse` object. Returns the modified `ErrorResponse` object.
* `public static ErrorResponse WithTraceId(this ErrorResponse errorResponse, string traceId)`: Adds a trace ID to an `ErrorResponse` object. The `traceId` parameter is a string that identifies the error. Returns the modified `ErrorResponse` object.
* `public static string ToJson(this ErrorResponse errorResponse)`: Converts an `ErrorResponse` object to a JSON string. Returns the JSON representation of the error response.
* `public static bool IsClientError(this ErrorResponse errorResponse)`: Determines whether an `ErrorResponse` object represents a client-side error. Returns `true` if the error is a client error, `false` otherwise.
* `public static bool IsServerError(this ErrorResponse errorResponse)`: Determines whether an `ErrorResponse` object represents a server-side error. Returns `true` if the error is a server error, `false` otherwise.
* `public static ErrorResponse ToLogFormat(this ErrorResponse errorResponse)`: Converts an `ErrorResponse` object to a log format. Returns the log-formatted error response.

## Usage
The following examples demonstrate how to use the `ErrorResponseExtensions` class:
```csharp
// Example 1: Adding context and converting to JSON
var errorResponse = new ErrorResponse { ErrorMessage = "Invalid request" };
var errorResponseWithContext = errorResponse.WithContext(new { UserId = 123, RequestId = "abc" });
var jsonError = errorResponseWithContext.ToJson();
Console.WriteLine(jsonError);

// Example 2: Determining error type and logging
var errorResponse2 = new ErrorResponse { ErrorMessage = "Database connection failed" };
if (errorResponse2.IsServerError())
{
    var logError = errorResponse2.ToLogFormat();
    Console.WriteLine(logError);
}
```

## Notes
When using the `WithErrorResponseExtensions` class, note the following:
* The `WithContext` method will overwrite any existing context in the `ErrorResponse` object.
* The `WithTimestamp` method will add the current timestamp to the `ErrorResponse` object.
* The `WithTraceId` method will overwrite any existing trace ID in the `ErrorResponse` object.
* The `ToJson` method will serialize the entire `ErrorResponse` object, including any added context or trace ID.
* The `IsClientError` and `IsServerError` methods rely on the error code or message to determine the error type.
* The `ToLogFormat` method will transform the `ErrorResponse` object into a log-friendly format, which may include additional information such as timestamps or trace IDs.
* The `ErrorResponseExtensions` class is thread-safe, as it only provides static methods that operate on immutable objects. However, the underlying `ErrorResponse` objects may not be thread-safe, depending on their implementation.
