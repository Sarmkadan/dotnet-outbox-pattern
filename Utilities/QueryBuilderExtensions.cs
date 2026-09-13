#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Convenience extension methods for <see cref="QueryBuilder"/>.
/// </summary>
public static class QueryBuilderExtensions
{
    /// <summary>
    /// Gets the number of filter conditions in the query builder.
    /// </summary>
    /// <param name="qb">The query builder instance.</param>
    /// <returns>The count of filter conditions.</returns>
    public static int GetConditionCount(this QueryBuilder qb)
    {
        return qb.GetConditions().Count;
    }

    /// <summary>
    /// Determines whether the query builder has any filter conditions.
    /// </summary>
    /// <param name="qb">The query builder instance.</param>
    /// <returns>true if the query builder has at least one condition; otherwise, false.</returns>
    public static bool HasAnyConditions(this QueryBuilder qb)
    {
        return qb.GetConditions().Count > 0;
    }
}