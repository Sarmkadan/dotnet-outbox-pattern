# ExternalApiClient

## Purpose

`ExternalApiClient` implements `IExternalApiClient` for posting an object to an external HTTP endpoint as JSON. It uses `ResilientHttpClient` to send the request, records the elapsed request time, and logs request or response-deserialization failures.

The non-generic overload returns an `ApiCallResult` containing:

- `IsSuccess`: the value of `HttpResponseMessage.IsSuccessStatusCode`.
- `StatusCode`: the numeric HTTP status code, or `0` when an exception occurs while making the call.
- `ResponseBody`: the response content read as a string after an HTTP response is received.
- `ErrorMessage`: the exception message when an exception occurs while making the call.
- `DurationMs`: elapsed time in milliseconds.

Requests serialize the supplied payload with `System.Text.Json`, use UTF-8, and set the content type to `application/json`. Optional headers are added to the request content headers.

## Public Methods

### `Task<ApiCallResult> CallAsync(string url, object payload, Dictionary<string, string>? headers = null)`

Posts `payload` as JSON to `url` and returns the status, response body, and duration in an `ApiCallResult`. A non-success HTTP response is returned with `IsSuccess` set to `false`; its status code and response body remain available.

If sending or reading the response throws, the method logs the exception and returns a failed result with `StatusCode` set to `0` and `ErrorMessage` set to the exception message.

The method throws `ArgumentException` when `url` is null, empty, or whitespace, and `ArgumentNullException` when `payload` is null. These validations occur before request error handling.

### `Task<T?> CallAsync<T>(string url, object payload, Dictionary<string, string>? headers = null)`

Calls the non-generic overload and deserializes a successful, non-empty response body to `T` with `System.Text.Json`. It returns `default` when the HTTP result is unsuccessful, the response body is null or empty, or deserialization throws. Deserialization failures are logged.

The URL and payload validation behavior is inherited from the non-generic overload.

## Usage Example

`AddOutboxPatternPhase2` registers `IExternalApiClient` as a scoped service and configures its `ResilientHttpClient` dependency. A consuming service can request the interface through dependency injection:

```csharp
using DotnetOutboxPattern.Integration;

public sealed class OrderGateway
{
    private readonly IExternalApiClient _apiClient;

    public OrderGateway(IExternalApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<CreateOrderResponse?> CreateOrderAsync(Guid orderId)
    {
        var payload = new { OrderId = orderId };
        var headers = new Dictionary<string, string>
        {
            ["X-Correlation-Id"] = orderId.ToString()
        };

        return await _apiClient.CallAsync<CreateOrderResponse>(
            "https://api.example.com/orders",
            payload,
            headers);
    }
}

public sealed record CreateOrderResponse(string ExternalOrderId);
```

The registration is added to an `IServiceCollection` with:

```csharp
using DotnetOutboxPattern.Configuration;

builder.Services.AddOutboxPatternPhase2();
```
