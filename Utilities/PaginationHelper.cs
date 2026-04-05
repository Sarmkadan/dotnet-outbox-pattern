// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Helper utilities for pagination - simplifies working with paginated collections
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Validates pagination parameters
    /// </summary>
    public static bool IsValidPageSize(int pageSize, int maxPageSize = 500)
    {
        return pageSize > 0 && pageSize <= maxPageSize;
    }

    /// <summary>
    /// Validates page number
    /// </summary>
    public static bool IsValidPageNumber(int page)
    {
        return page >= 1;
    }

    /// <summary>
    /// Calculates skip count for offset-based pagination
    /// </summary>
    public static int CalculateSkip(int page, int pageSize)
    {
        return (page - 1) * pageSize;
    }

    /// <summary>
    /// Calculates the total number of pages
    /// </summary>
    public static int CalculateTotalPages(int totalItems, int pageSize)
    {
        if (pageSize <= 0)
            return 0;

        return (int)Math.Ceiling(totalItems / (double)pageSize);
    }

    /// <summary>
    /// Checks if page exists in paginated result set
    /// </summary>
    public static bool PageExists(int page, int totalPages)
    {
        return page >= 1 && page <= totalPages;
    }

    /// <summary>
    /// Gets next page number, or -1 if no next page
    /// </summary>
    public static int GetNextPage(int currentPage, int totalPages)
    {
        return currentPage < totalPages ? currentPage + 1 : -1;
    }

    /// <summary>
    /// Gets previous page number, or -1 if no previous page
    /// </summary>
    public static int GetPreviousPage(int currentPage)
    {
        return currentPage > 1 ? currentPage - 1 : -1;
    }

    /// <summary>
    /// Paginates a collection into pages
    /// </summary>
    public static List<List<T>> Paginate<T>(this IEnumerable<T> source, int pageSize)
    {
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        var pages = new List<List<T>>();
        var currentPage = new List<T>();

        foreach (var item in source)
        {
            currentPage.Add(item);

            if (currentPage.Count == pageSize)
            {
                pages.Add(currentPage);
                currentPage = new List<T>();
            }
        }

        if (currentPage.Count > 0)
            pages.Add(currentPage);

        return pages;
    }

    /// <summary>
    /// Gets a specific page from a collection
    /// </summary>
    public static List<T> GetPage<T>(this IEnumerable<T> source, int page, int pageSize)
    {
        if (!IsValidPageNumber(page))
            throw new ArgumentException("Page must be >= 1", nameof(page));

        if (!IsValidPageSize(pageSize))
            throw new ArgumentException("Page size must be > 0 and <= 500", nameof(pageSize));

        return source
            .Skip(CalculateSkip(page, pageSize))
            .Take(pageSize)
            .ToList();
    }

    /// <summary>
    /// Creates pagination metadata
    /// </summary>
    public static PaginationMetadata CreateMetadata(int page, int pageSize, int totalItems)
    {
        var totalPages = CalculateTotalPages(totalItems, pageSize);

        return new PaginationMetadata
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1,
            NextPage = GetNextPage(page, totalPages),
            PreviousPage = GetPreviousPage(page)
        };
    }
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public int NextPage { get; set; } = -1;
    public int PreviousPage { get; set; } = -1;
}
