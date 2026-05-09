// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Example 5: Metrics and Monitoring
///
/// Demonstrates how to:
/// - Track message publishing metrics
/// - Monitor queue health
/// - Implement health checks
/// - Create observability dashboards
/// </summary>

namespace Examples
{
    /// <summary>
    /// Comprehensive metrics collection for the outbox pattern.
    /// </summary>
    public class OutboxMetricsCollector
    {
        private readonly IOutboxService _outboxService;
        private readonly IDeadLetterService _dlService;
        private readonly ILogger<OutboxMetricsCollector> _logger;

        public OutboxMetricsCollector(
            IOutboxService outboxService,
            IDeadLetterService dlService,
            ILogger<OutboxMetricsCollector> logger)
        {
            _outboxService = outboxService;
            _dlService = dlService;
            _logger = logger;
        }

        /// <summary>
        /// Collects and logs comprehensive outbox metrics.
        /// Can be called from a background job (e.g., every minute).
        /// </summary>
        public async Task CollectMetricsAsync()
        {
            var stats = await _outboxService.GetStatisticsAsync();
            var unreviewed = await _dlService.GetUnreviewedAsync();

            _logger.LogInformation(
                "Outbox Metrics: " +
                "Total={Total}, Pending={Pending}, Published={Published}, DLQ={DLQ}, " +
                "SuccessRate={SuccessRate:P}, AvgRetries={AvgRetries:F2}, " +
                "UnreviewedDLQ={UnreviewedDLQ}, LastProcessed={LastProcessed}",
                stats.TotalMessages,
                stats.PendingMessages,
                stats.PublishedMessages,
                stats.DeadLetterCount,
                stats.SuccessRate,
                stats.AveragePublishTime.TotalSeconds,
                unreviewed.Count,
                DateTime.UtcNow);

            // Example: Alert if pending count is growing
            if (stats.PendingMessages > 1000)
            {
                _logger.LogWarning(
                    "Alert: Pending message count is high: {PendingCount}",
                    stats.PendingMessages);
            }

            // Example: Alert if DLQ is too large
            if (stats.DeadLetterCount > 100)
            {
                _logger.LogError(
                    "Alert: Dead letter queue is large: {DeadLetterCount}",
                    stats.DeadLetterCount);
            }
        }

        /// <summary>
        /// Gets detailed metrics broken down by topic.
        /// </summary>
        public async Task<string> GetDetailedMetricsAsync()
        {
            var stats = await _outboxService.GetStatisticsAsync();

            var report = $@"
═══════════════════════════════════════════
Outbox Pattern Metrics Report
═══════════════════════════════════════════

Summary:
  Total Messages:      {stats.TotalMessages}
  Pending:            {stats.PendingMessages}
  Published:          {stats.PublishedMessages}
  Failed (DLQ):       {stats.DeadLetterCount}
  Success Rate:       {stats.SuccessRate:P2}
  Average Retries:    {stats.AveragePublishTime.TotalSeconds:F2}
  Last Processed:     {DateTime.UtcNow:O}

Quality Indicators:
  Messages/Sec:       {CalculateMessagesPerSecond(stats)}
  Failure Rate:       {(1 - stats.SuccessRate):P2}
  DLQ Percentage:     {((double)stats.DeadLetterCount / stats.TotalMessages):P2}
";
            return report;
        }

        private double CalculateMessagesPerSecond(dynamic stats)
        {
            // Simplified calculation - in production would use proper time tracking
            return 0.0;
        }
    }

    /// <summary>
    /// Health check implementation for Kubernetes/Docker container orchestration.
    /// </summary>
    public class OutboxHealthCheck
    {
        private readonly IOutboxService _outboxService;
        private readonly IDeadLetterService _dlService;
        private readonly ILogger<OutboxHealthCheck> _logger;

        private DateTime _lastProcessed = DateTime.UtcNow;
        private const int StaleThresholdSeconds = 60;
        private const int DlqThreshold = 100;
        private const int PendingThreshold = 5000;

        public OutboxHealthCheck(
            IOutboxService outboxService,
            IDeadLetterService dlService,
            ILogger<OutboxHealthCheck> logger)
        {
            _outboxService = outboxService;
            _dlService = dlService;
            _logger = logger;
        }

