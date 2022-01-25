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
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
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
    public const string OutboxMessagePrefix = "outbox:message:";
    public const string OutboxStatsPrefix = "outbox:stats:";
    public const string WebhookPrefix = "webhook:";
    public const string DeadLetterPrefix = "deadletter:";

    public static string BuildMessageKey(Guid messageId) => $"{OutboxMessagePrefix}{messageId}";
    public static string BuildStatsKey(string aggregateType) => $"{OutboxStatsPrefix}{aggregateType}";
    public static string BuildWebhookKey(Guid webhookId) => $"{WebhookPrefix}{webhookId}";
    public static string BuildDeadLetterKey(Guid deadLetterId) => $"{DeadLetterPrefix}{deadLetterId}";

    public static string GetPrefix(string fullKey)
    {
        ArgumentNullException.ThrowIfNull(fullKey);

        var colonIndex = fullKey.LastIndexOf(':');
        return colonIndex > 0 ? fullKey[..(colonIndex + 1)] : fullKey;
    }
}
