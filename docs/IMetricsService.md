# IMetricsService

`IMetricsService` defines the contract for collecting, aggregating, and exposing operational telemetry within the outbox pattern infrastructure. It provides a unified interface for retrieving system health status, performance counters, error analytics, throughput and latency measurements, Prometheus-formatted metrics, active alerts, and resource utilization data. Implementations are expected to gather metrics from the outbox processor, message dispatcher, and underlying data store.

## API

### MetricsService

```csharp
public MetricsService
```

Default constructor for the concrete `MetricsService` implementation. Initializes internal metric collectors, counters, and histogram reservoirs. Does not start background sampling; metric population occurs lazily upon the first call to any query method.

### GetSystemHealthAsync

```csharp
public async Task<dynamic> GetSystemHealthAsync()
```

**Purpose:** Returns a snapshot of overall system health, including outbox processor status, database connectivity, and message broker reachability.

**Parameters:** None.

**Return Value:** A dynamic object containing boolean flags (e.g., `isHealthy`, `isDatabaseConnected`, `isBrokerConnected`) and a string `status` field with values such as `"Healthy"`, `"Degraded"`, or `"Unhealthy"`.

**Exceptions:** Throws `InvalidOperationException` when the metrics infrastructure itself has not been initialized. Throws `TimeoutException` if health probes exceed the default 5-second timeout.

### GetPerformanceMetricsAsync

```csharp
public async Task<dynamic> GetPerformanceMetricsAsync()
```

**Purpose:** Retrieves CPU usage, memory consumption, thread pool utilization, and garbage collection statistics for the outbox host process.

**Parameters:** None.

**Return Value:** A dynamic object with numeric fields (`cpuPercent`, `memoryMB`, `threadPoolActive`, `gcGen0`, `gcGen1`, `gcGen2`) and a `collectedAt` UTC timestamp.

**Exceptions:** Throws `InvalidOperationException` when the metrics service is not initialized. May throw `Win32Exception` on Windows when performance counters are unavailable.

### GetErrorAnalyticsAsync

```csharp
public async Task<dynamic> GetErrorAnalyticsAsync()
```

**Purpose:** Aggregates error counts, categorized by exception type, outbox message type, and time buckets (last hour, last 24 hours).

**Parameters:** None.

**Return Value:** A dynamic object containing an `errorCounts` dictionary keyed by exception type name, a `byMessageType` breakdown, and `hourly`/`daily` arrays of timestamped counts.

**Exceptions:** Throws `InvalidOperationException` when the service is not initialized.

### GetThroughputMetricsAsync

```csharp
public async Task<dynamic> GetThroughputMetricsAsync()
```

**Purpose:** Reports message processing throughput in messages per second over sliding windows (1-minute, 5-minute, 15-minute averages).

**Parameters:** None.

**Return Value:** A dynamic object with fields `messagesPerSec1m`, `messagesPerSec5m`, `messagesPerSec15m`, and `totalProcessed`.

**Exceptions:** Throws `InvalidOperationException` when the service is not initialized.

### GetLatencyMetricsAsync

```csharp
public async Task<dynamic> GetLatencyMetricsAsync()
```

**Purpose:** Exposes end-to-end latency percentiles (p50, p90, p95, p99) and average latency for outbox message dispatch, measured from database commit to broker acknowledgment.

**Parameters:** None.

**Return Value:** A dynamic object with `averageMs`, `p50Ms`, `p90Ms`, `p95Ms`, `p99Ms`, and `sampleCount`.

**Exceptions:** Throws `InvalidOperationException` when the service is not initialized.

### GetPrometheusMetricsAsync

```csharp
public async Task<string> GetPrometheusMetricsAsync()
```

**Purpose:** Produces a Prometheus exposition-format string containing all registered counters, gauges, and histograms for scraping by a Prometheus server.

**Parameters:** None.

**Return Value:** A plain-text string conforming to the Prometheus text-based exposition format, including `# HELP` and `# TYPE` lines.

**Exceptions:** Throws `InvalidOperationException` when the service is not initialized.

### GetActiveAlertsAsync

```csharp
public async Task<List<dynamic>> GetActiveAlertsAsync()
```

**Purpose:** Returns all currently firing alerts based on configured thresholds (e.g., error rate exceeding 5%, latency p99 above 2 seconds, database disconnected).

**Parameters:** None.

**Return Value:** A `List<dynamic>` where each element contains `alertName`, `severity` (`"Critical"`, `"Warning"`, `"Info"`), `triggeredAt`, and `description`.

**Exceptions:** Throws `InvalidOperationException` when the service is not initialized.

### GetResourceMetricsAsync

```csharp
public async Task<dynamic> GetResourceMetricsAsync()
```

**Purpose:** Reports resource utilization specific to the outbox storage and broker: database connection pool usage, broker channel count, and queue depth.

**Parameters:** None.

**Return Value:** A dynamic object with `dbConnectionPoolActive`, `dbConnectionPoolIdle`, `dbConnectionPoolMax`, `brokerChannelCount`, and `outboxQueueDepth`.

**Exceptions:** Throws `InvalidOperationException` when the service is not initialized. Throws `TimeoutException` if resource queries exceed the default timeout.

## Usage

### Example 1: Health Check Endpoint in an ASP.NET Controller

```csharp
[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metrics;

    public MetricsController(IMetricsService metrics)
    {
        _metrics = metrics;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        dynamic health = await _metrics.GetSystemHealthAsync();
        if (health.status == "Healthy")
            return Ok(health);
        return StatusCode(503, health);
    }
}
```

### Example 2: Prometheus Scrape Endpoint with Alert Inspection

```csharp
[HttpGet("prometheus")]
public async Task<IActionResult> GetPrometheus()
{
    string exposition = await _metrics.GetPrometheusMetricsAsync();

    // Optionally inject active alerts as comments
    List<dynamic> alerts = await _metrics.GetActiveAlertsAsync();
    if (alerts.Count > 0)
    {
        var sb = new StringBuilder(exposition);
        foreach (dynamic alert in alerts)
        {
            sb.AppendLine($"# ALERT {alert.alertName} severity={alert.severity} {alert.description}");
        }
        exposition = sb.ToString();
    }

    return Content(exposition, "text/plain; version=0.0.4");
}
```

## Notes

- **Initialization requirement:** All async query methods throw `InvalidOperationException` if the underlying metric collectors have not been initialized. Callers should ensure the `MetricsService` instance is properly configured before invoking any query method.
- **Dynamic return types:** Return values are `dynamic` to accommodate evolving metric schemas without breaking interface compatibility. Consumers should use property bag access patterns and handle missing fields gracefully.
- **Thread safety:** The concrete `MetricsService` implementation is designed for concurrent access. Internal counters and histograms use lock-free or fine-grained locking structures. All public methods are safe to call from multiple threads simultaneously.
- **Timeout behavior:** `GetSystemHealthAsync` and `GetResourceMetricsAsync` may throw `TimeoutException` when underlying probes (database ping, broker connectivity check) exceed their configured timeouts. Callers should implement retry or circuit-breaking logic for these calls.
- **Prometheus format:** `GetPrometheusMetricsAsync` returns the full metric registry snapshot. Frequent scraping (sub-15-second intervals) may cause measurable CPU overhead in high-throughput outbox deployments. Cache the result for short durations if multiple scrapers are in use.
- **Alert lifecycle:** Alerts returned by `GetActiveAlertsAsync` reflect the state at call time. Alerts auto-resolve when metric values return within thresholds; there is no acknowledgment API. Consumers polling for alerts should track `triggeredAt` to detect new occurrences.
