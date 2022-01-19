#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Extension methods for collections - provides convenient operations on lists and enumerables
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Chunks a collection into smaller collections of specified size
    /// Useful for batch processing
    /// </summary>
    /// <param name="source">The source collection to chunk</param>
    /// <param name="chunkSize">The size of each chunk (must be greater than 0)</param>
    /// <returns>A list of chunks, each containing at most <paramref name="chunkSize"/> elements</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chunkSize"/> is less than or equal to 0</exception>
    public static List<List<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be greater than 0", nameof(chunkSize));

        var result = new List<List<T>>();
        var chunk = new List<T>(chunkSize);

        foreach (var item in source)
        {
            chunk.Add(item);

            if (chunk.Count == chunkSize)
            {
                result.Add(chunk);
                chunk = new List<T>(chunkSize);
            }
        }

        if (chunk.Count > 0)
            result.Add(chunk);

        return result;
    }

    /// <summary>
    /// Groups collection items and returns only groups where count matches predicate
    /// Useful for finding duplicate or singleton items
    /// </summary>
    /// <param name="source">The source collection to group</param>
    /// <param name="keySelector">Function to extract the key for grouping</param>
    /// <param name="countPredicate">Predicate to filter groups by their count</param>
    /// <returns>Filtered groupings that match the count predicate</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="keySelector"/> is null</exception>
    public static IEnumerable<IGrouping<TKey, TValue>> GroupByCount<TKey, TValue>(
        this IEnumerable<TValue> source,
        Func<TValue, TKey> keySelector,
        Func<int, bool> countPredicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(countPredicate);

        return source
            .GroupBy(keySelector)
            .Where(g => countPredicate(g.Count()));
    }

    /// <summary>
    /// Safely gets the element at an index, returning a default value if out of bounds
    /// </summary>
    /// <param name="source">The source collection</param>
    /// <param name="index">The index to retrieve</param>
    /// <param name="defaultValue">The default value to return if index is out of bounds</param>
    /// <returns>The element at the specified index, or defaultValue if out of bounds</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null</exception>
    public static T? SafeGetAt<T>(this IList<T> source, int index, T? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return index >= 0 && index < source.Count ? source[index] : defaultValue;
    }

    /// <summary>
    /// Gets the next element in a collection after the given item
    /// Returns null if item not found or is the last element
    /// </summary>
    /// <param name="source">The source collection</param>
    /// <param name="item">The item to find the next element after</param>
    /// <returns>The next element, or null if item not found or is the last element</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null</exception>
    public static T? GetNext<T>(this IList<T> source, T item)
    {
        ArgumentNullException.ThrowIfNull(source);

        var index = source.IndexOf(item);
        return index >= 0 && index < source.Count - 1 ? source[index + 1] : default;
    }

    /// <summary>
    /// Gets the previous element in a collection before the given item
    /// </summary>
    /// <param name="source">The source collection</param>
    /// <param name="item">The item to find the previous element before</param>
    /// <returns>The previous element, or null if item not found or is the first element</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null</exception>
    public static T? GetPrevious<T>(this IList<T> source, T item)
    {
        ArgumentNullException.ThrowIfNull(source);

        var index = source.IndexOf(item);
        return index > 0 ? source[index - 1] : default;
    }

    /// <summary>
    /// Adds multiple items to a collection at once
    /// </summary>
    /// <param name="collection">The target collection</param>
    /// <param name="items">The items to add</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="items"/> is null</exception>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
            collection.Add(item);
    }

    /// <summary>
    /// Removes items from a collection that match a predicate
    /// </summary>
    /// <param name="collection">The collection to modify</param>
    /// <param name="predicate">The predicate to identify items to remove</param>
    /// <returns>The number of items removed</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="predicate"/> is null</exception>
    public static int RemoveWhere<T>(this IList<T> collection, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(predicate);

        int count = 0;
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (predicate(collection[i]))
            {
                collection.RemoveAt(i);
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Checks if a collection is null or empty
    /// More readable than checking .Count == 0
    /// </summary>
    /// <param name="source">The collection to check</param>
    /// <returns>True if the collection is null or empty, false otherwise</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }

    /// <summary>
    /// Returns a random element from the collection
    /// </summary>
    /// <param name="source">The source collection</param>
    /// <returns>A random element, or default if collection is empty</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null</exception>
    public static T? RandomElement<T>(this IList<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Count == 0)
            return default;

        var random = new Random();
        return source[random.Next(source.Count)];
    }

    /// <summary>
    /// Paginate a collection into a list of pages
    /// </summary>
    /// <param name="source">The source collection to paginate</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>A list of pages, each containing at most <paramref name="pageSize"/> elements</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageSize"/> is less than or equal to 0</exception>
    public static List<List<T>> Paginate<T>(this IEnumerable<T> source, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        return source.Chunk(pageSize);
    }

    /// <summary>
    /// Gets a specific page from a collection
    /// </summary>
    /// <param name="source">The source collection</param>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>A list containing the items for the requested page</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="page"/> is less than 1 or <paramref name="pageSize"/> is less than or equal to 0</exception>
    public static List<T> GetPage<T>(this IEnumerable<T> source, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (page < 1)
            throw new ArgumentException("Page must be >= 1", nameof(page));

        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        return source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    /// <summary>
    /// Finds duplicates in a collection based on a key selector
    /// </summary>
    /// <param name="source">The source collection</param>
    /// <param name="keySelector">Function to extract the key for duplicate detection</param>
    /// <returns>All elements that have duplicate keys</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="keySelector"/> is null</exception>
    public static IEnumerable<T> FindDuplicates<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return source
            .GroupBy(keySelector)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g);
    }

    /// <summary>
    /// Merges multiple enumerables into a single enumerable
    /// Flattens nested collections
    /// </summary>
    /// <param name="sources">The collections to merge</param>
    /// <returns>A single enumerable containing all elements from all sources</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sources"/> is null or contains null elements</exception>
    public static IEnumerable<T> Merge<T>(params IEnumerable<T>[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        return sources.SelectMany(s => s);
    }
}