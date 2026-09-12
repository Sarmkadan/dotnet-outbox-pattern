# Rate-Limiting Middleware

## Purpose

`Middleware/RateLimitingMiddleware.cs` limits incoming HTTP requests separately for each client. It identifies a client from the `X-Api-Key` request header when that header is present and otherwise uses `HttpContext.Connection.RemoteIpAddress`. If neither provides an address, it uses `ip:unknown`.

Allowed responses receive `X-RateLimit-Limit` and `X-RateLimit-Remaining` headers. When a client has exhausted its request count, the middleware stops the pipeline and returns:

- HTTP status `429 Too Many Requests`
- `Retry-After`, set to the configured window length in seconds
- `X-RateLimit-Limit`, set to the configured request limit
- `X-RateLimit-Remaining: 0`
- a plain response body of `Rate limit exceeded`

The middleware stores its counters in memory, so limits are local to the middleware instance and are not shared between application processes or servers.

## Algorithm

Although the class summary calls the implementation a "sliding window token bucket," the code implements a per-client fixed-window counter:

1. Build a client key as `api-key:<header value>` when `X-Api-Key` exists, or as `ip:<remote IP>` otherwise.
2. Get or create that client's state in a `ConcurrentDictionary`. New state starts its window at the current UTC time.
3. Lock the client's state so concurrent requests for the same client update it atomically.
4. If `WindowStart` is earlier than the current UTC time minus `WindowSeconds`, start a new window and reset the request count to zero.
5. Record the current UTC time as `LastRequest`.
6. Reject the request if the count is already greater than or equal to `RequestsPerWindow`. Otherwise increment the count and continue to the next middleware.

The remaining count on an allowed request is `RequestsPerWindow - RequestCount` after the increment, with a minimum of zero.

A background loop starts when the middleware is constructed. Every minute it removes clients whose last request is older than twice the configured window length. Exceptions within the loop are logged and the loop continues; an unexpected terminal fault is also logged.

## Configuration

`RateLimitingOptions` exposes two mutable properties:

| Property | Default | Used for |
| --- | ---: | --- |
| `RequestsPerWindow` | `1000` | Maximum allowed requests for one client in its current window. |
| `WindowSeconds` | `60` | Window duration, `Retry-After` value, and cleanup-expiration calculation. |

Passing `null` to `UseRateLimiting` or constructing the middleware without an options instance selects these defaults. The code does not validate either property.

`UseOutboxPatternMiddleware(rateLimitingOptions)` applies the rate limiter after error handling and request logging, and before performance monitoring. It forwards the supplied options directly to `UseRateLimiting`. Calling `UseOutboxPatternMiddleware()` therefore uses the defaults.

`ConfigureRateLimiting(requestsPerWindow, windowSeconds)` registers `IOptions<RateLimitingOptions>` in the service collection. The middleware constructor does not consume `IOptions<RateLimitingOptions>`; it consumes the `RateLimitingOptions` object passed during middleware registration. Consequently, the values registered by `ConfigureRateLimiting` are not read by this middleware implementation unless they are separately retrieved and passed to `UseRateLimiting` or `UseOutboxPatternMiddleware`.

## Usage Example

Pass the desired options when adding the middleware to the request pipeline:

```csharp
using DotnetOutboxPattern.Middleware;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseRateLimiting(new RateLimitingOptions
{
    RequestsPerWindow = 200,
    WindowSeconds = 60
});

app.MapGet("/", () => "Hello");
app.Run();
```

The combined middleware extension accepts the same options:

```csharp
using DotnetOutboxPattern.Configuration;
using DotnetOutboxPattern.Middleware;

app.UseOutboxPatternMiddleware(new RateLimitingOptions
{
    RequestsPerWindow = 200,
    WindowSeconds = 60
});
```

Middleware order matters: only endpoints and middleware registered after the rate limiter are subject to it.
