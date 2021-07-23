// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using System.Text;

namespace DotnetOutboxPattern.Middleware;

/// <summary>
/// Logs all incoming requests and outgoing responses with timing information
/// Provides visibility into API performance and request patterns
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Store original body stream so we can read it later
        var originalBodyStream = context.Response.Body;

        try
        {
            // Read and log request body for POST/PUT operations
            if (context.Request.Method == "POST" || context.Request.Method == "PUT")
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                _logger.LogInformation(
                    "Request started: {Method} {Path} | Body: {Body} | TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    body.Length > 500 ? body.Substring(0, 500) + "..." : body,
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogInformation(
                    "Request started: {Method} {Path} | TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
            }

            // Use memory stream to capture response
            using var responseMemoryStream = new MemoryStream();
            context.Response.Body = responseMemoryStream;

            await _next(context);

            stopwatch.Stop();

            // Log response
            var responseBody = Encoding.UTF8.GetString(responseMemoryStream.ToArray());
            _logger.LogInformation(
                "Request completed: {Method} {Path} | StatusCode: {StatusCode} | Duration: {DurationMs}ms | TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                context.TraceIdentifier);

            // Copy response back to original stream
            await responseMemoryStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Request failed: {Method} {Path} | Duration: {DurationMs}ms | TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                context.TraceIdentifier);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}

/// <summary>
/// Extension method to register request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
