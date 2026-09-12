#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Serialization;

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Fluent query builder for constructing complex filter conditions
/// Simplifies building dynamic queries without SQL injection risks
/// </summary>
public sealed class QueryBuilder : IEquatable<QueryBuilder>
{
    private readonly List<FilterCondition> _conditions = new();
    private string? _orderBy;
    private bool _orderDescending = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBuilder"/> class.
    /// </summary>
    public QueryBuilder()
    {
    }

    /// <summary>
    /// Adds an equality filter condition
    /// </summary>
    public QueryBuilder Where(string field, object value)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);

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
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);

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
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);

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
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);

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
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(values);

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
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(minValue);
        ArgumentNullException.ThrowIfNull(maxValue);

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
        ArgumentNullException.ThrowIfNull(field);

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
        ArgumentNullException.ThrowIfNull(field);

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
        ArgumentNullException.ThrowIfNull(field);

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

    [JsonPropertyName("conditions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private List<FilterCondition> ConditionsForSerialization => _conditions;

    [JsonPropertyName("orderBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? OrderByForSerialization => _orderBy;

    [JsonPropertyName("orderDescending")]
    private bool OrderDescending => _orderDescending;

    // ------------------------------------------------------------------------
    // Equality members
    // ------------------------------------------------------------------------

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>true if the current object is equal to the <paramref name="other">parameter</paramref>; otherwise, false.</returns>
    public bool Equals(QueryBuilder? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (_orderBy != other._orderBy) return false;
        if (_orderDescending != other._orderDescending) return false;
        if (_conditions.Count != other._conditions.Count) return false;

        for (int i = 0; i < _conditions.Count; i++)
        {
            var a = _conditions[i];
            var b = other._conditions[i];

            if (a.Field != b.Field) return false;
            if (a.Operator != b.Operator) return false;

            if (a.Value is null && b.Value is not null) return false;
            if (a.Value is not null && b.Value is null) return false;

            if (a.Value is Array aArr && b.Value is Array bArr)
            {
                if (aArr.Length != bArr.Length) return false;
                for (int j = 0; j < aArr.Length; j++)
                {
                    if (!object.Equals(aArr.GetValue(j), bArr.GetValue(j))) return false;
                }
            }
            else
            {
                if (!object.Equals(a.Value, b.Value)) return false;
            }
        }

        return true;
    }

    /// <summary>
/// Determines whether the specified object is equal to the current object.
/// </summary>
/// <param name="obj">The object to compare with the current object.</param>
/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
public override bool Equals(object? obj) => Equals(obj as QueryBuilder);

    /// <summary>
/// Returns the hash code for this instance.
/// </summary>
/// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_orderBy);
        hash.Add(_orderDescending);
        hash.Add(_conditions.Count);

        foreach (var condition in _conditions)
        {
            hash.Add(condition.Field);
            hash.Add(condition.Operator);
            if (condition.Value is Array arr)
            {
                foreach (var item in arr)
                {
                    hash.Add(item);
                }
            }
            else
            {
                hash.Add(condition.Value);
            }
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(QueryBuilder? left, QueryBuilder? right) =>
        EqualityComparer<QueryBuilder>.Default.Equals(left, right);

    public static bool operator !=(QueryBuilder? left, QueryBuilder? right) => !(left == right);
}

/// <summary>
/// Represents a single filter condition
/// </summary>
public sealed class FilterCondition
{
    /// <summary>
    /// Gets or sets the field name for the filter condition.
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter operator for the condition.
    /// </summary>
    [JsonPropertyName("operator")]
    public FilterOperator Operator { get; set; }

    /// <summary>
    /// Gets or sets the value for the filter condition.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// Returns a string representation of the filter condition.
    /// </summary>
    /// <returns>A string that represents the filter condition.</returns>
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
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FilterOperator
{
    /// <summary>
    /// Equality operator (=)
    /// </summary>
    Equals = 0,

    /// <summary>
    /// Greater than operator (>)
    /// </summary>
    GreaterThan = 1,

    /// <summary>
    /// Less than operator (<)
    /// </summary>
    LessThan = 2,

    /// <summary>
    /// Greater than or equal operator (>=)
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// Less than or equal operator (<=)
    /// </summary>
    LessThanOrEqual = 4,

    /// <summary>
    /// Contains operator (LIKE '%value%')
    /// </summary>
    Contains = 5,

    /// <summary>
    /// Starts with operator (LIKE 'value%')
    /// </summary>
    StartsWith = 6,

    /// <summary>
    /// Ends with operator (LIKE '%value')
    /// </summary>
    EndsWith = 7,

    /// <summary>
    /// In operator (matches any value in a list)
    /// </summary>
    In = 8,

    /// <summary>
    /// Between operator (inclusive range)
    /// </summary>
    Between = 9,

    /// <summary>
    /// Is null operator
    /// </summary>
    IsNull = 10,

    /// <summary>
    /// Is not null operator
    /// </summary>
    IsNotNull = 11,

    /// <summary>
    /// Not equal operator (<>)
    /// </summary>
    NotEqual = 12
}