        /// <summary>
        /// Health check for orchestrators like Kubernetes.
        /// Returns 200 if healthy, 503 if degraded/unhealthy.
        /// </summary>
        public async Task<(int statusCode, string message)> CheckHealthAsync()
        {
            try
            {
                var stats = await _outboxService.GetStatisticsAsync();
                var unreviewed = await _dlService.GetUnreviewedAsync();

                _lastProcessed = DateTime.UtcNow;

                // Check 1: Processing is happening
                var timeSinceLastProcess = DateTime.UtcNow - _lastProcessed;
                if (timeSinceLastProcess.TotalSeconds > StaleThresholdSeconds)
                {
                    return (503, $"Processing stale for {timeSinceLastProcess.TotalSeconds:F0} seconds");
                }

                // Check 2: DLQ not overflowing
                if (unreviewed.Count > DlqThreshold)
                {
                    return (503, $"Too many unreviewed dead letters: {unreviewed.Count}");
                }

                // Check 3: Pending queue not overflowing
                if (stats.PendingMessages > PendingThreshold)
                {
                    return (503, $"Pending queue too large: {stats.PendingMessages}");
                }

                // Check 4: Success rate acceptable
                if (stats.SuccessRate < 0.95)
                {
                    return (503, $"Success rate too low: {stats.SuccessRate:P}");
                }

                return (200, "Healthy");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return (503, "Health check failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Readiness check - ensures processor is able to accept work.
        /// </summary>
        public async Task<bool> IsReadyAsync()
        {
            try
            {
                var (status, _) = await CheckHealthAsync();
                return status == 200;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Liveness check - ensures processor is alive.
        /// </summary>
        public bool IsAlive() => true;  // Simple check for now
    }

    /// <summary>
    /// Alerting system for monitoring outbox health.
    /// </summary>
    public class OutboxAlertingService
    {
        private readonly IOutboxService _outboxService;
        private readonly ILogger<OutboxAlertingService> _logger;

        // Alert thresholds - customize for your environment
        private readonly AlertThresholds _thresholds = new()
        {
            MaxPendingMessages = 1000,
            MaxDlqMessages = 100,
            MaxRetries = 5,
            StaleProcessingSeconds = 60,
            MaxSuccessRateDecrease = 0.05,  // Alert if drops 5% from baseline
            MaxFailureRate = 0.05
        };

        private double _lastSuccessRate = 1.0;

        public OutboxAlertingService(
            IOutboxService outboxService,
            ILogger<OutboxAlertingService> logger)
        {
            _outboxService = outboxService;
            _logger = logger;
        }

        /// <summary>
        /// Checks for alert conditions and logs warnings/errors.
        /// Call from a background job (e.g., every minute).
        /// </summary>
        public async Task CheckAndAlertAsync()
        {
            var stats = await _outboxService.GetStatisticsAsync();

            // Check pending count
            if (stats.PendingMessages > _thresholds.MaxPendingMessages)
            {
                LogAlert(AlertLevel.Warning,
                    $"Pending messages exceed threshold: {stats.PendingMessages} > {_thresholds.MaxPendingMessages}");
            }

            // Check DLQ count
            if (stats.DeadLetterCount > _thresholds.MaxDlqMessages)
            {
                LogAlert(AlertLevel.Error,
                    $"Dead letter queue exceeds threshold: {stats.DeadLetterCount} > {_thresholds.MaxDlqMessages}");
            }

            // Check success rate
            if (stats.SuccessRate < (1 - _thresholds.MaxFailureRate))
            {
                LogAlert(AlertLevel.Error,
                    $"Failure rate too high: {(1 - stats.SuccessRate):P} > {_thresholds.MaxFailureRate:P}");
            }

            // Check success rate decline
            if (_lastSuccessRate - stats.SuccessRate > _thresholds.MaxSuccessRateDecrease)
            {
                LogAlert(AlertLevel.Warning,
                    $"Success rate dropped: {_lastSuccessRate:P} -> {stats.SuccessRate:P}");
            }

            _lastSuccessRate = stats.SuccessRate;
        }

        private void LogAlert(AlertLevel level, string message)
        {
            switch (level)
            {
                case AlertLevel.Warning:
                    _logger.LogWarning("ALERT: {Message}", message);
                    break;
                case AlertLevel.Error:
                    _logger.LogError("ALERT: {Message}", message);
                    break;
            }
        }

        public enum AlertLevel { Info, Warning, Error }

        public class AlertThresholds
        {
            public int MaxPendingMessages { get; set; }
            public int MaxDlqMessages { get; set; }
            public int MaxRetries { get; set; }
            public int StaleProcessingSeconds { get; set; }
            public double MaxSuccessRateDecrease { get; set; }
            public double MaxFailureRate { get; set; }
        }
    }

    /// <summary>
    /// Background job that periodically collects metrics and checks health.
    /// </summary>
    public class MetricsCollectionJob
    {
        private readonly OutboxMetricsCollector _metricsCollector;
        private readonly OutboxAlertingService _alertingService;
        private readonly ILogger<MetricsCollectionJob> _logger;

        public MetricsCollectionJob(
            OutboxMetricsCollector metricsCollector,
            OutboxAlertingService alertingService,
            ILogger<MetricsCollectionJob> logger)
        {
            _metricsCollector = metricsCollector;
            _alertingService = alertingService;
            _logger = logger;
        }

        /// <summary>
        /// Runs the metrics collection job.
        /// Should be scheduled to run every 1-5 minutes.
        /// </summary>
        public async Task RunAsync()
        {
            try
            {
                _logger.LogInformation("Running metrics collection job");

                await _metricsCollector.CollectMetricsAsync();
                await _alertingService.CheckAndAlertAsync();

                _logger.LogInformation("Metrics collection job completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Metrics collection job failed");
            }
        }
    }

    /// <summary>
    /// Example setup for monitoring in Program.cs
    /// </summary>
    public static class MonitoringSetup
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddScoped<OutboxMetricsCollector>();
            services.AddScoped<OutboxAlertingService>();
            services.AddScoped<OutboxHealthCheck>();
            services.AddScoped<MetricsCollectionJob>();

            // Register health check with ASP.NET Core
            // services.AddHealthChecks()
            //     .AddCheck<OutboxHealthCheck>("outbox");

            // Schedule metrics collection job
            // services.AddHostedService<MetricsCollectionBackgroundService>();
        }
    }

    public static class MetricsAndMonitoringExample
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Example: Metrics and Monitoring");
            Console.WriteLine("Features:");
            Console.WriteLine("  - Comprehensive metrics collection");
            Console.WriteLine("  - Health checks for orchestrators");
            Console.WriteLine("  - Alerting on thresholds");
            Console.WriteLine("  - Performance monitoring");

            await Task.CompletedTask;
        }
    }
}
