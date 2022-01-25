#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Globalization;
using System.Net;

namespace DotnetOutboxPattern.Middleware;

/// <summary>
/// Rate limiting middleware using sliding window token bucket algorithm
/// Protects the API from abuse and ensures fair resource allocation
/// </summary>
public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<string, ClientRateLimit> _clientLimits;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitingOptions? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new RateLimitingOptions();
        _clientLimits = new ConcurrentDictionary<string, ClientRateLimit>();

        // Cleanup expired entries periodically. Failures are logged inside the loop,
        // so the continuation only has to catch a hard failure of the loop itself.
        _ = CleanupExpiredEntriesAsync().ContinueWith(
            t => _logger.LogError(t.Exception, "Rate limit cleanup loop terminated unexpectedly"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var clientId = GetClientIdentifier(context);

        if (!TryAcquire(clientId, out var remaining))
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientId}. Limit: {Limit} requests per {WindowSeconds}s",
                clientId, _options.RequestsPerWindow, _options.WindowSeconds);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = _options.WindowSeconds.ToString(CultureInfo.InvariantCulture);
            context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerWindow.ToString(CultureInfo.InvariantCulture);
            context.Response.Headers["X-RateLimit-Remaining"] = "0";

            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerWindow.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString(CultureInfo.InvariantCulture);

        await _next(context);
    }

    /// <summary>
    /// Tries to consume one request slot for the client within the current fixed window
    /// </summary>
    /// <returns><c>true</c> when the request is allowed, <c>false</c> when the limit is exhausted.</returns>
    private bool TryAcquire(string clientId, out int remaining)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-_options.WindowSeconds);

        var clientLimit = _clientLimits.GetOrAdd(clientId, _ => new ClientRateLimit { WindowStart = now });

        // The counter is shared between concurrent requests of the same client, so the
        // read-modify-write has to be atomic. AddOrUpdate cannot provide that: its
        // factory may run more than once and lose increments.
        lock (clientLimit.SyncRoot)
        {
            if (clientLimit.WindowStart < windowStart)
            {
                clientLimit.WindowStart = now;
                clientLimit.RequestCount = 0;
            }

            clientLimit.LastRequest = now;

            if (clientLimit.RequestCount >= _options.RequestsPerWindow)
            {
                remaining = 0;
                return false;
            }

            clientLimit.RequestCount++;
            remaining = Math.Max(0, _options.RequestsPerWindow - clientLimit.RequestCount);
            return true;
        }
    }

    /// <summary>
    /// Gets client identifier - uses IP address by default
    /// Can be extended to use API keys, user IDs, etc.
    /// </summary>
    private static string GetClientIdentifier(HttpContext context)
    {
        // Try to get API key from header first
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            return $"api-key:{apiKey}";

        // Fall back to IP address
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }

    /// <summary>
    /// Cleans up expired rate limit entries to prevent memory leaks
    /// </summary>
    private async Task CleanupExpiredEntriesAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                var expiredWindow = DateTime.UtcNow.AddSeconds(-_options.WindowSeconds * 2);
                var expiredClients = _clientLimits
                    .Where(kvp => kvp.Value.GetLastRequest() < expiredWindow)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var client in expiredClients)
                {
                    _clientLimits.TryRemove(client, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rate limit cleanup");
            }
        }
    }
}

/// <summary>
/// Configuration options for rate limiting
/// </summary>
public sealed class RateLimitingOptions
{
    public int RequestsPerWindow { get; set; } = 1000;
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// Tracks rate limit state for a single client
/// </summary>
internal sealed class ClientRateLimit
{
    /// <summary>
    /// Guards all mutable state of this entry against concurrent requests of the same client.
    /// </summary>
    public object SyncRoot { get; } = new();

    public DateTime WindowStart { get; set; }
    public int RequestCount { get; set; }
    public DateTime LastRequest { get; set; }

    /// <summary>
    /// Reads the last request timestamp under the entry lock.
    /// </summary>
    public DateTime GetLastRequest()
    {
        lock (SyncRoot)
        {
            return LastRequest;
        }
    }
}

/// <summary>
/// Extension method to register rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(
        this IApplicationBuilder app,
        RateLimitingOptions? options = null)
    {
        return app.UseMiddleware<RateLimitingMiddleware>(options);
    }
}
