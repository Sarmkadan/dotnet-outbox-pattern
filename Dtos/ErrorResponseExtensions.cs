#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;

namespace DotnetOutboxPattern.Dtos;

/// <summary>
/// Extension methods for ErrorResponse to provide additional functionality
/// </summary>
public static class ErrorResponseExtensions
{
    /// <summary>
    /// Creates a standardized error response with additional context
    /// </summary>
    /// <param name="errorResponse">The source error response</param>
    /// <param name="context">Additional context to include</param>
    /// <returns>A new ErrorResponse with merged context</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorResponse"/> is null</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null</exception>
    public static ErrorResponse WithContext(this ErrorResponse errorResponse, string context)
    {
        ArgumentNullException.ThrowIfNull(errorResponse);
        ArgumentNullException.ThrowIfNull(context);

        return new ErrorResponse
        {
            Message = $"{errorResponse.Message} | Context: {context}",
            Code = errorResponse.Code,
            Timestamp = errorResponse.Timestamp,
            TraceId = errorResponse.TraceId
        };
    }

    /// <summary>
    /// Creates a copy of the error response with updated timestamp
    /// </summary>
    /// <param name="errorResponse">The source error response</param>
    /// <param name="newTimestamp">The new timestamp to set</param>
    /// <returns>A new ErrorResponse with the updated timestamp</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorResponse"/> is null</exception>
    public static ErrorResponse WithTimestamp(this ErrorResponse errorResponse, DateTime newTimestamp)
    {
        ArgumentNullException.ThrowIfNull(errorResponse);

        return new ErrorResponse
        {
            Message = errorResponse.Message,
            Code = errorResponse.Code,
            Timestamp = newTimestamp,
            TraceId = errorResponse.TraceId
        };
    }

    /// <summary>
    /// Creates a copy of the error response with a new trace ID
    /// </summary>
    /// <param name="errorResponse">The source error response</param>
    /// <param name="newTraceId">The new trace ID to set</param>
    /// <returns>A new ErrorResponse with the updated trace ID</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorResponse"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newTraceId"/> is null or empty</exception>
    public static ErrorResponse WithTraceId(this ErrorResponse errorResponse, string newTraceId)
    {
        ArgumentNullException.ThrowIfNull(errorResponse);
        ArgumentException.ThrowIfNullOrEmpty(newTraceId);

        return new ErrorResponse
        {
            Message = errorResponse.Message,
            Code = errorResponse.Code,
            Timestamp = errorResponse.Timestamp,
            TraceId = newTraceId
        };
    }

    /// <summary>
    /// Serializes the error response to JSON with optional formatting
    /// </summary>
    /// <param name="errorResponse">The error response to serialize</param>
    /// <param name="indentJson">Whether to indent the JSON output</param>
    /// <returns>JSON string representation of the error response</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorResponse"/> is null</exception>
    public static string ToJson(this ErrorResponse errorResponse, bool indentJson = false)
    {
        ArgumentNullException.ThrowIfNull(errorResponse);

        var options = new JsonSerializerOptions
        {
            WriteIndented = indentJson,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(errorResponse, options);
    }

    /// <summary>
    /// Determines if the error response represents a client error (4xx status code)
    /// </summary>
    /// <param name="errorResponse">The error response to check</param>
    /// <returns>True if the error code starts with '4', false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorResponse"/> is null</exception>
    public static bool IsClientError(this ErrorResponse errorResponse)
    {
        ArgumentNullException.ThrowIfNull(errorResponse);

        return errorResponse.Code.StartsWith("4", StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines if the error response represents a server error (5xx status code)
    /// </summary>
    /// <param name="errorResponse">The error response to check</param>
    /// <returns>True if the error code starts with '5', false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorResponse"/> is null</exception>
    public static bool IsServerError(this ErrorResponse errorResponse)
    {
        ArgumentNullException.ThrowIfNull(errorResponse);

        return errorResponse.Code.StartsWith("5", StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a simplified error response suitable for logging
    /// </summary>
    /// <param name="errorResponse">The source error response</param>
    /// <returns>A simplified error response with key information</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorResponse"/> is null</exception>
    public static ErrorResponse ToLogFormat(this ErrorResponse errorResponse)
    {
        ArgumentNullException.ThrowIfNull(errorResponse);

        return new ErrorResponse
        {
            Message = errorResponse.Message,
            Code = errorResponse.Code,
            Timestamp = errorResponse.Timestamp,
            TraceId = errorResponse.TraceId
        };
    }
}