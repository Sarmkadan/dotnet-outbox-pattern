# HttpRequestException

The `HttpRequestException` class is a custom exception used within the outbox pattern implementation to represent failures that occur during HTTP requests. It captures the HTTP status code (if available), the URL of the failed request, and the HTTP method that was used. This information aids in diagnosing transient or persistent communication errors when publishing outbox messages to external endpoints.

## API

### `public int? StatusCode`

Gets the HTTP status code returned by the server, or `null` if the request did not complete (e.g., a network error prevented a response).

- **Type:** `int?`
- **Remarks:** This property is read-only. A value of `null` indicates that no status code was received.

### `public string? RequestUrl`

Gets the absolute URL of the request that failed, or `null` if the URL was not captured.

- **Type:** `string?`
- **Remarks:** This property is read-only. It typically contains the full URI (e.g., `https://api.example.com/events`).

### `public string? Method`

Gets the HTTP method (e.g., `"POST"`, `"PUT"`) used in the failed request, or `null` if the method was not captured.

- **Type:** `string?`
- **Remarks:** This property is read-only. The value is case-sensitive and matches the method string used by the underlying HTTP client.

### `public HttpRequestException()`

Initializes a new instance of the `HttpRequestException` class.

- **Parameters:** None.
- **Return value:** A new `HttpRequestException` instance with all properties set to `null`.
- **Throws:** This constructor does not throw.

## Usage

### Example 1: Inspecting a caught exception during outbox processing

```csharp
public async Task ProcessOutboxMessageAsync(OutboxMessage message)
{
    try
    {
        await _httpClient.PostAsync(message.DestinationUrl, message.Payload);
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(
            "HTTP request failed. StatusCode: {StatusCode}, URL: {Url}, Method: {Method}",
            ex.StatusCode,
            ex.RequestUrl,
            ex.Method);
        // Optionally decide to retry or dead-letter the message
    }
}
```

### Example 2: Using status code in a retry policy

```csharp
public bool ShouldRetry(HttpRequestException exception)
{
    // Retry only on server errors (5xx) or when no status code is available
    return exception.StatusCode is null or >= 500;
}
```

## Notes

- **Edge cases:**  
  - `StatusCode` may be `null` when the request fails before receiving a response (e.g., DNS resolution failure, connection timeout).  
  - `RequestUrl` and `Method` may be `null` if the exception was thrown without capturing those details. Code consuming this exception should always check for `null` before using these properties.  
  - The exception does not provide a default message; callers should rely on the structured properties for diagnostics.

- **Thread safety:**  
  Instances of `HttpRequestException` are immutable after construction (the properties are read-only). They are safe to read from multiple threads concurrently. The exception is typically thrown and caught on the same execution context, so no synchronization is required.
