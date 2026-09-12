# Program.cs

`Program.cs` is the ASP.NET Core application entry point. It creates the host, registers the outbox and HTTP services, applies database migrations, builds the request pipeline, and starts the web server and background outbox processor.

## Startup sequence

Startup proceeds in this order:

1. `WebApplication.CreateBuilder(args)` creates the application builder and loads the standard ASP.NET Core configuration sources, including `appsettings.json`, environment-specific JSON, environment variables, and command-line arguments.
2. A global Serilog logger is created with an `Information` minimum level and the console sink. `builder.Host.UseSerilog()` makes it the host logger. This setup is constructed in code; the `Serilog` section in `appsettings.json` is not read by `Program.cs`.
3. The startup `try` block logs `Starting Outbox Pattern application`.
4. `ConnectionStrings:DefaultConnection` is read. Startup throws `InvalidOperationException` if it is absent.
5. The `Outbox` configuration section is bound once to `DotnetOutboxPatternOptions` and separately to `OutboxProcessorOptions`. Data-annotation validation is enabled for `OutboxProcessorOptions`.
6. Core outbox services, Phase 2 services, the default publisher, the hosted processor, MVC controllers, and API endpoint metadata services are registered.
7. `builder.Build()` creates the `WebApplication` and its service provider.
8. `InitializeDatabaseAsync()` creates a scope, resolves `OutboxDbContext`, and calls EF Core's `Database.MigrateAsync()` before the request pipeline starts.
9. The custom outbox middleware, HTTPS redirection, authorization, controller endpoints, Prometheus endpoint, and `/health` endpoint are added in the order described below.
10. `Application started successfully` is logged and `RunAsync()` starts the host. The registered `OutboxProcessor` runs as a hosted background service while the host is running.
11. Any exception from steps inside the `try` block is logged at `Fatal` as `Application terminated unexpectedly`. The exception is not rethrown. Serilog is flushed and closed in `finally`.

## Registered services

In addition to framework services installed by `WebApplication.CreateBuilder`, `Program.cs` registers the following application services.

### Configuration and processing

| Registration | Lifetime or mechanism | Implementation/details |
| --- | --- | --- |
| `IOptions<DotnetOutboxPatternOptions>` | Options | Bound from `Outbox` |
| `IOptions<OutboxProcessorOptions>` | Options | Bound from `Outbox` with data-annotation validation |
| `IOutboxProcessorOptions` | Singleton | The current `OutboxProcessorOptions` options value |
| `OutboxProcessor` | Hosted service | Polls and publishes pending and scheduled messages |
| `IMessagePublisher` | Scoped | `DefaultMessagePublisher` |

The two option classes are distinct. In particular, `DotnetOutboxPatternOptions` exposes `ProcessorEnabled`, while `OutboxProcessorOptions` exposes `Enabled`; both are bound from the same `Outbox` section. Consequently, the sample `Outbox:ProcessorEnabled` setting does not bind to `OutboxProcessorOptions.Enabled`, which retains its default value of `true` unless an `Outbox:Enabled` value is supplied.

### Core outbox registrations

`AddOutboxPattern(connectionString)` adds:

| Service | Lifetime | Implementation/details |
| --- | --- | --- |
| `OutboxDbContext` | Scoped | SQL Server provider; retries up to 3 times with a 5-second maximum retry delay and a 30-second command timeout |
| `IOutboxRepository` | Scoped | `OutboxRepository` |
| `IDeadLetterRepository` | Scoped | `DeadLetterRepository` |
| `IOutboxSerializer` | Singleton, if absent | `SystemTextJsonOutboxSerializer` |
| `IOutboxTypeResolver` | Singleton, if absent | `DefaultOutboxTypeResolver` |
| `PublishingOptions` | Singleton, if absent | Default instance |
| `OutboxRetryOptions` | Singleton, if absent | Default instance |
| `IOutboxService` | Scoped | `OutboxService` |
| `IMessagePublishingService` | Scoped | `MessagePublishingService` |
| `IDeadLetterService` | Scoped | `DeadLetterService` |
| `IOutboxMessageDeserializer` | Scoped | `OutboxMessageDeserializer` |

### Phase 2 registrations

`AddOutboxPatternPhase2()` adds:

| Service | Lifetime | Implementation/details |
| --- | --- | --- |
| `IMetricsService` | Scoped | `MetricsService` |
| `IWebhookService` | Scoped | `WebhookService` |
| `IWebhookHandler` | Scoped | `WebhookHandler` |
| `ICacheService` | Singleton | `MemoryCacheService` |
| `MessageArchivalOptions` | Singleton | Default instance |
| `HealthCheckOptions` | Singleton | Default instance |
| `IMessageSearchService` | Scoped | `MessageSearchService` |
| `INotificationService` | Scoped | `NotificationService` |
| `IEventPublisher` | Singleton | `EventPublisher` |
| `IOutboxSerializer` | Singleton | `SystemTextJsonOutboxSerializer` |
| Custom `IHttpClientFactory` | Singleton | `CustomHttpClientFactory` from the integration namespace |
| `ResilientHttpClient` | Typed HTTP client | Registered through `AddHttpClient` |
| `IExternalApiClient` | Scoped | `ExternalApiClient` |
| `IDataFormatter` | Scoped, three registrations | `JsonFormatter`, `CsvFormatter`, and `XmlFormatter` |
| `CliCommandRegistry` | Scoped | Concrete registration |
| `PerformanceMonitor` | Singleton | Concrete registration |
| `OutboxMetrics` | Singleton | Concrete registration |

