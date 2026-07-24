#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Interface for dead letter queue operations
/// </summary>
public interface IDeadLetterService
{
    Task<DeadLetter> MoveToDlqAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<DeadLetter?> GetAsync(Guid deadLetterId, CancellationToken cancellationToken = default);
    Task<List<DeadLetter>> GetUnreviewedAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task ReviewAsync(Guid deadLetterId, string notes, CancellationToken cancellationToken = default);
    Task RequeueAsync(Guid deadLetterId, string reason, CancellationToken cancellationToken = default);
    Task<int> GetUnreviewedCountAsync(CancellationToken cancellationToken = default);
    Task<List<DeadLetter>> GetByTopicAsync(string topic, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid deadLetterId, CancellationToken cancellationToken = default);
    Task<HealthMetrics> GetHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing dead letter messages
/// Handles review, requeue, and analysis of failed messages
/// </summary>
public sealed class DeadLetterService : IDeadLetterService
{
    private readonly IDeadLetterRepository _dlRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<DeadLetterService> _logger;
    private readonly OutboxRetryOptions _retryOptions;

    /// <summary>
    /// Creates a new dead letter service.
    /// </summary>
    /// <param name="dlRepository">Repository used to persist and query dead letter records.</param>
    /// <param name="outboxRepository">Repository used to read and update outbox messages on requeue.</param>
    /// <param name="outboxService">Outbox service used by dependent operations.</param>
    /// <param name="logger">Logger for dead letter diagnostics.</param>
    /// <param name="retryOptions">
    /// Retry-with-backoff policy used to restore <see cref="OutboxMessage.MaxPublishAttempts"/>
    /// when a dead letter is requeued. Defaults to a new <see cref="OutboxRetryOptions"/> instance
    /// when omitted, so a requeued message is bound by the same attempt ceiling the dispatch loop
    /// would have enforced before dead-lettering it in the first place.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="dlRepository"/>, <paramref name="outboxRepository"/>,
    /// <paramref name="outboxService"/>, or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public DeadLetterService(
        IDeadLetterRepository dlRepository,
        IOutboxRepository outboxRepository,
        IOutboxService outboxService,
        ILogger<DeadLetterService> logger,
        OutboxRetryOptions? retryOptions = null)
    {
        _dlRepository = dlRepository ?? throw new ArgumentNullException(nameof(dlRepository));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryOptions = retryOptions ?? new OutboxRetryOptions();
    }

    /// <summary>
    /// Moves an outbox message to the dead letter queue
    /// </summary>
    public async Task<DeadLetter> MoveToDlqAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            var deadLetter = DeadLetter.FromOutboxMessage(message);
            var result = await _dlRepository.AddAsync(deadLetter, cancellationToken);

