#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;

namespace DotnetOutboxPattern.Configuration;

/// <summary>
/// Extension methods for registering outbox pattern services in dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds outbox pattern services to the dependency injection container
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to</param>
    /// <param name="connectionString">The database connection string</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="connectionString"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is empty or whitespace</exception>
    public static IServiceCollection AddOutboxPattern(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

        // Configure database context with retry and timeout policies
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            }));

        // Register repositories
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();

        // Dependencies of the services below: without them nothing that touches
        // IOutboxService or IMessagePublishingService can be resolved at all.
        // TryAdd keeps any caller-provided implementation in place.
        services.TryAddSingleton<IOutboxSerializer, SystemTextJsonOutboxSerializer>();
        services.TryAddSingleton(_ => new PublishingOptions());
        services.TryAddSingleton(_ => new OutboxRetryOptions());

        // Register services
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IMessagePublishingService, MessagePublishingService>();
        services.AddScoped<IDeadLetterService, DeadLetterService>();

        return services;
    }

    /// <summary>
    /// Adds a custom message publisher implementation
    /// </summary>
    /// <typeparam name="TPublisher">The type of message publisher to register</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection AddMessagePublisher<TPublisher>(this IServiceCollection services)
        where TPublisher : class, IMessagePublisher
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IMessagePublisher, TPublisher>();
        return services;
    }

    /// <summary>
    /// Adds a factory function for message publisher
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to</param>
    /// <param name="factory">The factory function that creates the message publisher</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="factory"/> is null</exception>
    public static IServiceCollection AddMessagePublisher(
        this IServiceCollection services,
        Func<IServiceProvider, IMessagePublisher> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.AddScoped(factory);
        return services;
    }

    /// <summary>
    /// Initializes the database schema by applying pending migrations
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when required services cannot be resolved</exception>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        await context.Database.MigrateAsync();
    }
}