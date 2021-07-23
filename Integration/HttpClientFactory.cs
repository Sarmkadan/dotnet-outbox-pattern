// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using System.Net.Http.Headers;

namespace DotnetOutboxPattern.Integration;

/// <summary>
/// Factory for creating and configuring HTTP clients for external integrations
/// Handles connection pooling, timeouts, and retry policies
/// </summary>
public interface IHttpClientFactory
{
    HttpClient CreateClient(string name, HttpClientConfig? config = null);
    HttpClient GetNamedClient(string name);
}

/// <summary>
/// Configuration for HTTP client creation
/// </summary>
public class HttpClientConfig
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public string? UserAgent { get; set; }
    public Dictionary<string, string>? DefaultHeaders { get; set; }
    public bool FollowRedirects { get; set; } = true;
    public int? MaxRedirects { get; set; }
    public ProxyConfig? ProxyConfig { get; set; }
}

/// <summary>
/// Proxy configuration for HTTP clients
/// </summary>
public class ProxyConfig
{
    public string? ProxyUrl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public List<string>? BypassList { get; set; }
}

/// <summary>
/// Default HTTP client factory implementation
/// </summary>
public class CustomHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients = new();
    private readonly ILogger<CustomHttpClientFactory> _logger;

    public CustomHttpClientFactory(ILogger<CustomHttpClientFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public HttpClient CreateClient(string name, HttpClientConfig? config = null)
    {
        config ??= new HttpClientConfig();

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = config.FollowRedirects,
            MaxAutomaticRedirections = config.MaxRedirects ?? 5
        };

        // Configure proxy if specified
        if (config.ProxyConfig?.ProxyUrl != null)
        {
            var proxy = new WebProxy(config.ProxyConfig.ProxyUrl);

            if (!string.IsNullOrEmpty(config.ProxyConfig.Username))
            {
                proxy.Credentials = new System.Net.NetworkCredential(
                    config.ProxyConfig.Username,
                    config.ProxyConfig.Password);
            }

            if (config.ProxyConfig.BypassList?.Count > 0)
            {
                proxy.BypassList = config.ProxyConfig.BypassList.ToArray();
            }

            handler.Proxy = proxy;
        }

        var client = new HttpClient(handler)
        {
            Timeout = config.Timeout
        };

        // Set default user agent
        if (!string.IsNullOrEmpty(config.UserAgent))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);
        }
        else
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DotnetOutboxPattern/1.0");
        }

        // Add default headers
        if (config.DefaultHeaders != null)
        {
            foreach (var header in config.DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        _clients[name] = client;

        _logger.LogInformation("HTTP client created: {ClientName} with timeout {Timeout}ms",
            name, config.Timeout.TotalMilliseconds);

        return client;
    }

    public HttpClient GetNamedClient(string name)
    {
        if (!_clients.TryGetValue(name, out var client))
        {
            throw new InvalidOperationException($"HTTP client '{name}' not found");
        }

        return client;
    }
}

/// <summary>
/// Resilient HTTP client wrapper with retry and timeout handling
/// </summary>
public class ResilientHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResilientHttpClient> _logger;
    private readonly int _maxRetries;

    public ResilientHttpClient(HttpClient httpClient, ILogger<ResilientHttpClient> logger, int maxRetries = 3)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRetries = maxRetries;
    }

    public async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(async () =>
            await _httpClient.GetAsync(uri, cancellationToken), cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string uri, HttpContent content, CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(async () =>
            await _httpClient.PostAsync(uri, content, cancellationToken), cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(string uri, HttpContent content, CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(async () =>
            await _httpClient.PutAsync(uri, content, cancellationToken), cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string uri, CancellationToken cancellationToken = default)
    {
        return await SendWithRetryAsync(async () =>
            await _httpClient.DeleteAsync(uri, cancellationToken), cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken,
        int attempt = 0)
    {
        try
        {
            return await request();
        }
        catch (HttpRequestException ex) when (attempt < _maxRetries && IsTransientError(ex))
        {
            var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
            _logger.LogWarning(
                "HTTP request failed (attempt {Attempt}/{MaxRetries}), retrying after {DelayMs}ms",
                attempt + 1, _maxRetries, delay.TotalMilliseconds);

            await Task.Delay(delay, cancellationToken);
            return await SendWithRetryAsync(request, cancellationToken, attempt + 1);
        }
    }

    private static bool IsTransientError(HttpRequestException ex)
    {
        // Transient errors that warrant retry
        return ex.InnerException is TimeoutException ||
               ex.InnerException is IOException ||
               ex.Message.Contains("Connection refused") ||
               ex.Message.Contains("Timeout");
    }
}
