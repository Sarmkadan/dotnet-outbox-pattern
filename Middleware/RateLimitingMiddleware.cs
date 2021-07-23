// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Net;

namespace DotnetOutboxPattern.Middleware;

/// <summary>
/// Rate limiting middleware using sliding window token bucket algorithm
/// Protects the API from abuse and ensures fair resource allocation
/// </summary>
public class RateLimitingMiddleware
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

        // Cleanup expired entries periodically
        _ = CleanupExpiredEntriesAsync();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);

        if (!IsRateLimited(clientId, out var remaining))
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientId}. Limit: {Limit} requests per {WindowSeconds}s",
                clientId, _options.RequestsPerWindow, _options.WindowSeconds);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = _options.WindowSeconds.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerWindow.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";

            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = _options.RequestsPerWindow.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

        await _next(context);
    }

    /// <summary>
    /// Checks if client has exceeded rate limit using token bucket algorithm
    /// </summary>
    private bool IsRateLimited(string clientId, out int remaining)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-_options.WindowSeconds);

        var clientLimit = _clientLimits.AddOrUpdate(
            clientId,
            new ClientRateLimit
            {
                WindowStart = now,
                RequestCount = 1,
                LastRequest = now
            },
            (key, existing) =>
            {
                // Reset window if expired
                if (existing.WindowStart < windowStart)
                {
                    return new ClientRateLimit
                    {
                        WindowStart = now,
                        RequestCount = 1,
                        LastRequest = now
                    };
                }

                // Increment counter if within window
                if (existing.RequestCount < _options.RequestsPerWindow)
                {
                    existing.RequestCount++;
                    existing.LastRequest = now;
                    return existing;
                }

                return existing;
            });

        remaining = Math.Max(0, _options.RequestsPerWindow - clientLimit.RequestCount);
        return clientLimit.RequestCount <= _options.RequestsPerWindow;
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
                    .Where(kvp => kvp.Value.LastRequest < expiredWindow)
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
public class RateLimitingOptions
{
    public int RequestsPerWindow { get; set; } = 1000;
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// Tracks rate limit state for a single client
/// </summary>
internal class ClientRateLimit
{
    public DateTime WindowStart { get; set; }
    public int RequestCount { get; set; }
    public DateTime LastRequest { get; set; }
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
