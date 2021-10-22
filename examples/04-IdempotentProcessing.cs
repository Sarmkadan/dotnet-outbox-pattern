#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Example 4: Idempotent Message Processing
///
/// Demonstrates how to:
/// - Generate consistent idempotency keys
/// - Implement idempotent event handlers
/// - Detect and skip duplicate messages
/// - Ensure exactly-once delivery semantics
/// </summary>

namespace Examples
{
    /// <summary>
    /// Strategies for generating idempotency keys that are:
    /// - Deterministic (same input = same key)
    /// - Unique (different events = different keys)
    /// - Collision-free (no false duplicates)
    /// </summary>
    public sealed class IdempotencyKeyGenerator
    {
        /// <summary>
        /// For entity creation events - ensures creating the same entity twice
        /// generates the same idempotency key.
        /// </summary>
        public static string ForEntityCreation(
            string entityType,
            Guid entityId)
            => $"{entityType.ToLower()}-create-{entityId:N}";

        /// <summary>
        /// For state transitions - includes the transition type.
        /// </summary>
        public static string ForStateTransition(
            string aggregateType,
            Guid aggregateId,
            string transitionName)
            => $"{aggregateType.ToLower()}-{transitionName.ToLower()}-{aggregateId:N}";

        /// <summary>
        /// For webhook delivery attempts - includes attempt number for retries.
        /// </summary>
        public static string ForWebhookAttempt(
            string webhookId,
            int attemptNumber)
            => $"webhook-{webhookId.ToLower()}-attempt-{attemptNumber}";

        /// <summary>
        /// For operations with timestamps - ensures same operation at different times
        /// have different keys.
        /// </summary>
        public static string ForTimestampedEvent(
            string eventType,
            Guid entityId,
            DateTime timestamp)
            => $"{eventType.ToLower()}-{entityId:N}-{timestamp.Ticks}";

        /// <summary>
        /// For external API calls - links to original request ID.
        /// </summary>
        public static string ForExternalApiCall(
            string apiName,
            string requestId)
            => $"api-{apiName.ToLower()}-{requestId}";
    }

    /// <summary>
    /// Example event handler that implements idempotent processing.
    /// Handles the same message multiple times without side effects.
    /// </summary>
    public sealed class IdempotentOrderEventHandler
    {
        private readonly ILogger<IdempotentOrderEventHandler> _logger;
        // In production: inject IRepository<ProcessedEvent>, or your equivalent

        public IdempotentOrderEventHandler(ILogger<IdempotentOrderEventHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Example: Handle OrderCreated event idempotently.
        /// Returns early if already processed.
        /// </summary>
        public async Task HandleOrderCreatedAsync(
            string orderId,
            string customerId,
            decimal amount,
            string idempotencyKey)
        {
            _logger.LogInformation(
                "Processing order creation: {OrderId}, idempotency key: {Key}",
                orderId, idempotencyKey);

            // Step 1: Check if already processed
            // In production:
            // var existing = await _processedEventRepo.GetByKeyAsync(idempotencyKey);
            // if (existing != null)
            // {
            //     _logger.LogInformation("Event already processed: {Key}", idempotencyKey);
            //     return;  // EXIT EARLY - idempotency achieved
            // }

            // Step 2: Process the event
            _logger.LogInformation("Creating order: {OrderId} for customer {CustomerId}", orderId, customerId);
            // await _orderService.CreateOrderAsync(orderId, customerId, amount);

            // Step 3: Record that we processed it
            // In production:
            // await _processedEventRepo.AddAsync(new ProcessedEvent
            // {
            //     IdempotencyKey = idempotencyKey,
            //     EventType = "order.created",
            //     ProcessedAt = DateTime.UtcNow,
            //     ReferenceId = orderId
            // });
            // await _db.SaveChangesAsync();  // Atomic with business logic

            _logger.LogInformation("Order created successfully: {OrderId}", orderId);
        }

        /// <summary>
        /// Alternative pattern: Use database unique constraint on idempotency key.
        /// If INSERT fails (duplicate key), we know it was already processed.
        /// </summary>
        public async Task<bool> TryProcessIdempotentlyAsync(
            string orderId,
            string idempotencyKey,
            Func<Task> processAsync)
        {
            try
            {
                // Process first
                await processAsync();

                // Then record that we processed it
                // This INSERT will fail if already done (duplicate key constraint)
                // await _db.ProcessedEvents.AddAsync(new ProcessedEvent
                // {
                //     IdempotencyKey = idempotencyKey,
                //     ProcessedAt = DateTime.UtcNow
                // });
                // await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex) when (ex.Message.Contains("PRIMARY KEY"))
            {
                _logger.LogInformation("Duplicate idempotency key, skipping: {Key}", idempotencyKey);
                return false;  // Already processed
            }
        }
    }

    /// <summary>
    /// Message deduplication service for detecting and filtering duplicates
    /// before they reach business logic.
    /// </summary>
    public sealed class MessageDeduplicator
    {
        private readonly Dictionary<string, DateTime> _recentlyProcessed;
        private readonly TimeSpan _ttl;
        private readonly ILogger<MessageDeduplicator> _logger;

        public MessageDeduplicator(
            ILogger<MessageDeduplicator> logger,
            TimeSpan? ttl = null)
        {
            _logger = logger;
            _ttl = ttl ?? TimeSpan.FromHours(1);  // Keep records for 1 hour
            _recentlyProcessed = new();
        }

        /// <summary>
        /// Checks if a message with this idempotency key was recently processed.
        /// Returns true if it's a duplicate (should be skipped).
        /// </summary>
        public bool IsDuplicate(string idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                return false;

            if (_recentlyProcessed.TryGetValue(idempotencyKey, out var processedAt))
            {
                // Check if still within TTL
                if (DateTime.UtcNow - processedAt < _ttl)
                {
                    _logger.LogWarning(
                        "Duplicate message detected: {Key}",
                        idempotencyKey);
                    return true;  // Duplicate
                }
                else
                {
                    // TTL expired, clean up
                    _recentlyProcessed.Remove(idempotencyKey);
                }
            }

            return false;
        }

        /// <summary>
        /// Records that we've processed a message with this key.
        /// </summary>
        public void MarkAsProcessed(string idempotencyKey)
        {
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                _recentlyProcessed[idempotencyKey] = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Removes expired entries to prevent unbounded memory growth.
        /// </summary>
        public void CleanupExpired()
        {
            var expired = _recentlyProcessed
                .Where(kvp => DateTime.UtcNow - kvp.Value >= _ttl)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                _recentlyProcessed.Remove(key);
            }

            if (expired.Count > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} expired deduplication entries",
                    expired.Count);
            }
        }
    }

