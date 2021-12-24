# ICacheService

`ICacheService` defines the caching abstraction used throughout the outbox pattern implementation. It provides asynchronous methods to store, retrieve, and invalidate cached entries, along with a set of static key-building utilities that enforce consistent key naming conventions for message payloads, statistics, webhook deliveries, and dead-letter records. The service also exposes metadata properties on cached items—creation time, expiration, last access, and access count—enabling cache analytics and eviction monitoring.

## API

### `MemoryCacheService`

A concrete implementation of `ICacheService` backed by an in-process memory store. It is the default cache provider registered in the application's dependency injection container.

### `async Task<T?> GetAsync<T>(string key)`

Retrieves a cached value by its string key. Returns the deserialized object of type `T` if the key exists and has not expired; otherwise returns `null`. Throws `ArgumentNullException` when `key` is null or empty.

### `async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)`

Stores a value in the cache under the specified key. If `expiration` is provided, the entry is evicted after that duration; otherwise a default sliding or absolute expiration policy applies. Throws `ArgumentNullException` when `key` is null or empty.

### `async Task RemoveAsync(string key)`

Immediately removes the entry associated with the given key from the cache. No exception is thrown if the key does not exist. Throws `ArgumentNullException` when `key` is null or empty.

### `async Task RemoveByPrefixAsync(string prefix)`

Removes all cache entries whose keys start with the specified prefix. This is typically used to bulk-invalidate related entries (e.g., all keys for a given message type). Throws `ArgumentNullException` when `prefix` is null or empty.

### `async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)`

Atomically retrieves an existing cached value or, if the key is absent or expired, invokes the `factory` delegate, stores its result, and returns it. This prevents cache stampedes by ensuring the factory runs at most once per key. Throws `ArgumentNullException` when `key` or `factory` is null.

### `object? Value`

Gets the raw cached object associated with the current entry. Returns `null` if no entry is loaded or the entry has expired.

### `DateTime CreatedAt`

The UTC timestamp when the cached entry was originally created or last overwritten via `SetAsync` or `GetOrSetAsync`.

### `DateTime ExpiresAt`

The UTC timestamp at which the entry is scheduled for eviction. For entries without an explicit expiration, this may be set to a sentinel value representing no expiration.

### `DateTime LastAccessed`

The UTC timestamp of the most recent read via `GetAsync` or `GetOrSetAsync`. Updated on every cache hit.

### `int AccessCount`

The number of times the entry has been successfully retrieved from the cache since its creation. Incremented on each cache hit.

### `static string BuildMessageKey(string messageId)`

Constructs a standardized cache key for a message payload given its unique identifier. The returned string follows the convention `messages:{messageId}`.

### `static string BuildStatsKey(string metricName)`

Constructs a standardized cache key for a statistics or metrics entry. The returned string follows the convention `stats:{metricName}`.

### `static string BuildWebhookKey(string deliveryId)`

Constructs a standardized cache key for a webhook delivery record. The returned string follows the convention `webhooks:{deliveryId}`.

### `static string BuildDeadLetterKey(string messageId)`

Constructs a standardized cache key for a dead-letter entry associated with a failed message. The returned string follows the convention `deadletter:{messageId}`.

### `static string GetPrefix(string category)`

Returns the key prefix segment for a given logical category (e.g., `"messages"`, `"stats"`, `"webhooks"`, `"deadletter"`). Used internally by the other static builder methods and available for custom key construction that must adhere to the same naming scheme.

## Usage

### Retrieving a message with fallback to database

```csharp
var cacheKey = ICacheService.BuildMessageKey(outboxMessage.Id);
var payload = await cache.GetOrSetAsync(cacheKey, async () =>
{
    var message = await dbContext.OutboxMessages
        .AsNoTracking()
        .FirstOrDefaultAsync(m => m.Id == outboxMessage.Id);
    return message?.Payload;
}, TimeSpan.FromMinutes(5));

if (payload is null)
{
    // Message not found in cache or database
    return;
}

await ProcessPayload(payload);
```

### Bulk-invalidating webhook delivery entries after configuration change

```csharp
var prefix = ICacheService.GetPrefix("webhooks");
await cache.RemoveByPrefixAsync(prefix);

// Repopulate with updated delivery settings
foreach (var delivery in updatedDeliveries)
{
    var key = ICacheService.BuildWebhookKey(delivery.Id);
    await cache.SetAsync(key, delivery, TimeSpan.FromHours(1));
}
```

## Notes

- The `Value`, `CreatedAt`, `ExpiresAt`, `LastAccessed`, and `AccessCount` properties reflect the state of a single cached entry at the time it was last accessed. They are not global cache statistics.
- `GetOrSetAsync` guarantees that the factory delegate is executed at most once per logical key, even under concurrent requests. This is critical for avoiding redundant database queries or external calls during cache misses.
- `RemoveByPrefixAsync` may be implemented by iterating over keys in the underlying store. In memory-backed implementations this is fast; in distributed cache implementations it may require scanning or key-tagging strategies.
- The static key-building methods do not validate the uniqueness of their arguments. It is the caller's responsibility to supply identifiers that are unique within their category to avoid key collisions.
- All async methods are designed to be non-blocking and should be awaited. Calling them without `await` will result in fire-and-forget behavior that may leave cache operations incomplete.
- Thread safety for `MemoryCacheService` is provided by the underlying `IMemoryCache` instance, which handles concurrent reads and writes safely. The access-count and last-accessed metadata updates are best-effort and may exhibit eventual consistency under high concurrency.
