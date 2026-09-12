#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Caching;

/// <summary>
/// Service interface for caching frequently accessed data
/// Reduces database load and improves response times
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns the cached value for the key, or the default value when it is absent or expired.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value, or the default value of <typeparamref name="T"/> when absent or expired.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Stores a value under the key with the given expiration.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiration">The expiration duration, or null to use the default.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a single cache entry.
    /// </summary>
    /// <param name="key">The cache key.</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes every cache entry whose key starts with the given prefix.
    /// </summary>
    /// <param name="prefix">The key prefix to match.</param>
    Task RemoveByPrefixAsync(string prefix);

    /// <summary>
    /// Returns the cached value for the key, invoking the factory and caching its result on a miss.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory invoked to produce the value on a cache miss.</param>
    /// <param name="expiration">The expiration duration, or null to use the default.</param>
    /// <returns>The cached value, or the value produced by <paramref name="factory"/>.</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
}

/// <summary>
/// In-memory cache implementation with TTL support
/// Suitable for distributed systems using cache invalidation
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly object _lock = new();
    private readonly ILogger<MemoryCacheService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheService"/> class and starts the background cleanup loop.
    /// </summary>
    /// <param name="logger">The logger used to report cache activity and failures.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is null.</exception>
    public MemoryCacheService(ILogger<MemoryCacheService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Failures inside the loop are already logged; this only reports a hard exit of the loop.
        _ = CleanupExpiredEntriesAsync().ContinueWith(
            t => _logger.LogError(t.Exception, "Cache cleanup loop terminated unexpectedly"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    /// <summary>
    /// Returns the cached value for the key, or the default value when it is absent or expired.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> is null or whitespace.</exception>
    public Task<T?> GetAsync<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    entry.AccessCount++;
                    return Task.FromResult(entry.Value is T typed ? typed : default);
                }

                _cache.Remove(key);
            }
        }

        return Task.FromResult<T?>(default);
    }

    /// <summary>
    /// Stores a value under the key with the given expiration (10 minutes by default).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> is null or whitespace.</exception>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (_lock)
        {
            var expiresAt = expiration.HasValue
                ? DateTime.UtcNow.Add(expiration.Value)
                : DateTime.UtcNow.AddMinutes(10);

            _cache[key] = new CacheEntry
            {
                Value = value,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                LastAccessed = DateTime.UtcNow,
                AccessCount = 0
            };

            if (_cache.Count % 1000 == 0)
            {
                _logger.LogInformation("Cache size: {Size}", _cache.Count);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a single cache entry.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> is null or whitespace.</exception>
    public Task RemoveAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (_lock)
        {
            _cache.Remove(key);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes every cache entry whose key starts with the given prefix.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="prefix"/> is null or whitespace.</exception>
    public Task RemoveByPrefixAsync(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        lock (_lock)
        {
            var keysToRemove = _cache.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            _logger.LogInformation("Removed {Count} cache entries with prefix: {Prefix}",
                keysToRemove.Count, prefix);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the cached value for the key, invoking the factory and caching its result on a miss.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="key"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await GetAsync<T>(key);
        if (cached is not null)
            return cached;

        var value = await factory();
        await SetAsync(key, value, expiration);
        return value;
    }

    private async Task CleanupExpiredEntriesAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5));

                lock (_lock)
                {
                    var now = DateTime.UtcNow;
                    var expiredKeys = _cache
                        .Where(kvp => kvp.Value.ExpiresAt <= now)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in expiredKeys)
                    {
                        _cache.Remove(key);
                    }

                    if (expiredKeys.Count > 0)
                    {
                        _logger.LogInformation("Cleaned up {Count} expired cache entries",
                            expiredKeys.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cache cleanup");
            }
        }
    }

    private sealed class CacheEntry
    {
        public object? Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
    }
}

/// <summary>
/// Cache key builder for consistent key generation
/// </summary>
public static class CacheKeyBuilder
{
    /// <summary>
    /// Prefix for outbox message cache keys.
    /// </summary>
    public const string OutboxMessagePrefix = "outbox:message:";
    /// <summary>
    /// Prefix for outbox statistics cache keys.
    /// </summary>
    public const string OutboxStatsPrefix = "outbox:stats:";
    /// <summary>
    /// Prefix for webhook cache keys.
    /// </summary>
    public const string WebhookPrefix = "webhook:";
    /// <summary>
    /// Prefix for dead letter cache keys.
    /// </summary>
    public const string DeadLetterPrefix = "deadletter:";

    /// <summary>
    /// Builds a cache key for an outbox message.
    /// </summary>
    /// <param name="messageId">The unique identifier of the outbox message.</param>
    /// <returns>A cache key for the specified outbox message.</returns>
    public static string BuildMessageKey(Guid messageId) => $"{OutboxMessagePrefix}{messageId}";
    /// <summary>
    /// Builds a cache key for outbox statistics.
    /// </summary>
    /// <param name="aggregateType">The aggregate type for which statistics are tracked.</param>
    /// <returns>A cache key for the specified outbox statistics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="aggregateType"/> is null.</exception>
    public static string BuildStatsKey(string aggregateType)
    {
        ArgumentNullException.ThrowIfNull(aggregateType);
        return $"{OutboxStatsPrefix}{aggregateType}";
    }
    /// <summary>
    /// Builds a cache key for a webhook.
    /// </summary>
    /// <param name="webhookId">The unique identifier of the webhook.</param>
    /// <returns>A cache key for the specified webhook.</returns>
    public static string BuildWebhookKey(Guid webhookId) => $"{WebhookPrefix}{webhookId}";
    /// <summary>
    /// Builds a cache key for a dead letter.
    /// </summary>
    /// <param name="deadLetterId">The unique identifier of the dead letter.</param>
    /// <returns>A cache key for the specified dead letter.</returns>
    public static string BuildDeadLetterKey(Guid deadLetterId) => $"{DeadLetterPrefix}{deadLetterId}";

    /// <summary>
    /// Extracts the prefix from a full cache key.
    /// </summary>
    /// <param name="fullKey">The full cache key.</param>
    /// <returns>The prefix portion of the cache key, or the full key if no colon is present.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fullKey"/> is null.</exception>
    public static string GetPrefix(string fullKey)
    {
        ArgumentNullException.ThrowIfNull(fullKey);

        var colonIndex = fullKey.LastIndexOf(':');
        return colonIndex > 0 ? fullKey[..(colonIndex + 1)] : fullKey;
    }
}
