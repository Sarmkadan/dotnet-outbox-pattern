# DependencyInjectionExtensions

The `DependencyInjectionExtensions` class provides a set of extension methods for `IServiceCollection` and `IApplicationBuilder` that simplify the integration of the outbox pattern into an ASP.NET Core application. These methods encapsulate the registration of services, middleware, telemetry, health checks, HTTP clients, and configuration options required for a complete outbox processing pipeline. All methods are designed to be called during application startup and follow the standard convention of returning the same collection or builder to enable chaining.

## API

### `AddOutboxPatternPhase2`
```csharp
public static IServiceCollection AddOutboxPatternPhase2(this IServiceCollection services)
```
Registers the services required for the second phase of the outbox pattern (typically the processing of outbox messages from the database and dispatching them to the message broker).  
**Parameters:**  
- `services` – The `IServiceCollection` to add services to.  

**Returns:** The same `IServiceCollection` instance for chaining.  
**Throws:** `ArgumentNullException` if `services` is `null`.

### `AddOutboxOpenTelemetry`
```csharp
public static IServiceCollection AddOutboxOpenTelemetry(this IServiceCollection services)
```
Adds OpenTelemetry instrumentation for outbox-related operations, enabling distributed tracing and metrics collection.  
**Parameters:**  
- `services` – The `IServiceCollection` to add OpenTelemetry services to.  

**Returns:** The same `IServiceCollection` instance for chaining.  
**Throws:** `ArgumentNullException` if `services` is `null`.

### `UseOutboxPatternMiddleware`
```csharp
public static IApplicationBuilder UseOutboxPatternMiddleware(this IApplicationBuilder app)
```
Adds the outbox pattern middleware to the ASP.NET Core request pipeline. This middleware typically intercepts outgoing HTTP requests or responses to capture messages for the outbox.  
**Parameters:**  
- `app` – The `IApplicationBuilder` to configure.  

**Returns:** The same `IApplicationBuilder` instance for chaining.  
**Throws:** `ArgumentNullException` if `app` is `null`.

### `AddDataFormatter<T>`
```csharp
public static IServiceCollection AddDataFormatter<T>(this IServiceCollection services)
    where T : class
```
Registers a custom data formatter of type `T` that controls how outbox messages are serialized or deserialized. The formatter must implement the expected interface (e.g., `IDataFormatter`).  
**Parameters:**  
- `T` – The concrete type of the data formatter.  
- `services` – The `IServiceCollection` to register the formatter in.  

**Returns:** The same `IServiceCollection` instance for chaining.  
**Throws:** `ArgumentNullException` if `services` is `null`.

### `ConfigureRateLimiting`
```csharp
public static IServiceCollection ConfigureRateLimiting(this IServiceCollection services)
```
Configures rate-limiting policies for outbox message processing, preventing excessive throughput that could overwhelm downstream systems.  
**Parameters:**  
- `services` – The `IServiceCollection` to configure rate limiting on.  

**Returns:** The same `IServiceCollection` instance for chaining.  
**Throws:** `ArgumentNullException` if `services` is `null`.

### `ConfigureMessageArchival`
```csharp
public static IServiceCollection ConfigureMessageArchival(this IServiceCollection services)
```
Sets up message archival logic, moving processed or failed outbox messages to a long-term storage location (e.g., a separate table or blob storage).  
**Parameters:**  
- `services` – The `IServiceCollection` to configure archival on.  

**Returns:** The same `IServiceCollection` instance for chaining.  
**Throws:** `ArgumentNullException` if `services` is `null`.

### `ConfigureHealthCheck`
```csharp
public static IServiceCollection ConfigureHealthCheck(this IServiceCollection services)
```
Adds health check endpoints that monitor the health of the outbox system, including database connectivity and message processing status.  
**Parameters:**  
- `services` – The `IServiceCollection` to add health checks to.  

**Returns:** The same `IServiceCollection` instance for chaining.  
**Throws:** `ArgumentNullException` if `services` is `null`.

### `AddExternalHttpClients`
```csharp
public static IServiceCollection AddExternalHttpClients(this IServiceCollection services)
```
Registers typed `HttpClient` instances for communicating with external services that the outbox pattern relies on (e.g., message brokers, downstream APIs).  
**Parameters:**  
- `services` – The `IServiceCollection` to add HTTP clients to.  

**Returns:** The same `IServiceCollection` instance for chaining.  
**Throws:** `ArgumentNullException` if `services` is `null`.

## Usage

### Example 1: Basic outbox pattern setup with telemetry and health checks
```csharp
using OutboxPattern;

var builder = WebApplication.CreateBuilder(args);

// Register core outbox services
builder.Services.AddOutboxPatternPhase2();

// Add OpenTelemetry for monitoring
builder.Services.AddOutboxOpenTelemetry();

// Configure health checks
builder.Services.ConfigureHealthCheck();

// Register a custom data formatter
builder.Services.AddDataFormatter<JsonDataFormatter>();

// Add external HTTP clients
builder.Services.AddExternalHttpClients();

var app = builder.Build();

// Insert the outbox middleware into the pipeline
app.UseOutboxPatternMiddleware();

app.Run();
```

### Example 2: Full configuration with rate limiting and message archival
```csharp
using OutboxPattern;

var builder = WebApplication.CreateBuilder(args);

// Phase 2 services
builder.Services.AddOutboxPatternPhase2();

// Rate limiting to control processing speed
builder.Services.ConfigureRateLimiting();

// Archive processed messages after 24 hours
builder.Services.ConfigureMessageArchival();

// Register a custom formatter for Avro serialization
builder.Services.AddDataFormatter<AvroDataFormatter>();

var app = builder.Build();

app.UseOutboxPatternMiddleware();

app.Run();
```

## Notes

- **Thread safety:** The extension methods on `IServiceCollection` and `IApplicationBuilder` are intended to be called only during application startup, from a single thread. They are not thread-safe and should not be invoked concurrently. The middleware added by `UseOutboxPatternMiddleware` is designed to be thread-safe for concurrent HTTP requests.
- **Order of registration:** While most methods are independent, `AddOutboxPatternPhase2` should be called before any method that depends on its services (e.g., `ConfigureRateLimiting` or `ConfigureMessageArchival`). `UseOutboxPatternMiddleware` must be called after all service registrations are complete.
- **Duplicate calls:** Calling any of these methods multiple times may result in duplicate service registrations or configuration overrides. It is recommended to call each method exactly once.
- **Null arguments:** All methods throw `ArgumentNullException` if the provided `IServiceCollection` or `IApplicationBuilder` is `null`.
- **Data formatter constraints:** `AddDataFormatter<T>` requires `T` to be a non-abstract class. The type must be resolvable by the dependency injection container; if it has dependencies, they must be registered beforehand.
- **External HTTP clients:** `AddExternalHttpClients` registers clients with default settings. To customize client configuration (e.g., base address, timeouts), use the standard `IHttpClientFactory` patterns after calling this method.
