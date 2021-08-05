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
        _ = CleanupExpiredEntriesAsync();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    entry.AccessCount++;
                    return (T?)entry.Value;
                }

                _cache.Remove(key);
            }
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
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
    }

    public async Task RemoveAsync(string key)
    {
        lock (_lock)
        {
            _cache.Remove(key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        lock (_lock)
        {
            var keysToRemove = _cache.Keys
                .Where(k => k.StartsWith(prefix))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            _logger.LogInformation("Removed {Count} cache entries with prefix: {Prefix}",
                keysToRemove.Count, prefix);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
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

    private class CacheEntry
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
        var colonIndex = fullKey.LastIndexOf(':');
        return colonIndex > 0 ? fullKey.Substring(0, colonIndex + 1) : fullKey;
    }
}
