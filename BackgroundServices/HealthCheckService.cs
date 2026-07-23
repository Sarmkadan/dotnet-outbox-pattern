#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Hosting;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Caching;
using DotnetOutboxPattern.Infrastructure;

namespace DotnetOutboxPattern.BackgroundServices;

/// <summary>
/// Background service that periodically checks system health and alerts on issues
/// Monitors message processing rates, error patterns, and resource usage
/// </summary>
public sealed class HealthCheckService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly ICacheService _cacheService;
    private readonly HealthCheckOptions _options;
    private List<HealthAlert> _activeAlerts = new();

    public HealthCheckService(
        IServiceProvider serviceProvider,
        ILogger<HealthCheckService> logger,
        ICacheService cacheService,
        HealthCheckOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _options = options ?? new HealthCheckOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthCheckAsync(stoppingToken);
                await Task.Delay(_options.CheckIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Health check service cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health check service");
                await Task.Delay(10000, stoppingToken);
            }
        }

        _logger.LogInformation("Health check service stopped");
    }

    private async Task PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
		var outboxProcessor = scope.ServiceProvider.GetService<OutboxProcessor>();

        try
        {
            var stats = await repository.GetStatisticsAsync();


			// Check circuit breaker state - circuit breaker open is a degradation, not a failure
			if (outboxProcessor?.GetHealth().IsCircuitOpen == true)
			{
				RaiseAlert(
					"CircuitBreakerOpen",
					"Circuit breaker is open - downstream service may be unavailable");
			}
			else
			{
				ClearAlert("CircuitBreakerOpen");
			}

            // Check for high failure rate
            var totalMessages = stats.PublishedMessages + stats.FailedMessages;
            var failureRate = totalMessages > 0 ? stats.FailedMessages / (double)totalMessages : 0;

            if (failureRate > _options.HighFailureRateThreshold)
            {
                RaiseAlert(
                    "HighFailureRate",
                    $"Failure rate is {failureRate:P2}, threshold: {_options.HighFailureRateThreshold:P2}");
            }
            else
            {
                ClearAlert("HighFailureRate");
            }

            // Check for stuck messages
            if (stats.ProcessingMessages > _options.StuckMessageThreshold)
            {
                RaiseAlert(
                    "StuckMessages",
                    $"Found {stats.ProcessingMessages} messages stuck in processing");
            }
            else
            {
                ClearAlert("StuckMessages");
            }

            // Check for dead letter accumulation
            if (stats.DeadLetterCount > _options.DeadLetterThreshold)
            {
                RaiseAlert(
                    "DeadLetterAccumulation",
                    $"Dead letter queue has {stats.DeadLetterCount} messages");
            }
            else
            {
                ClearAlert("DeadLetterAccumulation");
            }

            // Cache the health status
            var health = new
            {
                Status = _activeAlerts.Count == 0 ? "Healthy" : "Unhealthy",
                CheckTime = DateTime.UtcNow,
                Alerts = _activeAlerts,
                Statistics = stats,
                OldestMessageAge = stats.OldestPendingAge
            };

            await _cacheService.SetAsync("system:health", health, TimeSpan.FromMinutes(1));

            if (_activeAlerts.Count > 0)
            {
                _logger.LogWarning(
                    "System health check detected {AlertCount} alerts",
                    _activeAlerts.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check");
        }
    }

    private void RaiseAlert(string alertType, string message)
    {
        if (!_activeAlerts.Any(a => a.Type == alertType))
        {
            _activeAlerts.Add(new HealthAlert
            {
                Type = alertType,
                Message = message,
                RaisedAt = DateTime.UtcNow
            });

            _logger.LogWarning("Alert raised: {AlertType} - {Message}", alertType, message);
        }
    }

    private void ClearAlert(string alertType)
    {
        var alert = _activeAlerts.FirstOrDefault(a => a.Type == alertType);
        if (alert is not null)
        {
            _activeAlerts.Remove(alert);
            _logger.LogInformation("Alert cleared: {AlertType}", alertType);
        }
    }

    public IReadOnlyList<HealthAlert> GetActiveAlerts() => _activeAlerts.AsReadOnly();
}

/// <summary>
/// Configuration options for health checks
/// </summary>
public sealed class HealthCheckOptions
{
    /// <summary>
    /// How often to run health checks (default: every 5 minutes)
    /// </summary>
    public int CheckIntervalMs { get; set; } = 5 * 60 * 1000;

    /// <summary>
    /// Alert if failure rate exceeds this threshold (default: 10%)
    /// </summary>
    public double HighFailureRateThreshold { get; set; } = 0.10;

    /// <summary>
    /// Alert if messages are stuck in processing (default: 100 messages)
    /// </summary>
    public int StuckMessageThreshold { get; set; } = 100;

    /// <summary>
    /// Alert if dead letter queue exceeds this size (default: 50 messages)
    /// </summary>
    public int DeadLetterThreshold { get; set; } = 50;
}

/// <summary>
/// Represents a system health alert
/// </summary>
public sealed class HealthAlert
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime RaisedAt { get; set; }
}
