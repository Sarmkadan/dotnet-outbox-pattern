#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Configuration;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Configuration options for the outbox processor background service
/// </summary>
public sealed class OutboxProcessorOptions : IOutboxProcessorOptions
{
    /// <summary>
    /// Whether to enable the background processor
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Batch size for processing messages
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Delay between processing batches (milliseconds)
    /// </summary>
    public int DelayBetweenBatches { get; set; } = 5000;

    /// <summary>
    /// How often to check for expired locks (milliseconds)
    /// </summary>
    public int CheckExpiredLocksInterval { get; set; } = 60000;

    /// <summary>
    /// Lock duration for processing (seconds)
    /// </summary>
    public int LockDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to process partitioned messages sequentially
    /// </summary>
    public bool PreservePartitionOrdering { get; set; } = true;

    /// <summary>
    /// Age threshold (in minutes) beyond which an unprocessed message triggers a warning log.
    /// Default is 5 minutes.
    /// </summary>
    public int OldestMessageAgeThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// Strategy used to grow the delay between batches when consecutive batches find no work.
    /// Defaults to <see cref="BackoffStrategy.None"/>, which keeps the fixed
    /// <see cref="DelayBetweenBatches"/> the service has always used.
    /// </summary>
    public BackoffStrategy BackoffStrategy { get; set; } = BackoffStrategy.None;

    /// <summary>
    /// Multiplier applied per empty batch when <see cref="BackoffStrategy"/> is
    /// <see cref="BackoffStrategy.Exponential"/>. Must be greater than or equal to 1.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Upper bound (milliseconds) on the delay produced by backoff, so an idle processor never
    /// waits longer than this between polls. Must be greater than or equal to
    /// <see cref="DelayBetweenBatches"/>.
    /// </summary>
    public int MaxDelayBetweenBatches { get; set; } = 60000;
}

/// <summary>
/// Strategy for scaling the poll delay when the outbox is idle. Backing off while there is no
/// work reduces needless database round-trips; the delay resets to the base value as soon as a
/// batch does work.
/// </summary>
public enum BackoffStrategy
{
    /// <summary>Always wait the fixed <see cref="OutboxProcessorOptions.DelayBetweenBatches"/>.</summary>
    None = 0,

    /// <summary>Wait a constant delay (identical to <see cref="None"/>, named for intent).</summary>
    Fixed = 1,

    /// <summary>Multiply the delay by <see cref="OutboxProcessorOptions.BackoffMultiplier"/> per empty batch, capped at the max.</summary>
    Exponential = 2
}

