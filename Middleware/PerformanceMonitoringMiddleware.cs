// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;

namespace DotnetOutboxPattern.Middleware;

/// <summary>
/// Captures request/response performance metrics for monitoring and alerting
/// Tracks latency, throughput, and identifies performance bottlenecks
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly PerformanceMonitor _monitor;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger,
        PerformanceMonitor monitor)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var metric = new RequestMetric
            {
                Path = context.Request.Path.ToString(),
                Method = context.Request.Method,
                StatusCode = context.Response.StatusCode,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Timestamp = DateTime.UtcNow
            };

            _monitor.RecordMetric(metric);

            // Log slow requests
            if (stopwatch.ElapsedMilliseconds > 5000)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} took {DurationMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}

/// <summary>
/// Singleton service that tracks performance metrics across requests
/// </summary>
public class PerformanceMonitor
{
    private readonly List<RequestMetric> _metrics = new();
    private readonly object _lock = new();

    public void RecordMetric(RequestMetric metric)
    {
        lock (_lock)
        {
            _metrics.Add(metric);

            // Keep only recent metrics to prevent memory issues
            if (_metrics.Count > 10000)
            {
                var cutoff = DateTime.UtcNow.AddHours(-1);
                _metrics.RemoveAll(m => m.Timestamp < cutoff);
            }
        }
    }

    public List<RequestMetric> GetRecentMetrics(int minutes = 60)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
            return _metrics.Where(m => m.Timestamp > cutoff).ToList();
        }
    }

    public PerformanceStats GetStats(int minutes = 60)
    {
        var recent = GetRecentMetrics(minutes);

        if (recent.Count == 0)
            return new PerformanceStats();

        var durations = recent.Select(m => m.DurationMs).OrderBy(d => d).ToList();

        return new PerformanceStats
        {
            RequestCount = recent.Count,
            AverageDurationMs = (long)durations.Average(),
            MinDurationMs = durations.First(),
            MaxDurationMs = durations.Last(),
            P50DurationMs = GetPercentile(durations, 50),
            P95DurationMs = GetPercentile(durations, 95),
            P99DurationMs = GetPercentile(durations, 99),
            ErrorCount = recent.Count(m => m.StatusCode >= 400),
            ErrorRate = recent.Count(m => m.StatusCode >= 400) / (double)recent.Count
        };
    }

    private static long GetPercentile(List<long> values, int percentile)
    {
        var index = (int)Math.Ceiling(values.Count * (percentile / 100.0)) - 1;
        return values[Math.Max(0, index)];
    }
}

/// <summary>
/// Represents a single request metric
/// </summary>
public class RequestMetric
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Aggregated performance statistics
/// </summary>
public class PerformanceStats
{
    public int RequestCount { get; set; }
    public long AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public long P50DurationMs { get; set; }
    public long P95DurationMs { get; set; }
    public long P99DurationMs { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRate { get; set; }
}

/// <summary>
/// Extension method to register performance monitoring middleware
/// </summary>
public static class PerformanceMonitoringMiddlewareExtensions
{
    public static IApplicationBuilder UsePerformanceMonitoring(
        this IApplicationBuilder app,
        PerformanceMonitor? monitor = null)
    {
        var mon = monitor ?? new PerformanceMonitor();
        app.ApplicationServices.GetRequiredService<IServiceCollection>();
        return app.UseMiddleware<PerformanceMonitoringMiddleware>(mon);
    }
}
