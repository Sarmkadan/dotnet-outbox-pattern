#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetOutboxPattern.Configuration;

/// <summary>
/// Extension methods for registering batch processing services into the dependency injection container
/// </summary>
public static class BatchProcessingExtensions
{
    /// <summary>
    /// Adds <see cref="IBatchProcessingService"/> with default <see cref="BatchProcessingOptions"/>
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The same <paramref name="services"/> for chaining</returns>
    public static IServiceCollection AddBatchProcessing(this IServiceCollection services)
        => services.AddBatchProcessing(new BatchProcessingOptions());

    /// <summary>
    /// Adds <see cref="IBatchProcessingService"/> with the supplied options instance
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="options">Pre-configured options to register as a singleton</param>
    /// <returns>The same <paramref name="services"/> for chaining</returns>
    public static IServiceCollection AddBatchProcessing(
        this IServiceCollection services,
        BatchProcessingOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.AddScoped<IBatchProcessingService, BatchProcessingService>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IBatchProcessingService"/> with options configured via a delegate
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configure">Delegate that receives and mutates a <see cref="BatchProcessingOptions"/> instance</param>
    /// <returns>The same <paramref name="services"/> for chaining</returns>
    public static IServiceCollection AddBatchProcessing(
        this IServiceCollection services,
        Action<BatchProcessingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BatchProcessingOptions();
        configure(options);

        return services.AddBatchProcessing(options);
    }

    /// <summary>
    /// Configures batch processing to split messages into chunks of the specified size
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="chunkSize">Maximum number of messages per chunk; must be greater than zero</param>
    /// <returns>The same <paramref name="services"/> for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="chunkSize"/> is not positive</exception>
    public static IServiceCollection WithChunkSize(this IServiceCollection services, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero");

        GetOrAddOptions(services).ChunkSize = chunkSize;
        return services;
    }

    /// <summary>
    /// Enables concurrent chunk processing with bounded parallelism
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="maxConcurrency">Maximum number of chunks to run simultaneously; must be greater than zero</param>
    /// <returns>The same <paramref name="services"/> for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxConcurrency"/> is not positive</exception>
    public static IServiceCollection WithParallelChunks(this IServiceCollection services, int maxConcurrency = 2)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Concurrency level must be greater than zero");

        var options = GetOrAddOptions(services);
        options.EnableParallelChunks = true;
        options.MaxParallelChunks = maxConcurrency;

        return services;
    }

    /// <summary>
    /// Inserts a delay between sequential chunks to pace downstream throughput
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="milliseconds">Milliseconds to wait after each chunk; must be non-negative</param>
    /// <returns>The same <paramref name="services"/> for chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="milliseconds"/> is negative</exception>
    public static IServiceCollection WithDelayBetweenChunks(this IServiceCollection services, int milliseconds)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "Delay cannot be negative");

        GetOrAddOptions(services).DelayBetweenChunksMs = milliseconds;
        return services;
    }

    /// <summary>
    /// Configures sequential batch processing to abort remaining chunks after the first chunk failure
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The same <paramref name="services"/> for chaining</returns>
    public static IServiceCollection StopBatchOnChunkFailure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        GetOrAddOptions(services).StopOnChunkFailure = true;
        return services;
    }

    private static BatchProcessingOptions GetOrAddOptions(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(BatchProcessingOptions));

        if (descriptor?.ImplementationInstance is BatchProcessingOptions existing)
            return existing;

        var options = new BatchProcessingOptions();
        services.AddSingleton(options);
        return options;
    }
}
