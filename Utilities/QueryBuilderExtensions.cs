using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetOutboxPattern.Utilities
{
	/// <summary>
	/// Provides extension methods for <see cref="QueryBuilder"/> to fluently build query conditions.
	/// </summary>
	public static class QueryBuilderExtensions
	{
		/// <summary>
		/// Adds an equality filter condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="value">The value to compare against</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder Where(this QueryBuilder builder, string field, object value)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			return builder.Where(field, value);
		}

		/// <summary>
		/// Adds a "greater than" condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="value">The value to compare against</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereGreaterThan(this QueryBuilder builder, string field, object value)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			return builder.WhereGreaterThan(field, value);
		}

		/// <summary>
		/// Adds a "greater than or equal" condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="value">The value to compare against</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereGreaterThanOrEqual(this QueryBuilder builder, string field, object value)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			_ = builder.Where(field, value);
			_ = builder.GetConditions().Last().Operator = FilterOperator.GreaterThanOrEqual;
			return builder;
		}

		/// <summary>
		/// Adds a "less than" condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="value">The value to compare against</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereLessThan(this QueryBuilder builder, string field, object value)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			return builder.WhereLessThan(field, value);
		}

		/// <summary>
		/// Adds a "less than or equal" condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="value">The value to compare against</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereLessThanOrEqual(this QueryBuilder builder, string field, object value)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			_ = builder.Where(field, value);
			_ = builder.GetConditions().Last().Operator = FilterOperator.LessThanOrEqual;
			return builder;
		}

		/// <summary>
		/// Adds a "contains" (LIKE) condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="value">The value to search for within the field</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereContains(this QueryBuilder builder, string field, string value)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			if (string.IsNullOrWhiteSpace(value))
			{
				return builder;
			}

			return builder.WhereContains(field, value);
		}

		/// <summary>
		/// Adds a "starts with" condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="value">The value that the field should start with</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereStartsWith(this QueryBuilder builder, string field, string value)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			_ = builder.Where(field, value);
			_ = builder.GetConditions().Last().Operator = FilterOperator.StartsWith;
			return builder;
		}

		/// <summary>
		/// Adds an "ends with" condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="value">The value that the field should end with</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereEndsWith(this QueryBuilder builder, string field, string value)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			_ = builder.Where(field, value);
			_ = builder.GetConditions().Last().Operator = FilterOperator.EndsWith;
			return builder;
		}

		/// <summary>
		/// Adds an "in" condition (matches any value in list)
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="values">The values to match against</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereIn(this QueryBuilder builder, string field, params object[] values)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			if (values == null || values.Length == 0)
			{
				return builder;
			}

			return builder.WhereIn(field, values);
		}

		/// <summary>
		/// Adds a "between" condition (inclusive)
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="minValue">The minimum value (inclusive)</param>
		/// <param name="maxValue">The maximum value (inclusive)</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereBetween(this QueryBuilder builder, string field, object minValue, object maxValue)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			return builder.WhereBetween(field, minValue, maxValue);
		}

		/// <summary>
		/// Adds a "is null" condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereIsNull(this QueryBuilder builder, string field)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			return builder.WhereIsNull(field);
		}

		/// <summary>
		/// Adds an "is not null" condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereIsNotNull(this QueryBuilder builder, string field)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			return builder.WhereIsNotNull(field);
		}

		/// <summary>
		/// Adds a "not equal" condition
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <param name="value">The value to compare against</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder WhereNotEqual(this QueryBuilder builder, string field, object value)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			_ = builder.Where(field, value);
			_ = builder.GetConditions().Last().Operator = FilterOperator.NotEqual;
			return builder;
		}

		/// <summary>
		/// Sets the sort order with ascending sorting
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder OrderBy(this QueryBuilder builder, string field)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			return builder.OrderBy(field, descending: false);
		}

		/// <summary>
		/// Sets the sort order with descending sorting
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <param name="field">The field name to filter on</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentException"><paramref name="field"/> is null or whitespace</exception>
		public static QueryBuilder OrderByDescending(this QueryBuilder builder, string field)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentException.ThrowIfNullOrWhiteSpace(field);

			return builder.OrderBy(field, descending: true);
		}

		/// <summary>
		/// Combines multiple QueryBuilder instances using AND logic
		/// </summary>
		/// <param name="builder">The primary query builder instance</param>
		/// <param name="other">The secondary query builder to combine with</param>
		/// <returns>The primary query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="other"/> is <see langword="null"/></exception>
		public static QueryBuilder And(this QueryBuilder builder, QueryBuilder other)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(other);

			var thisConditions = builder.GetConditions();
			var otherConditions = other.GetConditions();

			if (thisConditions != null && otherConditions != null)
			{
				thisConditions.AddRange(otherConditions);
			}

			return builder;
		}

		/// <summary>
		/// Resets the query builder to its initial state by clearing all conditions
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <returns>The query builder for method chaining</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		public static QueryBuilder Reset(this QueryBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);

			var conditions = builder.GetConditions();
			conditions?.Clear();

			return builder;
		}

		/// <summary>
		/// Gets a summary of applied filters as a dictionary mapping condition keys to their values
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <returns>A dictionary containing filter summaries</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		public static Dictionary<string, object?> GetFilterSummary(this QueryBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);

			return builder.GetFilterSummary();
		}

		/// <summary>
		/// Gets all conditions as a list of <see cref="FilterCondition"/> objects
		/// </summary>
		/// <param name="builder">The query builder instance</param>
		/// <returns>A list of filter conditions</returns>
		/// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
		public static List<FilterCondition> GetConditions(this QueryBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);

			return builder.GetConditions();
		}
	}
}
