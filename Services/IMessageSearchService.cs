#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Data;
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
public sealed class MessageSearchService : IMessageSearchService
{
    private const int MaxPageSize = 500;

    private readonly IOutboxRepository _repository;
    private readonly ILogger<MessageSearchService> _logger;

    public MessageSearchService(
        IOutboxRepository repository,
        ILogger<MessageSearchService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches messages with complex filters and returns paginated results
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    public async Task<PaginatedResponse<OutboxMessageDto>> SearchAsync(MessageSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogInformation(
                "Searching messages with filters - Aggregate: {AggregateId}, Topic: {Topic}, State: {State}",
                request.AggregateId, request.Topic, request.State);

            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            var candidates = ApplyFilters(await _repository.GetAllAsync(), request);
            var sorted = ApplySort(candidates, request.SortBy, request.SortOrder).ToList();

            var items = sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new OutboxMessageDto(m))
                .ToList();

            return new PaginatedResponse<OutboxMessageDto>
            {
                Page = page,
                PageSize = pageSize,
                Items = items,
                TotalItems = sorted.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching messages");
            throw;
        }
    }

    /// <summary>
    /// Gets the most recent messages published to the given topic
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="topic"/> is null or whitespace.</exception>
    public async Task<List<OutboxMessageDto>> GetByTopicAsync(string topic, int limit = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        try
        {
            _logger.LogInformation("Getting messages for topic: {Topic}", topic);

            var messages = await _repository.GetByTopicAsync(topic, NormalizeLimit(limit));
            return ToDtos(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages by topic");
            throw;
        }
    }

    /// <summary>
    /// Gets messages emitted by a single aggregate, optionally narrowed to a state
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="aggregateId"/> or <paramref name="aggregateType"/> is null or whitespace.</exception>
    public async Task<List<OutboxMessageDto>> GetByAggregateAsync(
        string aggregateId,
        string aggregateType,
        OutboxMessageState? state = null,
        int limit = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);

        try
        {
            _logger.LogInformation(
                "Getting messages for aggregate {AggregateId} of type {AggregateType}",
                aggregateId, aggregateType);

            var messages = await _repository.GetByAggregateIdAsync(aggregateId);

            var filtered = messages
                .Where(m => string.Equals(m.AggregateType, aggregateType, StringComparison.Ordinal))
                .Where(m => state is null || m.State == state)
                .OrderByDescending(m => m.CreatedAt)
                .Take(NormalizeLimit(limit));

            return ToDtos(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages by aggregate");
            throw;
        }
    }

    /// <summary>
    /// Returns the most recent messages that carry an error message
    /// </summary>
    public async Task<List<OutboxMessageDto>> FindErrorsAsync(int limit = 100)
    {
        try
        {
            _logger.LogInformation("Finding messages with errors");

            var messages = await _repository.GetAllAsync();

            var withErrors = messages
                .Where(m => !string.IsNullOrWhiteSpace(m.ErrorMessage))
                .OrderByDescending(m => m.LastProcessedAt ?? m.CreatedAt)
                .Take(NormalizeLimit(limit));

            return ToDtos(withErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding error messages");
            throw;
        }
    }

    /// <summary>
    /// Returns messages whose error message contains the given substring (case-insensitive)
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is null or whitespace.</exception>
    public async Task<List<OutboxMessageDto>> FindByErrorPatternAsync(string pattern, int limit = 50)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        try
        {
            _logger.LogInformation("Finding messages with error pattern: {Pattern}", pattern);

            var messages = await _repository.GetAllAsync();

            var matches = messages
                .Where(m => m.ErrorMessage is not null &&
                            m.ErrorMessage.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.LastProcessedAt ?? m.CreatedAt)
                .Take(NormalizeLimit(limit));

            return ToDtos(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by pattern");
            throw;
        }
    }

    /// <summary>
    /// Returns messages that have been in the Processing state (or locked) longer than the given number of minutes
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="olderThanMinutes"/> is negative.</exception>
    public async Task<List<OutboxMessageDto>> FindStuckMessagesAsync(int olderThanMinutes = 30)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(olderThanMinutes);

        try
        {
            _logger.LogInformation(
                "Finding messages stuck in processing for more than {Minutes} minutes",
                olderThanMinutes);

            var threshold = DateTime.UtcNow.AddMinutes(-olderThanMinutes);
            var messages = await _repository.GetByStateAsync(OutboxMessageState.Processing);

            var stuck = messages
                .Where(m => (m.LastProcessedAt ?? m.CreatedAt) <= threshold || m.IsLocked)
                .OrderBy(m => m.LastProcessedAt ?? m.CreatedAt);

            return ToDtos(stuck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding stuck messages");
            throw;
        }
    }

    /// <summary>
    /// Returns messages created within the given inclusive time range
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="endTime"/> is earlier than <paramref name="startTime"/>.</exception>
    public async Task<List<OutboxMessageDto>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        int limit = 1000)
    {
        if (endTime < startTime)
            throw new ArgumentException("End time must not be earlier than start time.", nameof(endTime));

        try
        {
            _logger.LogInformation(
                "Getting messages created between {StartTime} and {EndTime}",
                startTime, endTime);

            var messages = await _repository.GetByDateRangeAsync(startTime, endTime);

            var limited = messages
                .OrderByDescending(m => m.CreatedAt)
                .Take(NormalizeLimit(limit));

            return ToDtos(limited);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages by time range");
            throw;
        }
    }

    private static IEnumerable<OutboxMessage> ApplyFilters(
        IEnumerable<OutboxMessage> messages,
        MessageSearchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.AggregateId))
            messages = messages.Where(m => string.Equals(m.AggregateId, request.AggregateId, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(request.AggregateType))
            messages = messages.Where(m => string.Equals(m.AggregateType, request.AggregateType, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(request.Topic))
            messages = messages.Where(m => string.Equals(m.Topic, request.Topic, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(request.State) &&
            Enum.TryParse<OutboxMessageState>(request.State, ignoreCase: true, out var state))
        {
            messages = messages.Where(m => m.State == state);
        }

        if (request.CreatedAfter.HasValue)
            messages = messages.Where(m => m.CreatedAt >= request.CreatedAfter.Value);

        if (request.CreatedBefore.HasValue)
            messages = messages.Where(m => m.CreatedAt <= request.CreatedBefore.Value);

        if (request.MinPublishAttempts.HasValue)
            messages = messages.Where(m => m.PublishAttempts >= request.MinPublishAttempts.Value);

        return messages;
    }

    private static IEnumerable<OutboxMessage> ApplySort(
        IEnumerable<OutboxMessage> messages,
        string? sortBy,
        string? sortOrder)
    {
        var descending = !string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

        Func<OutboxMessage, IComparable> selector = sortBy?.ToLowerInvariant() switch
        {
            "topic" => m => m.Topic,
            "state" => m => (int)m.State,
            "publishattempts" => m => m.PublishAttempts,
            "publishedat" => m => m.PublishedAt ?? DateTime.MinValue,
            "aggregateid" => m => m.AggregateId,
            _ => m => m.CreatedAt
        };

        return descending
            ? messages.OrderByDescending(selector)
            : messages.OrderBy(selector);
    }

    private static int NormalizeLimit(int limit) => Math.Clamp(limit, 1, 10000);

    private static List<OutboxMessageDto> ToDtos(IEnumerable<OutboxMessage> messages) =>
        messages.Select(m => new OutboxMessageDto(m)).ToList();
}
