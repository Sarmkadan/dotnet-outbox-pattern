// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Configuration options for the outbox processor background service
/// </summary>
public class OutboxProcessorOptions
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
}

/// <summary>
/// Background service for processing outbox messages
/// Continuously polls for pending messages and publishes them
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxProcessorOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly HealthMetrics _health;
    private DateTime _lastExpiredLockCheck = DateTime.UtcNow;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        OutboxProcessorOptions? options,
        ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? new OutboxProcessorOptions();
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

                // Process pending messages
                await ProcessPendingMessagesAsync(stoppingToken);

                // Process scheduled messages
                await ProcessScheduledMessagesAsync(stoppingToken);

                // Wait before next batch
                await Task.Delay(_options.DelayBetweenBatches, stoppingToken);
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
    /// Processes pending outbox messages
    /// </summary>
    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var publishingService = scope.ServiceProvider.GetRequiredService<IMessagePublishingService>();

        var result = await publishingService.ProcessPendingMessagesAsync(_options.BatchSize, cancellationToken);

        if (result.ProcessedCount > 0)
        {
            _logger.LogInformation(
                "Processed {Processed} messages, {Failed} failed",
                result.ProcessedCount, result.FailedCount);

            _health.IsHealthy = result.Success;
            _health.LastSuccessfulPublish = DateTime.UtcNow;
            _health.ConsecutiveFailures = 0;
        }
    }

    /// <summary>
    /// Processes messages scheduled for future delivery
    /// </summary>
    private async Task ProcessScheduledMessagesAsync(CancellationToken cancellationToken)
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
    /// Gets current health status of the processor
    /// </summary>
    public HealthMetrics GetHealth() => _health;
}
