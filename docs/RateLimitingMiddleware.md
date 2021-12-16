# RateLimitingMiddleware

Middleware component that enforces a sliding-window rate limit on incoming HTTP requests. Each instance tracks the number of requests within a configurable time window and rejects requests that exceed the configured limit.

## API

### `public RateLimitingMiddleware(int requestsPerWindow, int windowSeconds)`

Constructs a new rate-limiting middleware instance.

- **requestsPerWindow**: Maximum allowed requests within the sliding window.
- **windowSeconds**: Duration of the sliding window in seconds.

Throws `ArgumentOutOfRangeException` if `requestsPerWindow` is zero or negative, or if `windowSeconds` is zero or negative.

---

### `public async Task InvokeAsync(HttpContext context, RequestDelegate next)`

Invokes the middleware logic to evaluate the request against the rate limit.

- **context**: The `HttpContext` for the current request.
- **next**: The delegate representing the next middleware in the pipeline.

Returns a `Task` representing the asynchronous operation. If the rate limit is exceeded, the middleware short-circuits the pipeline and returns a `429 Too Many Requests` response.

---

### `public int RequestsPerWindow`

Gets the maximum number of requests allowed within the sliding window.

- **Returns**: The configured request limit.

---

### `public int WindowSeconds`

Gets the duration of the sliding window in seconds.

- **Returns**: The configured window duration in seconds.

---
### `public DateTime WindowStart`

Gets the start time of the current sliding window.

- **Returns**: The `DateTime` marking the beginning of the current window.

---
### `public int RequestCount`

Gets the number of requests counted in the current sliding window.

- **Returns**: The current request count within the window.

---
### `public DateTime LastRequest`

Gets the timestamp of the most recent request processed by this middleware.

- **Returns**: The `DateTime` of the last request, or `DateTime.MinValue` if no requests have been processed.

---
### `public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, int requestsPerWindow, int windowSeconds)`

Registers the `RateLimitingMiddleware` in the ASP.NET Core pipeline.

- **builder**: The `IApplicationBuilder` instance.
- **requestsPerWindow**: Maximum allowed requests within the sliding window.
- **windowSeconds**: Duration of the sliding window in seconds.

Returns the `IApplicationBuilder` for method chaining.

Throws `ArgumentOutOfRangeException` if `requestsPerWindow` is zero or negative, or if `windowSeconds` is zero or negative.

## Usage

### Basic Setup in `Program.cs`
