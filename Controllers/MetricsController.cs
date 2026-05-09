// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotnetOutboxPattern.Services;
using DotnetOutboxPattern.Dtos;

namespace DotnetOutboxPattern.Controllers;

/// <summary>
/// Provides operational metrics and health monitoring for the outbox system
/// Exposes performance data, error rates, throughput, and system health
/// </summary>
[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IMetricsService metricsService,
        IOutboxService outboxService,
        ILogger<MetricsController> logger)
    {
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets comprehensive system health metrics - useful for dashboards and alerting
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthAsync()
    {
        try
        {
            var health = await _metricsService.GetSystemHealthAsync();
            return Ok(new SystemHealthDto(health));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health metrics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving health metrics" });
        }
    }

    /// <summary>
    /// Gets performance metrics - throughput, latency, success rates over time periods
    /// </summary>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(PerformanceMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPerformanceAsync(
        [FromQuery] string period = "24h")
    {
        try
        {
            var valid = new[] { "1h", "24h", "7d", "30d" };
            if (!valid.Contains(period))
                return BadRequest(new ErrorResponse { Message = "Invalid period. Use: 1h, 24h, 7d, 30d" });

            var metrics = await _metricsService.GetPerformanceMetricsAsync(period);
            return Ok(new PerformanceMetricsDto(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving performance metrics" });
        }
    }

    /// <summary>
    /// Gets error rate analysis - failures, dead letters, error distribution
    /// </summary>
    [HttpGet("errors")]
    [ProducesResponseType(typeof(ErrorAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetErrorAnalyticsAsync(
        [FromQuery] int limit = 100)
    {
        try
        {
            var analytics = await _metricsService.GetErrorAnalyticsAsync(limit);
            return Ok(new ErrorAnalyticsDto(analytics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error analytics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving error analytics" });
        }
    }

    /// <summary>
    /// Gets message throughput metrics - messages published per time unit
    /// </summary>
    [HttpGet("throughput")]
    [ProducesResponseType(typeof(ThroughputMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThroughputAsync(
        [FromQuery] string granularity = "hour")
    {
        try
        {
            var valid = new[] { "minute", "hour", "day" };
            if (!valid.Contains(granularity))
                return BadRequest(new ErrorResponse { Message = "Invalid granularity" });

            var metrics = await _metricsService.GetThroughputMetricsAsync(granularity);
            return Ok(new ThroughputMetricsDto(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving throughput metrics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving throughput metrics" });
        }
    }

    /// <summary>
    /// Gets latency percentiles - P50, P95, P99 for message publishing
    /// </summary>
    [HttpGet("latency")]
    [ProducesResponseType(typeof(LatencyMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatencyAsync()
    {
        try
        {
            var metrics = await _metricsService.GetLatencyMetricsAsync();
            return Ok(new LatencyMetricsDto(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latency metrics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving latency metrics" });
        }
    }

    /// <summary>
    /// Prometheus-compatible metrics endpoint for monitoring integrations
    /// </summary>
    [HttpGet("prometheus")]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrometheusMetricsAsync()
    {
        try
        {
            var metrics = await _metricsService.GetPrometheusMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Prometheus metrics");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets alert summary - current alerts based on system thresholds
    /// </summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(List<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlertsAsync()
    {
        try
        {
            var alerts = await _metricsService.GetActiveAlertsAsync();
            return Ok(alerts.Select(a => new AlertDto(a)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving alerts" });
        }
    }

    /// <summary>
    /// Gets detailed resource consumption metrics - CPU, memory, database connections
    /// </summary>
    [HttpGet("resources")]
    [ProducesResponseType(typeof(ResourceMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResourceMetricsAsync()
    {
        try
        {
            var metrics = await _metricsService.GetResourceMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resource metrics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving resource metrics" });
        }
    }
}
