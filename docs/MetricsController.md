# MetricsController

The `MetricsController` provides a centralized HTTP API for monitoring and observability in the `dotnet-outbox-pattern` project. It exposes endpoints that report application health, performance characteristics, error analytics, throughput, latency distributions, Prometheus-formatted metrics, active alerts, and resource utilization. Each endpoint returns data in a format suitable for dashboard integration, automated alerting pipelines, or manual inspection during incident response.

## API

### `MetricsController`

Constructor. Initializes a new instance of the controller with the required monitoring dependencies. No parameters are exposed publicly; dependencies are injected by the DI container at startup.

### `public async Task<IActionResult> GetHealthAsync()`

Returns the current health status of the application and its critical subsystems. The response typically includes a composite status (Healthy, Degraded, or Unhealthy) along with per-component checks such as database connectivity, message-broker reachability, and outbox-processor liveness.

- **Parameters:** none.
- **Returns:** `IActionResult` containing a health-report payload, conventionally with HTTP 200 when healthy and HTTP 503 when unhealthy.
- **Throws:** may throw if a required health-check provider is unavailable or misconfigured; such exceptions are surfaced as HTTP 500.

### `public async Task<IActionResult> GetPerformanceAsync()`

Retrieves aggregated performance counters for the outbox processing pipeline. This includes metrics such as average processing time per message, batch sizes, and cache-hit ratios.

- **Parameters:** none.
- **Returns:** `IActionResult` with a JSON performance snapshot and HTTP 200.
- **Throws:** may throw if the underlying performance-counter store cannot be queried (e.g., transient storage failure).

### `public async Task<IActionResult> GetErrorAnalyticsAsync()`

Returns a breakdown of errors observed in the outbox pipeline over a recent time window. Data includes error categories, top error messages, and per-endpoint failure counts.

- **Parameters:** none.
- **Returns:** `IActionResult` with a JSON error-analytics document and HTTP 200.
- **Throws:** may throw when the analytics data source is unreachable or the query times out.

### `public async Task<IActionResult> GetThroughputAsync()`

Reports current and historical throughput of the outbox dispatcher, measured in messages processed per second over configurable intervals.

- **Parameters:** none.
- **Returns:** `IActionResult` with a JSON throughput report and HTTP 200.
- **Throws:** may throw if the throughput-tracking subsystem encounters an internal error.

### `public async Task<IActionResult> GetLatencyAsync()`

Provides latency percentile distributions (p50, p90, p99) for outbox message delivery and processing end-to-end, as well as per-stage breakdowns.

- **Parameters:** none.
- **Returns:** `IActionResult` with a JSON latency-distribution payload and HTTP 200.
- **Throws:** may throw if latency data cannot be computed due to missing samples or storage-layer failure.

### `public async Task<IActionResult> GetPrometheusMetricsAsync()`

Exposes all tracked metrics in the Prometheus exposition format. This endpoint is designed to be scraped by a Prometheus server or compatible agent.

- **Parameters:** none.
- **Returns:** `IActionResult` with `text/plain` content type containing Prometheus-formatted metric lines and HTTP 200.
- **Throws:** may throw if metric serialization fails; typically returns HTTP 500 in that case.

### `public async Task<IActionResult> GetAlertsAsync()`

Lists currently active monitoring alerts, including their severity, trigger conditions, and timestamps. Alerts are derived from thresholds evaluated against the same metrics exposed by the other endpoints.

- **Parameters:** none.
- **Returns:** `IActionResult` with a JSON array of active alerts and HTTP 200.
- **Throws:** may throw if the alert-evaluation engine is not reachable.

### `public async Task<IActionResult> GetResourceMetricsAsync()`

Returns host-level resource consumption metrics for the running process, such as CPU usage, memory working set, GC collection counts, and thread-pool utilization.

- **Parameters:** none.
- **Returns:** `IActionResult` with a JSON resource-metrics payload and HTTP 200.
- **Throws:** may throw if system diagnostics APIs are restricted or fail to collect data.

## Usage

**Example 1: Polling health and Prometheus metrics from a monitoring agent**

```csharp
using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };

// Check overall health before scraping metrics
var healthResponse = await httpClient.GetAsync("/metrics/health");
if (healthResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
{
    // Trigger alert, skip metric collection
    Console.WriteLine("Application unhealthy; skipping scrape.");
    return;
}

// Scrape Prometheus metrics
var metricsResponse = await httpClient.GetAsync("/metrics/prometheus");
var metricsText = await metricsResponse.Content.ReadAsStringAsync();
// Forward metricsText to Prometheus remote-write or local collector
Console.WriteLine($"Scraped {metricsText.Split('\n').Length} metric lines.");
```

**Example 2: Building a lightweight dashboard by combining multiple endpoints**

```csharp
var client = new HttpClient { BaseAddress = new Uri("https://api.example.com") };

var latencyTask = client.GetFromJsonAsync<LatencyReport>("/metrics/latency");
var throughputTask = client.GetFromJsonAsync<ThroughputReport>("/metrics/throughput");
var alertsTask = client.GetFromJsonAsync<List<Alert>>("/metrics/alerts");

await Task.WhenAll(latencyTask, throughputTask, alertsTask);

var dashboard = new
{
    Latency = latencyTask.Result,
    Throughput = throughputTask.Result,
    ActiveAlerts = alertsTask.Result
};

Console.WriteLine($"p99 latency: {dashboard.Latency.P99Ms} ms");
Console.WriteLine($"Throughput: {dashboard.Throughput.MessagesPerSecond} msg/s");
Console.WriteLine($"Active alerts: {dashboard.ActiveAlerts.Count}");
```

## Notes

- All public methods are asynchronous and return `Task<IActionResult>`. Callers should `await` them to avoid blocking the request thread.
- The controller does not expose mutable shared state; each method reads from thread-safe metric stores or snapshot providers. Concurrent requests are safe without external synchronization.
- When underlying data sources are temporarily unavailable, methods may throw rather than return partial data. Clients should implement retries with backoff for transient failures.
- The `GetPrometheusMetricsAsync` endpoint returns `text/plain` rather than `application/json`. Clients that expect JSON must handle the content-type difference explicitly.
- Resource metrics reported by `GetResourceMetricsAsync` reflect the process hosting the controller, not the entire host machine. Containerized deployments should account for cgroup limits when interpreting these values.
- Alert state returned by `GetAlertsAsync` is point-in-time; a subsequent call may return different alerts if thresholds have cleared or new conditions have been triggered in the interim.
