# PerformanceMonitoringMiddleware

`PerformanceMonitoringMiddleware` is an ASP.NET Core middleware component that captures and records performance metrics for incoming HTTP requests. It tracks request count, duration percentiles (P50, P95, P99), error rates, and per-endpoint statistics, exposing both raw metric records and aggregated performance summaries for monitoring and diagnostics.

## API

### `PerformanceMonitoringMiddleware` (constructor)

Initializes a new instance of the middleware.

- **Parameters:** Accepts the standard `RequestDelegate next` parameter required by ASP.NET Core middleware pipeline conventions.
- **Exceptions:** None thrown directly by the constructor.

### `async Task InvokeAsync`

Processes an incoming HTTP request, records its duration and outcome, and passes control to the next middleware in the pipeline.

- **Parameters:** `HttpContext context` — the current HTTP context for the request.
- **Returns:** A `Task` representing the asynchronous operation.
- **Exceptions:** Does not throw; exceptions from downstream middleware are caught internally for error-rate tracking and then rethrown to preserve normal ASP.NET Core exception-handling behavior.

### `void RecordMetric`

Records a single request metric into the middleware's in-memory store.

- **Parameters:**
  - `string Path` — the request path (e.g., `"/api/orders"`).
  - `string Method` — the HTTP method (e.g., `"GET"`, `"POST"`).
  - `int StatusCode` — the HTTP status code returned.
  - `long DurationMs` — the request duration in milliseconds.
  - `DateTime Timestamp` — the time at which the request was recorded.
- **Returns:** Nothing.
- **Exceptions:** None.

### `List<RequestMetric> GetRecentMetrics`

Retrieves a list of recently recorded individual request metrics.

- **Parameters:** None.
- **Returns:** A `List<RequestMetric>` containing the most recent metric entries. Each entry exposes `Path`, `Method`, `StatusCode`, `DurationMs`, and `Timestamp`.
- **Exceptions:** None.

### `PerformanceStats GetStats`

Computes and returns aggregated performance statistics across all recorded metrics.

- **Parameters:** None.
- **Returns:** A `PerformanceStats` object containing:
  - `int RequestCount` — total number of requests recorded.
  - `long AverageDurationMs` — mean duration in milliseconds.
  - `long MinDurationMs` — minimum observed duration.
  - `long MaxDurationMs` — maximum observed duration.
  - `long P50DurationMs` — 50th percentile (median) duration.
  - `long P95DurationMs` — 95th percentile duration.
  - `long P99DurationMs` — 99th percentile duration.
  - `int ErrorCount` — number of requests that resulted in errors (typically status codes ≥ 400 or exceptions).
  - `double ErrorRate` — proportion of requests that were errors, as a value between 0.0 and 1.0.
- **Exceptions:** None.

### `static IApplicationBuilder UsePerformanceMonitoring`

Extension method on `IApplicationBuilder` that registers the middleware into the ASP.NET Core request pipeline.

- **Parameters:** `this IApplicationBuilder builder` — the application builder instance.
- **Returns:** The `IApplicationBuilder` instance, enabling fluent chaining.
- **Exceptions:** None.

## Usage

### Example 1: Basic Registration and Metric Retrieval

Register the middleware in `Program.cs` or `Startup.cs`, then inject a diagnostic endpoint that exposes recent metrics.

```csharp
// Program.cs — register the middleware early in the pipeline
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UsePerformanceMonitoring();

app.MapGet("/metrics/recent", (PerformanceMonitoringMiddleware middleware) =>
{
    var metrics = middleware.GetRecentMetrics();
    return Results.Ok(metrics.Select(m => new
    {
        m.Path,
        m.Method,
        m.StatusCode,
        m.DurationMs,
        m.Timestamp
    }));
});

app.Run();
```

### Example 2: Aggregated Stats Endpoint

Expose an endpoint that returns percentile-based performance statistics for monitoring dashboards.

```csharp
app.MapGet("/metrics/stats", (PerformanceMonitoringMiddleware middleware) =>
{
    var stats = middleware.GetStats();
    return Results.Ok(new
    {
        stats.RequestCount,
        stats.AverageDurationMs,
        stats.P50DurationMs,
        stats.P95DurationMs,
        stats.P99DurationMs,
        stats.ErrorCount,
        ErrorRate = stats.ErrorRate.ToString("P2")
    });
});
```

## Notes

- **Thread safety:** `RecordMetric`, `GetRecentMetrics`, and `GetStats` operate on shared in-memory state. The implementation is expected to use appropriate synchronization (e.g., locks or concurrent collections) to ensure correctness under concurrent requests. Callers do not need to provide external synchronization.
- **Memory management:** `GetRecentMetrics` returns a bounded list; the middleware should cap the number of retained metrics to prevent unbounded memory growth under sustained traffic. Older entries are evicted as new ones arrive.
- **Percentile calculation:** `GetStats` computes P50, P95, and P99 from the in-memory sample set. When the sample size is very small (e.g., fewer than 100 requests), high-percentile values may be identical to `MaxDurationMs` or lack statistical significance.
- **Error classification:** `ErrorCount` and `ErrorRate` typically treat HTTP 4xx and 5xx status codes as errors, as well as unhandled exceptions that propagate through the pipeline. The exact threshold is determined by the middleware's internal logic.
- **Pipeline ordering:** Register `UsePerformanceMonitoring` early in the pipeline to capture the full request lifecycle. Middleware registered before it will not have their processing time included in the duration measurement.
- **Metric timestamp:** The `Timestamp` on each `RequestMetric` reflects the time the metric was recorded (typically after the response completes), not the time the request arrived.
