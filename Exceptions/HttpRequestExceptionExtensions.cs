using System;
using System.Net;

namespace DotnetOutboxPattern.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="HttpRequestException"/> to facilitate error handling and analysis of HTTP request failures.
    /// </summary>
    public static class HttpRequestExceptionExtensions
    {
        /// <summary>
        /// Gets the HTTP status code from the exception, returning 0 if not available.
        /// </summary>
        /// <param name="exception">The HTTP request exception to extract the status code from.</param>
        /// <returns>The HTTP status code if available; otherwise, 0.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static int GetStatusCode(this HttpRequestException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return exception.StatusCode ?? 0;
        }

        /// <summary>
        /// Determines if the exception represents a client error (4xx status code).
        /// </summary>
        /// <param name="exception">The HTTP request exception to analyze.</param>
        /// <returns>True if the exception represents a client error (400-499); otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static bool IsClientError(this HttpRequestException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            var statusCode = exception.GetStatusCode();
            return statusCode >= 400 && statusCode < 500;
        }

        /// <summary>
        /// Determines if the exception represents a server error (5xx status code).
        /// </summary>
        /// <param name="exception">The HTTP request exception to analyze.</param>
        /// <returns>True if the exception represents a server error (500-599); otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static bool IsServerError(this HttpRequestException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            var statusCode = exception.GetStatusCode();
            return statusCode >= 500 && statusCode < 600;
        }

        /// <summary>
        /// Creates a formatted error message containing request details.
        /// </summary>
        /// <param name="exception">The HTTP request exception to format.</param>
        /// <returns>A formatted error message including HTTP method, request URL, and status code.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static string ToErrorMessage(this HttpRequestException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return $"HTTP {exception.Method} request to {exception.RequestUrl} failed with status code {exception.GetStatusCode()}";
        }
    }
}