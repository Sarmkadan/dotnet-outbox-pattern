# HTTP Client Factory

## Purpose

`Integration/HttpClientFactory.cs` provides two related HTTP client components:

- `CustomHttpClientFactory` creates, stores, retrieves, replaces, and disposes named `HttpClient` instances.
- `ResilientHttpClient` wraps an `HttpClient` and retries selected transient `HttpRequestException` failures with exponential delays.

The file also defines the `IHttpClientFactory` contract and the `HttpClientConfig` and `ProxyConfig` configuration types. This is the project-specific `DotnetOutboxPattern.Integration.IHttpClientFactory`, not `System.Net.Http.IHttpClientFactory`.

## Public Methods

### `CustomHttpClientFactory(ILogger<CustomHttpClientFactory> logger)`

Creates the factory. It throws `ArgumentNullException` when `logger` is null.

### `HttpClient CreateClient(string name, HttpClientConfig? config = null)`

Creates an `HttpClient`, stores it under `name`, and returns it. A null configuration uses a new `HttpClientConfig` with its default values. If the name already exists, the factory replaces and disposes the previous client.

The method throws `ArgumentException` when `name` is null, empty, or whitespace. Invalid header, user-agent, proxy, timeout, or redirect values can also cause the underlying .NET APIs to throw.

### `HttpClient GetNamedClient(string name)`

Returns the client previously stored under `name`. It throws `ArgumentException` when the name is null, empty, or whitespace, and `InvalidOperationException` when no client exists under that name.

Names use ordinal, case-sensitive comparison.

### `void Dispose()`

Disposes all clients currently stored by the factory and clears the named-client collection. The factory also owns and disposes a client when that named entry is replaced.

### `ResilientHttpClient(HttpClient httpClient, ILogger<ResilientHttpClient> logger, int maxRetries = 3)`

Creates the retrying wrapper. It throws `ArgumentNullException` when either `httpClient` or `logger` is null. `maxRetries` controls the number of retries after the initial attempt.

### Resilient request methods

- `Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken = default)`
- `Task<HttpResponseMessage> PostAsync(string uri, HttpContent content, CancellationToken cancellationToken = default)`
- `Task<HttpResponseMessage> PutAsync(string uri, HttpContent content, CancellationToken cancellationToken = default)`
- `Task<HttpResponseMessage> DeleteAsync(string uri, CancellationToken cancellationToken = default)`

Each method delegates to the corresponding `HttpClient` method. A request is retried only when it throws `HttpRequestException` and the exception is classified as transient. The implementation treats timeout, I/O, and socket inner exceptions; HTTP status codes 408, 429, 502, 503, and 504; and exception messages containing `Connection refused` or `Timeout` as transient.

Retries wait 100 ms, 200 ms, 400 ms, and so on. The supplied cancellation token is passed to the request and retry delay. HTTP responses, including unsuccessful status codes, are returned directly and are not retried.

## Configuration

`HttpClientConfig` has these properties:

| Property | Default | Effect |
| --- | --- | --- |
| `Timeout` | 30 seconds | Assigned to `HttpClient.Timeout`. |
| `MaxRetries` | `3` | Holds a retry count, but `CustomHttpClientFactory.CreateClient` does not read it. Pass this value to the `ResilientHttpClient` constructor when manually composing the wrapper. |
| `UserAgent` | `null` | Sets the default `User-Agent`. Null or empty values use `DotnetOutboxPattern/1.0`. |
| `DefaultHeaders` | `null` | Adds each entry to `HttpClient.DefaultRequestHeaders`. |
| `FollowRedirects` | `true` | Sets `HttpClientHandler.AllowAutoRedirect`. |
| `MaxRedirects` | `null` | Sets `HttpClientHandler.MaxAutomaticRedirections`; null uses `5`. |
| `ProxyConfig` | `null` | Configures a `WebProxy` only when `ProxyUrl` is non-null. |

`ProxyConfig` contains:

| Property | Effect |
| --- | --- |
| `ProxyUrl` | Address passed to `WebProxy`. A null value means no proxy is assigned by the factory. |
| `Username` | When non-null and non-empty, creates proxy credentials. |
| `Password` | Password supplied with the proxy username; it may be null. |
| `BypassList` | When non-null and non-empty, copied to `WebProxy.BypassList`. |

`AddOutboxPatternPhase2` registers `IHttpClientFactory` as a singleton backed by `CustomHttpClientFactory`. It separately registers `ResilientHttpClient` through `AddHttpClient<ResilientHttpClient>()`; that registration uses the wrapper constructor's default retry count and does not consume a named client or `HttpClientConfig`.

## Usage Example

The following example uses the project factory through dependency injection, creates a configured named client, retrieves the same instance, and manually applies the configured retry count to `ResilientHttpClient`:

```csharp
using DotnetOutboxPattern.Integration;
using Microsoft.Extensions.Logging;

public sealed class CatalogGateway
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ResilientHttpClient> _resilientLogger;

    public CatalogGateway(
        IHttpClientFactory factory,
        ILogger<ResilientHttpClient> resilientLogger)
    {
        _factory = factory;
        _resilientLogger = resilientLogger;
    }

    public async Task<HttpResponseMessage> GetProductAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        var config = new HttpClientConfig
        {
            Timeout = TimeSpan.FromSeconds(10),
            MaxRetries = 2,
            UserAgent = "CatalogGateway/1.0",
            DefaultHeaders = new Dictionary<string, string>
            {
                ["X-Client"] = "outbox"
            },
            FollowRedirects = true,
            MaxRedirects = 3
        };

        _factory.CreateClient("catalog", config);
        var client = _factory.GetNamedClient("catalog");
        var resilientClient = new ResilientHttpClient(
            client,
            _resilientLogger,
            config.MaxRetries);

        return await resilientClient.GetAsync(
            $"https://api.example.com/products/{productId}",
            cancellationToken);
    }
}
```

Register the project services before resolving `CatalogGateway`:

```csharp
using DotnetOutboxPattern.Configuration;

builder.Services.AddOutboxPatternPhase2();
builder.Services.AddScoped<CatalogGateway>();
```

Because the factory owns its named clients, callers should not dispose a retrieved client while it remains registered for reuse.
