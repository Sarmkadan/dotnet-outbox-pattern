#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Hosting;
using DotnetOutboxPattern.Data;

namespace DotnetOutboxPattern.BackgroundServices;

/// <summary>
/// Background service that automatically archives old published messages
/// Improves database performance by removing processed messages from active tables
/// </summary>
public sealed class MessageArchivalService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageArchivalService> _logger;
    private readonly MessageArchivalOptions _options;

    public MessageArchivalService(
        IServiceProvider serviceProvider,
        ILogger<MessageArchivalService> logger,
        MessageArchivalOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new MessageArchivalOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message archival service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ArchiveOldMessagesAsync(stoppingToken);
                await Task.Delay(_options.ArchivalIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Message archival service cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message archival service");
                await Task.Delay(60000, stoppingToken); // Wait a minute before retrying
            }
        }

        _logger.LogInformation("Message archival service stopped");
    }

    private async Task ArchiveOldMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_options.ArchiveDaysThreshold);

        await repository.ArchiveOldMessagesAsync(cutoffDate, cancellationToken);

        _logger.LogInformation("Archived messages published before {CutoffDate}", cutoffDate);
    }
}

/// <summary>
/// Configuration options for message archival
/// </summary>
public sealed class MessageArchivalOptions
{
    /// <summary>
    /// How many days old a message must be before archival (default: 30 days)
    /// </summary>
    public int ArchiveDaysThreshold { get; set; } = 30;

    /// <summary>
    /// How often to run archival check (default: every 6 hours)
    /// </summary>
    public int ArchivalIntervalMs { get; set; } = 6 * 60 * 60 * 1000;

    /// <summary>
    /// Maximum messages to archive per batch
    /// </summary>
    public int BatchSize { get; set; } = 5000;
}
