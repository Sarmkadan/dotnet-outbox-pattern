# CollectionExtensions
The `CollectionExtensions` class provides a set of extension methods for working with collections in C#. It offers various methods for manipulating and querying collections, including chunking, grouping, pagination, and more. These methods can be used to simplify common collection operations and improve code readability.

## API
* `public static List<List<T>> Chunk<T>(...)`: Chunks a collection into smaller sub-collections of a specified size. Parameters: the collection to chunk, the chunk size. Return value: a list of lists, where each inner list is a chunk of the original collection. Throws: `ArgumentNullException` if the collection is null.
* `public static IEnumerable<IGrouping<TKey, TValue>> GroupByCount<TKey, TValue>(...)`: Groups a collection by the count of each element. Parameters: the collection to group, the key selector. Return value: an enumerable of groupings, where each grouping contains the key (the element) and the count of occurrences. Throws: `ArgumentNullException` if the collection is null.
* `public static T? SafeGetAt<T>(...)`: Safely retrieves an element at a specified index from a collection. Parameters: the collection, the index. Return value: the element at the specified index, or null if the index is out of range. Throws: `ArgumentNullException` if the collection is null.
* `public static T? GetNext<T>(...)`: Retrieves the next element in a collection. Parameters: the collection, the current element. Return value: the next element in the collection, or null if the current element is the last one. Throws: `ArgumentNullException` if the collection is null.
* `public static T? GetPrevious<T>(...)`: Retrieves the previous element in a collection. Parameters: the collection, the current element. Return value: the previous element in the collection, or null if the current element is the first one. Throws: `ArgumentNullException` if the collection is null.
* `public static void AddRange<T>(...)`: Adds a range of elements to a collection. Parameters: the collection, the elements to add. Throws: `ArgumentNullException` if the collection is null.
* `public static int RemoveWhere<T>(...)`: Removes elements from a collection that match a specified condition. Parameters: the collection, the condition. Return value: the number of elements removed. Throws: `ArgumentNullException` if the collection is null.
* `public static bool IsNullOrEmpty<T>(...)`: Checks if a collection is null or empty. Parameters: the collection. Return value: true if the collection is null or empty, false otherwise. Throws: none.
* `public static T? RandomElement<T>(...)`: Retrieves a random element from a collection. Parameters: the collection. Return value: a random element from the collection, or null if the collection is empty. Throws: `ArgumentNullException` if the collection is null.
* `public static List<List<T>> Paginate<T>(...)`: Paginates a collection into smaller sub-collections of a specified size. Parameters: the collection to paginate, the page size. Return value: a list of lists, where each inner list is a page of the original collection. Throws: `ArgumentNullException` if the collection is null.
* `public static List<T> GetPage<T>(...)`: Retrieves a page of elements from a collection. Parameters: the collection, the page index, the page size. Return value: a list of elements in the specified page. Throws: `ArgumentNullException` if the collection is null.
* `public static IEnumerable<T> FindDuplicates<T, TKey>(...)`: Finds duplicate elements in a collection based on a specified key. Parameters: the collection, the key selector. Return value: an enumerable of duplicate elements. Throws: `ArgumentNullException` if the collection is null.
* `public static IEnumerable<T> Merge<T>(...)`: Merges multiple collections into a single collection. Parameters: the collections to merge. Return value: an enumerable of merged elements. Throws: `ArgumentNullException` if any of the collections are null.

## Usage
```csharp
// Example 1: Chunking a collection
var numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
var chunks = numbers.Chunk(3);
foreach (var chunk in chunks)
{
    Console.WriteLine(string.Join(", ", chunk));
}

// Example 2: Paginating a collection
var items = new List<string> { "item1", "item2", "item3", "item4", "item5" };
var pages = items.Paginate(2);
foreach (var page in pages)
{
    Console.WriteLine(string.Join(", ", page));
}
```

## Notes
* The `CollectionExtensions` class is not thread-safe, as it relies on the underlying collections being thread-safe. If you need to use these methods in a multi-threaded environment, ensure that the collections are properly synchronized.
* The `Chunk` and `Paginate` methods will throw an `ArgumentOutOfRangeException` if the chunk or page size is less than 1.
* The `FindDuplicates` method will return an empty enumerable if there are no duplicate elements in the collection.
* The `Merge` method will return an empty enumerable if all input collections are empty.
