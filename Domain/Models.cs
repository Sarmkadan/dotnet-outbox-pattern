#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Defines the operating states of a circuit breaker.
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// The circuit is closed, and requests are allowed to pass through.
    /// </summary>
    Closed,

    /// <summary>
    /// The circuit is open, and requests are blocked.
    /// </summary>
    Open,

    /// <summary>
    /// The circuit is half-open, allowing a limited number of requests to test whether the downstream service has recovered.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Represents the result of processing a batch of outbox messages.
/// </summary>
public sealed class OutboxProcessingResult
{
    /// <summary>
    /// Gets or sets a value indicating whether processing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of messages that were processed.
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of messages that failed.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of messages moved to the dead-letter queue.
    /// </summary>
    public int DeadLetterCount { get; set; }

    /// <summary>
    /// Gets or sets the error message when processing fails.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the stack trace associated with the processing error.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the time at which processing started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the time at which processing completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Gets the duration of processing.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Gets or sets the identifiers of messages that were processed.
    /// </summary>
    public List<Guid> ProcessedMessageIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the identifiers of messages that failed.
    /// </summary>
    public List<Guid> FailedMessageIds { get; set; } = new();
}

/// <summary>
/// Represents configuration for an outbox processor.
/// </summary>
public sealed class OutboxProcessorConfig
{
    /// <summary>
    /// Gets or sets the number of messages to process in each batch.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets how long a message remains locked while it is processed.
    /// </summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the delay between processing batches.
    /// </summary>
    public TimeSpan DelayBetweenBatches { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the number of messages to process before taking a break.
    /// </summary>
    public int MessagesBeforeBreak { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the duration of the break taken after processing <see cref="MessagesBeforeBreak"/> messages.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets a value indicating whether messages are processed in parallel.
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;

    /// <summary>
    /// Gets or sets a value indicating whether dead-letter processing is enabled.
    /// </summary>
    public bool EnableDeadLetterProcessing { get; set; } = true;
}

/// <summary>
/// Represents statistics about messages in the outbox.
/// </summary>
public sealed class OutboxStatistics
{
    /// <summary>
    /// Gets or sets the total number of messages.
    /// </summary>
    public long TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of pending messages.
    /// </summary>
    public long PendingMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of messages being processed.
    /// </summary>
    public long ProcessingMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of published messages.
    /// </summary>
    public long PublishedMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of failed messages.
    /// </summary>
    public long FailedMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of archived messages.
    /// </summary>
    public long ArchivedMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of messages in the dead-letter queue.
    /// </summary>
    public long DeadLetterCount { get; set; }

    /// <summary>
    /// Gets or sets the average time required to publish a message.
    /// </summary>
    public TimeSpan AveragePublishTime { get; set; }

    /// <summary>
    /// Gets or sets the age of the oldest pending message.
    /// </summary>
    public TimeSpan? OldestPendingAge { get; set; }

    /// <summary>
    /// Gets the percentage of messages that were successfully published.
    /// </summary>
    public double SuccessRate => TotalMessages > 0 ? (double)PublishedMessages / TotalMessages * 100 : 0;
}

/// <summary>
/// Represents options that control message publishing.
/// </summary>
public sealed class PublishingOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>
    /// Gets or sets the retry policy to use.
    /// </summary>
    public RetryPolicyType RetryPolicy { get; set; } = RetryPolicyType.ExponentialBackoff;

    /// <summary>
    /// Gets or sets the initial delay before the first retry.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum delay between retries.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the multiplier used for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the delivery guarantee level.
    /// </summary>
    public DeliveryGuarantee DeliveryGuarantee { get; set; } = DeliveryGuarantee.AtLeastOnce;

    /// <summary>
    /// Gets or sets a value indicating whether jitter is added to retry delays.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for publishing a single message.
    /// </summary>
    public TimeSpan PublishTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the clock-skew tolerance applied to the deduplication window.
    /// </summary>
    public TimeSpan ClockSkewTolerance { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets a value indicating whether competing consumers claim batches by using row-level locking.
    /// </summary>
    public bool UseBatchClaiming { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of messages to claim when <see cref="UseBatchClaiming"/> is enabled.
    /// </summary>
    public int MaxBatchClaimSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the message lock duration, in seconds, used during batch claiming.
    /// </summary>
    public int LockDurationSeconds { get; set; } = 300;
}

/// <summary>
/// Represents health metrics for outbox message publication.
/// </summary>
public sealed class HealthMetrics
{
    /// <summary>
    /// Gets or sets a value indicating whether the outbox processor is healthy.
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// Gets or sets the time at which a message was last published successfully.
    /// </summary>
    public DateTime? LastSuccessfulPublish { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive publication failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets the current error message when the processor is unhealthy.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the time at which the health check was last run.
    /// </summary>
    public DateTime LastHealthCheckAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the number of messages that are currently locked.
    /// </summary>
    public int LockedMessagesCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether any messages have expired locks.
    /// </summary>
    public bool HasExpiredLocks { get; set; }

    /// <summary>
    /// Gets or sets the age of the oldest unprocessed pending message, or <c>null</c> when there are no pending messages.
    /// </summary>
    public TimeSpan? OldestMessageAge { get; set; }

    /// <summary>
    /// Gets or sets the current circuit-breaker state.
    /// </summary>
    public CircuitState CircuitState { get; set; } = CircuitState.Closed;

    /// <summary>
    /// Gets a value indicating whether the circuit breaker is currently open.
    /// </summary>
    public bool IsCircuitOpen => CircuitState == CircuitState.Open;
}