It also configures OpenTelemetry with service name `dotnet-outbox-pattern`, subscribes to meter `DotnetOutboxPattern.Outbox`, and adds the Prometheus exporter. Because Phase 2 runs after the core registrations, its unconditional `IOutboxSerializer` registration is the implementation resolved by default.

Finally, `AddControllers()` registers MVC controller services, and `AddEndpointsApiExplorer()` registers endpoint metadata services used by API-description tooling. `Program.cs` does not register authentication, an authorization policy, Swagger, or the built-in ASP.NET Core health-check services.

## Middleware and endpoints

Middleware is added in this order, so requests enter from top to bottom and responses unwind in reverse order:

1. `ErrorHandlingMiddleware` catches otherwise-unhandled exceptions, logs them, and writes the repository's JSON error response.
2. `RequestLoggingMiddleware` logs requests and response status/timing; it buffers POST and PUT request bodies and captures response bodies.
3. `RateLimitingMiddleware` identifies callers by `X-Api-Key`, falling back to remote IP. Because `Program.cs` passes no options, it uses 1,000 requests per 60-second window.
4. `PerformanceMonitoringMiddleware` records request duration and status and warns for requests longer than five seconds.
5. ASP.NET Core HTTPS redirection.
6. ASP.NET Core authorization.

Endpoint mappings then add:

- Attribute-routed MVC controllers through `MapControllers()`.
- The Prometheus scrape endpoint through `MapPrometheusScrapingEndpoint()`; its default path is `/metrics`.
- `GET /health`, named `Health`. It resolves `IOutboxService`, calls `GetStatisticsAsync()`, and returns an object containing `status: "healthy"`, the current UTC timestamp, and the returned statistics.

`Program.cs` contains no environment-specific pipeline branches and does not add static files, routing explicitly, CORS, authentication, or Swagger middleware.

### Current startup limitation

As currently implemented, the parameterless `UsePerformanceMonitoring()` extension tries to resolve `IServiceCollection` from `app.ApplicationServices`. The built service provider does not register `IServiceCollection`, so this call throws during pipeline construction in a normal run. The outer `catch` logs the fatal exception and the host does not reach `RunAsync()`. This document records that behavior; it does not change it.

## Configuration required to run

The project targets .NET 10. `global.json` requests SDK `10.0.100` and permits roll-forward to the latest installed minor SDK. A reachable SQL Server is also required because database migration runs before the HTTP host starts.

The checked-in `appsettings.json` uses Windows LocalDB:

```text
Server=(localdb)\mssqllocaldb;Database=OutboxPattern;Trusted_Connection=true;
```

Use that as-is only where LocalDB is available. Otherwise override `ConnectionStrings:DefaultConnection`. ASP.NET Core maps nested configuration keys to double-underscore environment-variable names, for example:

```bash
export ConnectionStrings__DefaultConnection='Server=localhost,1433;Database=OutboxPattern;User Id=sa;Password=YourStrongPassword123!;Encrypt=false;TrustServerCertificate=true'
```

## Run locally

From the repository root:

```bash
dotnet restore DotnetOutboxPattern.csproj
dotnet run --project DotnetOutboxPattern.csproj
```

`make run` is the repository's equivalent convenience target; it restores, builds in `Release`, and runs the project.

The listening URL is not fixed in `Program.cs`. Set it with standard ASP.NET Core configuration when needed:

```bash
ASPNETCORE_URLS=http://localhost:8080 dotnet run --project DotnetOutboxPattern.csproj
```

Subject to the current startup limitation above, the mapped probes are then available at `http://localhost:8080/health` and `http://localhost:8080/metrics`. HTTPS redirection remains in the pipeline even when only an HTTP URL is configured.

## Run with Docker Compose

The production compose file starts SQL Server, the API, and Redis, although `Program.cs` does not register or use Redis. It supplies the API's SQL Server connection string and binds host port 8080 to container port 8080.

```bash
docker compose up --build
```

The default SQL Server password in `docker-compose.yml` can be replaced by setting `SA_PASSWORD` before starting the stack. The same current middleware startup limitation applies inside the container.

The development compose file is not a complete run command by itself: its API service targets the Dockerfile's `build` stage, mounts the source at `/src`, and has no command that launches the application. It also does not override the LocalDB connection string from `appsettings.json`.