            _logger.LogError(
                "Message {MessageId} moved to DLQ. Topic: {Topic}, Attempts: {Attempts}, Error: {Error}",
                message.Id, message.Topic, message.PublishAttempts, message.ErrorMessage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving message to DLQ");
            throw new DeadLetterException("Failed to move message to dead letter queue", message?.Id ?? Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Retrieves a dead letter by its ID
    /// </summary>
    public async Task<DeadLetter?> GetAsync(Guid deadLetterId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dlRepository.GetByIdAsync(deadLetterId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letter {DeadLetterId}", deadLetterId);
            throw new DeadLetterException("Failed to retrieve dead letter", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Retrieves unreviewed dead letters that require operator action
    /// </summary>
    public async Task<List<DeadLetter>> GetUnreviewedAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dlRepository.GetUnreviewedAsync(limit, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unreviewed dead letters");
            throw new DeadLetterException("Failed to retrieve unreviewed dead letters", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Marks a dead letter as reviewed with operator notes
    /// </summary>
    public async Task ReviewAsync(Guid deadLetterId, string notes, CancellationToken cancellationToken = default)
    {
        try
        {
            var deadLetter = await _dlRepository.GetByIdAsync(deadLetterId, cancellationToken);
            if (deadLetter is null)
                throw new DeadLetterException("Dead letter not found", Guid.Empty);

            deadLetter.MarkAsReviewed(notes);
            await _dlRepository.UpdateAsync(deadLetter, cancellationToken);

            _logger.LogInformation("Dead letter {DeadLetterId} marked as reviewed", deadLetterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing dead letter {DeadLetterId}", deadLetterId);
            throw new DeadLetterException("Failed to review dead letter", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Requeues a dead letter message back to the outbox for retry
    /// </summary>
    public async Task RequeueAsync(Guid deadLetterId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var deadLetter = await _dlRepository.GetByIdAsync(deadLetterId, cancellationToken);
            if (deadLetter is null)
                throw new DeadLetterException("Dead letter not found", Guid.Empty);

            var message = await _outboxRepository.GetByIdAsync(deadLetter.OutboxMessageId, cancellationToken);

            if (message is null)
            {
                // Create a new message from the dead letter data
                message = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    IdempotencyKey = deadLetter.IdempotencyKey + "_requeue_" + Guid.NewGuid(),
                    AggregateId = deadLetter.AggregateId,
                    AggregateType = deadLetter.AggregateType,
                    EventType = deadLetter.EventType,
                    EventData = deadLetter.EventData,
                    EventTypeName = deadLetter.EventTypeName,
                    Topic = deadLetter.Topic,
                    State = OutboxMessageState.Pending,
                    CreatedAt = DateTime.UtcNow,
                    CorrelationId = deadLetter.CorrelationId,
                    CausationId = deadLetter.CausationId,
                    Metadata = deadLetter.Metadata,
                    PublishAttempts = 0,
                    MaxPublishAttempts = _retryOptions.MaxAttempts
                };

                await _outboxRepository.AddAsync(message, cancellationToken);
            }
            else
            {
                // Reset existing message
                message.State = OutboxMessageState.Pending;
                message.PublishAttempts = 0;
                message.MaxPublishAttempts = _retryOptions.MaxAttempts;
                message.ErrorMessage = null;
                message.ErrorStackTrace = null;
                message.LastProcessedAt = null;

                await _outboxRepository.UpdateAsync(message, cancellationToken);
            }

            // Mark the dead letter as requeued
            deadLetter.MarkAsRequeued(reason);
            await _dlRepository.UpdateAsync(deadLetter, cancellationToken);

            _logger.LogInformation(
                "Dead letter {DeadLetterId} requeued as message {MessageId}. Reason: {Reason}",
                deadLetterId, message.Id, reason);
        }
        catch (DeadLetterException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requeuing dead letter {DeadLetterId}", deadLetterId);
            throw new DeadLetterException("Failed to requeue dead letter", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Gets the count of unreviewed dead letters
    /// </summary>
    public async Task<int> GetUnreviewedCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dlRepository.GetUnreviewedCountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unreviewed dead letter count");
            throw new DeadLetterException("Failed to get unreviewed count", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Gets dead letters for a specific topic
    /// </summary>
    public async Task<List<DeadLetter>> GetByTopicAsync(string topic, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dlRepository.GetByTopicAsync(topic, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead letters for topic {Topic}", topic);
            throw new DeadLetterException("Failed to retrieve dead letters by topic", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Deletes a dead letter record after review/resolution
    /// </summary>
    public async Task DeleteAsync(Guid deadLetterId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dlRepository.DeleteAsync(deadLetterId, cancellationToken);
            _logger.LogInformation("Deleted dead letter {DeadLetterId}", deadLetterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dead letter {DeadLetterId}", deadLetterId);
            throw new DeadLetterException("Failed to delete dead letter", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Gets health metrics for the dead letter queue
    /// </summary>
    public async Task<HealthMetrics> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var unreviewedCount = await _dlRepository.GetUnreviewedCountAsync(cancellationToken);
            var totalCount = await _dlRepository.GetCountAsync(cancellationToken);

            var metrics = new HealthMetrics
            {
                IsHealthy = unreviewedCount == 0,
                LastHealthCheckAt = DateTime.UtcNow,
                ErrorMessage = unreviewedCount > 0 ? $"{unreviewedCount} unreviewed dead letters" : null
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dead letter health metrics");
            throw new DeadLetterException("Failed to get health metrics", Guid.Empty, ex);
        }
    }
}