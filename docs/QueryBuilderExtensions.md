# QueryBuilderExtensions

The `QueryBuilderExtensions` class provides a fluent, expressive API for constructing database queries within the `dotnet-outbox-pattern` framework. It allows developers to programmatically define complex filtering criteria, ordering operations, and logical conditions, facilitating dynamic query generation for repository operations while maintaining readability and type safety.

## API

All members are implemented as extension methods for the `QueryBuilder` class.

*   **`Where(string property, object value)`**: Adds an equality filter to the query. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property` is null.
*   **`WhereGreaterThan(string property, object value)`**: Adds a greater-than filter to the query. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property` is null.
*   **`WhereLessThan(string property, object value)`**: Adds a less-than filter to the query. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property` is null.
*   **`WhereContains(string property, string value)`**: Adds a string-contains filter to the query. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property` or `value` is null.
*   **`WhereIn(string property, IEnumerable<object> values)`**: Adds a set-inclusion filter (checking if the property value is in the provided set) to the query. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property` or `values` is null.
*   **`WhereBetween(string property, object start, object end)`**: Adds a range filter (inclusive) to the query. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property`, `start`, or `end` is null.
*   **`WhereIsNull(string property)`**: Adds a null-check filter to the query. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property` is null.
*   **`WhereIsNotNull(string property)`**: Adds a non-null-check filter to the query. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property` is null.
*   **`OrderBy(string property)`**: Specifies that the results should be ordered by the given property in ascending order. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property` is null.
*   **`OrderByDescending(string property)`**: Specifies that the results should be ordered by the given property in descending order. Returns the `QueryBuilder` instance. Throws `ArgumentNullException` if `property` is null.
*   **`And()`**: Explicitly adds a logical AND operator to the current condition set. Returns the `QueryBuilder` instance.
*   **`Reset()`**: Removes all filters and ordering conditions, resetting the `QueryBuilder` to its default state. Returns the `QueryBuilder` instance.
*   **`GetFilterSummary()`**: Returns a `Dictionary<string, object?>` containing a summarized view of the applied filters, mapping property names to their filter values.
*   **`GetConditions()`**: Returns a `List<FilterCondition>` containing the structured representations of all applied filter conditions.

## Usage

### Basic Filtering and Ordering
```csharp
var query = new QueryBuilder()
    .Where("Status", "Pending")
    .OrderByDescending("CreatedAt");

var results = await _repository.FindAsync(query);
```

### Complex Filtering with Logical Operators
```csharp
var query = new QueryBuilder()
    .WhereIn("Category", new List<object> { "Order", "Payment" })
    .And()
    .WhereBetween("Amount", 100.0, 500.0);

var results = await _repository.FindAsync(query);
```

## Notes

*   **Thread Safety**: The `QueryBuilder` instance and the extension methods in `QueryBuilderExtensions` are not thread-safe. They are designed for fluent, synchronous chaining within a single thread context. Do not share a `QueryBuilder` instance across multiple threads without external synchronization.
*   **Property Mapping**: Ensure that the `property` strings passed to these methods correspond accurately to the underlying database column names or the mapped property names expected by the repository implementation.
*   **Validation**: While the methods throw `ArgumentNullException` for null property names or mandatory arguments, they do not validate the existence of the specified properties on the domain entity. Runtime errors may occur during query execution if properties are invalid.
*   **Fluent Interface**: Most methods return the `QueryBuilder` instance to enable method chaining. Ensure that operations are chained in a logical sequence. Using `Reset()` will discard any previously chained operations.
