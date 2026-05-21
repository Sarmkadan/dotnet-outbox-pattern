#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Defines the transactional outbox operations for reliable event publishing.
/// Messages are first persisted to the outbox table within the same database transaction
/// as the business operation, then delivered asynchronously by a background processor.
/// </summary>
/// <remarks>
/// This pattern guarantees at-least-once delivery: if the application crashes after
/// committing the business transaction, the outbox processor will pick up and deliver
/// the pending messages on restart. Idempotency keys prevent duplicate publishing.
/// </remarks>
public interface IOutboxService
{
    /// <summary>Publishes an event to the outbox with automatic idempotency key generation.</summary>
    Task<OutboxMessage> PublishEventAsync(PublishableEvent publishableEvent, CancellationToken cancellationToken = default);

    /// <summary>Publishes a domain event to the specified topic with an optional partition key for ordering.</summary>
    Task<OutboxMessage> PublishEventAsync(DomainEvent domainEvent, string topic, string? partitionKey = null, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a specific outbox message by its ID, or <c>null</c> if not found.</summary>
    Task<OutboxMessage?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>Returns aggregate statistics (pending, delivered, failed counts) for the outbox.</summary>
    Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>Resets a failed message to pending state for redelivery. Returns <c>true</c> if successful.</summary>
    Task<bool> RetryFailedMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>Moves delivered messages older than <paramref name="olderThan"/> to the archive.</summary>
    Task ArchiveOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>Returns all outbox messages regardless of status. Use with caution on large tables.</summary>
    Task<List<OutboxMessage>> GetAllMessagesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Core service for managing outbox message publishing
/// Handles transactional consistency and message deduplication
/// </summary>
public sealed class OutboxService : IOutboxService
{
    private readonly IOutboxRepository _repository;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(IOutboxRepository repository, ILogger<OutboxService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes an event using the outbox pattern
    /// Ensures reliable delivery through transactional consistency
    /// </summary>
    public async Task<OutboxMessage> PublishEventAsync(PublishableEvent publishableEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishableEvent);

        try
        {
            var idempotencyKey = publishableEvent.Event.EventId.ToString();

            // Check for duplicate using idempotency key
            var existing = await _repository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                _logger.LogInformation("Message with idempotency key {IdempotencyKey} already exists", idempotencyKey);
                return existing;
            }

            var eventData = JsonSerializer.Serialize(publishableEvent.Event);
            var correlationId = publishableEvent.Event.CorrelationId ?? Guid.NewGuid().ToString();

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = idempotencyKey,
                AggregateId = GetAggregateId(publishableEvent.Event),
                AggregateType = publishableEvent.Event.GetType().Name,
                EventType = GetEventType(publishableEvent.Event),
                EventData = eventData,
                EventTypeName = publishableEvent.Event.GetType().FullName ?? publishableEvent.Event.GetType().Name,
                Topic = publishableEvent.Topic,
                PartitionKey = publishableEvent.PartitionKey,
                MaxPublishAttempts = publishableEvent.MaxAttempts,
                CreatedAt = DateTime.UtcNow,
                DeliveryGuarantee = publishableEvent.DeliveryGuarantee,
                CorrelationId = correlationId,
                CausationId = publishableEvent.Event.CausationId,
                State = OutboxMessageState.Pending
            };

            var result = await _repository.AddAsync(message, cancellationToken);

            _logger.LogInformation(
                "Outbox message published: Id={MessageId}, Topic={Topic}, AggregateId={AggregateId}",
                result.Id, result.Topic, result.AggregateId);

            return result;
        }
        catch (OutboxException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event to outbox");
            throw new OutboxException("Failed to publish event to outbox", ex);
        }
    }

    /// <summary>
    /// Publishes a domain event to the outbox with default settings
    /// </summary>
    public async Task<OutboxMessage> PublishEventAsync(
        DomainEvent domainEvent,
        string topic,
        string? partitionKey = null,
        CancellationToken cancellationToken = default)
    {
        var publishable = new PublishableEvent
        {
            Event = domainEvent,
            Topic = topic,
            PartitionKey = partitionKey
        };

        return await PublishEventAsync(publishable, cancellationToken);
    }

    /// <summary>
    /// Retrieves a message by its ID
    /// </summary>
    public async Task<OutboxMessage?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetByIdAsync(messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving message {MessageId}", messageId);
            throw new OutboxException("Failed to retrieve message", ex, resourceId: messageId.ToString());
        }
    }

    /// <summary>
    /// Gets comprehensive statistics about the outbox
    /// </summary>
    public async Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetStatisticsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving outbox statistics");
            throw new OutboxException("Failed to retrieve statistics", ex);
        }
    }

    /// <summary>
    /// Retries a previously failed message by resetting its state
    /// </summary>
    public async Task<bool> RetryFailedMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _repository.GetByIdAsync(messageId, cancellationToken);
            if (message is null)
                throw new OutboxMessageNotFoundException(messageId);

            if (message.State != OutboxMessageState.Failed)
            {
                _logger.LogWarning("Cannot retry message {MessageId} with state {State}", messageId, message.State);
                return false;
            }

            message.State = OutboxMessageState.Pending;
            message.PublishAttempts = 0;
            message.ErrorMessage = null;
            message.ErrorStackTrace = null;
            message.LastProcessedAt = null;

            await _repository.UpdateAsync(message, cancellationToken);

            _logger.LogInformation("Message {MessageId} reset for retry", messageId);
            return true;
        }
        catch (OutboxException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying message {MessageId}", messageId);
            throw new OutboxException("Failed to retry message", ex, resourceId: messageId.ToString());
        }
    }

    /// <summary>
    /// Retrieves all outbox messages up to the specified limit
    /// </summary>
    public async Task<List<OutboxMessage>> GetAllMessagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetAllAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all messages");
            throw new OutboxException("Failed to retrieve messages", ex);
        }
    }

    /// <summary>
    /// Archives published messages older than the specified date
    /// </summary>
    public async Task ArchiveOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            await _repository.ArchiveOldMessagesAsync(olderThan, cancellationToken);
            _logger.LogInformation("Archived messages older than {OlderThan}", olderThan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving old messages");
            throw new OutboxException("Failed to archive messages", ex);
        }
    }

    /// <summary>
    /// Extracts the aggregate ID from a domain event
    /// </summary>
    private static string GetAggregateId(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            EntityCreatedEvent e => e.EntityId,
            EntityUpdatedEvent e => e.EntityId,
            EntityDeletedEvent e => e.EntityId,
            CustomDomainEvent e => e.AggregateId,
            NotificationEvent e => e.RecipientId,
            _ => domainEvent.EventId.ToString()
        };
    }

    /// <summary>
    /// Determines the event type from a domain event
    /// </summary>
    private static EventType GetEventType(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            EntityCreatedEvent => EventType.Created,
            EntityUpdatedEvent => EventType.Updated,
            EntityDeletedEvent => EventType.Deleted,
            CustomDomainEvent => EventType.Custom,
            NotificationEvent => EventType.Notification,
            _ => EventType.Custom
        };
    }
}
