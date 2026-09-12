#nullable enable

namespace DotnetOutboxPattern.Exceptions;

/// <summary>
/// Represents an error that occurs when an HTTP request fails.
/// </summary>
public sealed class HttpRequestException : OutboxException
{
    /// <summary>
    /// Gets the HTTP status code returned by the request, if available.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the URL of the request that failed, if available.
    /// </summary>
    public string? RequestUrl { get; }

    /// <summary>
    /// Gets the HTTP method used by the request, if available.
    /// </summary>
    public string? Method { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="requestUrl">The URL of the request that failed, if available.</param>
    /// <param name="method">The HTTP method used by the request, if available.</param>
    /// <param name="statusCode">The HTTP status code returned by the request, if available.</param>
    /// <param name="innerException">The exception that caused the current exception, if available.</param>
    public HttpRequestException(string message, string? requestUrl = null, string? method = null, int? statusCode = null, Exception? innerException = null)
        : base(message, "HTTP_REQUEST_FAILED", requestUrl)
    {
        RequestUrl = requestUrl;
        Method = method;
        StatusCode = statusCode;
    }
}
