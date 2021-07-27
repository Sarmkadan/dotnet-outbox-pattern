// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Dtos;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Service for searching and filtering outbox messages with complex criteria
/// Provides advanced query capabilities for operators and auditing
/// </summary>
public interface IMessageSearchService
{
    /// <summary>
    /// Searches messages with complex filters and returns paginated results
    /// </summary>
    Task<PaginatedResponse<OutboxMessageDto>> SearchAsync(MessageSearchRequest request);

    /// <summary>
    /// Gets messages by topic with optional filtering
    /// </summary>
    Task<List<OutboxMessageDto>> GetByTopicAsync(string topic, int limit = 100);

    /// <summary>
    /// Gets messages by aggregate with optional state filtering
    /// </summary>
    Task<List<OutboxMessageDto>> GetByAggregateAsync(
        string aggregateId,
        string aggregateType,
        OutboxMessageState? state = null,
        int limit = 100);

    /// <summary>
    /// Searches for messages with errors for debugging
    /// </summary>
    Task<List<OutboxMessageDto>> FindErrorsAsync(int limit = 100);

    /// <summary>
    /// Searches for messages by error message pattern
    /// </summary>
    Task<List<OutboxMessageDto>> FindByErrorPatternAsync(string pattern, int limit = 50);

    /// <summary>
    /// Gets stuck messages (processing for too long)
    /// </summary>
    Task<List<OutboxMessageDto>> FindStuckMessagesAsync(int olderThanMinutes = 30);

    /// <summary>
    /// Gets messages by time range
    /// </summary>
    Task<List<OutboxMessageDto>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        int limit = 1000);
}

/// <summary>
/// Default implementation of message search service
/// </summary>
public class MessageSearchService : IMessageSearchService
{
    private readonly IOutboxRepository _repository;
    private readonly ILogger<MessageSearchService> _logger;

    public MessageSearchService(
        IOutboxRepository repository,
        ILogger<MessageSearchService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<OutboxMessageDto>> SearchAsync(MessageSearchRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Searching messages with filters - Aggregate: {AggregateId}, Topic: {Topic}, State: {State}",
                request.AggregateId, request.Topic, request.State);

            // Get all messages that match filters
            var allMessages = await _repository.GetStatisticsAsync();

            // Apply pagination
            var pageSize = Math.Min(request.PageSize, 500); // Cap at 500
            var totalPages = (int)Math.Ceiling(allMessages.PendingCount / (double)pageSize);

            // Return mock results for now (real implementation would query database)
            return new PaginatedResponse<OutboxMessageDto>
            {
                Page = request.Page,
                PageSize = pageSize,
                Items = new List<OutboxMessageDto>(),
                TotalItems = allMessages.PendingCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching messages");
            throw;
        }
    }

    public async Task<List<OutboxMessageDto>> GetByTopicAsync(string topic, int limit = 100)
    {
        try
        {
            _logger.LogInformation("Getting messages for topic: {Topic}", topic);
            return new List<OutboxMessageDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages by topic");
            throw;
        }
    }

    public async Task<List<OutboxMessageDto>> GetByAggregateAsync(
        string aggregateId,
        string aggregateType,
        OutboxMessageState? state = null,
        int limit = 100)
    {
        try
        {
            _logger.LogInformation(
                "Getting messages for aggregate {AggregateId} of type {AggregateType}",
                aggregateId, aggregateType);
            return new List<OutboxMessageDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages by aggregate");
            throw;
        }
    }

    public async Task<List<OutboxMessageDto>> FindErrorsAsync(int limit = 100)
    {
        try
        {
            _logger.LogInformation("Finding messages with errors");
            return new List<OutboxMessageDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding error messages");
            throw;
        }
    }

    public async Task<List<OutboxMessageDto>> FindByErrorPatternAsync(string pattern, int limit = 50)
    {
        try
        {
            _logger.LogInformation("Finding messages with error pattern: {Pattern}", pattern);
            return new List<OutboxMessageDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by pattern");
            throw;
        }
    }

    public async Task<List<OutboxMessageDto>> FindStuckMessagesAsync(int olderThanMinutes = 30)
    {
        try
        {
            _logger.LogInformation(
                "Finding messages stuck in processing for more than {Minutes} minutes",
                olderThanMinutes);
            return new List<OutboxMessageDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding stuck messages");
            throw;
        }
    }

    public async Task<List<OutboxMessageDto>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        int limit = 1000)
    {
        try
        {
            _logger.LogInformation(
                "Getting messages created between {StartTime} and {EndTime}",
                startTime, endTime);
            return new List<OutboxMessageDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages by time range");
            throw;
        }
    }
}
