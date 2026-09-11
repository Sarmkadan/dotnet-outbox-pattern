# CacheService

## Purpose
The `CacheService` provides an in-memory caching mechanism with Time-To-Live (TTL) support to reduce database load and improve application response times. It implements the `ICacheService` interface and is suitable for distributed systems using cache invalidation strategies.

## Public Methods

### `Task<T?> GetAsync<T>(string key)`
Retrieves a cached value by its key. Returns the cached value if it exists and hasn't expired; otherwise returns the default value for type `T`.
- **Exceptions**: Throws `ArgumentException` if `key` is null or whitespace.

### `Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)`
Stores a value in the cache with an optional expiration time. If no expiration is specified, defaults to 10 minutes.
- **Exceptions**: Throws `ArgumentException` if `key` is null or whitespace.

### `Task RemoveAsync(string key)`
Removes a specific cache entry by its key.
- **Exceptions**: Throws `ArgumentException` if `key` is null or whitespace.

### `Task RemoveByPrefixAsync(string prefix)`
Removes all cache entries whose keys start with the specified prefix.
- **Exceptions**: Throws `ArgumentException` if `prefix` is null or whitespace.

### `Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)`
Retrieves a cached value if available; otherwise invokes the factory function to create the value, caches it, and returns it.
- **Exceptions**: 
  - Throws `ArgumentException` if `key` is null or whitespace.
  - Throws `ArgumentNullException` if `factory` is null.

## Cache Key Strategy
Cache keys are string identifiers used to store and retrieve cached items. The service includes a `CacheKeyBuilder` static class to ensure consistent key generation for common entities:

- **Outbox Messages**: `outbox:message:{messageId}`
- **Outbox Statistics**: `outbox:stats:{aggregateType}`
- **Webhooks**: `webhook:{webhookId}`
- **Dead Letters**: `deadletter:{deadLetterId}`

The `CacheKeyBuilder.GetPrefix(string fullKey)` method extracts the prefix portion of a key (everything up to and including the last colon) for use with `RemoveByPrefixAsync`.

## Usage Example

### Basic Usage
```csharp
// Inject ICacheService into your class constructor
public class MyService
{
    private readonly ICacheService _cacheService;
    
    public MyService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }
    
    public async Task<User> GetUserByIdAsync(Guid userId)
    {
        // Try to get from cache first
        var cachedUser = await _cacheService.GetAsync<User>($"user:{userId}");
        if (cachedUser != null)
        {
            return cachedUser;
        }
        
        // If not in cache, fetch from database
        var user = await _userRepository.GetByIdAsync(userId);
        
        // Cache the result for 30 minutes
        await _cacheService.SetAsync($"user:{userId}", user, TimeSpan.FromMinutes(30));
        
        return user;
    }
}
```

### Using GetOrSetAsync
```csharp
public async Task<User> GetUserByIdAsync(Guid userId)
{
    return await _cacheService.GetOrSetAsync(
        $"user:{userId}",
        () => _userRepository.GetByIdAsync(userId),
        TimeSpan.FromMinutes(30)
    );
}
```

### Removing Related Cache Entries
```csharp
// When a user is updated, remove all cached data related to that user
await _cacheService.RemoveByPrefixAsync($"user:{userId}:");

// Or remove a specific user cache entry
await _cacheService.RemoveAsync($"user:{userId}");
```

### Using CacheKeyBuilder for Consistency
```csharp
// Generate consistent keys for outbox messages
var messageKey = CacheKeyBuilder.BuildMessageKey(messageId);
var cachedMessage = await _cacheService.GetAsync<OutboxMessage>(messageKey);

// Remove all outbox message caches
await _cacheService.RemoveByPrefixAsync(CacheKeyBuilder.OutboxMessagePrefix);
```