# OutboxConfigurationBuilder

`OutboxConfigurationBuilder` provides a fluent API for constructing `PublishingOptions` instances that control how outbox messages are published. It exposes methods to configure retry behavior, delivery guarantees, timing parameters, and backoff strategies, along with several static presets for common scenarios.

## API

### WithMaxRetries

```csharp
public OutboxConfigurationBuilder WithMaxRetries(int maxRetries)
```

Sets the maximum number of delivery attempts before a message is considered failed. A value of zero means no retries are attempted.

**Parameters:**
- `maxRetries` — A non-negative integer specifying the retry limit.

**Returns:** The current builder instance for chaining.

**Throws:** `ArgumentOutOfRangeException` when `maxRetries` is negative.

---

### WithRetryPolicy

```csharp
public OutboxConfigurationBuilder WithRetryPolicy(RetryPolicy retryPolicy)
```

Assigns a custom retry policy that governs whether a given attempt should be retried based on the exception or result of the previous attempt.

**Parameters:**
- `retryPolicy` — An instance of `RetryPolicy`. Must not be null.

**Returns:** The current builder instance for chaining.

**Throws:** `ArgumentNullException` when `retryPolicy` is null.

---

### WithInitialRetryDelay

```csharp
public OutboxConfigurationBuilder WithInitialRetryDelay(TimeSpan delay)
```

Specifies the delay before the first retry attempt. This value serves as the base delay for backoff calculations.

**Parameters:**
- `delay` — A non-negative `TimeSpan`.

**Returns:** The current builder instance for chaining.

**Throws:** `ArgumentOutOfRangeException` when `delay` is negative.

---

### WithMaxRetryDelay

```csharp
public OutboxConfigurationBuilder WithMaxRetryDelay(TimeSpan maxDelay)
```

Sets an upper bound on the delay between retry attempts. When a backoff strategy would produce a delay exceeding this value, the delay is clamped to `maxDelay`.

**Parameters:**
- `maxDelay` — A non-negative `TimeSpan`.

**Returns:** The current builder instance for chaining.

**Throws:** `ArgumentOutOfRangeException` when `maxDelay` is negative.

---

### WithBackoffMultiplier

```csharp
public OutboxConfigurationBuilder WithBackoffMultiplier(double multiplier)
```

Defines the multiplier applied to the current delay when calculating the next delay in exponential or linear backoff strategies. A value of 2.0 doubles the delay each step; a value of 1.0 keeps it constant.

**Parameters:**
- `multiplier` — A double greater than or equal to 1.0.

**Returns:** The current builder instance for chaining.

**Throws:** `ArgumentOutOfRangeException` when `multiplier` is less than 1.0.

---

### WithDeliveryGuarantee

```csharp
public OutboxConfigurationBuilder WithDeliveryGuarantee(DeliveryGuarantee guarantee)
```

Sets the delivery guarantee level, controlling whether messages must be delivered at least once, at most once, or exactly once.

**Parameters:**
- `guarantee` — A `DeliveryGuarantee` enum value.

**Returns:** The current builder instance for chaining.

**Throws:** `ArgumentException` when an invalid or unrecognized enum value is supplied.

---

### WithJitter

```csharp
public OutboxConfigurationBuilder WithJitter(TimeSpan jitter)
```

Adds a randomized offset to retry delays to avoid thundering-herd problems. The actual delay used will be the calculated delay plus or minus a random value within the jitter range.

**Parameters:**
- `jitter` — A non-negative `TimeSpan` representing the maximum jitter magnitude.

**Returns:** The current builder instance for chaining.

**Throws:** `ArgumentOutOfRangeException` when `jitter` is negative.

---

### WithPublishTimeout

```csharp
public OutboxConfigurationBuilder WithPublishTimeout(TimeSpan timeout)
```

Sets the per-attempt timeout for publishing a message. If the publish operation does not complete within this duration, it is treated as a failure and retry logic applies.

**Parameters:**
- `timeout` — A non-negative `TimeSpan`.

**Returns:** The current builder instance for chaining.

**Throws:** `ArgumentOutOfRangeException` when `timeout` is negative.

---

### UseExponentialBackoff

```csharp
public OutboxConfigurationBuilder UseExponentialBackoff()
```

Configures the builder to use exponential backoff, where each retry delay is multiplied by `WithBackoffMultiplier` (default 2.0) raised to the attempt number, clamped by `WithMaxRetryDelay`.

**Returns:** The current builder instance for chaining.

---

### UseLinearBackoff

```csharp
public OutboxConfigurationBuilder UseLinearBackoff()
```

Configures the builder to use linear backoff, where each retry delay increases by a fixed amount equal to the initial delay multiplied by `WithBackoffMultiplier`.

**Returns:** The current builder instance for chaining.

---

