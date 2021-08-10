#nullable enable

using System.Diagnostics.Metrics;
using DotnetOutboxPattern.Data;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Provides OpenTelemetry metrics for the outbox pattern.
/// </summary>
public sealed class OutboxMetrics
{
    private readonly Meter _meter;
    private readonly IOutboxRepository _outboxRepository;

    public Counter<long> PublishErrorsTotal { get; }
    public Counter<long> DeadLettersTotal { get; }
    public Histogram<double> ProcessingDurationSeconds { get; }

    public OutboxMetrics(IOutboxRepository outboxRepository)
    {
        _outboxRepository = outboxRepository;
        _meter = new Meter("DotnetOutboxPattern.Outbox", "1.0.0");

        PublishErrorsTotal = _meter.CreateCounter<long>("outbox_publish_errors_total", "messages", "Total number of failed message publications.");
        DeadLettersTotal = _meter.CreateCounter<long>("outbox_dead_letter_total", "messages", "Total number of messages moved to the dead letter queue.");
        ProcessingDurationSeconds = _meter.CreateHistogram<double>("outbox_processing_duration_seconds", "seconds", "Duration of outbox message processing.");

        _meter.CreateObservableGauge("outbox_pending_messages_total", GetPendingMessagesCount, "messages", "Current count of pending outbox messages.");
    }

    private IEnumerable<Measurement<long>> GetPendingMessagesCount()
    {
        var count = _outboxRepository.GetPendingCountAsync().Result; // Await is not allowed in observable gauge callback.
        yield return new Measurement<long>(count);
    }
}