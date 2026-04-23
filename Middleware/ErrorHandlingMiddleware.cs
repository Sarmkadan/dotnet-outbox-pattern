#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using System.Text.Json;
using DotnetOutboxPattern.Dtos;
using DotnetOutboxPattern.Exceptions;

namespace DotnetOutboxPattern.Middleware;

/// <summary>
/// Centralized error handling middleware that catches and formats all unhandled exceptions
/// Ensures consistent error responses and proper logging of errors
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in request {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Maps exceptions to appropriate HTTP responses with consistent error formatting
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = argEx.Message;
                response.Code = "VALIDATION_ERROR";
                break;

            case OutboxException outboxEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = outboxEx.Message;
                response.Code = "OUTBOX_ERROR";
                break;

            case OperationCanceledException:
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Message = "Request was cancelled";
                response.Code = "REQUEST_TIMEOUT";
                break;

            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = exception.Message;
                response.Code = "INVALID_OPERATION";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An unexpected error occurred";
                response.Code = "INTERNAL_SERVER_ERROR";
                break;
        }

        response.Timestamp = DateTime.UtcNow;
        response.TraceId = context.TraceIdentifier;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension method to register error handling middleware
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
