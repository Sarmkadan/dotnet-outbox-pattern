using System;
using System.Net;

namespace DotnetOutboxPattern.Exceptions
{
    public static class HttpRequestExceptionExtensions
    {
        /// <summary>
        /// Gets the HTTP status code from the exception, returning 0 if not available
        /// </summary>
        public static int GetStatusCode(this HttpRequestException exception) =>
            exception.StatusCode ?? 0;

        /// <summary>
        /// Determines if the exception represents a client error (4xx status code)
        /// </summary>
        public static bool IsClientError(this HttpRequestException exception) =>
            exception.GetStatusCode() >= 400 && exception.GetStatusCode() < 500;

        /// <summary>
        /// Determines if the exception represents a server error (5xx status code)
        /// </summary>
        public static bool IsServerError(this HttpRequestException exception) =>
            exception.GetStatusCode() >= 500 && exception.GetStatusCode() < 600;

        /// <summary>
        /// Creates a formatted error message containing request details
        /// </summary>
        public static string ToErrorMessage(this HttpRequestException exception) =>
            $"HTTP {exception.Method} request to {exception.RequestUrl} failed with status code {exception.GetStatusCode()}";
    }
}