### UseFixedInterval

```csharp
public OutboxConfigurationBuilder UseFixedInterval()
```

Configures the builder to use a fixed interval between retries, ignoring any multiplier. Each retry delay equals the initial delay, clamped by `WithMaxRetryDelay`.

**Returns:** The current builder instance for chaining.

---

### Build

```csharp
public PublishingOptions Build()
```

Constructs an immutable `PublishingOptions` instance from the accumulated configuration. All unset values revert to sensible defaults: 3 max retries, 1-second initial delay, 30-second max delay, 2.0 backoff multiplier, at-least-once delivery, zero jitter, 30-second publish timeout, and exponential backoff.

**Returns:** A new `PublishingOptions` instance.

**Throws:** `InvalidOperationException` if the combination of settings is internally inconsistent (e.g., `WithMaxRetries` set to zero but a retry policy is also specified).

---

### Production

```csharp
public static PublishingOptions Production { get; }
```

A static preset suitable for production environments. Uses 5 retries, exponential backoff starting at 2 seconds with a 60-second cap, 500 ms jitter, and at-least-once delivery.

---

### Development

```csharp
public static PublishingOptions Development { get; }
```

A static preset for local development. Uses 2 retries, a fixed 1-second interval, no jitter, and a short 5-second publish timeout.

---

### Testing

```csharp
public static PublishingOptions Testing { get; }
```

A static preset for integration tests. Uses 1 retry with a 100 ms fixed interval, no jitter, and a 2-second publish timeout.

---

### HighReliability

```csharp
public static PublishingOptions HighReliability { get; }
```

A static preset emphasizing delivery guarantees. Uses 10 retries, exponential backoff from 1 second to 5 minutes, 1-second jitter, and exactly-once delivery semantics.

---

### FastFail

```csharp
public static PublishingOptions FastFail { get; }
```

A static preset that minimizes latency at the cost of reliability. Uses zero retries, no backoff, and a 1-second publish timeout. Messages that fail on the first attempt are immediately discarded or dead-lettered.

---

## Usage

### Example 1: Custom configuration with exponential backoff and jitter

```csharp
var options = new OutboxConfigurationBuilder()
    .WithMaxRetries(5)
    .WithInitialRetryDelay(TimeSpan.FromSeconds(1))
    .WithMaxRetryDelay(TimeSpan.FromMinutes(2))
    .WithBackoffMultiplier(2.0)
    .UseExponentialBackoff()
    .WithJitter(TimeSpan.FromMilliseconds(300))
    .WithPublishTimeout(TimeSpan.FromSeconds(10))
    .WithDeliveryGuarantee(DeliveryGuarantee.AtLeastOnce)
    .Build();

// Use options with an outbox publisher
await outboxPublisher.ProcessMessagesAsync(options, cancellationToken);
```

### Example 2: Selecting a preset and overriding one property

```csharp
var options = new OutboxConfigurationBuilder()
    .WithMaxRetries(OutboxConfigurationBuilder.Production.MaxRetries)
    .WithInitialRetryDelay(OutboxConfigurationBuilder.Production.InitialRetryDelay)
    .WithMaxRetryDelay(OutboxConfigurationBuilder.Production.MaxRetryDelay)
    .WithBackoffMultiplier(OutboxConfigurationBuilder.Production.BackoffMultiplier)
    .UseExponentialBackoff()
    .WithJitter(OutboxConfigurationBuilder.Production.Jitter)
    .WithPublishTimeout(TimeSpan.FromSeconds(15)) // Override timeout only
    .WithDeliveryGuarantee(OutboxConfigurationBuilder.Production.DeliveryGuarantee)
    .Build();

await outboxPublisher.ProcessMessagesAsync(options, cancellationToken);
```

## Notes

- The builder is **not thread-safe**. It is designed for sequential configuration within a single thread, typically during application startup. Concurrent calls to builder methods from multiple threads produce undefined behavior.
- Static preset properties (`Production`, `Development`, etc.) return new `PublishingOptions` instances on each access. They are safe to read from multiple threads and can be used as base configurations for further customization.
- Calling `Build()` resets no internal state; subsequent calls on the same builder instance produce identical `PublishingOptions` objects unless further configuration methods are invoked between calls.
- When both a custom retry policy and a backoff strategy are specified, the retry policy determines *whether* to retry, while the backoff strategy determines *when*. If the retry policy rejects a retry, the backoff delay for that attempt is not applied.
- Setting `WithMaxRetries(0)` effectively disables all retry behavior. Any configured backoff strategy, jitter, or retry policy is ignored in this case.
- The `Build()` method validates consistency at call time. For example, a configuration with `WithMaxRetries(0)` and a non-null retry policy will throw `InvalidOperationException`, as a retry policy is meaningless when no retries are permitted.
