# Error Handling Middleware

## Purpose

Centralized error handling middleware that catches and formats all unhandled exceptions in the ASP.NET Core request pipeline. Ensures consistent error responses and proper logging of errors across the application.

## Request Pipeline Behavior

The middleware wraps the request processing pipeline and catches any exceptions that occur during request handling:

1. **Normal Flow**: Calls the next middleware in the pipeline via `_next(context)`
2. **Exception Handling**: If any exception occurs, it:
   - Logs the error with request path information using `ILogger<ErrorHandlingMiddleware>`
   - Maps the exception to an appropriate HTTP response via `HandleExceptionAsync`
   - Returns a JSON-formatted error response

## Error Response Shape

All error responses follow a consistent JSON format with these properties:

| Property | Type | Description |
|----------|------|-------------|
| `message` | string | Human-readable error description |
| `code` | string | Machine-readable error code |
| `timestamp` | string (ISO 8601) | UTC timestamp when error occurred |
| `traceId` | string | Unique identifier for tracing the request |

### Exception to HTTP Status Code Mapping

The middleware maps specific exception types to appropriate HTTP status codes:

| Exception Type | HTTP Status | Error Code | Message Source |
|----------------|-------------|------------|----------------|
| `ArgumentException` | 400 Bad Request | `VALIDATION_ERROR` | Exception message |
| `OutboxException` | 400 Bad Request | `OUTBOX_ERROR` | Exception message |
| `OperationCanceledException` | 408 Request Timeout | `REQUEST_TIMEOUT` | "Request was cancelled" |
| `InvalidOperationException` | 409 Conflict | `INVALID_OPERATION` | Exception message |
| All other exceptions | 500 Internal Server Error | `INTERNAL_SERVER_ERROR` | "An unexpected error occurred" |

### Response Format Example

```json
{
  "message": "Validation error description",
  "code": "VALIDATION_ERROR",
  "timestamp": "2026-09-11T10:30:00Z",
  "traceId": "0HM23456789ABCDEF00000000000000001"
}
```

## Usage

Register the middleware in the ASP.NET Core pipeline:

```csharp
var builder = WebApplication.CreateBuilder(args);
// ... other service registrations
var app = builder.Build();

// Register error handling middleware (should be early in pipeline)
app.UseErrorHandling();

// ... other middleware and endpoint registrations
app.Run();
```

The extension method `UseErrorHandling()` is provided via `ErrorHandlingMiddlewareExtensions` for convenient registration.

## Key Features

- **Centralized Exception Handling**: Catches all unhandled exceptions in one place
- **Consistent Response Format**: All errors return the same JSON structure
- **Proper Logging**: Logs exceptions with contextual information (request path)
- **HTTP Status Code Mapping**: Returns appropriate status codes based on exception type
- **Traceability**: Includes trace ID for request correlation
- **Content Type**: Always sets response content type to `application/json`

## Thread Safety

The middleware is thread-safe as it:
- Only reads from immutable fields after construction
- Uses local variables for request-specific data
- Doesn't modify shared state during request processing

## Dependencies

- `Microsoft.AspNetCore.Http` - For `HttpContext` and `RequestDelegate`
- `Microsoft.Extensions.Logging` - For `ILogger<T>`
- `System.Net` - For `HttpStatusCode` enumeration
- `System.Text.Json` - For JSON serialization
- `DotnetOutboxPattern.Dtos` - For `ErrorResponse` DTO
- `DotnetOutboxPattern.Exceptions` - For `OutboxException` type