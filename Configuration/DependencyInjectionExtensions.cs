#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.BackgroundServices;
using DotnetOutboxPattern.Caching;
using DotnetOutboxPattern.CLI;
using DotnetOutboxPattern.Events;
using DotnetOutboxPattern.Formatters;
using DotnetOutboxPattern.Integration;
using DotnetOutboxPattern.Middleware;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;

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

        // Add HTTP client factory
        services.AddSingleton<DotnetOutboxPattern.Integration.IHttpClientFactory, CustomHttpClientFactory>();

        // Add external API client with resilience
        services.AddScoped<ResilientHttpClient>();
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
    public static IServiceCollection AddOutboxOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("dotnet-outbox-pattern"))
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("DotnetOutboxPattern.Outbox") // The name of our custom meter
                    .AddPrometheusExporter();
            });

        return services;
    }

    /// <summary>
    /// Adds middleware for Phase 2 features
    /// Call this in Program.cs after app.Build()
    /// </summary>
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
    public static IServiceCollection AddDataFormatter<T>(this IServiceCollection services)
        where T : class, IDataFormatter
    {
        services.AddScoped<IDataFormatter, T>();
        return services;
    }

    /// <summary>
    /// Configures rate limiting options
    /// </summary>
    public static IServiceCollection ConfigureRateLimiting(
        this IServiceCollection services,
        int requestsPerWindow = 1000,
        int windowSeconds = 60)
    {
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
    public static IServiceCollection ConfigureMessageArchival(
        this IServiceCollection services,
        int archiveDaysThreshold = 30,
        int archivalIntervalHours = 6,
        int batchSize = 5000)
    {
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
    public static IServiceCollection ConfigureHealthCheck(
        this IServiceCollection services,
        double failureRateThreshold = 0.10,
        int stuckMessageThreshold = 100,
        int deadLetterThreshold = 50)
    {
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
    public static IServiceCollection AddExternalHttpClients(
        this IServiceCollection services,
        params (string Name, HttpClientConfig Config)[] clients)
    {
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
