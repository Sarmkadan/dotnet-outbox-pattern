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
    public static List<List<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
    {
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
    public static IEnumerable<IGrouping<TKey, TValue>> GroupByCount<TKey, TValue>(
        this IEnumerable<TValue> source,
        Func<TValue, TKey> keySelector,
        Func<int, bool> countPredicate)
    {
        return source
            .GroupBy(keySelector)
            .Where(g => countPredicate(g.Count()));
    }

    /// <summary>
    /// Safely gets the element at an index, returning a default value if out of bounds
    /// </summary>
    public static T? SafeGetAt<T>(this IList<T> source, int index, T? defaultValue = default)
    {
        return index >= 0 && index < source.Count ? source[index] : defaultValue;
    }

    /// <summary>
    /// Gets the next element in a collection after the given item
    /// Returns null if item not found or is the last element
    /// </summary>
    public static T? GetNext<T>(this IList<T> source, T item)
    {
        var index = source.IndexOf(item);
        return index >= 0 && index < source.Count - 1 ? source[index + 1] : default;
    }

    /// <summary>
    /// Gets the previous element in a collection before the given item
    /// </summary>
    public static T? GetPrevious<T>(this IList<T> source, T item)
    {
        var index = source.IndexOf(item);
        return index > 0 ? source[index - 1] : default;
    }

    /// <summary>
    /// Adds multiple items to a collection at once
    /// </summary>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
            collection.Add(item);
    }

    /// <summary>
    /// Removes items from a collection that match a predicate
    /// </summary>
    public static int RemoveWhere<T>(this IList<T> collection, Func<T, bool> predicate)
    {
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
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source == null || !source.Any();
    }

    /// <summary>
    /// Returns a random element from the collection
    /// </summary>
    public static T? RandomElement<T>(this IList<T> source)
    {
        if (source.Count == 0)
            return default;

        var random = new Random();
        return source[random.Next(source.Count)];
    }

    /// <summary>
    /// Paginate a collection into a list of pages
    /// </summary>
    public static List<List<T>> Paginate<T>(this IEnumerable<T> source, int pageSize)
    {
        return source.Chunk(pageSize);
    }

    /// <summary>
    /// Gets a specific page from a collection
    /// </summary>
    public static List<T> GetPage<T>(this IEnumerable<T> source, int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentException("Page must be >= 1", nameof(page));

        return source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    /// <summary>
    /// Finds duplicates in a collection based on a key selector
    /// </summary>
    public static IEnumerable<T> FindDuplicates<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        return source
            .GroupBy(keySelector)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g);
    }

    /// <summary>
    /// Merges multiple enumerables into a single enumerable
    /// Flattens nested collections
    /// </summary>
    public static IEnumerable<T> Merge<T>(params IEnumerable<T>[] sources)
    {
        return sources.SelectMany(s => s);
    }
}
