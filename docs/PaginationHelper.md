# PaginationHelper

A utility class for performing pagination on collections of items. It provides methods to validate page sizes and numbers, calculate skip values, determine total pages, and retrieve specific pages from a collection. The class also exposes properties to inspect pagination state and metadata.

## API

### `public static bool IsValidPageSize(int pageSize)`

Determines whether a given page size is valid.
- **Parameters**:
  - `pageSize` – The page size to validate.
- **Return value**: `true` if `pageSize` is greater than zero; otherwise, `false`.
- **Throws**: Does not throw exceptions.

### `public static bool IsValidPageNumber(int pageNumber)`

Determines whether a given page number is valid.
- **Parameters**:
  - `pageNumber` – The page number to validate.
- **Return value**: `true` if `pageNumber` is greater than zero; otherwise, `false`.
- **Throws**: Does not throw exceptions.

### `public static int CalculateSkip(int pageNumber, int pageSize)`

Calculates the number of items to skip when retrieving a page.
- **Parameters**:
  - `pageNumber` – The 1-based page number.
  - `pageSize` – The number of items per page.
- **Return value**: The number of items to skip before the start of the requested page.
- **Throws**: Does not throw exceptions.

### `public static int CalculateTotalPages(int totalItems, int pageSize)`

Calculates the total number of pages for a given total item count and page size.
- **Parameters**:
  - `totalItems` – The total number of items in the collection.
  - `pageSize` – The number of items per page.
- **Return value**: The total number of pages, rounded up to the nearest integer.
- **Throws**: Does not throw exceptions.

### `public static bool PageExists(int pageNumber, int totalItems, int pageSize)`

Determines whether a specific page exists given the total item count and page size.
- **Parameters**:
  - `pageNumber` – The 1-based page number to check.
  - `totalItems` – The total number of items in the collection.
  - `pageSize` – The number of items per page.
- **Return value**: `true` if the page exists; otherwise, `false`.
- **Throws**: Does not throw exceptions.

### `public static int GetNextPage(int currentPage, int totalPages)`

Calculates the next page number, if it exists.
- **Parameters**:
  - `currentPage` – The current 1-based page number.
  - `totalPages` – The total number of pages.
- **Return value**: The next page number, or `currentPage` if there is no next page.
- **Throws**: Does not throw exceptions.

### `public static int GetPreviousPage(int currentPage)`

Calculates the previous page number, if it exists.
- **Parameters**:
  - `currentPage` – The current 1-based page number.
- **Return value**: The previous page number, or `currentPage` if there is no previous page.
- **Throws**: Does not throw exceptions.

### `public static List<List<T>> Paginate<T>(IEnumerable<T> items, int pageNumber, int pageSize)`

Splits a collection into a list of pages.
- **Parameters**:
  - `items` – The collection of items to paginate.
  - `pageNumber` – The 1-based page number to retrieve.
  - `pageSize` – The number of items per page.
- **Return value**: A list of pages, where each page is a list of items. Returns an empty list if `items` is empty or if the page does not exist.
- **Throws**: Does not throw exceptions.

### `public static List<T> GetPage<T>(IEnumerable<T> items, int pageNumber, int pageSize)`

Retrieves a single page from a collection.
- **Parameters**:
  - `items` – The collection of items to paginate.
  - `pageNumber` – The 1-based page number to retrieve.
  - `pageSize` – The number of items per page.
- **Return value**: A list of items for the requested page. Returns an empty list if `items` is empty or if the page does not exist.
- **Throws**: Does not throw exceptions.

### `public static PaginationMetadata CreateMetadata(int currentPage, int pageSize, int totalItems)`

Creates a metadata object describing the pagination state.
- **Parameters**:
  - `currentPage` – The current 1-based page number.
  - `pageSize` – The number of items per page.
  - `totalItems` – The total number of items in the collection.
- **Return value**: A `PaginationMetadata` instance populated with the provided values.
- **Throws**: Does not throw exceptions.

### `public int CurrentPage`

Gets the current page number.
- **Type**: `int`
- **Access**: Read-only

### `public int PageSize`

Gets the number of items per page.
- **Type**: `int`
- **Access**: Read-only

### `public int TotalItems`

Gets the total number of items in the collection.
- **Type**: `int`
- **Access**: Read-only

### `public int TotalPages`

Gets the total number of pages.
- **Type**: `int`
- **Access**: Read-only

### `public bool HasNextPage`

Determines whether there is a next page.
- **Type**: `bool`
- **Access**: Read-only

### `public bool HasPreviousPage`

Determines whether there is a previous page.
- **Type**: `bool`
- **Access**: Read-only

### `public int NextPage`

Gets the next page number.
- **Type**: `int`
- **Access**: Read-only

### `public int PreviousPage`

Gets the previous page number.
- **Type**: `int`
- **Access**: Read-only

## Usage

### Example 1: Basic Pagination