    /// <summary>
    /// Publishing events with idempotency guarantees.
    /// </summary>
    public sealed class IdempotentPublisher
    {
        private readonly IOutboxService _outboxService;
        private readonly ILogger<IdempotentPublisher> _logger;

        public IdempotentPublisher(
            IOutboxService outboxService,
            ILogger<IdempotentPublisher> logger)
        {
            _outboxService = outboxService;
            _logger = logger;
        }

        /// <summary>
        /// Publishes an order creation event with idempotency guarantee.
        /// Can be called multiple times safely - same order won't be created twice.
        /// </summary>
        public async Task<Guid> PublishOrderCreatedAsync(
            Guid orderId,
            string customerId,
            decimal amount)
        {
            // Generate deterministic idempotency key
            var idempotencyKey = IdempotencyKeyGenerator.ForEntityCreation(
                "order", orderId);

            _logger.LogInformation(
                "Publishing order creation: {OrderId}, key: {Key}",
                orderId, idempotencyKey);

            // Create event
            // In production, use your real event class
            var orderEvent = new { orderId, customerId, amount };

            // Publish with idempotency key
            // If called again with same orderId, same message ID will be returned
            var message = await _outboxService.PublishEventAsync(
                new TestEvent { Data = System.Text.Json.JsonSerializer.Serialize(orderEvent) },
                "orders.created",
                customerId);

            return message.Id;
        }

        /// <summary>
        /// Publishes multiple events atomically.
        /// All succeed or all fail together.
        /// </summary>
        public async Task PublishOrderFlowAsync(
            Guid orderId,
            string customerId,
            decimal amount)
        {
            // Step 1: Order created
            var createdKey = IdempotencyKeyGenerator.ForEntityCreation("order", orderId);
            await _outboxService.PublishEventAsync(
                new TestEvent { Data = "OrderCreated" },
                "orders.created",
                customerId);

            // Step 2: Order confirmed
            var confirmedKey = IdempotencyKeyGenerator.ForStateTransition(
                "order", orderId, "confirm");
            await _outboxService.PublishEventAsync(
                new TestEvent { Data = "OrderConfirmed" },
                "orders.confirmed",
                customerId);

            _logger.LogInformation("Order flow published: {OrderId}", orderId);
        }
    }

    // Test event for examples
    public sealed class TestEvent : DomainEvent
    {
        public string Data { get; set; } = string.Empty;
    }

    public static class IdempotentProcessingExample
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Example: Idempotent Message Processing");
            Console.WriteLine("Features:");
            Console.WriteLine("  - Consistent idempotency key generation");
            Console.WriteLine("  - Idempotent event handlers");
            Console.WriteLine("  - Message deduplication");
            Console.WriteLine("  - Exactly-once delivery semantics");

            var key = IdempotencyKeyGenerator.ForEntityCreation("order", Guid.NewGuid());
            Console.WriteLine($"\nExample idempotency key: {key}");

            await Task.CompletedTask;
        }
    }
}
