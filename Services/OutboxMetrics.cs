#nullable enable

using System.Diagnostics.Metrics;
using DotnetOutboxPattern.Data;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Provides OpenTelemetry metrics for the outbox pattern.
/// </summary>
public sealed class OutboxMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxMetrics> _logger;

    public Counter<long> PublishErrorsTotal { get; }
    public Counter<long> DeadLettersTotal { get; }
    public Histogram<double> ProcessingDurationSeconds { get; }

    /// <summary>
    /// Creates the meter and registers the observable gauge for pending messages.
    /// </summary>
    /// <exception cref="ArgumentNullException">A dependency is null.</exception>
    public OutboxMetrics(IServiceScopeFactory scopeFactory, ILogger<OutboxMetrics> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
        _meter = new Meter("DotnetOutboxPattern.Outbox", "1.0.0");

        PublishErrorsTotal = _meter.CreateCounter<long>("outbox_publish_errors_total", "messages", "Total number of failed message publications.");
        DeadLettersTotal = _meter.CreateCounter<long>("outbox_dead_letter_total", "messages", "Total number of messages moved to the dead letter queue.");
        ProcessingDurationSeconds = _meter.CreateHistogram<double>("outbox_processing_duration_seconds", "seconds", "Duration of outbox message processing.");

        _meter.CreateObservableGauge("outbox_pending_messages_total", GetPendingMessagesCount, "messages", "Current count of pending outbox messages.");
    }

    private IEnumerable<Measurement<long>> GetPendingMessagesCount()
    {
        // The gauge callback is synchronous. The repository is scoped, so a fresh
        // scope is created per observation and the query is pushed onto the thread
        // pool so no ambient context can be blocked while waiting for it.
        long count;
        try
        {
            count = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                return await repository.GetPendingCountAsync().ConfigureAwait(false);
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // A failing observation must not tear down the metrics pipeline.
            _logger.LogError(ex, "Failed to observe pending outbox message count");
            yield break;
        }

        yield return new Measurement<long>(count);
    }

    /// <summary>
    /// Releases the underlying meter.
    /// </summary>
    public void Dispose() => _meter.Dispose();
}
