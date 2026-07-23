#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Interface for message publishing to external systems.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to an external system.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the message publishing service.
/// </summary>
public interface IMessagePublishingService
{
    /// <summary>
    /// Processes pending outbox messages in a batch.
    /// </summary>
    /// <param name="batchSize">The number of messages to process in the batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the batch processing.</returns>
    Task<OutboxProcessingResult> ProcessPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes messages scheduled for future delivery.
    /// </summary>
    /// <param name="batchSize">The number of messages to process in the batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the batch processing.</returns>
    Task<OutboxProcessingResult> ProcessScheduledMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes messages for a specific partition to maintain ordering.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="batchSize">The number of messages to process in the batch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the partition processing.</returns>
    Task<OutboxProcessingResult> ProcessPartitionAsync(string partitionKey, int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a single outbox message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the message was successfully processed, false otherwise.</returns>
    Task<bool> ProcessSingleMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases the lock on a message for reprocessing.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
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
    private readonly OutboxRetryOptions _retryOptions;
    private readonly IDeadLetterService? _deadLetterService;

    /// <summary>
    /// Creates a new message publishing service.
    /// </summary>
    /// <param name="outboxRepository">Repository used to load and update outbox messages.</param>
    /// <param name="deadLetterRepository">
    /// Repository used as a fallback dead-letter sink when no <paramref name="deadLetterService"/> is supplied.
    /// </param>
    /// <param name="publisher">The message publisher used to deliver each message.</param>
    /// <param name="logger">Logger for processing diagnostics.</param>
    /// <param name="options">Publishing options (lock duration, publish timeout, etc.).</param>
    /// <param name="retryOptions">
    /// Retry-with-backoff policy consulted on every publish failure. Defaults to a new
    /// <see cref="OutboxRetryOptions"/> instance when omitted.
    /// </param>
    /// <param name="deadLetterService">
    /// Optional dead letter service used to move exhausted messages to the dead letter store.
    /// When omitted, messages are written directly via <paramref name="deadLetterRepository"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="outboxRepository"/>, <paramref name="deadLetterRepository"/>,
    /// <paramref name="publisher"/>, <paramref name="logger"/>, or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public MessagePublishingService(
        IOutboxRepository outboxRepository,
        IDeadLetterRepository deadLetterRepository,
        IMessagePublisher publisher,
        ILogger<MessagePublishingService> logger,
        PublishingOptions options,
        OutboxRetryOptions? retryOptions = null,
        IDeadLetterService? deadLetterService = null)
    {
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _deadLetterRepository = deadLetterRepository ?? throw new ArgumentNullException(nameof(deadLetterRepository));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _retryOptions = retryOptions ?? new OutboxRetryOptions();
        _deadLetterService = deadLetterService;
    }

    /// <summary>
    /// Processes pending outbox messages in a batch
    /// </summary>
    public async Task<OutboxProcessingResult> ProcessPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var result = new OutboxProcessingResult { StartedAt = DateTime.UtcNow };

