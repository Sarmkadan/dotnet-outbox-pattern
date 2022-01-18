#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Extension methods for <see cref="BatchProcessingOptions"/> to provide common batch configuration scenarios
/// </summary>
public static class BatchProcessingOptionsExtensions
{
    /// <summary>
    /// Configures the batch to process all available messages in the outbox
    /// </summary>
    /// <param name="options">The batch processing options to configure</param>
    /// <returns>The configured options for method chaining</returns>
    public static BatchProcessingOptions WithTotalBatchSize(this BatchProcessingOptions options, int totalBatchSize)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.TotalBatchSize = totalBatchSize;
        return options;
    }

    /// <summary>
    /// Configures the batch to process messages in smaller chunks for reduced memory pressure
    /// </summary>
    /// <param name="options">The batch processing options to configure</param>
    /// <param name="chunkSize">Number of messages per chunk (must be positive)</param>
    /// <returns>The configured options for method chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when chunkSize is not positive</exception>
    public static BatchProcessingOptions WithChunkSize(this BatchProcessingOptions options, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero");
        }
        options.ChunkSize = chunkSize;
        return options;
    }

    /// <summary>
    /// Enables parallel chunk processing with the specified maximum concurrency level
    /// </summary>
    /// <param name="options">The batch processing options to configure</param>
    /// <param name="maxParallelChunks">Maximum number of chunks to process concurrently (must be positive)</param>
    /// <returns>The configured options for method chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxParallelChunks is not positive</exception>
    public static BatchProcessingOptions WithParallelChunks(this BatchProcessingOptions options, int maxParallelChunks = 2)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (maxParallelChunks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxParallelChunks), "Concurrency level must be greater than zero");
        }
        options.EnableParallelChunks = true;
        options.MaxParallelChunks = maxParallelChunks;
        return options;
    }

    /// <summary>
    /// Disables parallel chunk processing, forcing sequential execution
    /// </summary>
    /// <param name="options">The batch processing options to configure</param>
    /// <returns>The configured options for method chaining</returns>
    public static BatchProcessingOptions AsSequential(this BatchProcessingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.EnableParallelChunks = false;
        options.MaxParallelChunks = 1;
        return options;
    }

    /// <summary>
    /// Adds a delay between sequential chunks to pace downstream systems and prevent throttling
    /// </summary>
    /// <param name="options">The batch processing options to configure</param>
    /// <param name="milliseconds">Milliseconds to wait between chunks (must be non-negative)</param>
    /// <returns>The configured options for method chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when milliseconds is negative</exception>
    public static BatchProcessingOptions WithDelayBetweenChunks(this BatchProcessingOptions options, int milliseconds)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (milliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "Delay cannot be negative");
        }
        options.DelayBetweenChunksMs = milliseconds;
        return options;
    }

    /// <summary>
    /// Configures the batch to stop processing on the first chunk failure
    /// </summary>
    /// <param name="options">The batch processing options to configure</param>
    /// <returns>The configured options for method chaining</returns>
    public static BatchProcessingOptions StopOnFailure(this BatchProcessingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.StopOnChunkFailure = true;
        return options;
    }

    /// <summary>
    /// Calculates the total number of chunks that will be created for the configured batch size and chunk size
    /// </summary>
    /// <param name="options">The batch processing options</param>
    /// <returns>The total number of chunks that will be created</returns>
    public static int CalculateTotalChunks(this BatchProcessingOptions options)
        => CalculateChunks(options.TotalBatchSize, options.ChunkSize);

    /// <summary>
    /// Calculates the total number of chunks that will be created for a given total size and chunk size
    /// </summary>
    /// <param name="totalBatchSize">Total number of messages to process</param>
    /// <param name="chunkSize">Number of messages per chunk</param>
    /// <returns>The total number of chunks that will be created</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when totalBatchSize is negative or chunkSize is not positive</exception>
    public static int CalculateChunks(int totalBatchSize, int chunkSize)
    {
        if (totalBatchSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalBatchSize), "Total batch size cannot be negative");
        }
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero");
        }

        return (int)Math.Ceiling((double)totalBatchSize / chunkSize);
    }

    /// <summary>
    /// Gets the estimated memory usage in bytes for processing a single chunk
    /// Assumes each message consumes approximately 1KB of memory
    /// </summary>
    /// <param name="options">The batch processing options</param>
    /// <returns>Estimated memory usage in bytes for one chunk</returns>
    public static long GetChunkMemoryUsage(this BatchProcessingOptions options)
        => GetChunkMemoryUsage(options.ChunkSize);

    /// <summary>
    /// Gets the estimated memory usage in bytes for processing a single chunk with the specified size
    /// Assumes each message consumes approximately 1KB of memory
    /// </summary>
    /// <param name="chunkSize">Number of messages per chunk</param>
    /// <returns>Estimated memory usage in bytes for one chunk</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when chunkSize is not positive</exception>
    public static long GetChunkMemoryUsage(int chunkSize)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero");
        }

        // Estimate: 1KB per message
        return chunkSize * 1024L;
    }

    /// <summary>
    /// Gets the estimated total memory usage in bytes for the entire batch
    /// Assumes each message consumes approximately 1KB of memory
    /// </summary>
    /// <param name="options">The batch processing options</param>
    /// <returns>Estimated total memory usage in bytes for the entire batch</returns>
    public static long GetTotalBatchMemoryUsage(this BatchProcessingOptions options)
        => GetTotalBatchMemoryUsage(options.TotalBatchSize);

    /// <summary>
    /// Gets the estimated total memory usage in bytes for a batch of the specified size
    /// Assumes each message consumes approximately 1KB of memory
    /// </summary>
    /// <param name="totalBatchSize">Total number of messages to process</param>
    /// <returns>Estimated total memory usage in bytes for the entire batch</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when totalBatchSize is negative</exception>
    public static long GetTotalBatchMemoryUsage(int totalBatchSize)
    {
        if (totalBatchSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalBatchSize), "Total batch size cannot be negative");
        }

        // Estimate: 1KB per message
        return totalBatchSize * 1024L;
    }

    /// <summary>
    /// Validates that the batch processing configuration is valid
    /// </summary>
    /// <param name="options">The batch processing options to validate</param>
    /// <param name="throwOnInvalid">Whether to throw on validation failure</param>
    /// <returns>True if valid; false if invalid and throwOnInvalid is false</returns>
    /// <exception cref="InvalidOperationException">Thrown when throwOnInvalid is true and configuration is invalid</exception>
    public static bool Validate(this BatchProcessingOptions options, bool throwOnInvalid = false)
    {
        ArgumentNullException.ThrowIfNull(options);

        var isValid = true;
        var errors = new List<string>();

        if (options.TotalBatchSize < 0)
        {
            isValid = false;
            errors.Add($"{nameof(options.TotalBatchSize)} cannot be negative");
        }

        if (options.ChunkSize <= 0)
        {
            isValid = false;
            errors.Add($"{nameof(options.ChunkSize)} must be greater than zero");
        }

        if (options.MaxParallelChunks <= 0)
        {
            isValid = false;
            errors.Add($"{nameof(options.MaxParallelChunks)} must be greater than zero");
        }

        if (options.DelayBetweenChunksMs < 0)
        {
            isValid = false;
            errors.Add($"{nameof(options.DelayBetweenChunksMs)} cannot be negative");
        }

        if (!isValid && throwOnInvalid)
        {
            throw new InvalidOperationException(string.Join("; ", errors));
        }

        return isValid;
    }
}