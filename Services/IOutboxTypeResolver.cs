#nullable enable

using System.Collections.Concurrent;

namespace DotnetOutboxPattern.Services;

/// <summary>
/// Resolves the stored <c>MessageType</c> name of an outbox envelope to the CLR type that should
/// be used to deserialize its payload. Decoupling the stored name from the CLR type lets a message
/// type be renamed or moved to a different namespace/assembly without breaking deserialization of
/// rows that were written to the outbox before the refactor shipped.
/// </summary>
public interface IOutboxTypeResolver
{
    /// <summary>
    /// Registers (or overwrites) the CLR type that a stored message type name resolves to.
    /// </summary>
    /// <param name="messageTypeName">The stable, stored name of the message type.</param>
    /// <param name="clrType">The CLR type to resolve <paramref name="messageTypeName"/> to.</param>
    /// <exception cref="ArgumentException"><paramref name="messageTypeName"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="clrType"/> is <see langword="null"/>.</exception>
    void Register(string messageTypeName, Type clrType);

    /// <summary>
    /// Attempts to resolve a stored message type name to a CLR type.
    /// </summary>
    /// <param name="messageTypeName">The stored message type name to resolve.</param>
    /// <param name="clrType">
    /// When this method returns <see langword="true"/>, the resolved CLR type; otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="messageTypeName"/> was resolved; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="messageTypeName"/> is null or empty.</exception>
    bool TryResolve(string messageTypeName, out Type? clrType);
}

/// <summary>
/// Default <see cref="IOutboxTypeResolver"/> backed by an explicit, in-memory registration table.
/// Message types must be registered up front (typically at startup); nothing is inferred from
/// <see cref="Type.GetType(string)"/>, so a renamed or removed CLR type never silently resolves to
/// the wrong type - it simply fails to resolve, which routes the row to the dead letter queue.
/// </summary>
public sealed class DefaultOutboxTypeResolver : IOutboxTypeResolver
{
    private readonly ConcurrentDictionary<string, Type> _typesByName = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates an empty resolver with no registered message types.
    /// </summary>
    public DefaultOutboxTypeResolver()
    {
    }

    /// <summary>
    /// Creates a resolver pre-populated with the supplied message type mappings.
    /// </summary>
    /// <param name="mappings">The initial set of message type name to CLR type mappings.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mappings"/> is <see langword="null"/>.</exception>
    public DefaultOutboxTypeResolver(IEnumerable<KeyValuePair<string, Type>> mappings)
    {
        ArgumentNullException.ThrowIfNull(mappings);

        foreach (var mapping in mappings)
        {
            Register(mapping.Key, mapping.Value);
        }
    }

    /// <inheritdoc />
    public void Register(string messageTypeName, Type clrType)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageTypeName);
        ArgumentNullException.ThrowIfNull(clrType);

        _typesByName[messageTypeName] = clrType;
    }

    /// <inheritdoc />
    public bool TryResolve(string messageTypeName, out Type? clrType)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageTypeName);

        return _typesByName.TryGetValue(messageTypeName, out clrType);
    }
}
