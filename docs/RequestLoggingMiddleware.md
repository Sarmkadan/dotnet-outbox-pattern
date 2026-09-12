# RequestLoggingMiddleware

## Purpose

`RequestLoggingMiddleware` logs the start and outcome of each HTTP request. It records request metadata, includes the request body for `POST` and `PUT` requests, measures elapsed processing time, and preserves the response by copying a captured response stream back to the original response stream.

## Logged Fields

All log messages use structured logging through `ILogger<RequestLoggingMiddleware>`.

### Request started

The information-level `Request started` log contains:

| Field | Value |
| --- | --- |
| `Method` | `HttpContext.Request.Method` |
| `Path` | `HttpContext.Request.Path` |
| `TraceId` | `HttpContext.TraceIdentifier` |

For requests whose method is exactly `POST` or `PUT`, the log also contains `Body`. The middleware reads the body as text and logs at most the first 500 characters. Bodies longer than 500 characters are truncated to 500 characters and suffixed with `...`.

Other HTTP methods do not have their request bodies read or logged.

### Request completed

After the next pipeline component completes successfully, the middleware produces an information-level `Request completed` log with:

| Field | Value |
| --- | --- |
| `Method` | `HttpContext.Request.Method` |
| `Path` | `HttpContext.Request.Path` |
| `StatusCode` | `HttpContext.Response.StatusCode` |
| `DurationMs` | Elapsed whole milliseconds measured by a `Stopwatch` |
| `TraceId` | `HttpContext.TraceIdentifier` |

The response body is captured and decoded as UTF-8, but its content is not included in the completion log.

### Request failed

If an exception reaches the middleware's `catch` block, it writes an error-level `Request failed` log with the exception and:

| Field | Value |
| --- | --- |
| `Method` | `HttpContext.Request.Method` |
| `Path` | `HttpContext.Request.Path` |
| `DurationMs` | Elapsed whole milliseconds measured by the stopped `Stopwatch` |
| `TraceId` | `HttpContext.TraceIdentifier` |

The exception is rethrown after it is logged.

## Pipeline Behavior

For each request, the middleware:

1. Starts a stopwatch and saves the original response body stream.
2. For `POST` and `PUT`, enables request buffering, reads the complete request body, resets `Request.Body.Position` to zero, and logs the request-start message with the body truncated as described above. For all other methods, it logs the request-start message without reading the body.
3. Replaces `Response.Body` with a new in-memory stream.
4. Awaits the next middleware through `_next(context)`.
5. On successful completion, stops the stopwatch, reads the captured response bytes as UTF-8, logs the response metadata, and copies the captured response to the original response stream.
6. Restores `Response.Body` to the original stream in a `finally` block, whether processing succeeds or throws.

If an exception occurs, the captured response is not copied to the original response stream before the exception is rethrown. Exception-to-response conversion, if required, must be supplied by another pipeline component.

The measured duration begins before request-body reading and ends immediately after the next middleware completes. It does not include the completion log or the copy from the in-memory response stream to the original stream.

## Registration

`RequestLoggingMiddlewareExtensions.UseRequestLogging` registers the middleware with ASP.NET Core:

```csharp
app.UseRequestLogging();
```

The extension returns the `IApplicationBuilder` returned by `UseMiddleware<RequestLoggingMiddleware>()`, allowing normal pipeline chaining. Its position determines which later middleware and endpoints are included in the measured duration and captured response.

## Constructor Validation

The constructor requires a `RequestDelegate` and an `ILogger<RequestLoggingMiddleware>`. It throws `ArgumentNullException` when either dependency is `null`.
