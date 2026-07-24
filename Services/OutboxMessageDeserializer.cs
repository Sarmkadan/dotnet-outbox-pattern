#nullable enable

using DotnetOutboxPattern.Domain;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Deserializes the version-tolerant envelope stored in an <see cref="OutboxMessage"/>'s payload,
/// routing messages that fail deserialization (unresolvable message type, or a payload that no
/// longer matches its CLR type after a refactor) to the dead letter queue instead of letting the
/// failure propagate and crash the dispatch loop.
/// </summary>
public interface IOutboxMessageDeserializer
{
    /// <summary>
    /// Attempts to deserialize <paramref name="message"/>'s payload. On failure the message is
    /// moved to the dead letter queue and <see langword="null"/> is returned.
    /// </summary>
    /// <param name="message">The outbox message whose payload should be deserialized.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized value, or <see langword="null"/> if deserialization failed and the message was dead-lettered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    Task<object?> DeserializeOrDeadLetterAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="IOutboxMessageDeserializer"/> implementation, composing an
/// <see cref="IOutboxSerializer"/>, an <see cref="IOutboxTypeResolver"/> and an
/// <see cref="IDeadLetterService"/>.
/// </summary>
public sealed class OutboxMessageDeserializer : IOutboxMessageDeserializer
{
    private readonly IOutboxSerializer _serializer;
    private readonly IOutboxTypeResolver _typeResolver;
    private readonly IDeadLetterService _deadLetterService;
    private readonly ILogger<OutboxMessageDeserializer> _logger;

    /// <summary>
    /// Creates a new version-tolerant outbox message deserializer.
    /// </summary>
    /// <param name="serializer">The serializer used to decode the envelope and payload.</param>
    /// <param name="typeResolver">The resolver used to map the stored message type name to a CLR type.</param>
    /// <param name="deadLetterService">The dead letter service used to route failed deserializations.</param>
    /// <param name="logger">Logger for deserialization diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Any of <paramref name="serializer"/>, <paramref name="typeResolver"/>,
    /// <paramref name="deadLetterService"/>, or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public OutboxMessageDeserializer(
        IOutboxSerializer serializer,
        IOutboxTypeResolver typeResolver,
        IDeadLetterService deadLetterService,
        ILogger<OutboxMessageDeserializer> logger)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
        _deadLetterService = deadLetterService ?? throw new ArgumentNullException(nameof(deadLetterService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<object?> DeserializeOrDeadLetterAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var result = _serializer.DeserializeEnvelope(message.EventData, _typeResolver);
        if (result.Success)
        {
            return result.Value;
        }

        _logger.LogError(
            "Message {MessageId} could not be deserialized (type: {MessageType}, schema version: {SchemaVersion}): {Error}. Moving to dead letter queue.",
            message.Id, result.MessageType, result.SchemaVersion, result.Error);

        message.RecordFailure(result.Error ?? "version-tolerant deserialization failed", null);
        await _deadLetterService.MoveToDlqAsync(message, cancellationToken);

        return null;
    }
}
