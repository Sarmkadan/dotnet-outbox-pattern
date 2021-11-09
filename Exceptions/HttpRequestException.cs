#nullable enable

namespace DotnetOutboxPattern.Exceptions;

/// <summary>
/// Exception thrown when HTTP requests fail
/// </summary>
public sealed class HttpRequestException : OutboxException
{
    /// <summary>
    /// HTTP status code if available
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Request URL that failed
    /// </summary>
    public string? RequestUrl { get; }

    /// <summary>
    /// Request method (GET, POST, etc.)
    /// </summary>
    public string? Method { get; }

    public HttpRequestException(string message, string? requestUrl = null, string? method = null, int? statusCode = null, Exception? innerException = null)
        : base(message, "HTTP_REQUEST_FAILED", requestUrl)
    {
        RequestUrl = requestUrl;
        Method = method;
        StatusCode = statusCode;
    }
}
