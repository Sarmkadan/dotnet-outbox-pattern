// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// AdvancedUsage.cs
/// 
/// Demonstrates advanced configuration: custom metadata, idempotency keys,
/// and error handling.
/// </summary>
namespace Examples
{
    public class AdvancedUsage
    {
        private readonly IOutboxService _outboxService;
        private readonly ILogger<AdvancedUsage> _logger;

        public AdvancedUsage(IOutboxService outboxService, ILogger<AdvancedUsage> logger)
        {
            _outboxService = outboxService;
            _logger = logger;
        }

        public async Task ExecuteAdvancedPublishAsync(object eventData, string orderId)
        {
            try
            {
                // Advanced publishing with metadata and idempotency key
                var metadata = new Dictionary<string, string>
                {
                    { "CorrelationId", Guid.NewGuid().ToString() },
                    { "Environment", "Production" }
                };

                await _outboxService.PublishEventAsync(
                    @event: eventData,
                    topic: "orders.processed",
                    partitionKey: orderId,
                    idempotencyKey: $"process-order-{orderId}",
                    metadata: metadata);
            }
            catch (Exception ex)
            {
                // Error handling
                _logger.LogError(ex, "Failed to publish event for order {OrderId}", orderId);
                throw;
            }
        }
    }
}
