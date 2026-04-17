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
/// Repository interface for managing dead letter messages
/// </summary>
public interface IDeadLetterRepository
{
    Task<DeadLetter> AddAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default);
    Task<DeadLetter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DeadLetter?> GetByOutboxMessageIdAsync(Guid outboxMessageId, CancellationToken cancellationToken = default);
    Task<List<DeadLetter>> GetUnreviewedAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<List<DeadLetter>> GetAllAsync(int skip = 0, int limit = 1000, CancellationToken cancellationToken = default);
    Task<List<DeadLetter>> GetByTopicAsync(string topic, CancellationToken cancellationToken = default);
    Task<List<DeadLetter>> GetByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task UpdateAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetUnreviewedCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetRequeuedCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of IDeadLetterRepository using Entity Framework Core
/// </summary>
public sealed class DeadLetterRepository : IDeadLetterRepository
{
    private readonly OutboxDbContext _context;

    public DeadLetterRepository(OutboxDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Adds a new dead letter record to the database
    /// </summary>
    public async Task<DeadLetter> AddAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.DeadLetters.Add(deadLetter);
            await _context.SaveChangesAsync(cancellationToken);
            return deadLetter;
        }
        catch (DbUpdateException ex)
        {
            throw new DeadLetterException("Failed to add dead letter", deadLetter.OutboxMessageId, ex);
        }
    }

    /// <summary>
    /// Retrieves a dead letter by its ID
    /// </summary>
    public async Task<DeadLetter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeadLetters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to retrieve dead letter by ID", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Retrieves a dead letter by its associated outbox message ID
    /// </summary>
    public async Task<DeadLetter?> GetByOutboxMessageIdAsync(Guid outboxMessageId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeadLetters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.OutboxMessageId == outboxMessageId, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to retrieve dead letter by outbox message ID", outboxMessageId, ex);
        }
    }

    /// <summary>
    /// Retrieves unreviewed dead letters that require operator attention
    /// </summary>
    public async Task<List<DeadLetter>> GetUnreviewedAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeadLetters.AsNoTracking()
                .Where(x => !x.IsReviewed)
                .OrderBy(x => x.MovedToDlqAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to retrieve unreviewed dead letters", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Retrieves all dead letters with pagination
    /// </summary>
    public async Task<List<DeadLetter>> GetAllAsync(int skip = 0, int limit = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeadLetters.AsNoTracking()
                .OrderByDescending(x => x.MovedToDlqAt)
                .Skip(skip)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to retrieve dead letters", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Retrieves dead letters for a specific topic
    /// </summary>
    public async Task<List<DeadLetter>> GetByTopicAsync(string topic, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeadLetters.AsNoTracking()
                .Where(x => x.Topic == topic)
                .OrderByDescending(x => x.MovedToDlqAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to retrieve dead letters by topic", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Retrieves dead letters for a specific aggregate
    /// </summary>
    public async Task<List<DeadLetter>> GetByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeadLetters.AsNoTracking()
                .Where(x => x.AggregateId == aggregateId)
                .OrderBy(x => x.MovedToDlqAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to retrieve dead letters by aggregate ID", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Updates an existing dead letter record
    /// </summary>
    public async Task UpdateAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.DeadLetters.Update(deadLetter);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DeadLetterException("Concurrency conflict while updating dead letter", deadLetter.OutboxMessageId, ex);
        }
        catch (DbUpdateException ex)
        {
            throw new DeadLetterException("Failed to update dead letter", deadLetter.OutboxMessageId, ex);
        }
    }

    /// <summary>
    /// Deletes a dead letter record
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deadLetter = await _context.DeadLetters.FindAsync(new object[] { id }, cancellationToken);
            if (deadLetter is not null)
            {
                _context.DeadLetters.Remove(deadLetter);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to delete dead letter", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Gets the total count of dead letters
    /// </summary>
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeadLetters.AsNoTracking().CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to get dead letter count", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Gets the count of unreviewed dead letters
    /// </summary>
    public async Task<int> GetUnreviewedCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeadLetters.AsNoTracking()
                .CountAsync(x => !x.IsReviewed, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to get unreviewed dead letter count", Guid.Empty, ex);
        }
    }

    /// <summary>
    /// Gets the count of requeued dead letters
    /// </summary>
    public async Task<int> GetRequeuedCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeadLetters.AsNoTracking()
                .CountAsync(x => x.IsRequeued, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DeadLetterException("Failed to get requeued dead letter count", Guid.Empty, ex);
        }
    }
}
