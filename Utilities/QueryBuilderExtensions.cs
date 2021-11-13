using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetOutboxPattern.Utilities
{
    public static class QueryBuilderExtensions
    {
        /// <summary>
        /// Adds an equality filter condition
        /// </summary>
        public static QueryBuilder Where(this QueryBuilder builder, string field, object value)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            builder.Where(field, value);
            return builder;
        }

        /// <summary>
        /// Adds a "greater than" condition
        /// </summary>
        public static QueryBuilder WhereGreaterThan(this QueryBuilder builder, string field, object value)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            builder.WhereGreaterThan(field, value);
            return builder;
        }

        /// <summary>
        /// Adds a "less than" condition
        /// </summary>
        public static QueryBuilder WhereLessThan(this QueryBuilder builder, string field, object value)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            builder.WhereLessThan(field, value);
            return builder;
        }

        /// <summary>
        /// Adds a "contains" (LIKE) condition
        /// </summary>
        public static QueryBuilder WhereContains(this QueryBuilder builder, string field, string value)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            if (string.IsNullOrWhiteSpace(value))
                return builder;

            builder.WhereContains(field, value);
            return builder;
        }

        /// <summary>
        /// Adds an "in" condition (matches any value in list)
        /// </summary>
        public static QueryBuilder WhereIn(this QueryBuilder builder, string field, params object[] values)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            if (values == null || values.Length == 0)
                return builder;

            builder.WhereIn(field, values);
            return builder;
        }

        /// <summary>
        /// Adds a "between" condition (inclusive)
        /// </summary>
        public static QueryBuilder WhereBetween(this QueryBuilder builder, string field, object minValue, object maxValue)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            builder.WhereBetween(field, minValue, maxValue);
            return builder;
        }

        /// <summary>
        /// Adds a "is null" condition
        /// </summary>
        public static QueryBuilder WhereIsNull(this QueryBuilder builder, string field)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            builder.WhereIsNull(field);
            return builder;
        }

        /// <summary>
        /// Adds an "is not null" condition
        /// </summary>
        public static QueryBuilder WhereIsNotNull(this QueryBuilder builder, string field)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            builder.WhereIsNotNull(field);
            return builder;
        }

        /// <summary>
        /// Sets the sort order with ascending sorting
        /// </summary>
        public static QueryBuilder OrderBy(this QueryBuilder builder, string field)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            builder.OrderBy(field, descending: false);
            return builder;
        }

        /// <summary>
        /// Sets the sort order with descending sorting
        /// </summary>
        public static QueryBuilder OrderByDescending(this QueryBuilder builder, string field)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field name cannot be null or empty", nameof(field));

            builder.OrderBy(field, descending: true);
            return builder;
        }

        /// <summary>
        /// Combines multiple QueryBuilder instances using AND logic
        /// </summary>
        public static QueryBuilder And(this QueryBuilder builder, QueryBuilder other)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var thisConditions = builder.GetConditions();
            var otherConditions = other.GetConditions();

            if (thisConditions != null && otherConditions != null)
            {
                thisConditions.AddRange(otherConditions);
            }

            return builder;
        }

        /// <summary>
        /// Resets the query builder to its initial state
        /// </summary>
        public static QueryBuilder Reset(this QueryBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // The QueryBuilder doesn't have a public Reset method, so we clear conditions
            var conditions = builder.GetConditions();
            if (conditions != null)
            {
                conditions.Clear();
            }

            return builder;
        }

        /// <summary>
        /// Gets a summary of applied filters
        /// </summary>
        public static Dictionary<string, object?> GetFilterSummary(this QueryBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.GetFilterSummary();
        }

        /// <summary>
        /// Gets all conditions as a list
        /// </summary>
        public static List<FilterCondition> GetConditions(this QueryBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.GetConditions();
        }
    }
}
