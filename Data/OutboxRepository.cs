// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Exceptions;

namespace DotnetOutboxPattern.Data;

/// <summary>
/// Repository for managing outbox messages in the database
/// </summary>
public interface IOutboxRepository
{
    Task<OutboxMessage> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetPendingByPartitionAsync(string partitionKey, int batchSize, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetScheduledMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetExpiredLocksAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default);
    Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetByTopicAsync(string topic, int limit = 1000, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task ArchiveOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    Task<int> DeleteArchivedMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of IOutboxRepository using Entity Framework Core
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly OutboxDbContext _context;

    public OutboxRepository(OutboxDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Adds a new outbox message to the database
    /// </summary>
    public async Task<OutboxMessage> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            message.Validate();
            _context.OutboxMessages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);
            return message;
        }
        catch (DbUpdateException ex)
        {
            throw new OutboxRepositoryException("Failed to add outbox message", nameof(AddAsync), ex);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidMessageException("Message validation failed", ex);
        }
    }

    /// <summary>
    /// Retrieves a message by its ID
    /// </summary>
    public async Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve message by ID", nameof(GetByIdAsync), ex);
        }
    }

    /// <summary>
    /// Retrieves a message by its idempotency key for deduplication
    /// </summary>
    public async Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve message by idempotency key", nameof(GetByIdempotencyKeyAsync), ex);
        }
    }

    /// <summary>
    /// Retrieves pending messages that should be published, ordered by priority and creation time
    /// </summary>
    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.State == OutboxMessageState.Pending && !x.IsLocked)
                .Where(x => !x.ScheduledFor.HasValue || x.ScheduledFor <= DateTime.UtcNow)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve pending messages", nameof(GetPendingMessagesAsync), ex);
        }
    }

    /// <summary>
    /// Retrieves pending messages for a specific partition to maintain ordering
    /// </summary>
    public async Task<List<OutboxMessage>> GetPendingByPartitionAsync(string partitionKey, int batchSize, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.PartitionKey == partitionKey)
                .Where(x => x.State == OutboxMessageState.Pending && !x.IsLocked)
                .Where(x => !x.ScheduledFor.HasValue || x.ScheduledFor <= DateTime.UtcNow)
                .OrderBy(x => x.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve pending messages by partition", nameof(GetPendingByPartitionAsync), ex);
        }
    }

    /// <summary>
    /// Retrieves messages scheduled for future delivery
    /// </summary>
    public async Task<List<OutboxMessage>> GetScheduledMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.ScheduledFor.HasValue && x.ScheduledFor <= now)
                .Where(x => x.State == OutboxMessageState.Pending && !x.IsLocked)
                .OrderBy(x => x.ScheduledFor)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve scheduled messages", nameof(GetScheduledMessagesAsync), ex);
        }
    }

    /// <summary>
    /// Retrieves messages with expired processing locks for recovery
    /// </summary>
    public async Task<List<OutboxMessage>> GetExpiredLocksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.IsLocked && x.LockExpiresAt.HasValue && x.LockExpiresAt <= now)
                .OrderBy(x => x.LockExpiresAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve expired locks", nameof(GetExpiredLocksAsync), ex);
        }
    }

    /// <summary>
    /// Updates an existing outbox message
    /// </summary>
    public async Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.OutboxMessages.Update(message);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new OutboxRepositoryException("Concurrency conflict while updating message", nameof(UpdateAsync), ex);
        }
        catch (DbUpdateException ex)
        {
            throw new OutboxRepositoryException("Failed to update outbox message", nameof(UpdateAsync), ex);
        }
    }

    /// <summary>
    /// Deletes an outbox message
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _context.OutboxMessages.FindAsync(new object[] { id }, cancellationToken);
            if (message != null)
            {
                _context.OutboxMessages.Remove(message);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to delete outbox message", nameof(DeleteAsync), ex);
        }
    }

    /// <summary>
    /// Gets the count of pending messages
    /// </summary>
    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .CountAsync(x => x.State == OutboxMessageState.Pending, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to get pending count", nameof(GetPendingCountAsync), ex);
        }
    }

    /// <summary>
    /// Gets the count of published messages
    /// </summary>
    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .CountAsync(x => x.State == OutboxMessageState.Published, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to get published count", nameof(GetPublishedCountAsync), ex);
        }
    }

    /// <summary>
    /// Gets the count of failed messages
    /// </summary>
    public async Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .CountAsync(x => x.State == OutboxMessageState.Failed, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to get failed count", nameof(GetFailedCountAsync), ex);
        }
    }

    /// <summary>
    /// Gets comprehensive statistics about the outbox
    /// </summary>
    public async Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _context.OutboxMessages.AsNoTracking().ToListAsync(cancellationToken);
            var dlCount = await _context.DeadLetters.AsNoTracking().CountAsync(cancellationToken);

            var oldestPending = messages
                .Where(x => x.State == OutboxMessageState.Pending)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefault();

            var avgPublishTime = messages
                .Where(x => x.PublishedAt.HasValue)
                .Average(x => (x.PublishedAt.Value - x.CreatedAt).TotalSeconds);

            return new OutboxStatistics
            {
                TotalMessages = messages.Count,
                PendingMessages = messages.Count(x => x.State == OutboxMessageState.Pending),
                ProcessingMessages = messages.Count(x => x.State == OutboxMessageState.Processing),
                PublishedMessages = messages.Count(x => x.State == OutboxMessageState.Published),
                FailedMessages = messages.Count(x => x.State == OutboxMessageState.Failed),
                ArchivedMessages = messages.Count(x => x.State == OutboxMessageState.Archived),
                DeadLetterCount = dlCount,
                AveragePublishTime = TimeSpan.FromSeconds(avgPublishTime),
                OldestPendingAge = oldestPending != null ? DateTime.UtcNow - oldestPending.CreatedAt : null
            };
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to get statistics", nameof(GetStatisticsAsync), ex);
        }
    }

    /// <summary>
    /// Retrieves all messages for a specific aggregate
    /// </summary>
    public async Task<List<OutboxMessage>> GetByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.AggregateId == aggregateId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve messages by aggregate ID", nameof(GetByAggregateIdAsync), ex);
        }
    }

    /// <summary>
    /// Retrieves all messages for a specific topic
    /// </summary>
    public async Task<List<OutboxMessage>> GetByTopicAsync(string topic, int limit = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.Topic == topic)
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve messages by topic", nameof(GetByTopicAsync), ex);
        }
    }

    /// <summary>
    /// Retrieves all messages with a specific correlation ID
    /// </summary>
    public async Task<List<OutboxMessage>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.CorrelationId == correlationId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve messages by correlation ID", nameof(GetByCorrelationIdAsync), ex);
        }
    }

    /// <summary>
    /// Archives old published messages
    /// </summary>
    public async Task ArchiveOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _context.OutboxMessages
                .Where(x => x.State == OutboxMessageState.Published && x.PublishedAt < olderThan)
                .ToListAsync(cancellationToken);

            foreach (var msg in messages)
            {
                msg.State = OutboxMessageState.Archived;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to archive old messages", nameof(ArchiveOldMessagesAsync), ex);
        }
    }

    /// <summary>
    /// Deletes archived messages older than specified date
    /// </summary>
    public async Task<int> DeleteArchivedMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _context.OutboxMessages
                .Where(x => x.State == OutboxMessageState.Archived && x.PublishedAt < olderThan)
                .ExecuteDeleteAsync(cancellationToken);

            return (int)count;
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to delete archived messages", nameof(DeleteArchivedMessagesAsync), ex);
        }
    }
}
