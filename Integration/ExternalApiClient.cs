// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;

namespace DotnetOutboxPattern.Integration;

/// <summary>
/// Client for calling external APIs as part of message publishing
/// Used when outbox events need to trigger external system calls
/// </summary>
public interface IExternalApiClient
{
    Task<ApiCallResult> CallAsync(string url, object payload, Dictionary<string, string>? headers = null);
    Task<T?> CallAsync<T>(string url, object payload, Dictionary<string, string>? headers = null);
}

/// <summary>
/// Result of an external API call
/// </summary>
public class ApiCallResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
}

/// <summary>
/// Default implementation of external API client
/// </summary>
public class ExternalApiClient : IExternalApiClient
{
    private readonly ResilientHttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;

    public ExternalApiClient(ResilientHttpClient httpClient, ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiCallResult> CallAsync(
        string url,
        object payload,
        Dictionary<string, string>? headers = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add custom headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    content.Headers.Add(header.Key, header.Value);
                }
            }

            var response = await _httpClient.PostAsync(url, content);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync();

            return new ApiCallResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ResponseBody = responseBody,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Error calling external API: {Url}", url);

            return new ApiCallResult
            {
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = ex.Message,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<T?> CallAsync<T>(
        string url,
        object payload,
        Dictionary<string, string>? headers = null)
    {
        var result = await CallAsync(url, payload, headers);

        if (!result.IsSuccess || string.IsNullOrEmpty(result.ResponseBody))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(result.ResponseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing API response from {Url}", url);
            return default;
        }
    }
}
