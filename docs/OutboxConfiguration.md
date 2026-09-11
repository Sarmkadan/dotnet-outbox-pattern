# Outbox Configuration

The outbox pattern implementation provides flexible configuration through a fluent builder pattern and predefined presets.

## Configuration Builder

The `OutboxConfigurationBuilder` class provides a fluent interface for configuring outbox message publishing options.

### Usage Example

```csharp
var options = new OutboxConfigurationBuilder()
    .WithMaxRetries(5)
    .UseExponentialBackoff(
        initialDelay: TimeSpan.FromSeconds(2),
        maxDelay: TimeSpan.FromMinutes(2),
        multiplier: 2.0)
    .WithDeliveryGuarantee(DeliveryGuarantee.AtLeastOnce)
    .WithJitter(true)
    .WithPublishTimeout(TimeSpan.FromSeconds(30))
    .Build();
```

### Settable Properties

| Method | Parameter | Description | Validation |
|--------|-----------|-------------|------------|
| `WithMaxRetries` | `int maxRetries` | Maximum number of retry attempts | Must be ≥ 1 |
| `WithRetryPolicy` | `RetryPolicyType policyType` | Type of retry policy to use | NoRetry, FixedInterval, ExponentialBackoff, LinearBackoff |
| `WithInitialRetryDelay` | `TimeSpan delay` | Initial delay before first retry | Cannot be negative |
| `WithMaxRetryDelay` | `TimeSpan maxDelay` | Maximum delay between retries | Cannot be negative |
| `WithBackoffMultiplier` | `double multiplier` | Backoff multiplier for exponential backoff | Must be ≥ 1.0 |
| `WithDeliveryGuarantee` | `DeliveryGuarantee guarantee` | Delivery guarantee level | AtMostOnce, AtLeastOnce, ExactlyOnce |
| `WithJitter` | `bool enabled` | Whether to add jitter to retry delays | No validation |
| `WithPublishTimeout` | `TimeSpan timeout` | Timeout for publishing a single message | Must be ≥ 1 second |
| `UseExponentialBackoff` | `TimeSpan initialDelay, TimeSpan maxDelay, double multiplier = 2.0` | Configures exponential backoff strategy | Sets RetryPolicy to ExponentialBackoff |
| `UseLinearBackoff` | `TimeSpan interval, TimeSpan maxDelay` | Configures linear backoff strategy | Sets RetryPolicy to LinearBackoff |
| `UseFixedInterval` | `TimeSpan interval` | Configures fixed interval retry strategy | Sets RetryPolicy to FixedInterval |

### Build Method

The `Build()` method validates and returns the configured `PublishingOptions` object. It ensures that `MaxRetryDelay` is not less than `InitialRetryDelay`.

## PublishingOptions

The `PublishingOptions` class contains all configuration properties for outbox message publishing with the following default values:

| Property | Default Value | Description |
|----------|---------------|-------------|
| `MaxRetries` | 5 | Maximum number of retry attempts |
| `RetryPolicy` | `RetryPolicyType.ExponentialBackoff` | Type of retry policy |
| `InitialRetryDelay` | 5 seconds | Delay before first retry |
| `MaxRetryDelay` | 5 minutes | Maximum delay between retries |
| `BackoffMultiplier` | 2.0 | Multiplier for exponential backoff |
| `DeliveryGuarantee` | `DeliveryGuarantee.AtLeastOnce` | Delivery guarantee level |
| `UseJitter` | true | Whether to add jitter to retry delays |
| `PublishTimeout` | 30 seconds | Timeout for publishing a single message |
| `ClockSkewTolerance` | 1 minute | Clock skew tolerance for deduplication |
| `UseBatchClaiming` | true | Whether to use batch claiming with row-level locking |
| `MaxBatchClaimSize` | 100 | Maximum messages to claim in a batch |
| `LockDurationSeconds` | 300 | Lock duration for processing (seconds) when using batch claiming |

## Predefined Configuration Presets

The `OutboxConfigurationPresets` static class provides commonly used configuration profiles:

### Production

```csharp
var options = OutboxConfigurationPresets.Production();
// MaxRetries: 10
// RetryPolicy: ExponentialBackoff (5s initial, 10m max, 2.0 multiplier)
// DeliveryGuarantee: AtLeastOnce
// UseJitter: true
```

### Development

```csharp
var options = OutboxConfigurationPresets.Development();
// MaxRetries: 3
// RetryPolicy: FixedInterval (1s interval)
// DeliveryGuarantee: AtLeastOnce
// UseJitter: false
```

### Testing

```csharp
var options = OutboxConfigurationPresets.Testing();
// MaxRetries: 1
// RetryPolicy: NoRetry
// PublishTimeout: 5 seconds
```

### HighReliability

```csharp
var options = OutboxConfigurationPresets.HighReliability();
// MaxRetries: 15
// RetryPolicy: ExponentialBackoff (1s initial, 30m max, 1.5 multiplier)
// DeliveryGuarantee: ExactlyOnce
// UseJitter: true
```

### FastFail

```csharp
var options = OutboxConfigurationPresets.FastFail();
// MaxRetries: 2
// RetryPolicy: FixedInterval (1s interval)
// DeliveryGuarantee: AtLeastOnce
// UseJitter: false
```

## Enums

### RetryPolicyType

| Value | Description |
|-------|-------------|
| `NoRetry` | No retry attempts |
| `FixedInterval` | Fixed delay between retries |
| `ExponentialBackoff` | Exponentially increasing delay between retries |
| `LinearBackoff` | Linearly increasing delay between retries |

### DeliveryGuarantee

| Value | Description |
|-------|-------------|
| `AtMostOnce` | No retries, message may be lost |
| `AtLeastOnce` | Retries until success, potential duplicates |
| `ExactlyOnce` | Best effort exactly-once delivery |

## Integration with Services

The configured `PublishingOptions` is typically passed to services that handle outbox message processing:

```csharp
// Example registration in DI container
builder.Services.AddSingleton(sp => 
    OutboxConfigurationPresets.Production());

builder.Services.AddHostedService<OutboxProcessorService>();
```

The `OutboxProcessorService` and related components use these options to determine retry behavior, timing, and delivery guarantees when publishing messages from the outbox table.