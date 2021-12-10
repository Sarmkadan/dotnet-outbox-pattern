# IExternalApiClient
The `IExternalApiClient` interface is designed to provide a standardized way of interacting with external APIs, allowing for the abstraction of underlying API call logic and the handling of responses. This interface is part of the `dotnet-outbox-pattern` project, which aims to provide a robust and scalable architecture for building distributed systems.

## API
The `IExternalApiClient` interface exposes the following members:
- `bool IsSuccess`: Indicates whether the API call was successful.
- `int StatusCode`: The HTTP status code returned by the API.
- `string? ResponseBody`: The response body of the API call, if any.
- `string? ErrorMessage`: An error message, if the API call failed.
- `long DurationMs`: The duration of the API call in milliseconds.
- `ExternalApiClient`: A property that returns an instance of `ExternalApiClient`.
- `async Task<ApiCallResult> CallAsync`: Makes an asynchronous API call and returns the result.
- `async Task<T?> CallAsync<T>`: Makes an asynchronous API call and returns the result deserialized to type `T`.

## Usage
Here are two examples of using the `IExternalApiClient` interface:
```csharp
// Example 1: Making a simple API call
var client = new ExternalApiClient();
var result = await client.CallAsync();
if (result.IsSuccess)
{
    Console.WriteLine($"API call successful with status code {result.StatusCode}");
}
else
{
    Console.WriteLine($"API call failed with error message {result.ErrorMessage}");
}

// Example 2: Making an API call with deserialization
var client = new ExternalApiClient();
var userData = await client.CallAsync<UserData>();
if (userData != null)
{
    Console.WriteLine($"User data: {userData.Name} {userData.Email}");
}
else
{
    Console.WriteLine("Failed to retrieve user data");
}
```

## Notes
When using the `IExternalApiClient` interface, consider the following:
- The `CallAsync` methods are asynchronous and may throw exceptions if the underlying API call fails.
- The `DurationMs` property provides a way to measure the performance of API calls.
- The `ExternalApiClient` property allows for the retrieval of the underlying client instance.
- The `IExternalApiClient` interface is designed to be thread-safe, but the implementation of the `ExternalApiClient` class should also ensure thread-safety to avoid issues with concurrent access.
- Edge cases, such as network errors or API rate limiting, should be handled by the implementation of the `ExternalApiClient` class.
- The `CallAsync<T>` method will return `null` if the API call fails or if the response cannot be deserialized to type `T`.
