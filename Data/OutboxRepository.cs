#nullable enable
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
    Task<List<OutboxMessage>> GetByStateAsync(OutboxMessageState state, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task ArchiveOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    Task<int> DeleteArchivedMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    Task<List<OutboxMessage>> GetAllAsync(int limit = 10000, CancellationToken cancellationToken = default);
    Task<DateTime?> GetOldestPendingMessageCreatedAtAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims a batch of pending messages with row-level locking for competing consumers
    /// Uses SQL Server's UPDLOCK/ROWLOCK/READPAST to atomically claim messages and prevent
    /// multiple instances from processing the same messages
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to claim</param>
    /// <param name="lockDurationSeconds">Duration in seconds for which messages should be locked</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of claimed messages that this instance should process</returns>
    Task<List<OutboxMessage>> ClaimPendingMessagesBatchAsync(int batchSize, int lockDurationSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims pending messages for a specific partition with row-level locking
    /// </summary>
    /// <param name="partitionKey">The partition key</param>
    /// <param name="batchSize">Maximum number of messages to claim</param>
    /// <param name="lockDurationSeconds">Duration in seconds for which messages should be locked</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of claimed messages that this instance should process</returns>
    Task<List<OutboxMessage>> ClaimPendingMessagesByPartitionBatchAsync(string partitionKey, int batchSize, int lockDurationSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims scheduled messages that are due for processing with row-level locking
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to claim</param>
    /// <param name="lockDurationSeconds">Duration in seconds for which messages should be locked</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of claimed messages that this instance should process</returns>
    Task<List<OutboxMessage>> ClaimScheduledMessagesBatchAsync(int batchSize, int lockDurationSeconds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of IOutboxRepository using Entity Framework Core
/// </summary>
public sealed class OutboxRepository : IOutboxRepository
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
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
    /// <exception cref="OutboxRepositoryException">The update could not be persisted.</exception>
    public async Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            // Reads go through AsNoTracking, but the same scope may still track another
            // instance with this key (for example the one that was just added). Attaching
            // a second instance of the same key throws, so detach the stale one first.
            var tracked = _context.OutboxMessages.Local.FirstOrDefault(x => x.Id == message.Id);
            if (tracked is not null && !ReferenceEquals(tracked, message))
            {
                _context.Entry(tracked).State = EntityState.Detached;
            }

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
            if (message is not null)
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

            var publishedMessages = messages.Where(x => x.PublishedAt.HasValue).ToList();
            var avgPublishTime = publishedMessages.Any()
                ? publishedMessages.Average(x => (x.PublishedAt!.Value - x.CreatedAt).TotalSeconds)
                : 0;

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
                OldestPendingAge = oldestPending is not null ? DateTime.UtcNow - oldestPending.CreatedAt : null
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
    /// Retrieves all messages with a specific processing state
    /// </summary>
    public async Task<List<OutboxMessage>> GetByStateAsync(OutboxMessageState state, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.State == state)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve messages by state", nameof(GetByStateAsync), ex);
        }
    }

    /// <summary>
    /// Retrieves all messages created within the specified date range
    /// </summary>
    public async Task<List<OutboxMessage>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.CreatedAt >= startDate && x.CreatedAt <= endDate)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve messages by date range", nameof(GetByDateRangeAsync), ex);
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

    public async Task<List<OutboxMessage>> GetAllAsync(int limit = 10000, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to retrieve all messages", nameof(GetAllAsync), ex);
        }
    }

    /// <summary>
    /// Returns the creation timestamp of the oldest pending (unprocessed) message,
    /// or <c>null</c> if there are no pending messages.
    /// </summary>
    public async Task<DateTime?> GetOldestPendingMessageCreatedAtAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages.AsNoTracking()
                .Where(x => x.State == OutboxMessageState.Pending && !x.IsLocked)
                .OrderBy(x => x.CreatedAt)
                .Select(x => (DateTime?)x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException(
                "Failed to retrieve oldest pending message timestamp",
                nameof(GetOldestPendingMessageCreatedAtAsync), ex);
        }
    }

    /// <summary>
    /// Claims a batch of pending messages with row-level locking for competing consumers
    /// Uses SQL Server's UPDLOCK/ROWLOCK/READPAST to atomically claim messages and prevent
    /// multiple instances from processing the same messages
    /// </summary>
    public async Task<List<OutboxMessage>> ClaimPendingMessagesBatchAsync(int batchSize, int lockDurationSeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var lockExpiresAt = DateTime.UtcNow.AddSeconds(lockDurationSeconds);

            // Use raw SQL to atomically claim messages with row-level locking
            // The OUTPUT clause captures the IDs of claimed messages so we can return them
            var sql = $@"
DECLARE @BatchSize INT = {batchSize};
DECLARE @Now DATETIME2 = {{0}};
DECLARE @LockExpiresAt DATETIME2 = DATEADD(SECOND, {lockDurationSeconds}, @Now);

-- Claim messages with row-level locking to prevent other instances from processing them
UPDATE TOP (@BatchSize) om
SET
    om.State = {(int)OutboxMessageState.Processing},
    om.IsLocked = 1,
    om.LockExpiresAt = @LockExpiresAt,
    om.LastProcessedAt = @Now
OUTPUT inserted.Id AS Id
FROM [OutboxMessages] om WITH (UPDLOCK, ROWLOCK, READPAST)
WHERE om.[State] = {(int)OutboxMessageState.Pending}
    AND (om.[ScheduledFor] IS NULL OR om.[ScheduledFor] <= @Now)
    AND om.[IsLocked] = 0
ORDER BY om.[Priority] DESC, om.[CreatedAt] ASC
";

            // Execute the SQL to get claimed message IDs
            var claimedMessageIds = await _context.Database
                .SqlQueryRaw<Guid>($@"{sql}", now)
                .ToListAsync(cancellationToken);

            // Retrieve the claimed messages to return them
            if (claimedMessageIds.Count > 0)
            {
                return await _context.OutboxMessages
                    .Where(m => claimedMessageIds.Contains(m.Id))
                    .ToListAsync(cancellationToken);
            }

            return new List<OutboxMessage>();
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to claim pending messages batch", nameof(ClaimPendingMessagesBatchAsync), ex);
        }
    }

    /// <summary>
    /// Claims pending messages for a specific partition with row-level locking
    /// </summary>
    public async Task<List<OutboxMessage>> ClaimPendingMessagesByPartitionBatchAsync(string partitionKey, int batchSize, int lockDurationSeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var lockExpiresAt = DateTime.UtcNow.AddSeconds(lockDurationSeconds);

            // Use raw SQL to atomically claim messages with row-level locking
            // The OUTPUT clause captures the IDs of claimed messages so we can return them
            var sql = $@"
DECLARE @BatchSize INT = {batchSize};
DECLARE @Now DATETIME2 = {{0}};
DECLARE @LockExpiresAt DATETIME2 = DATEADD(SECOND, {lockDurationSeconds}, @Now);
DECLARE @PartitionKey NVARCHAR(256) = '{partitionKey}';

-- Claim messages with row-level locking to prevent other instances from processing them
UPDATE TOP (@BatchSize) om
SET
    om.State = {(int)OutboxMessageState.Processing},
    om.IsLocked = 1,
    om.LockExpiresAt = @LockExpiresAt,
    om.LastProcessedAt = @Now
OUTPUT inserted.Id AS Id
FROM [OutboxMessages] om WITH (UPDLOCK, ROWLOCK, READPAST)
WHERE om.[PartitionKey] = @PartitionKey
    AND om.[State] = {(int)OutboxMessageState.Pending}
    AND (om.[ScheduledFor] IS NULL OR om.[ScheduledFor] <= @Now)
    AND om.[IsLocked] = 0
ORDER BY om.[CreatedAt] ASC
";

            // Execute the SQL to get claimed message IDs
            var claimedMessageIds = await _context.Database
                .SqlQueryRaw<Guid>($@"{sql}", now)
                .ToListAsync(cancellationToken);

            // Retrieve the claimed messages to return them
            if (claimedMessageIds.Count > 0)
            {
                return await _context.OutboxMessages
                    .Where(m => claimedMessageIds.Contains(m.Id))
                    .ToListAsync(cancellationToken);
            }

            return new List<OutboxMessage>();
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to claim pending messages by partition batch", nameof(ClaimPendingMessagesByPartitionBatchAsync), ex);
        }
    }

    /// <summary>
    /// Claims scheduled messages that are due for processing with row-level locking
    /// </summary>
    public async Task<List<OutboxMessage>> ClaimScheduledMessagesBatchAsync(int batchSize, int lockDurationSeconds, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Use raw SQL to atomically claim messages with row-level locking
            // The OUTPUT clause captures the IDs of claimed messages so we can return them
            var sql = $@"
DECLARE @BatchSize INT = {batchSize};
DECLARE @Now DATETIME2 = {{0}};
DECLARE @LockExpiresAt DATETIME2 = DATEADD(SECOND, {lockDurationSeconds}, @Now);

-- Claim messages with row-level locking to prevent other instances from processing them
UPDATE TOP (@BatchSize) om
SET
    om.State = {(int)OutboxMessageState.Processing},
    om.IsLocked = 1,
    om.LockExpiresAt = @LockExpiresAt,
    om.LastProcessedAt = @Now
OUTPUT inserted.Id AS Id
FROM [OutboxMessages] om WITH (UPDLOCK, ROWLOCK, READPAST)
WHERE om.[State] = {(int)OutboxMessageState.Pending}
    AND [ScheduledFor] IS NOT NULL
    AND [ScheduledFor] <= @Now
    AND [IsLocked] = 0
ORDER BY om.[ScheduledFor] ASC
";

            // Execute the SQL to get claimed message IDs
            var claimedMessageIds = await _context.Database
                .SqlQueryRaw<Guid>($@"{sql}", now)
                .ToListAsync(cancellationToken);

            // Retrieve the claimed messages to return them
            if (claimedMessageIds.Count > 0)
            {
                return await _context.OutboxMessages
                    .Where(m => claimedMessageIds.Contains(m.Id))
                    .ToListAsync(cancellationToken);
            }

            return new List<OutboxMessage>();
        }
        catch (Exception ex)
        {
            throw new OutboxRepositoryException("Failed to claim scheduled messages batch", nameof(ClaimScheduledMessagesBatchAsync), ex);
        }
    }
}