#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotnetOutboxPattern.BackgroundServices;
using DotnetOutboxPattern.Caching;
using DotnetOutboxPattern.CLI;
using DotnetOutboxPattern.Events;
using DotnetOutboxPattern.Formatters;
using DotnetOutboxPattern.Integration;
using DotnetOutboxPattern.Middleware;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace DotnetOutboxPattern.Configuration;

/// <summary>
/// Extension methods for registering Phase 2 services into the dependency injection container
/// Simplifies configuration of all middleware, services, and utilities
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds all Phase 2 features to the service collection
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection AddOutboxPatternPhase2(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add metrics service
        services.AddScoped<IMetricsService, MetricsService>();

        // Add webhook infrastructure
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IWebhookHandler, WebhookHandler>();

        // Add caching
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Add background services options
        services.AddSingleton<MessageArchivalOptions>();
        services.AddSingleton<HealthCheckOptions>();

        // Add search service
        services.AddScoped<IMessageSearchService, MessageSearchService>();

        // Add notification service
        services.AddScoped<INotificationService, NotificationService>();

        // Add event publisher
        services.AddSingleton<IEventPublisher, EventPublisher>();

        // Register the pluggable outbox serializer
        services.AddSingleton<IOutboxSerializer, SystemTextJsonOutboxSerializer>();

        // Add HTTP client factory
        services.AddSingleton<DotnetOutboxPattern.Integration.IHttpClientFactory, CustomHttpClientFactory>();

        // Add external API client with resilience. ResilientHttpClient depends on a
        // plain HttpClient, so it must be registered through AddHttpClient rather than
        // a bare AddScoped, otherwise DI cannot construct it.
        services.AddHttpClient<ResilientHttpClient>();
        services.AddScoped<IExternalApiClient, ExternalApiClient>();

        // Add formatters
        services.AddScoped<IDataFormatter, JsonFormatter>();
        services.AddScoped<IDataFormatter, CsvFormatter>();
        services.AddScoped<IDataFormatter, XmlFormatter>();

        // Add CLI command registry
        services.AddScoped<CliCommandRegistry>();

        // Add performance monitor for metrics
        services.AddSingleton<PerformanceMonitor>();

        // Add OpenTelemetry metrics
        services.AddOutboxOpenTelemetry();
        services.AddSingleton<OutboxMetrics>();

        return services;
    }

    /// <summary>
    /// Configures OpenTelemetry for the outbox pattern.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection AddOutboxOpenTelemetry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("dotnet-outbox-pattern"))
            .WithMetrics(builder =>
            {
                builder
                    .AddMeter("DotnetOutboxPattern.Outbox") // The name of our custom meter
                    .AddPrometheusExporter();
            });

        return services;
    }

    /// <summary>
    /// Adds middleware for Phase 2 features
    /// Call this in Program.cs after app.Build()
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="rateLimitingOptions">Optional rate limiting options</param>
    /// <returns>The configured application builder</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null</exception>
    public static IApplicationBuilder UseOutboxPatternMiddleware(
        this IApplicationBuilder app,
        RateLimitingOptions? rateLimitingOptions = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Add in specific order - error handling should be first, logging should be early
        app.UseErrorHandling();
        app.UseRequestLogging();
        app.UseRateLimiting(rateLimitingOptions);
        app.UsePerformanceMonitoring();

        return app;
    }

    /// <summary>
    /// Registers a specific data formatter for export
    /// </summary>
    /// <typeparam name="T">The formatter type to register</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection AddDataFormatter<T>(this IServiceCollection services)
        where T : class, IDataFormatter
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDataFormatter, T>();
        return services;
    }

    /// <summary>
    /// Configures rate limiting options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="requestsPerWindow">Maximum requests per time window</param>
    /// <param name="windowSeconds">Time window in seconds</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection ConfigureRateLimiting(
        this IServiceCollection services,
        int requestsPerWindow = 1000,
        int windowSeconds = 60)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<RateLimitingOptions>(options =>
        {
            options.RequestsPerWindow = requestsPerWindow;
            options.WindowSeconds = windowSeconds;
        });

        return services;
    }

    /// <summary>
    /// Configures message archival options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="archiveDaysThreshold">Number of days after which messages are archived</param>
    /// <param name="archivalIntervalHours">Interval between archival runs in hours</param>
    /// <param name="batchSize">Number of messages to process in each batch</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection ConfigureMessageArchival(
        this IServiceCollection services,
        int archiveDaysThreshold = 30,
        int archivalIntervalHours = 6,
        int batchSize = 5000)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<MessageArchivalOptions>(options =>
        {
            options.ArchiveDaysThreshold = archiveDaysThreshold;
            options.ArchivalIntervalMs = archivalIntervalHours * 60 * 60 * 1000;
            options.BatchSize = batchSize;
        });

        return services;
    }

    /// <summary>
    /// Configures health check options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="failureRateThreshold">Failure rate threshold for health checks</param>
    /// <param name="stuckMessageThreshold">Threshold for stuck messages</param>
    /// <param name="deadLetterThreshold">Threshold for dead letter messages</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection ConfigureHealthCheck(
        this IServiceCollection services,
        double failureRateThreshold = 0.10,
        int stuckMessageThreshold = 100,
        int deadLetterThreshold = 50)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<HealthCheckOptions>(options =>
        {
            options.HighFailureRateThreshold = failureRateThreshold;
            options.StuckMessageThreshold = stuckMessageThreshold;
            options.DeadLetterThreshold = deadLetterThreshold;
        });

        return services;
    }

    /// <summary>
    /// Registers HTTP clients for external integrations
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="clients">Collection of named HTTP client configurations</param>
    /// <returns>The configured service collection</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clients"/> is null</exception>
    public static IServiceCollection AddExternalHttpClients(
        this IServiceCollection services,
        params (string Name, HttpClientConfig Config)[] clients)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(clients);

        var factory = new CustomHttpClientFactory(
            new LoggerFactory().CreateLogger<CustomHttpClientFactory>());

        foreach (var (name, config) in clients)
        {
            var client = factory.CreateClient(name, config);
            services.AddScoped(_ => client);
        }

        return services;
    }
}
