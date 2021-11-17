#nullable enable

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Dtos;
using DotnetOutboxPattern.Services;

namespace DotnetOutboxPattern.Controllers;

/// <summary>
/// Extension methods for <see cref="OutboxMessageController"/> providing additional convenience APIs
/// and batch operations for outbox message management
/// </summary>
public static class OutboxMessageControllerExtensions
{
    /// <summary>
    /// Retrieves only failed messages from the outbox
    /// </summary>
    public static async Task<IActionResult> GetFailedMessagesAsync(
        this OutboxMessageController controller,
        int page = 1,
        int pageSize = 50)
    {
        if (page < 1 || pageSize < 1 || pageSize > 500)
            return controller.BadRequest(new ErrorResponse { Message = "Invalid pagination parameters" });

        try
        {
            var all = await controller.GetAllMessagesAsync();
            var failed = all.Where(m => m.State == OutboxMessageState.Failed).ToList();

            var paged = failed.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new PaginatedResponse<OutboxMessageDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = failed.Count,
                Items = paged.Select(m => new OutboxMessageDto(m)).ToList()
            };

            return controller.Ok(result);
        }
        catch (Exception ex)
        {
            controller.LogError(ex, "Error retrieving failed messages");
            return controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving failed messages" });
        }
    }

    /// <summary>
    /// Retrieves only pending messages from the outbox
    /// </summary>
    public static async Task<IActionResult> GetPendingMessagesAsync(
        this OutboxMessageController controller,
        int page = 1,
        int pageSize = 50)
    {
        if (page < 1 || pageSize < 1 || pageSize > 500)
            return controller.BadRequest(new ErrorResponse { Message = "Invalid pagination parameters" });

        try
        {
            var all = await controller.GetAllMessagesAsync();
            var pending = all.Where(m => m.State == OutboxMessageState.Pending).ToList();

            var paged = pending.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new PaginatedResponse<OutboxMessageDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = pending.Count,
                Items = paged.Select(m => new OutboxMessageDto(m)).ToList()
            };

            return controller.Ok(result);
        }
        catch (Exception ex)
        {
            controller.LogError(ex, "Error retrieving pending messages");
            return controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving pending messages" });
        }
    }

    /// <summary>
    /// Retrieves only published messages from the outbox
    /// </summary>
    public static async Task<IActionResult> GetPublishedMessagesAsync(
        this OutboxMessageController controller,
        int page = 1,
        int pageSize = 50)
    {
        if (page < 1 || pageSize < 1 || pageSize > 500)
            return controller.BadRequest(new ErrorResponse { Message = "Invalid pagination parameters" });

        try
        {
            var all = await controller.GetAllMessagesAsync();
            var published = all.Where(m => m.State == OutboxMessageState.Published).ToList();

            var paged = published.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new PaginatedResponse<OutboxMessageDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = published.Count,
                Items = paged.Select(m => new OutboxMessageDto(m)).ToList()
            };

            return controller.Ok(result);
        }
        catch (Exception ex)
        {
            controller.LogError(ex, "Error retrieving published messages");
            return controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving published messages" });
        }
    }

    /// <summary>
    /// Retrieves only archived messages from the outbox
    /// </summary>
    public static async Task<IActionResult> GetArchivedMessagesAsync(
        this OutboxMessageController controller,
        int page = 1,
        int pageSize = 50)
    {
        if (page < 1 || pageSize < 1 || pageSize > 500)
            return controller.BadRequest(new ErrorResponse { Message = "Invalid pagination parameters" });

        try
        {
            var all = await controller.GetAllMessagesAsync();
            var archived = all.Where(m => m.State == OutboxMessageState.Archived).ToList();

            var paged = archived.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new PaginatedResponse<OutboxMessageDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = archived.Count,
                Items = paged.Select(m => new OutboxMessageDto(m)).ToList()
            };

            return controller.Ok(result);
        }
        catch (Exception ex)
        {
            controller.LogError(ex, "Error retrieving archived messages");
            return controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving archived messages" });
        }
    }

    /// <summary>
    /// Retries all failed messages in a single batch operation
    /// </summary>
    public static async Task<IActionResult> RetryAllFailedMessagesAsync(
        this OutboxMessageController controller)
    {
        try
        {
            var all = await controller.GetAllMessagesAsync();
            var failedMessages = all.Where(m => m.State == OutboxMessageState.Failed).ToList();

            if (failedMessages.Count == 0)
            {
                return controller.Ok(new BatchResult
                {
                    Status = "No failed messages to retry",
                    Count = 0,
                    SuccessCount = 0
                });
            }

            var successCount = 0;
            var failedCount = 0;

            foreach (var message in failedMessages)
            {
                try
                {
                    var result = await controller.RetryMessageAsync(message.Id);
                    if (result is NoContentResult)
                        successCount++;
                    else
                        failedCount++;
                }
                catch
                {
                    failedCount++;
                }
            }

            controller.LogInformation("Batch retry completed: {SuccessCount} succeeded, {FailedCount} failed",
                successCount, failedCount);

            return controller.Ok(new BatchResult
            {
                Status = "Batch retry completed",
                Count = failedMessages.Count,
                SuccessCount = successCount,
                FailedCount = failedCount
            });
        }
        catch (Exception ex)
        {
            controller.LogError(ex, "Error in batch retry operation");
            return controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error performing batch retry" });
        }
    }

    /// <summary>
    /// Gets a summary view of message states without full message details
    /// </summary>
    public static async Task<IActionResult> GetMessageStateSummaryAsync(this OutboxMessageController controller)
    {
        try
        {
            var all = await controller.GetAllMessagesAsync();
            var summary = new MessageStateSummary
            {
                TotalMessages = all.Count,
                PendingCount = all.Count(m => m.State == OutboxMessageState.Pending),
                FailedCount = all.Count(m => m.State == OutboxMessageState.Failed),
                PublishedCount = all.Count(m => m.State == OutboxMessageState.Published),
                ArchivedCount = all.Count(m => m.State == OutboxMessageState.Archived)
            };

            return controller.Ok(summary);
        }
        catch (Exception ex)
        {
            controller.LogError(ex, "Error retrieving message state summary");
            return controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving message state summary" });
        }
    }

    /// <summary>
    /// Retrieves messages by topic name with optional state filter
    /// </summary>
    public static async Task<IActionResult> GetMessagesByTopicAsync(
        this OutboxMessageController controller,
        string topic,
        OutboxMessageState? state = null,
        int page = 1,
        int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return controller.BadRequest(new ErrorResponse { Message = "Topic is required" });

        if (page < 1 || pageSize < 1 || pageSize > 500)
            return controller.BadRequest(new ErrorResponse { Message = "Invalid pagination parameters" });

        try
        {
            var all = await controller.GetAllMessagesAsync();
            var filtered = all.Where(m => m.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase));

            if (state.HasValue)
                filtered = filtered.Where(m => m.State == state.Value);

            var paged = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new PaginatedResponse<OutboxMessageDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = filtered.Count(),
                Items = paged.Select(m => new OutboxMessageDto(m)).ToList()
            };

            return controller.Ok(result);
        }
        catch (Exception ex)
        {
            controller.LogError(ex, "Error retrieving messages for topic {Topic}", topic);
            return controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error retrieving messages by topic" });
        }
    }

    /// <summary>
    /// Bulk publishes multiple events in a single operation
    /// </summary>
    public static async Task<IActionResult> PublishEventsBatchAsync(
        this OutboxMessageController controller,
        IEnumerable<PublishableEvent> events)
    {
        if (events == null || !events.Any())
            return controller.BadRequest(new ErrorResponse { Message = "Events collection cannot be empty" });

        try
        {
            var results = new List<OutboxMessageDto>();
            var errors = 0;

            foreach (var @event in events)
            {
                try
                {
                    var result = await controller.PublishEventAsync(@event);
                    if (result is CreatedAtActionResult createdResult && createdResult.Value is OutboxMessageDto messageDto)
                    {
                        results.Add(messageDto);
                    }
                }
                catch
                {
                    errors++;
                }
            }

            controller.LogInformation("Batch publish completed: {SuccessCount} succeeded, {ErrorCount} failed",
                results.Count, errors);

            return controller.Ok(new BatchPublishResult
            {
                Status = "Batch publish completed",
                TotalEvents = events.Count(),
                SuccessCount = results.Count,
                FailedCount = errors,
                PublishedMessages = results
            });
        }
        catch (Exception ex)
        {
            controller.LogError(ex, "Error in batch publish operation");
            return controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse { Message = "Error performing batch publish" });
        }
    }

    private static async Task<List<OutboxMessage>> GetAllMessagesAsync(this OutboxMessageController controller)
    {
        // Access the private _outboxService field via reflection
        var serviceField = controller.GetType()
            .GetField("_outboxService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (serviceField == null)
            throw new InvalidOperationException("OutboxService field not found");

        var service = serviceField.GetValue(controller) as IOutboxService;

        if (service == null)
            throw new InvalidOperationException("OutboxService not available");

        return await service.GetAllMessagesAsync();
    }

    private static void LogInformation(this OutboxMessageController controller, string message, params object[] args)
    {
        var loggerField = controller.GetType()
            .GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (loggerField != null)
        {
            var logger = loggerField.GetValue(controller) as ILogger<OutboxMessageController>;
            logger?.LogInformation(message, args);
        }
    }

    private static void LogError(this OutboxMessageController controller, Exception exception, string message, params object[] args)
    {
        var loggerField = controller.GetType()
            .GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (loggerField != null)
        {
            var logger = loggerField.GetValue(controller) as ILogger<OutboxMessageController>;
            logger?.LogError(exception, message, args);
        }
    }
}

/// <summary>
/// Response DTO for batch operations
/// </summary>
public class BatchResult
{
    public string? Status { get; set; }
    public int Count { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}

/// <summary>
/// Response DTO for batch publish operations
/// </summary>
public class BatchPublishResult : BatchResult
{
    public int TotalEvents { get; set; }
    public IEnumerable<OutboxMessageDto>? PublishedMessages { get; set; }
}

/// <summary>
/// Summary of message states across the outbox
/// </summary>
public class MessageStateSummary
{
    public int TotalMessages { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public int PublishedCount { get; set; }
    public int ArchivedCount { get; set; }
}
