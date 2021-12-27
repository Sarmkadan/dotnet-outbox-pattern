# QueryBuilder
The `QueryBuilder` class is designed to facilitate the construction of complex queries by providing a fluent API for specifying filter conditions, sorting, and retrieving query summaries. It allows developers to build queries in a step-by-step manner, making it easier to manage and maintain complex query logic.

## API
* `public QueryBuilder Where`: Specifies a filter condition using the `Field`, `Operator`, and `Value` properties. Returns the current `QueryBuilder` instance.
* `public QueryBuilder WhereGreaterThan`: Specifies a filter condition where the field value is greater than the specified value. Returns the current `QueryBuilder` instance.
* `public QueryBuilder WhereLessThan`: Specifies a filter condition where the field value is less than the specified value. Returns the current `QueryBuilder` instance.
* `public QueryBuilder WhereContains`: Specifies a filter condition where the field value contains the specified value. Returns the current `QueryBuilder` instance.
* `public QueryBuilder WhereIn`: Specifies a filter condition where the field value is in the specified list of values. Returns the current `QueryBuilder` instance.
* `public QueryBuilder WhereBetween`: Specifies a filter condition where the field value is between the specified range of values. Returns the current `QueryBuilder` instance.
* `public QueryBuilder WhereIsNull`: Specifies a filter condition where the field value is null. Returns the current `QueryBuilder` instance.
* `public QueryBuilder WhereIsNotNull`: Specifies a filter condition where the field value is not null. Returns the current `QueryBuilder` instance.
* `public QueryBuilder OrderBy`: Specifies the sorting order for the query. Returns the current `QueryBuilder` instance.
* `public List<FilterCondition> GetConditions`: Retrieves the list of filter conditions specified in the query. Returns a list of `FilterCondition` objects.
* `public string ToFilterString`: Converts the query to a filter string representation. Returns a string.
* `public Dictionary<string, object?> GetFilterSummary`: Retrieves a summary of the filter conditions specified in the query. Returns a dictionary with filter condition summaries.
* `public string Field`: Gets or sets the field name for the filter condition.
* `public FilterOperator Operator`: Gets or sets the operator for the filter condition.
* `public object? Value`: Gets or sets the value for the filter condition.
* `public override string ToString`: Returns a string representation of the query. Returns a string.

## Usage
```csharp
// Example 1: Simple query with filter condition
var queryBuilder = new QueryBuilder();
queryBuilder.Where("Name", FilterOperator.Contains, "John");
var filterString = queryBuilder.ToFilterString;
Console.WriteLine(filterString); // Output: Name.Contains("John")

// Example 2: Complex query with multiple filter conditions and sorting
var queryBuilder2 = new QueryBuilder();
queryBuilder2.Where("Age", FilterOperator.GreaterThan, 18);
queryBuilder2.Where("Country", FilterOperator.Equals, "USA");
queryBuilder2.OrderBy("Name");
var filterSummary = queryBuilder2.GetFilterSummary;
Console.WriteLine(filterSummary); // Output: { Age = GreaterThan(18), Country = Equals(USA) }
```

## Notes
* The `QueryBuilder` class is not thread-safe, and its instances should not be shared across multiple threads.
* The `GetConditions` method returns a list of filter conditions in the order they were specified.
* The `ToFilterString` method may throw a `FormatException` if the query is invalid or cannot be converted to a filter string.
* The `GetFilterSummary` method may return an empty dictionary if no filter conditions are specified.
* The `Field`, `Operator`, and `Value` properties are used to specify filter conditions, and their values are reset after each `Where` method call.
