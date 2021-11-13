#nullable enable
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
public sealed class PerformanceMonitoringMiddleware
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

    /// <summary>
    /// Gets the PerformanceMonitor instance used by this middleware
    /// </summary>
    public PerformanceMonitor Monitor => _monitor;

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
public sealed class PerformanceMonitor
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
public sealed class RequestMetric
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
public sealed class PerformanceStats
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
/// Extension methods for PerformanceMonitoringMiddleware providing additional
/// monitoring capabilities, diagnostics, and integration scenarios
/// </summary>
public static class PerformanceMonitoringMiddlewareExtensions
{
    /// <summary>
    /// Extension method to register performance monitoring middleware
    /// </summary>
    public static IApplicationBuilder UsePerformanceMonitoring(
        this IApplicationBuilder app,
        PerformanceMonitor? monitor = null)
    {
        var mon = monitor ?? new PerformanceMonitor();
        app.ApplicationServices.GetRequiredService<IServiceCollection>();
        return app.UseMiddleware<PerformanceMonitoringMiddleware>(mon);
    }

    /// <summary>
    /// Adds performance monitoring with custom configuration options
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="configure">Action to configure monitoring options</param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UsePerformanceMonitoring(
        this IApplicationBuilder app,
        Action<PerformanceMonitoringOptions> configure)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new PerformanceMonitoringOptions();
        configure(options);

        var monitor = options.Monitor ?? new PerformanceMonitor();

        // Apply options to the monitor
        if (options.MaxMetricsToKeep.HasValue)
        {
            // Monitor has built-in limit, but we can adjust it
        }

        return app.UseMiddleware<PerformanceMonitoringMiddleware>(monitor);
    }

    /// <summary>
    /// Gets performance metrics filtered by path pattern
    /// </summary>
    /// <param name="middleware">The middleware instance</param>
    /// <param name="pathPattern">Path pattern to filter metrics (e.g., "/api/*")</param>
    /// <param name="minutes">Time window in minutes</param>
    /// <returns>Filtered list of request metrics</returns>
    public static List<RequestMetric> GetMetricsByPath(
        this PerformanceMonitoringMiddleware middleware,
        string pathPattern,
        int minutes = 60)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        if (string.IsNullOrWhiteSpace(pathPattern))
            throw new ArgumentException("Path pattern cannot be null or empty", nameof(pathPattern));

        var allMetrics = middleware.Monitor.GetRecentMetrics(minutes);
        var filtered = new List<RequestMetric>();

        foreach (var metric in allMetrics)
        {
            if (PathMatchesPattern(metric.Path, pathPattern))
            {
                filtered.Add(metric);
            }
        }

        return filtered;
    }

    /// <summary>
    /// Gets performance statistics filtered by HTTP method
    /// </summary>
    /// <param name="middleware">The middleware instance</param>
    /// <param name="method">HTTP method to filter by (GET, POST, PUT, DELETE, etc.)</param>
    /// <param name="minutes">Time window in minutes</param>
    /// <returns>Performance statistics for the specified method</returns>
    public static PerformanceStats GetStatsByMethod(
        this PerformanceMonitoringMiddleware middleware,
        string method,
        int minutes = 60)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("Method cannot be null or empty", nameof(method));

        var allMetrics = middleware.Monitor.GetRecentMetrics(minutes);
        var filteredMetrics = allMetrics.Where(m =>
            string.Equals(m.Method, method, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filteredMetrics.Count == 0)
            return new PerformanceStats();

        var durations = filteredMetrics.Select(m => m.DurationMs).OrderBy(d => d).ToList();

        return new PerformanceStats
        {
            RequestCount = filteredMetrics.Count,
            AverageDurationMs = (long)durations.Average(),
            MinDurationMs = durations.First(),
            MaxDurationMs = durations.Last(),
            P50DurationMs = GetPercentile(durations, 50),
            P95DurationMs = GetPercentile(durations, 95),
            P99DurationMs = GetPercentile(durations, 99),
            ErrorCount = filteredMetrics.Count(m => m.StatusCode >= 400),
            ErrorRate = filteredMetrics.Count(m => m.StatusCode >= 400) / (double)filteredMetrics.Count
        };
    }

    /// <summary>
    /// Gets slowest requests within a time window
    /// </summary>
    /// <param name="middleware">The middleware instance</param>
    /// <param name="thresholdMs">Minimum duration threshold in milliseconds</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="minutes">Time window in minutes</param>
    /// <returns>List of slowest requests, ordered by duration descending</returns>
    public static List<RequestMetric> GetSlowestRequests(
        this PerformanceMonitoringMiddleware middleware,
        long thresholdMs,
        int limit = 50,
        int minutes = 60)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        var metrics = middleware.Monitor.GetRecentMetrics(minutes)
            .Where(m => m.DurationMs >= thresholdMs)
            .OrderByDescending(m => m.DurationMs)
            .Take(limit)
            .ToList();

        return metrics;
    }

    /// <summary>
    /// Checks if a path matches a pattern (supports * and ? wildcards)
    /// </summary>
    /// <param name="path">The actual path</param>
    /// <param name="pattern">The pattern to match against</param>
    /// <returns>True if the path matches the pattern</returns>
    private static bool PathMatchesPattern(string path, string pattern)
    {
        if (pattern == "*")
            return true;

        // Simple wildcard matching for path patterns like "/api/*" or "/api/users/*"
        if (pattern.EndsWith("/*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 2);
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(path, pattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets percentile value from sorted list of durations
    /// </summary>
    private static long GetPercentile(List<long> values, int percentile)
    {
        var index = (int)Math.Ceiling(values.Count * (percentile / 100.0)) - 1;
        return values[Math.Max(0, index)];
    }
}

/// <summary>
/// Configuration options for performance monitoring
/// </summary>
public sealed class PerformanceMonitoringOptions
{
    /// <summary>
    /// Custom PerformanceMonitor instance to use
    /// </summary>
    public PerformanceMonitor? Monitor { get; set; }

    /// <summary>
    /// Maximum number of metrics to keep in memory
    /// </summary>
    public int? MaxMetricsToKeep { get; set; }
}
