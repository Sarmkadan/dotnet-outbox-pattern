#nullable enable

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Extension methods for <see cref="DefaultMessagePublisher"/> to provide additional functionality
/// </summary>
public static class DefaultMessagePublisherExtensions
{
    /// <summary>
    /// Gets the default retry delay for transient failures
    /// </summary>
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Creates a new <see cref="DefaultMessagePublisher"/> with the specified logger
    /// </summary>
    /// <param name="logger">The logger to use for publishing operations</param>
    /// <returns>A new instance of <see cref="DefaultMessagePublisher"/></returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null</exception>
    public static DefaultMessagePublisher WithLogger(this ILogger<DefaultMessagePublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return new(logger);
    }

    /// <summary>
    /// Publishes multiple messages asynchronously in a single batch
    /// </summary>
    /// <param name="publisher">The message publisher instance</param>
    /// <param name="messages">The collection of messages to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous publish operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> or <paramref name="messages"/> is null</exception>
    public static async Task PublishBatchAsync(
        this DefaultMessagePublisher publisher,
        IEnumerable<OutboxMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(messages);

        foreach (var message in messages)
        {
            await publisher.PublishAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Publishes multiple messages asynchronously in parallel with throttling
    /// </summary>
    /// <param name="publisher">The message publisher instance</param>
    /// <param name="messages">The collection of messages to publish</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of concurrent publishes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous publish operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> or <paramref name="messages"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxDegreeOfParallelism"/> is less than 1</exception>
    public static async Task PublishBatchAsync(
        this DefaultMessagePublisher publisher,
        IEnumerable<OutboxMessage> messages,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, 1);

        var throttler = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = messages.Select(async message =>
        {
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await publisher.PublishAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a message with retry logic for transient failures
    /// </summary>
    /// <param name="publisher">The message publisher instance</param>
    /// <param name="message">The message to publish</param>
    /// <param name="maxRetries">Maximum number of retry attempts. Must be non-negative.</param>
    /// <param name="retryDelay">Delay between retry attempts. If null, uses <see cref="DefaultRetryDelay"/></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous publish operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> or <paramref name="message"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRetries"/> is less than 0</exception>
    public static async Task PublishWithRetryAsync(
        this DefaultMessagePublisher publisher,
        OutboxMessage message,
        int maxRetries = 3,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        retryDelay ??= DefaultRetryDelay;

        var attempts = 0;
        while (true)
        {
            try
            {
                await publisher.PublishAsync(message, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception) when (attempts < maxRetries)
            {
                attempts++;
                await Task.Delay(retryDelay.Value, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Creates a logging publisher that wraps the current publisher
    /// </summary>
    /// <param name="publisher">The message publisher instance</param>
    /// <param name="logger">Logger for the logging publisher</param>
    /// <returns>A new logging publisher instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> or <paramref name="logger"/> is null</exception>
    public static IMessagePublisher WithLoggingDecorator(
        this DefaultMessagePublisher publisher,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(logger);

        return MessagePublisherFactory.CreateLoggingPublisher(logger);
    }
}