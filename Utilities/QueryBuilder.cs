// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Fluent query builder for constructing complex filter conditions
/// Simplifies building dynamic queries without SQL injection risks
/// </summary>
public class QueryBuilder
{
    private readonly List<FilterCondition> _conditions = new();
    private string? _orderBy;
    private bool _orderDescending = true;

    /// <summary>
    /// Adds an equality filter condition
    /// </summary>
    public QueryBuilder Where(string field, object value)
    {
        _conditions.Add(new FilterCondition
        {
            Field = field,
            Operator = FilterOperator.Equals,
            Value = value
        });
        return this;
    }

    /// <summary>
    /// Adds a "greater than" condition
    /// </summary>
    public QueryBuilder WhereGreaterThan(string field, object value)
    {
        _conditions.Add(new FilterCondition
        {
            Field = field,
            Operator = FilterOperator.GreaterThan,
            Value = value
        });
        return this;
    }

    /// <summary>
    /// Adds a "less than" condition
    /// </summary>
    public QueryBuilder WhereLessThan(string field, object value)
    {
        _conditions.Add(new FilterCondition
        {
            Field = field,
            Operator = FilterOperator.LessThan,
            Value = value
        });
        return this;
    }

    /// <summary>
    /// Adds a "contains" (LIKE) condition
    /// </summary>
    public QueryBuilder WhereContains(string field, string value)
    {
        _conditions.Add(new FilterCondition
        {
            Field = field,
            Operator = FilterOperator.Contains,
            Value = value
        });
        return this;
    }

    /// <summary>
    /// Adds an "in" condition (matches any value in list)
    /// </summary>
    public QueryBuilder WhereIn(string field, params object[] values)
    {
        _conditions.Add(new FilterCondition
        {
            Field = field,
            Operator = FilterOperator.In,
            Value = values
        });
        return this;
    }

    /// <summary>
    /// Adds a "between" condition (inclusive)
    /// </summary>
    public QueryBuilder WhereBetween(string field, object minValue, object maxValue)
    {
        _conditions.Add(new FilterCondition
        {
            Field = field,
            Operator = FilterOperator.Between,
            Value = new[] { minValue, maxValue }
        });
        return this;
    }

    /// <summary>
    /// Adds a "is null" condition
    /// </summary>
    public QueryBuilder WhereIsNull(string field)
    {
        _conditions.Add(new FilterCondition
        {
            Field = field,
            Operator = FilterOperator.IsNull,
            Value = null
        });
        return this;
    }

    /// <summary>
    /// Adds an "is not null" condition
    /// </summary>
    public QueryBuilder WhereIsNotNull(string field)
    {
        _conditions.Add(new FilterCondition
        {
            Field = field,
            Operator = FilterOperator.IsNotNull,
            Value = null
        });
        return this;
    }

    /// <summary>
    /// Sets the sort order
    /// </summary>
    public QueryBuilder OrderBy(string field, bool descending = false)
    {
        _orderBy = field;
        _orderDescending = descending;
        return this;
    }

    /// <summary>
    /// Gets all conditions as a list
    /// </summary>
    public List<FilterCondition> GetConditions() => _conditions;

    /// <summary>
    /// Gets the filter expression as a human-readable string
    /// </summary>
    public string ToFilterString()
    {
        if (_conditions.Count == 0)
            return "(no filters)";

        var parts = _conditions.Select(c => c.ToString()).ToList();
        return string.Join(" AND ", parts);
    }

    /// <summary>
    /// Gets a summary of applied filters
    /// </summary>
    public Dictionary<string, object?> GetFilterSummary()
    {
        var summary = new Dictionary<string, object?>();

        foreach (var condition in _conditions)
        {
            var key = $"{condition.Field}_{condition.Operator}";
            summary[key] = condition.Value;
        }

        return summary;
    }
}

/// <summary>
/// Represents a single filter condition
/// </summary>
public class FilterCondition
{
    public string Field { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public object? Value { get; set; }

    public override string ToString()
    {
        return Operator switch
        {
            FilterOperator.Equals => $"{Field} = {Value}",
            FilterOperator.GreaterThan => $"{Field} > {Value}",
            FilterOperator.LessThan => $"{Field} < {Value}",
            FilterOperator.Contains => $"{Field} CONTAINS {Value}",
            FilterOperator.In => $"{Field} IN ({string.Join(",", (object[])Value!)})",
            FilterOperator.Between => $"{Field} BETWEEN {((object[])Value!)[0]} AND {((object[])Value!)[1]}",
            FilterOperator.IsNull => $"{Field} IS NULL",
            FilterOperator.IsNotNull => $"{Field} IS NOT NULL",
            _ => $"{Field} {Operator}"
        };
    }
}

/// <summary>
/// Filter operator types
/// </summary>
public enum FilterOperator
{
    Equals = 0,
    GreaterThan = 1,
    LessThan = 2,
    GreaterThanOrEqual = 3,
    LessThanOrEqual = 4,
    Contains = 5,
    StartsWith = 6,
    EndsWith = 7,
    In = 8,
    Between = 9,
    IsNull = 10,
    IsNotNull = 11,
    NotEqual = 12
}
