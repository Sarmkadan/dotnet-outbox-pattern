// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    public static IServiceCollection AddOutboxPattern(
        this IServiceCollection services,
        string connectionString,
        Action<PublishingOptions>? configureOptions = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

        // Configure database context
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                sqlOptions.CommandTimeout(30);
            }));

        // Configure publishing options
        var publishingOptions = new PublishingOptions();
        configureOptions?.Invoke(publishingOptions);
        services.AddSingleton(publishingOptions);

        // Register repositories
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();

        // Register services
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IMessagePublishingService, MessagePublishingService>();
        services.AddScoped<IDeadLetterService, DeadLetterService>();

        return services;
    }

    /// <summary>
    /// Adds a custom message publisher implementation
    /// </summary>
    public static IServiceCollection AddMessagePublisher<TPublisher>(this IServiceCollection services)
        where TPublisher : class, IMessagePublisher
    {
        services.AddScoped<IMessagePublisher, TPublisher>();
        return services;
    }

    /// <summary>
    /// Adds a factory function for message publisher
    /// </summary>
    public static IServiceCollection AddMessagePublisher(
        this IServiceCollection services,
        Func<IServiceProvider, IMessagePublisher> factory)
    {
        services.AddScoped(factory);
        return services;
    }

    /// <summary>
    /// Initializes the database schema
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Configures publishing options
    /// </summary>
    public static IServiceCollection ConfigurePublishingOptions(
        this IServiceCollection services,
        Action<PublishingOptions> configure)
    {
        var options = new PublishingOptions();
        configure(options);
        services.AddSingleton(options);
        return services;
    }
}
