#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Result of processing an outbox message
/// </summary>
public sealed class OutboxProcessingResult
{
    /// <summary>
    /// Whether the processing was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of messages processed
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Number of messages that failed
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Number of messages moved to dead letter
    /// </summary>
    public int DeadLetterCount { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace of any error
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// When the processing started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the processing completed
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Duration of processing
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// IDs of messages that were processed
    /// </summary>
    public List<Guid> ProcessedMessageIds { get; set; } = new();

    /// <summary>
    /// IDs of messages that failed
    /// </summary>
    public List<Guid> FailedMessageIds { get; set; } = new();
}

/// <summary>
/// Configuration for an outbox processor
/// </summary>
public sealed class OutboxProcessorConfig
{
    /// <summary>
    /// How many messages to process in a batch
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// How long to lock a message while processing it
    /// </summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Delay between processing batches
    /// </summary>
    public TimeSpan DelayBetweenBatches { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How many messages to process before taking a break
    /// </summary>
    public int MessagesBeforeBreak { get; set; } = 1000;

    /// <summary>
    /// Duration of the break after reaching MessagesBeforeBreak
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Whether to process messages in parallel
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// Maximum degree of parallelism
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;

    /// <summary>
    /// Whether to enable dead letter processing
    /// </summary>
    public bool EnableDeadLetterProcessing { get; set; } = true;
}

/// <summary>
/// Statistics about the outbox
/// </summary>
public sealed class OutboxStatistics
{
    /// <summary>
    /// Total number of messages
    /// </summary>
    public long TotalMessages { get; set; }

    /// <summary>
    /// Number of pending messages
    /// </summary>
    public long PendingMessages { get; set; }

    /// <summary>
    /// Number of messages being processed
    /// </summary>
    public long ProcessingMessages { get; set; }

    /// <summary>
    /// Number of published messages
    /// </summary>
    public long PublishedMessages { get; set; }

    /// <summary>
    /// Number of failed messages
    /// </summary>
    public long FailedMessages { get; set; }

    /// <summary>
    /// Number of archived messages
    /// </summary>
    public long ArchivedMessages { get; set; }

    /// <summary>
    /// Number of messages in dead letter queue
    /// </summary>
    public long DeadLetterCount { get; set; }

    /// <summary>
    /// Average time to publish a message
    /// </summary>
    public TimeSpan AveragePublishTime { get; set; }

    /// <summary>
    /// The oldest pending message age
    /// </summary>
    public TimeSpan? OldestPendingAge { get; set; }

    /// <summary>
    /// Percentage of messages that were successfully published
    /// </summary>
    public double SuccessRate => TotalMessages > 0 ? (double)PublishedMessages / TotalMessages * 100 : 0;
}

/// <summary>
/// Message publishing options
/// </summary>
public sealed class PublishingOptions
{
    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Type of retry policy
    /// </summary>
    public RetryPolicyType RetryPolicy { get; set; } = RetryPolicyType.ExponentialBackoff;

    /// <summary>
    /// Initial delay before first retry
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum delay between retries
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Multiplier for exponential backoff
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Delivery guarantee level
    /// </summary>
    public DeliveryGuarantee DeliveryGuarantee { get; set; } = DeliveryGuarantee.AtLeastOnce;

    /// <summary>
    /// Whether to add jitter to retry delays
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Timeout for publishing a single message
    /// </summary>
    public TimeSpan PublishTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Clock skew tolerance for deduplication window (how far in the past/future to consider messages as duplicates)
    /// </summary>
    public TimeSpan ClockSkewTolerance { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Publication health metrics
/// </summary>
public sealed class HealthMetrics
{
    /// <summary>
    /// Whether the outbox processor is healthy
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// Last time a message was successfully published
    /// </summary>
    public DateTime? LastSuccessfulPublish { get; set; }

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Current error message if unhealthy
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the health check was last run
    /// </summary>
    public DateTime LastHealthCheckAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// How many messages are currently locked
    /// </summary>
    public int LockedMessagesCount { get; set; }

    /// <summary>
    /// Whether there are messages with expired locks
    /// </summary>
    public bool HasExpiredLocks { get; set; }
}
