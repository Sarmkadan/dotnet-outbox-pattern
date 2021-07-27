// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

/// <summary>
/// Example 2: Custom Message Publisher Implementation
///
/// Demonstrates how to implement a custom IMessagePublisher to publish
/// messages to your actual message broker (RabbitMQ, Kafka, etc.)
/// </summary>

namespace Examples
{
    // Example 1: RabbitMQ Publisher
    public class RabbitMqMessagePublisher : IMessagePublisher
    {
        private readonly ILogger<RabbitMqMessagePublisher> _logger;

        // In production, inject IConnectionFactory and IModel from RabbitMQ.Client
        public RabbitMqMessagePublisher(ILogger<RabbitMqMessagePublisher> logger)
        {
            _logger = logger;
        }

        public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Publishing message {MessageId} to RabbitMQ exchange {Topic}",
                    message.Id, message.Topic);

                // In production implementation:
                // var channel = _connection.CreateModel();
                // channel.ExchangeDeclare(message.Topic, ExchangeType.Topic, durable: true);
                // var body = Encoding.UTF8.GetBytes(message.EventData);
                // var properties = channel.CreateBasicProperties();
                // properties.DeliveryMode = 2;  // Persistent
                // properties.ContentType = "application/json";
                // channel.BasicPublish(
                //     exchange: message.Topic,
                //     routingKey: message.PartitionKey ?? message.Topic,
                //     basicProperties: properties,
                //     body: body);

                _logger.LogInformation("Message published successfully to RabbitMQ");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message {MessageId}", message.Id);
                throw;
            }
        }
    }

    // Example 2: Kafka Publisher
    public class KafkaMessagePublisher : IMessagePublisher
    {
        private readonly ILogger<KafkaMessagePublisher> _logger;

        public KafkaMessagePublisher(ILogger<KafkaMessagePublisher> logger)
        {
            _logger = logger;
        }

        public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Publishing message {MessageId} to Kafka topic {Topic}",
                    message.Id, message.Topic);

                // In production implementation:
                // using var producer = new ProducerBuilder<string, string>(config).Build();
                // var result = await producer.ProduceAsync(
                //     topic: message.Topic,
                //     message: new Message<string, string>
                //     {
                //         Key = message.PartitionKey,
                //         Value = message.EventData,
                //         Headers = new Headers
                //         {
                //             new Header("message-id", Encoding.UTF8.GetBytes(message.Id.ToString())),
                //             new Header("idempotency-key", Encoding.UTF8.GetBytes(message.IdempotencyKey ?? ""))
                //         }
                //     },
                //     cancellationToken);

                _logger.LogInformation("Message published successfully to Kafka");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message {MessageId}", message.Id);
                throw;
            }
        }
    }

    // Example 3: Azure Service Bus Publisher
    public class AzureServiceBusPublisher : IMessagePublisher
    {
        private readonly ILogger<AzureServiceBusPublisher> _logger;

        public AzureServiceBusPublisher(ILogger<AzureServiceBusPublisher> logger)
        {
            _logger = logger;
        }

        public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Publishing message {MessageId} to Azure Service Bus topic {Topic}",
                    message.Id, message.Topic);

                // In production implementation:
                // var sender = _client.CreateSender(message.Topic);
                // var sbMessage = new ServiceBusMessage(message.EventData)
                // {
                //     ContentType = "application/json",
                //     CorrelationId = message.IdempotencyKey,
                //     SessionId = message.PartitionKey,
                //     ApplicationProperties =
                //     {
                //         { "outbox-id", message.Id.ToString() },
                //         { "aggregate-id", message.AggregateId }
                //     }
                // };
                // await sender.SendMessageAsync(sbMessage, cancellationToken);

                _logger.LogInformation("Message published successfully to Azure Service Bus");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message {MessageId}", message.Id);
                throw;
            }
        }
    }

    // Example 4: HTTP Webhook Publisher (for webhook delivery)
    public class WebhookPublisher : IMessagePublisher
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebhookPublisher> _logger;

        public WebhookPublisher(HttpClient httpClient, ILogger<WebhookPublisher> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            try
            {
                // For webhook delivery, the topic would be the webhook URL
                // e.g., topic: "https://webhook.example.com/events"
                var webhookUrl = message.Topic;

                _logger.LogInformation(
                    "Publishing message {MessageId} via webhook to {Url}",
                    message.Id, webhookUrl);

                var content = new StringContent(
                    message.EventData,
                    Encoding.UTF8,
                    "application/json");

                // Add custom headers for webhook delivery
                var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
                {
                    Content = content
                };
                request.Headers.Add("X-Outbox-Message-Id", message.Id.ToString());
                request.Headers.Add("X-Idempotency-Key", message.IdempotencyKey ?? "");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation(
                    "Message delivered via webhook, status code: {StatusCode}",
                    response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deliver webhook for message {MessageId}", message.Id);
                throw;
            }
        }
    }

    // Example setup for dependency injection
    public static class PublisherSetupExample
    {
        public static void Main()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            // Register custom publisher - choose one:

            // For RabbitMQ:
            // services.AddMessagePublisher<RabbitMqMessagePublisher>();
            // services.AddSingleton(sp => new ConnectionFactory { HostName = "localhost" });

            // For Kafka:
            // services.AddMessagePublisher<KafkaMessagePublisher>();

            // For Azure Service Bus:
            // services.AddMessagePublisher<AzureServiceBusPublisher>();
            // services.AddSingleton(sp => new ServiceBusClient(connectionString));

            // For Webhooks:
            // services.AddMessagePublisher<WebhookPublisher>();
            // services.AddHttpClient<WebhookPublisher>();

            Console.WriteLine("Custom Message Publisher Examples");
            Console.WriteLine("Choose one implementation based on your message broker");
        }
    }
}
