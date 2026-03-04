#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Configuration;

/// <summary>
/// Configuration builder for fluent outbox pattern setup
/// </summary>
public sealed class OutboxConfigurationBuilder
{
    private readonly PublishingOptions _options = new();

    /// <summary>
    /// Sets the maximum number of retries
    /// </summary>
    public OutboxConfigurationBuilder WithMaxRetries(int maxRetries)
    {
        if (maxRetries < 1)
            throw new ArgumentException("MaxRetries must be at least 1", nameof(maxRetries));

        _options.MaxRetries = maxRetries;
        return this;
    }

    /// <summary>
    /// Sets the retry policy strategy
    /// </summary>
    public OutboxConfigurationBuilder WithRetryPolicy(RetryPolicyType policyType)
    {
        _options.RetryPolicy = policyType;
        return this;
    }

    /// <summary>
    /// Sets the initial delay before first retry
    /// </summary>
    public OutboxConfigurationBuilder WithInitialRetryDelay(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentException("Delay cannot be negative", nameof(delay));

        _options.InitialRetryDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retries
    /// </summary>
    public OutboxConfigurationBuilder WithMaxRetryDelay(TimeSpan maxDelay)
    {
        if (maxDelay < TimeSpan.Zero)
            throw new ArgumentException("Delay cannot be negative", nameof(maxDelay));

        _options.MaxRetryDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Sets the backoff multiplier for exponential backoff
    /// </summary>
    public OutboxConfigurationBuilder WithBackoffMultiplier(double multiplier)
    {
        if (multiplier < 1.0)
            throw new ArgumentException("Multiplier must be at least 1.0", nameof(multiplier));

        _options.BackoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Sets the delivery guarantee level
    /// </summary>
    public OutboxConfigurationBuilder WithDeliveryGuarantee(DeliveryGuarantee guarantee)
    {
        _options.DeliveryGuarantee = guarantee;
        return this;
    }

    /// <summary>
    /// Enables or disables jitter in retry delays
    /// </summary>
    public OutboxConfigurationBuilder WithJitter(bool enabled)
    {
        _options.UseJitter = enabled;
        return this;
    }

    /// <summary>
    /// Sets the timeout for publishing a single message
    /// </summary>
    public OutboxConfigurationBuilder WithPublishTimeout(TimeSpan timeout)
    {
        if (timeout < TimeSpan.FromSeconds(1))
            throw new ArgumentException("Timeout must be at least 1 second", nameof(timeout));

        _options.PublishTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures exponential backoff strategy
    /// </summary>
    public OutboxConfigurationBuilder UseExponentialBackoff(
        TimeSpan initialDelay,
        TimeSpan maxDelay,
        double multiplier = 2.0)
    {
        _options.RetryPolicy = RetryPolicyType.ExponentialBackoff;
        _options.InitialRetryDelay = initialDelay;
        _options.MaxRetryDelay = maxDelay;
        _options.BackoffMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Configures linear backoff strategy
    /// </summary>
    public OutboxConfigurationBuilder UseLinearBackoff(TimeSpan interval, TimeSpan maxDelay)
    {
        _options.RetryPolicy = RetryPolicyType.LinearBackoff;
        _options.InitialRetryDelay = interval;
        _options.MaxRetryDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Configures fixed interval retry strategy
    /// </summary>
    public OutboxConfigurationBuilder UseFixedInterval(TimeSpan interval)
    {
        _options.RetryPolicy = RetryPolicyType.FixedInterval;
        _options.InitialRetryDelay = interval;
        _options.MaxRetryDelay = interval;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured PublishingOptions
    /// </summary>
    public PublishingOptions Build()
    {
        // Validation
        if (_options.MaxRetryDelay < _options.InitialRetryDelay)
        {
            _options.MaxRetryDelay = _options.InitialRetryDelay;
        }

        return _options;
    }
}

/// <summary>
/// Predefined configuration presets
/// </summary>
public static class OutboxConfigurationPresets
{
    /// <summary>
    /// Production preset with aggressive retries and exponential backoff
    /// </summary>
    public static PublishingOptions Production()
    {
        return new OutboxConfigurationBuilder()
            .WithMaxRetries(10)
            .UseExponentialBackoff(
                initialDelay: TimeSpan.FromSeconds(5),
                maxDelay: TimeSpan.FromMinutes(10),
                multiplier: 2.0)
            .WithDeliveryGuarantee(DeliveryGuarantee.AtLeastOnce)
            .WithJitter(true)
            .Build();
    }

    /// <summary>
    /// Development preset with quick retries
    /// </summary>
    public static PublishingOptions Development()
    {
        return new OutboxConfigurationBuilder()
            .WithMaxRetries(3)
            .UseFixedInterval(TimeSpan.FromSeconds(1))
            .WithDeliveryGuarantee(DeliveryGuarantee.AtLeastOnce)
            .WithJitter(false)
            .Build();
    }

    /// <summary>
    /// Testing preset with single attempt, no retries
    /// </summary>
    public static PublishingOptions Testing()
    {
        return new OutboxConfigurationBuilder()
            .WithMaxRetries(1)
            .WithRetryPolicy(RetryPolicyType.NoRetry)
            .WithPublishTimeout(TimeSpan.FromSeconds(5))
            .Build();
    }

    /// <summary>
    /// High-reliability preset for critical messages
    /// </summary>
    public static PublishingOptions HighReliability()
    {
        return new OutboxConfigurationBuilder()
            .WithMaxRetries(15)
            .UseExponentialBackoff(
                initialDelay: TimeSpan.FromSeconds(1),
                maxDelay: TimeSpan.FromMinutes(30),
                multiplier: 1.5)
            .WithDeliveryGuarantee(DeliveryGuarantee.ExactlyOnce)
            .WithJitter(true)
            .Build();
    }

    /// <summary>
    /// Fast-fail preset for non-critical messages
    /// </summary>
    public static PublishingOptions FastFail()
    {
        return new OutboxConfigurationBuilder()
            .WithMaxRetries(2)
            .UseFixedInterval(TimeSpan.FromSeconds(1))
            .WithDeliveryGuarantee(DeliveryGuarantee.AtLeastOnce)
            .WithJitter(false)
            .Build();
    }
}