        try
        {
            List<OutboxMessage> messages;

            if (_options.UseBatchClaiming)
            {
                // Use batch claiming with row-level locking for competing consumers
                messages = await _outboxRepository.ClaimPendingMessagesBatchAsync(
                    Math.Min(batchSize, _options.MaxBatchClaimSize),
                    _options.LockDurationSeconds,
                    cancellationToken);
            }
            else
            {
                // Fallback to original method for backward compatibility
                messages = await _outboxRepository.GetPendingMessagesAsync(batchSize, cancellationToken);
            }

            _logger.LogInformation("Processing {Count} pending messages", messages.Count);

            foreach (var message in messages)
            {
                var outcome = await ProcessSingleMessageCoreAsync(message.Id, cancellationToken);

                if (outcome == MessageProcessingOutcome.Published)
                {
                    result.ProcessedCount++;
                    result.ProcessedMessageIds.Add(message.Id);
                }
                else if (outcome == MessageProcessingOutcome.Failed)
                {
                    result.FailedCount++;
                    result.FailedMessageIds.Add(message.Id);
                }
                // Skipped (locked by another instance / not yet due) counts as neither
                // processed nor failed - it will be picked up again on a later pass.
            }

            result.Success = true;
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
            List<OutboxMessage> messages;

            if (_options.UseBatchClaiming)
            {
                // Use batch claiming with row-level locking for competing consumers
                messages = await _outboxRepository.ClaimScheduledMessagesBatchAsync(
                    Math.Min(batchSize, _options.MaxBatchClaimSize),
                    _options.LockDurationSeconds,
                    cancellationToken);
            }
            else
            {
                // Fallback to original method for backward compatibility
                messages = await _outboxRepository.GetScheduledMessagesAsync(batchSize, cancellationToken);
            }

            _logger.LogInformation("Processing {Count} scheduled messages", messages.Count);

            foreach (var message in messages)
            {
                var outcome = await ProcessSingleMessageCoreAsync(message.Id, cancellationToken);

                if (outcome == MessageProcessingOutcome.Published)
                {
                    result.ProcessedCount++;
                    result.ProcessedMessageIds.Add(message.Id);
                }
                else if (outcome == MessageProcessingOutcome.Failed)
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
            List<OutboxMessage> messages;

            if (_options.UseBatchClaiming)
            {
                // Use batch claiming with row-level locking for competing consumers
                messages = await _outboxRepository.ClaimPendingMessagesByPartitionBatchAsync(
                    partitionKey,
                    Math.Min(batchSize, _options.MaxBatchClaimSize),
                    _options.LockDurationSeconds,
                    cancellationToken);
            }
            else
            {
                // Fallback to original method for backward compatibility
                messages = await _outboxRepository.GetPendingByPartitionAsync(partitionKey, batchSize, cancellationToken);
            }

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
    /// Handles publishing, retries, and dead letter routing.
    /// When multiple processor instances run concurrently, an already-locked message is
    /// detected either via the in-memory <see cref="OutboxMessage.IsLocked"/> flag (best-effort
    /// pre-check) or via a <see cref="DbUpdateConcurrencyException"/> raised during the lock
    /// update, ensuring only one instance actually publishes each message.
    /// </summary>
    public async Task<bool> ProcessSingleMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
        => await ProcessSingleMessageCoreAsync(messageId, cancellationToken) == MessageProcessingOutcome.Published;

    /// <summary>
    /// Core single-message processing logic, distinguishing an actual publish failure from a
    /// message that was merely skipped (already locked by another instance, or not yet due).
    /// Batch callers use this distinction to avoid counting skipped messages as failures.
    /// </summary>
    private async Task<MessageProcessingOutcome> ProcessSingleMessageCoreAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        OutboxMessage? message = null;

        try
        {
            message = await _outboxRepository.GetByIdAsync(messageId, cancellationToken);
            if (message is null)
            {
                _logger.LogWarning("Message {MessageId} not found", messageId);
                return MessageProcessingOutcome.Skipped;
            }

            // Pre-check: skip messages already being processed by another instance.
            // This reduces unnecessary lock-contention; the concurrency exception below
            // provides a second safety net for the narrow race window.
            if (message.IsLocked || message.State == OutboxMessageState.Processing)
            {
                _logger.LogDebug(
                    "Message {MessageId} is already locked by another instance, skipping",
                    messageId);
                return MessageProcessingOutcome.Skipped;
            }

            // Honor the schedule even when this message is targeted directly (e.g. via a
            // manual retry): GetPendingMessagesAsync/GetScheduledMessagesAsync already filter
            // out not-yet-due messages, but a caller can still reach this message by ID.
            if (message.ScheduledFor.HasValue && message.ScheduledFor.Value > DateTime.UtcNow)
            {
                _logger.LogDebug(
                    "Message {MessageId} is scheduled for {ScheduledFor}, skipping",
                    messageId, message.ScheduledFor);
                return MessageProcessingOutcome.Skipped;
            }

        // For batch claiming, messages are already locked by the SQL query (UPDLOCK)
        // For non-batch claiming, we need to lock the message
        if (!message.IsLocked)
        {
            // Only lock if not already locked (batch claiming already locked it)
            message.Lock(_options.PublishTimeout);
            await _outboxRepository.UpdateAsync(message, cancellationToken);
        }

            // Attempt to publish
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.PublishTimeout);

            await _publisher.PublishAsync(message, cts.Token);

            // Mark as published
            message.MarkAsPublished();
            await _outboxRepository.UpdateAsync(message, cancellationToken);

            _logger.LogInformation(
                "Message {MessageId} published successfully on attempt {Attempt}",
                messageId, message.PublishAttempts + 1);

            return MessageProcessingOutcome.Published;
        }
        catch (OutboxRepositoryException ex) when (ex.InnerException is DbUpdateConcurrencyException)
        {
            // Another instance won the race and locked this message first — skip gracefully.
            _logger.LogDebug(
                "Message {MessageId} was taken by another instance (concurrency conflict), skipping",
                messageId);
            return MessageProcessingOutcome.Skipped;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Publishing timed out for message {MessageId}", messageId);
            await HandlePublishingFailureAsync(message, "Publishing timed out", null, cancellationToken);
            return MessageProcessingOutcome.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message {MessageId}", messageId);
            await HandlePublishingFailureAsync(message, ex.Message, ex.StackTrace, cancellationToken);
            return MessageProcessingOutcome.Failed;
        }
    }

    /// <summary>
    /// Outcome of processing a single message, distinguishing a genuine publish failure from a
    /// message that was skipped without an attempt (locked elsewhere, or not yet due).
    /// </summary>
    private enum MessageProcessingOutcome
    {
        Published,
        Failed,
        Skipped
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

            // The retry policy's MaxAttempts is the authority on when a message is exhausted;
            // it is combined with the row's own MaxPublishAttempts so a caller can never exceed
            // whichever limit is stricter.
            var attemptCeiling = Math.Min(_retryOptions.MaxAttempts, message.MaxPublishAttempts);

            if (message.PublishAttempts >= attemptCeiling)
            {
                _logger.LogError(
                    "Message {MessageId} exhausted retries ({Attempts}). Moving to dead letter.",
                    message.Id, message.PublishAttempts);

                if (_deadLetterService is not null)
                {
                    await _deadLetterService.MoveToDlqAsync(message, cancellationToken);
                }
                else
                {
                    var deadLetter = DeadLetter.FromOutboxMessage(message);
                    await _deadLetterRepository.AddAsync(deadLetter, cancellationToken);
                }

                message.State = OutboxMessageState.Failed; // Explicitly mark as failed / dead-lettered
            }
            else
            {
                // Not yet exhausted: schedule the next attempt according to the configured
                // backoff strategy instead of letting it be picked up again immediately.
                var delay = _retryOptions.ComputeNextDelay(message.PublishAttempts);
                message.ScheduledFor = DateTime.UtcNow.Add(delay);
                message.State = OutboxMessageState.Pending;

                _logger.LogWarning(
                    "Message {MessageId} failed attempt {Attempt}/{MaxAttempts}. Retrying at {ScheduledFor} using {Strategy}.",
                    message.Id, message.PublishAttempts, attemptCeiling, message.ScheduledFor, _retryOptions.BackoffStrategy);
            }

            await _outboxRepository.UpdateAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling publishing failure for message {MessageId}", message?.Id);
        }
    }
}
