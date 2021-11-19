#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using DotnetOutboxPattern.Domain;

namespace Examples
{
    /// <summary>
    /// Provides System.Text.Json serialization extensions for OutboxMetricsCollector.
    /// </summary>
    public static class OutboxMetricsCollectorJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the OutboxMetricsCollector statistics to a JSON string.
        /// </summary>
        /// <param name="value">The metrics collector instance.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the metrics.</returns>
        public static string ToJson(this OutboxMetricsCollector value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var statistics = new
            {
                TotalMessages = value.TotalMessages,
                PendingMessages = value.PendingMessages,
                PublishedMessages = value.PublishedMessages,
                FailedMessages = value.FailedMessages,
                ArchivedMessages = value.ArchivedMessages,
                DeadLetterCount = value.DeadLetterCount,
                AveragePublishTime = value.AveragePublishTime.TotalSeconds,
                OldestPendingAge = value.OldestPendingAge?.TotalSeconds,
                SuccessRate = value.SuccessRate
            };

            var options = indented
                ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
                : _jsonSerializerOptions;

            return JsonSerializer.Serialize(statistics, options);
        }

        /// <summary>
        /// Deserializes a JSON string to an OutboxMetricsCollector instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>An OutboxMetricsCollector instance, or null if the JSON is invalid.</returns>
        public static OutboxMetricsCollector? FromJson(string json)
        {
            try
            {
                var statistics = JsonSerializer.Deserialize<OutboxStatisticsDto>(json, _jsonSerializerOptions);
                if (statistics == null)
                {
                    return null;
                }

                return new OutboxMetricsCollector(
                    new FakeOutboxService(statistics),
                    new FakeDeadLetterService(statistics),
                    new FakeLogger<OutboxMetricsCollector>()
                );
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to an OutboxMetricsCollector instance.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized OutboxMetricsCollector instance, or null if deserialization failed.</param>
        /// <returns>True if deserialization succeeded, false otherwise.</returns>
        public static bool TryFromJson(string json, out OutboxMetricsCollector? value)
        {
            try
            {
                value = FromJson(json);
                return value != null;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// DTO for JSON serialization/deserialization of OutboxStatistics.
        /// </summary>
        private sealed class OutboxStatisticsDto
        {
            public long TotalMessages { get; set; }
            public long PendingMessages { get; set; }
            public long PublishedMessages { get; set; }
            public long FailedMessages { get; set; }
            public long ArchivedMessages { get; set; }
            public long DeadLetterCount { get; set; }
            public double AveragePublishTime { get; set; }
            public double? OldestPendingAge { get; set; }
            public double SuccessRate { get; set; }
        }

        /// <summary>
        /// Fake IOutboxService implementation for deserialization.
        /// </summary>
        private sealed class FakeOutboxService(OutboxStatisticsDto statistics) : IOutboxService
        {
            public Task<OutboxMessage> PublishEventAsync(PublishableEvent publishableEvent, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task<OutboxMessage> PublishEventAsync(DomainEvent domainEvent, string topic, string? partitionKey = null, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task<OutboxMessage?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
            {
                var stats = new OutboxStatistics
                {
                    TotalMessages = statistics.TotalMessages,
                    PendingMessages = statistics.PendingMessages,
                    PublishedMessages = statistics.PublishedMessages,
                    FailedMessages = statistics.FailedMessages,
                    ArchivedMessages = statistics.ArchivedMessages,
                    DeadLetterCount = statistics.DeadLetterCount,
                    AveragePublishTime = TimeSpan.FromSeconds(statistics.AveragePublishTime),
                    OldestPendingAge = statistics.OldestPendingAge.HasValue ? TimeSpan.FromSeconds(statistics.OldestPendingAge.Value) : null
                };
                return Task.FromResult(stats);
            }

            public Task<bool> RetryFailedMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task ArchiveOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task<List<OutboxMessage>> GetAllMessagesAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task<List<OutboxMessage>> GetMessagesByTopicAsync(string topic, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task<List<OutboxMessage>> GetMessagesByAggregateAsync(string aggregateId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task<List<OutboxMessage>> GetMessagesByStateAsync(OutboxMessageState state, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task<List<OutboxMessage>> GetMessagesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");
        }

        /// <summary>
        /// Fake IDeadLetterService implementation for deserialization.
        /// </summary>
        private sealed class FakeDeadLetterService(OutboxStatisticsDto statistics) : IDeadLetterService
        {
            public Task<DeadLetter?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task<List<DeadLetter>> GetUnreviewedAsync(CancellationToken cancellationToken = default)
            {
                var count = statistics.DeadLetterCount;
                return Task.FromResult(new List<DeadLetter>());
            }

            public Task<int> GetUnreviewedCountAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult((int)statistics.DeadLetterCount);
            }

            public Task<DeadLetter> AddAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task UpdateAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");

            public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
                => throw new NotSupportedException("This is a fake service for deserialization only.");
        }

        /// <summary>
        /// Fake ILogger implementation for deserialization.
        /// </summary>
        private sealed class FakeLogger<T> : ILogger<T>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}