#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Default implementation of IMessagePublisher for testing/demo purposes
/// In production, replace this with your actual message broker implementation
/// (e.g., RabbitMQ, Azure Service Bus, Kafka, AWS SQS, etc.)
/// </summary>
public sealed class DefaultMessagePublisher : IMessagePublisher
{
    private readonly ILogger<DefaultMessagePublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DefaultMessagePublisher(ILogger<DefaultMessagePublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Publishes a message to the configured message broker
    /// This is a stub implementation that logs the message
    /// </summary>
    /// <param name="message">The outbox message to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Simulate some work
        _logger.LogInformation(
            "Publishing message {MessageId} to topic {Topic}. AggregateId: {AggregateId}, EventType: {EventType}",
            message.Id, message.Topic, message.AggregateId, message.EventType);

        // TODO: Implement actual publishing to your message broker here
        // Example implementations:
        // - RabbitMQ: channel.BasicPublish(exchange, topic, null, Encoding.UTF8.GetBytes(message.EventData));
        // - Azure Service Bus: await sender.SendMessageAsync(new ServiceBusMessage(message.EventData));
        // - Kafka: await producer.ProduceAsync(topic, new Message<string, string> { Key = message.PartitionKey, Value = message.EventData });

        return Task.CompletedTask;
    }
}

/// <summary>
/// Helper class for creating message publishers with different transports
/// </summary>
public static class MessagePublisherFactory
{
    /// <summary>
    /// Creates a publisher that logs messages (for testing)
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <returns>An instance of IMessagePublisher.</returns>
    public static IMessagePublisher CreateLoggingPublisher(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return new LoggingPublisher(logger);
    }

    /// <summary>
    /// Logging publisher for testing and debugging
    /// </summary>
    private class LoggingPublisher : IMessagePublisher
    {
        private readonly ILogger _logger;

        public LoggingPublisher(ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
        }

        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            _logger.LogInformation(
                "PUBLISHED: MessageId={MessageId}, Topic={Topic}, AggregateId={AggregateId}, Attempts={Attempts}",
                message.Id, message.Topic, message.AggregateId, message.PublishAttempts + 1);

            return Task.CompletedTask;
        }
    }
}