/// <summary>
/// Background service for processing outbox messages
/// Continuously polls for pending messages and publishes them
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOutboxProcessorOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly HealthMetrics _health;
    private DateTime _lastExpiredLockCheck = DateTime.UtcNow;
    private int _consecutiveEmptyBatches;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        IOutboxProcessorOptions options,
        ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _health = new HealthMetrics();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Outbox processor is disabled");
            return;
        }

        _logger.LogInformation(
            "Outbox processor started. BatchSize: {BatchSize}, DelayBetweenBatches: {Delay}ms",
            _options.BatchSize, _options.DelayBetweenBatches);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check for and release expired locks
                if (DateTime.UtcNow - _lastExpiredLockCheck > TimeSpan.FromMilliseconds(_options.CheckExpiredLocksInterval))
                {
                    await ReleaseExpiredLocksAsync(stoppingToken);
                    _lastExpiredLockCheck = DateTime.UtcNow;
                }

                // Warn if messages are stuck / growing too old
                await CheckOldestMessageAgeAsync(stoppingToken);

                // Process pending messages
                var pendingWorked = await ProcessPendingMessagesAsync(stoppingToken);

                // Process scheduled messages
                var scheduledWorked = await ProcessScheduledMessagesAsync(stoppingToken);

                // Track idle streak so an idle processor can back off its polling and stop
                // hammering the database when there is nothing to publish.
                if (pendingWorked || scheduledWorked)
                {
                    _consecutiveEmptyBatches = 0;
                }
                else if (_consecutiveEmptyBatches < int.MaxValue)
                {
                    _consecutiveEmptyBatches++;
                }

                // Wait before next batch (honouring the configured backoff strategy when the
                // concrete options type exposes one; otherwise the fixed delay is used).
                await Task.Delay(NextDelay(), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Outbox processor cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox processor");
                _health.IsHealthy = false;
                _health.ErrorMessage = ex.Message;
                _health.ConsecutiveFailures++;

                // Wait a bit longer before retrying after an error
                await Task.Delay(10000, stoppingToken);
            }
        }

        _logger.LogInformation("Outbox processor stopped");
    }

    /// <summary>
    /// Resolves the delay before the next poll. When the injected options are the concrete
    /// <see cref="OutboxProcessorOptions"/>, the configured backoff strategy is applied via
    /// <see cref="OutboxBackoffExtensions.ComputeDelay"/>; otherwise the interface's fixed
    /// <see cref="IOutboxProcessorOptions.DelayBetweenBatches"/> is used unchanged.
    /// </summary>
    private TimeSpan NextDelay()
    {
        if (_options is OutboxProcessorOptions concrete)
        {
            return concrete.ComputeDelay(_consecutiveEmptyBatches);
        }

        return TimeSpan.FromMilliseconds(_options.DelayBetweenBatches);
    }

    /// <summary>
    /// Processes pending outbox messages. Returns <c>true</c> when at least one message was
    /// processed, so the caller can reset its idle-backoff streak.
    /// </summary>
    private async Task<bool> ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var publishingService = scope.ServiceProvider.GetRequiredService<IMessagePublishingService>();

        var result = await publishingService.ProcessPendingMessagesAsync(_options.BatchSize, cancellationToken);

        if (result.ProcessedCount > 0)
        {
            _logger.LogInformation(
                "Processed {Processed} messages, {Failed} failed",
                result.ProcessedCount, result.FailedCount);

            if (result.Success)
            {
                _health.IsHealthy = true;
                _health.ConsecutiveFailures = 0;
            }
            else
            {
                _health.IsHealthy = false;
            }
            _health.LastSuccessfulPublish = DateTime.UtcNow;
        }

        return result.ProcessedCount > 0 || result.FailedCount > 0;
    }

    /// <summary>
    /// Processes messages scheduled for future delivery. Returns <c>true</c> when at least one
    /// message was processed, so the caller can reset its idle-backoff streak.
    /// </summary>
    private async Task<bool> ProcessScheduledMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var publishingService = scope.ServiceProvider.GetRequiredService<IMessagePublishingService>();

        var result = await publishingService.ProcessScheduledMessagesAsync(_options.BatchSize, cancellationToken);

        if (result.ProcessedCount > 0)
        {
            _logger.LogInformation(
                "Processed {Processed} scheduled messages, {Failed} failed",
                result.ProcessedCount, result.FailedCount);
        }

        return result.ProcessedCount > 0 || result.FailedCount > 0;
    }

    /// <summary>
    /// Releases locks on messages that have expired
    /// Allows them to be retried by other processors
    /// </summary>
    private async Task ReleaseExpiredLocksAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var publishingService = scope.ServiceProvider.GetRequiredService<IMessagePublishingService>();

            var expiredLocks = await repository.GetExpiredLocksAsync(cancellationToken);

            if (expiredLocks.Count > 0)
            {
                _logger.LogWarning(
                    "Found {Count} messages with expired locks, releasing them",
                    expiredLocks.Count);

                _health.HasExpiredLocks = true;

                foreach (var message in expiredLocks)
                {
                    await publishingService.ReleaseLockAsync(message.Id, cancellationToken);
                }
            }
            else
            {
                _health.HasExpiredLocks = false;
            }

            _health.LockedMessagesCount = expiredLocks.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for expired locks");
        }
    }

    /// <summary>
    /// Checks the age of the oldest unprocessed message and logs a warning when it
    /// exceeds <see cref="OutboxProcessorOptions.OldestMessageAgeThresholdMinutes"/>.
    /// Updates <see cref="HealthMetrics.OldestMessageAge"/> so health-check consumers
    /// can surface this data.
    /// </summary>
    private async Task CheckOldestMessageAgeAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var oldestCreatedAt = await repository.GetOldestPendingMessageCreatedAtAsync(cancellationToken);

            if (oldestCreatedAt.HasValue)
            {
                var age = DateTime.UtcNow - oldestCreatedAt.Value;
                _health.OldestMessageAge = age;

                var threshold = TimeSpan.FromMinutes(_options.OldestMessageAgeThresholdMinutes);
                if (age > threshold)
                {
                    _logger.LogWarning(
                        "Oldest unprocessed outbox message is {AgeMinutes:F1} minutes old, which exceeds the threshold of {ThresholdMinutes} minutes. Check for processing failures.",
                        age.TotalMinutes,
                        _options.OldestMessageAgeThresholdMinutes);
                }
            }
            else
            {
                _health.OldestMessageAge = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking oldest message age");
        }
    }

    /// <summary>
    /// Gets current health status of the processor
    /// </summary>
    public HealthMetrics GetHealth() => _health;
}
