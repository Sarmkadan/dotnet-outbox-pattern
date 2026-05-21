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
/// Interface for message publishing to external systems
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the message publishing service
/// </summary>
public interface IMessagePublishingService
{
    Task<OutboxProcessingResult> ProcessPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<OutboxProcessingResult> ProcessScheduledMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<OutboxProcessingResult> ProcessPartitionAsync(string partitionKey, int batchSize, CancellationToken cancellationToken = default);
    Task<bool> ProcessSingleMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task ReleaseLockAsync(Guid messageId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for processing and publishing outbox messages
/// Handles retries, dead letter routing, and lock management
/// </summary>
public sealed class MessagePublishingService : IMessagePublishingService
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IDeadLetterRepository _deadLetterRepository;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<MessagePublishingService> _logger;
    private readonly PublishingOptions _options;

    public MessagePublishingService(
        IOutboxRepository outboxRepository,
        IDeadLetterRepository deadLetterRepository,
        IMessagePublisher publisher,
        ILogger<MessagePublishingService> logger,
        PublishingOptions options)
    {
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _deadLetterRepository = deadLetterRepository ?? throw new ArgumentNullException(nameof(deadLetterRepository));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Processes pending outbox messages in a batch.
    /// Cancellation is checked between messages to allow graceful shutdown.
    /// </summary>
    public async Task<OutboxProcessingResult> ProcessPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive");

        var result = new OutboxProcessingResult { StartedAt = DateTime.UtcNow };

        try
        {
            var messages = await _outboxRepository.GetPendingMessagesAsync(batchSize, cancellationToken);

            _logger.LogInformation("Processing {Count} pending messages (batch size: {BatchSize})",
                messages.Count, batchSize);

            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var success = await ProcessSingleMessageAsync(message.Id, cancellationToken);

                if (success)
                {
                    result.ProcessedCount++;
                    result.ProcessedMessageIds.Add(message.Id);
                }
                else
                {
                    result.FailedCount++;
                    result.FailedMessageIds.Add(message.Id);
                }
            }

            result.Success = true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Pending message processing cancelled after {Processed}/{Failed} processed/failed",
                result.ProcessedCount, result.FailedCount);
            result.Success = false;
            result.ErrorMessage = "Processing was cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending messages");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            var elapsed = result.CompletedAt.Value - result.StartedAt;
            _logger.LogInformation(
                "Batch completed in {ElapsedMs}ms: {Processed} processed, {Failed} failed",
                (long)elapsed.TotalMilliseconds, result.ProcessedCount, result.FailedCount);
        }

        return result;
    }

    /// <summary>
    /// Processes messages scheduled for future delivery
    /// </summary>
    public async Task<OutboxProcessingResult> ProcessScheduledMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var result = new OutboxProcessingResult { StartedAt = DateTime.UtcNow };

        try
        {
            var messages = await _outboxRepository.GetScheduledMessagesAsync(batchSize, cancellationToken);

            _logger.LogInformation("Processing {Count} scheduled messages", messages.Count);

            foreach (var message in messages)
            {
                var success = await ProcessSingleMessageAsync(message.Id, cancellationToken);

                if (success)
                {
                    result.ProcessedCount++;
                    result.ProcessedMessageIds.Add(message.Id);
                }
                else
                {
                    result.FailedCount++;
                    result.FailedMessageIds.Add(message.Id);
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled messages");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Processes messages for a specific partition to maintain ordering
    /// </summary>
    public async Task<OutboxProcessingResult> ProcessPartitionAsync(
        string partitionKey,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var result = new OutboxProcessingResult { StartedAt = DateTime.UtcNow };

        try
        {
            var messages = await _outboxRepository.GetPendingByPartitionAsync(partitionKey, batchSize, cancellationToken);

            _logger.LogInformation("Processing {Count} messages in partition {PartitionKey}", messages.Count, partitionKey);

            // Process messages sequentially in a partition to maintain order
            foreach (var message in messages)
            {
                var success = await ProcessSingleMessageAsync(message.Id, cancellationToken);

                if (success)
                {
                    result.ProcessedCount++;
                    result.ProcessedMessageIds.Add(message.Id);
                }
                else
                {
                    result.FailedCount++;
                    result.FailedMessageIds.Add(message.Id);
                    // Stop processing this partition if a message fails
                    break;
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing partition {PartitionKey}", partitionKey);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Processes a single outbox message.
    /// Acquires a lock, attempts publishing with a timeout, and handles
    /// failure routing to the dead letter queue when retries are exhausted.
    /// </summary>
    public async Task<bool> ProcessSingleMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        OutboxMessage? message = null;

        try
        {
            message = await _outboxRepository.GetByIdAsync(messageId, cancellationToken);
            if (message is null)
            {
                _logger.LogWarning("Message {MessageId} not found, skipping", messageId);
                return false;
            }

            if (message.IsLocked && message.LockExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Message {MessageId} is locked until {LockExpiry}, skipping",
                    messageId, message.LockExpiresAt);
                return false;
            }

            // Lock the message to prevent concurrent processing
            message.Lock(_options.PublishTimeout);
            await _outboxRepository.UpdateAsync(message, cancellationToken);

            // Attempt to publish with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.PublishTimeout);

            await _publisher.PublishAsync(message, cts.Token);

            // Mark as published
            message.MarkAsPublished();
            await _outboxRepository.UpdateAsync(message, cancellationToken);

            _logger.LogInformation(
                "Message {MessageId} published to {Topic} (attempt {Attempt}, aggregate: {AggregateId})",
                messageId, message.Topic, message.PublishAttempts, message.AggregateId);

            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Only handle as timeout if the parent token was not cancelled
            _logger.LogWarning(
                "Publishing timed out for message {MessageId} after {Timeout}ms",
                messageId, _options.PublishTimeout.TotalMilliseconds);
            await HandlePublishingFailureAsync(message, "Publishing timed out", null, cancellationToken);
            return false;
        }
        catch (OperationCanceledException)
        {
            // Parent cancellation - release the lock so another processor can pick it up
            if (message is not null)
                await ReleaseLockAsync(message.Id, CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message {MessageId} to {Topic}",
                messageId, message?.Topic);
            await HandlePublishingFailureAsync(message, ex.Message, ex.StackTrace, cancellationToken);
            return false;
        }
    }

    /// <summary>
    /// Releases the lock on a message for reprocessing
    /// </summary>
    public async Task ReleaseLockAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _outboxRepository.GetByIdAsync(messageId, cancellationToken);
            if (message is not null && message.IsLocked)
            {
                message.IsLocked = false;
                message.LockExpiresAt = null;
                if (message.State == OutboxMessageState.Processing)
                {
                    message.State = OutboxMessageState.Pending;
                }

                await _outboxRepository.UpdateAsync(message, cancellationToken);
                _logger.LogInformation("Released lock for message {MessageId}", messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for message {MessageId}", messageId);
        }
    }

    /// <summary>
    /// Handles a publishing failure by recording the error and determining next steps
    /// </summary>
    private async Task HandlePublishingFailureAsync(
        OutboxMessage? message,
        string errorMessage,
        string? stackTrace,
        CancellationToken cancellationToken)
    {
        if (message is null) return;

        try
        {
            message.RecordFailure(errorMessage, stackTrace);

            // If max attempts reached, move to dead letter
            if (message.State == OutboxMessageState.Failed)
            {
                _logger.LogError(
                    "Message {MessageId} exhausted retries ({Attempts}). Moving to dead letter.",
                    message.Id, message.PublishAttempts);

                var deadLetter = DeadLetter.FromOutboxMessage(message);
                await _deadLetterRepository.AddAsync(deadLetter, cancellationToken);
            }

            await _outboxRepository.UpdateAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling publishing failure for message {MessageId}", message?.Id);
        }
    }
}
