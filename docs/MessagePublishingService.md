# MessagePublishingService

## Purpose
Service for processing and publishing outbox messages. Handles retries, dead letter routing, and lock management for reliable message delivery in distributed systems.

## Public Methods

### ProcessPendingMessagesAsync
```csharp
Task<OutboxProcessingResult> ProcessPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
```
Processes pending outbox messages in a batch. Uses batch claiming with row-level locking when enabled for competing consumers, otherwise falls back to original method. Returns processing result with counts of processed and failed messages.

### ProcessScheduledMessagesAsync
```csharp
Task<OutboxProcessingResult> ProcessScheduledMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
```
Processes messages scheduled for future delivery. Similar to pending messages processing but for scheduled messages. Uses batch claiming when enabled.

### ProcessPartitionAsync
```csharp
Task<OutboxProcessingResult> ProcessPartitionAsync(string partitionKey, int batchSize, CancellationToken cancellationToken = default)
```
Processes messages for a specific partition to maintain ordering. Processes messages sequentially within a partition to guarantee order. Stops processing the partition if a message fails.

### ProcessSingleMessageAsync
```csharp
Task<bool> ProcessSingleMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
```
Processes a single outbox message. Returns true if the message was successfully published, false otherwise (including skipped due to locking or scheduling).

### ReleaseLockAsync
```csharp
Task ReleaseLockAsync(Guid messageId, CancellationToken cancellationToken = default)
```
Releases the lock on a message for reprocessing. Resets lock state and returns message to pending state if it was processing.

## Usage Example

```csharp
// Setup dependencies (typically via DI container)
var outboxRepository = new OutboxRepository(dbContext);
var deadLetterRepository = new DeadLetterRepository(dbContext);
var publisher = new HttpMessagePublisher(httpClient, logger);
var logger = loggerFactory.CreateLogger<MessagePublishingService>();
var options = new PublishingOptions 
{ 
    UseBatchClaiming = true, 
    MaxBatchClaimSize = 100,
    LockDurationSeconds = 30,
    PublishTimeout = TimeSpan.FromSeconds(10)
};
var retryOptions = new OutboxRetryOptions 
{ 
    MaxAttempts = 5,
    BackoffStrategy = BackoffStrategy.Exponential
};
var deadLetterService = new DeadLetterService(deadLetterRepository, logger);

// Create service
var messagePublishingService = new MessagePublishingService(
    outboxRepository,
    deadLetterRepository,
    publisher,
    logger,
    options,
    retryOptions,
    deadLetterService);

// Process pending messages in batches of 50
var result = await messagePublishingService.ProcessPendingMessagesAsync(50);

if (result.Success)
{
    logger.LogInformation("Processed {ProcessedCount} messages, {FailedCount} failed", 
        result.ProcessedCount, result.FailedCount);
}
else
{
    logger.LogError("Error processing messages: {ErrorMessage}", result.ErrorMessage);
}

// Process a specific partition for ordered messaging
var partitionResult = await messagePublishingService.ProcessPartitionAsync("user-123", 20);

// Handle a single message by ID
var messageId = Guid.NewGuid();
var success = await messagePublishingService.ProcessSingleMessageAsync(messageId);

// Release lock on a message (e.g., after manual intervention)
await messagePublishingService.ReleaseLockAsync(messageId);
```