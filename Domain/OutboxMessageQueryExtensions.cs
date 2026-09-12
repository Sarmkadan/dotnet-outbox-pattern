#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Provides LINQ-style extension methods for querying <see cref="OutboxMessage"/> collections.
/// </summary>
public static class OutboxMessageQueryExtensions
{
    /// <summary>
    /// Filters messages by their processing state.
    /// </summary>
    /// <param name="source">The source collection of outbox messages.</param>
    /// <param name="state">The state to filter by.</param>
    /// <returns>An IEnumerable containing only messages with the specified state.</returns>
    public static IEnumerable<OutboxMessage> WhereState(this IEnumerable<OutboxMessage> source, OutboxMessageState state)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return source.Where(m => m.State == state);
    }

    /// <summary>
    /// Orders messages by their creation timestamp in ascending order (oldest first).
    /// </summary>
    /// <param name="source">The source collection of outbox messages.</param>
    /// <returns>An IEnumerable ordered by CreatedAt ascending.</returns>
    public static IEnumerable<OutboxMessage> OrderByCreatedAt(this IEnumerable<OutboxMessage> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return source.OrderBy(m => m.CreatedAt);
    }

    /// <summary>
    /// Orders messages by their creation timestamp in descending order (newest first).
    /// </summary>
    /// <param name="source">The source collection of outbox messages.</param>
    /// <returns>An IEnumerable ordered by CreatedAt descending.</returns>
    public static IEnumerable<OutboxMessage> OrderByCreatedAtDescending(this IEnumerable<OutboxMessage> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return source.OrderByDescending(m => m.CreatedAt);
    }
}