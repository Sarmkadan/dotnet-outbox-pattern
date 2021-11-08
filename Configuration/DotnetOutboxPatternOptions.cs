#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Configuration;

/// <summary>
/// Configuration options for the Outbox Pattern library
/// </summary>
public sealed class DotnetOutboxPatternOptions
{
    /// <summary>
    /// Gets or sets the section name in configuration files
    /// </summary>
    public const string SectionName = "Outbox";

    /// <summary>
    /// Gets or sets whether the background processor is enabled
    /// </summary>
    [Required]
    public bool ProcessorEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch size for processing messages
    /// </summary>
    [Range(1, 10000, ErrorMessage = "BatchSize must be between 1 and 10000")]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the delay between processing batches in milliseconds
    /// </summary>
    [Range(100, 3600000, ErrorMessage = "DelayBetweenBatches must be between 100ms and 1 hour")]
    public int DelayBetweenBatches { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the maximum number of retries for failed messages
    /// </summary>
    [Range(0, 100, ErrorMessage = "MaxRetries must be between 0 and 100")]
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Gets or sets the retry policy type
    /// </summary>
    public RetryPolicyType RetryPolicy { get; set; } = RetryPolicyType.ExponentialBackoff;

    /// <summary>
    /// Gets or sets the initial retry delay in seconds
    /// </summary>
    [Range(1, 3600, ErrorMessage = "InitialRetryDelaySeconds must be between 1 and 3600 seconds")]
    public int InitialRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum retry delay in seconds
    /// </summary>
    [Range(1, 86400, ErrorMessage = "MaxRetryDelaySeconds must be between 1 and 86400 seconds (24 hours)")]
    public int MaxRetryDelaySeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the backoff multiplier for exponential backoff
    /// </summary>
    [Range(1.0, 10.0, ErrorMessage = "BackoffMultiplier must be between 1.0 and 10.0")]
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the delivery guarantee level
    /// </summary>
    public DeliveryGuarantee DeliveryGuarantee { get; set; } = DeliveryGuarantee.AtLeastOnce;

    /// <summary>
    /// Gets or sets whether to add jitter to retry delays
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for publishing a single message in seconds
    /// </summary>
    [Range(1, 3600, ErrorMessage = "PublishTimeoutSeconds must be between 1 and 3600 seconds")]
    public int PublishTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the message time-to-live in days
    /// </summary>
    [Range(1, 3650, ErrorMessage = "MessageTtlDays must be between 1 and 3650 days")]
    public int MessageTtlDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets whether to preserve partition ordering
    /// </summary>
    public bool PreservePartitionOrdering { get; set; } = true;

    /// <summary>
    /// Gets or sets the lock duration in seconds for processing messages
    /// </summary>
    [Range(30, 3600, ErrorMessage = "LockDurationSeconds must be between 30 and 3600 seconds")]
    public int LockDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the clock skew tolerance in seconds for deduplication
    /// </summary>
    [Range(1, 3600, ErrorMessage = "ClockSkewToleranceSeconds must be between 1 and 3600 seconds")]
    public int ClockSkewToleranceSeconds { get; set; } = 60;

    /// <summary>
    /// Validates the configuration options
    /// </summary>
    /// <returns>Validation result</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Ensure MaxRetryDelay is not less than InitialRetryDelay
        if (MaxRetryDelaySeconds < InitialRetryDelaySeconds)
        {
            yield return new ValidationResult(
                "MaxRetryDelaySeconds must be greater than or equal to InitialRetryDelaySeconds",
                [nameof(MaxRetryDelaySeconds), nameof(InitialRetryDelaySeconds)]);
        }

        // Validate retry policy combinations
        if (RetryPolicy == RetryPolicyType.NoRetry && MaxRetries > 0)
        {
            yield return new ValidationResult(
                "MaxRetries must be 0 when RetryPolicy is NoRetry",
                [nameof(RetryPolicy), nameof(MaxRetries)]);
        }

        if (RetryPolicy == RetryPolicyType.FixedInterval && MaxRetryDelaySeconds < InitialRetryDelaySeconds)
        {
            yield return new ValidationResult(
                "MaxRetryDelaySeconds must be greater than or equal to InitialRetryDelaySeconds for FixedInterval policy",
                [nameof(MaxRetryDelaySeconds), nameof(InitialRetryDelaySeconds)]);
        }
    }
}

