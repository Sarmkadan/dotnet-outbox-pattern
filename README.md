// ... existing content ...
// =============================================================================
// Utilities
// =============================================================================

// ... other utility docs ...

// ## DateTimeHelper
// The `DateTimeHelper` utility provides a set of static methods for common date/time operations used throughout the outbox pattern,
// including parsing timestamps, calculating time deltas, and formatting durations. This utility helps ensure consistent handling
// of date/time data across the application.
/// <summary>
/// Gets a human-readable relative time string (e.g., "2 hours ago")
/// </summary>
// Example Usage
```csharp
// Get the current UTC timestamp
var utcNow = DateTimeHelper.UtcNow;

// Convert a Unix timestamp to a DateTime
var dateTimeFromUnix = DateTimeHelper.FromUnixTimestamp(1643723400);

// Calculate age in seconds
var ageInSeconds = DateTimeHelper.GetAgeSeconds(DateTime.UtcNow.AddHours(-2));

// Check if older than X days
var isOlder = DateTimeHelper.IsOlderThan(DateTime.UtcNow.AddDays(-5), 3);

// Round down to nearest hour
var roundedDownHour = DateTimeHelper.RoundDownToHour(DateTime.UtcNow);

// Round down to nearest day
var roundedDownDay = DateTimeHelper.RoundDownToDay(DateTime.UtcNow);

// Get business hour start
var businessHourStart = DateTimeHelper.GetBusinessHourStart(DateTime.UtcNow);

// Get business hour end
var businessHourEnd = DateTimeHelper.GetBusinessHourEnd(DateTime.UtcNow);

// Check if within business hours
var isBusinessHour = DateTimeHelper.IsBusinessHours(DateTime.UtcNow);

// Parse relative time period
var relativePeriod = DateTimeHelper.ParseRelativePeriod("12h");

// Format duration
var formattedDuration = DateTimeHelper.FormatDuration(3600000);

// Get relative time string
var relativeTimeString = DateTimeHelper.GetRelativeTimeString(DateTime.UtcNow.AddHours(-2));
```

// ## ValidationHelper
// The `ValidationHelper` utility provides a set of static methods and a fluent validation context for validating method parameters and business rules.
// It supports both simple validation with immediate exceptions and fluent validation with chained conditions that can collect multiple errors before throwing.

/// <summary>
/// Validation helper utilities
/// </summary>

// Example Usage
```csharp
// Basic validation methods
ValidationHelper.ValidateNotEmpty(userInput, nameof(userInput));
ValidationHelper.ValidateNotNull(customer, nameof(customer));
ValidationHelper.ValidatePositive(orderQuantity, nameof(orderQuantity));
ValidationHelper.ValidateRange(age, 18, 120, nameof(age));
ValidationHelper.ValidateLength(productName, 3, 100, nameof(productName));
ValidationHelper.ValidateAny(items, item => item.IsActive, "No active items found");
ValidationHelper.ValidateAll(emails, email => email.Contains("@"), "All emails must be valid");
ValidationHelper.ValidateEqual(expectedId, actualId, nameof(actualId));
ValidationHelper.ValidateCondition(hasPermission, "User does not have required permission");

// Fluent validation with ValidationContext
var user = new User
{
    Name = "John Doe",
    Email = "john@example.com",
    Age = 25
};

var validationContext = ValidationHelper.Validate(user)
    .NotNull(u => u.Name, nameof(user.Name))
    .NotEmpty(u => u.Name, nameof(user.Name))
    .MinLength(u => u.Name, 2, nameof(user.Name))
    .MaxLength(u => u.Email, 100, nameof(user.Email))
    .Condition(u => u.Age >= 18, "User must be at least 18 years old");

if (!validationContext.IsValid)
{
    Console.WriteLine("Validation failed:");
    foreach (var error in validationContext.Errors)
    {
        Console.WriteLine($"- {error}");
    }
}

// Or throw immediately if any validation fails
ValidationHelper.Validate(user)
    .NotNull(u => u.Name, nameof(user.Name))
    .NotEmpty(u => u.Email, nameof(user.Email))
    .ThrowIfInvalid();

// Validate a collection of items
var products = new List<Product>
{
    new Product { Id = 1, Name = "Widget", Price = 9.99m },
    new Product { Id = 2, Name = "Gadget", Price = 19.99m }
};

ValidationHelper.ValidateAll(products, p => p.Price > 0, "All product prices must be positive");
```

// ## PaginationHelper
// The `PaginationHelper` utility provides utilities for working with paginated collections, including validation of pagination
// parameters, calculating skip values for offset-based pagination, and creating pagination metadata. It simplifies the
// implementation of paginated APIs and data retrieval operations.

/// <summary>
/// Validates pagination parameters
/// </summary>

// Example Usage
```csharp
// Create a list of 150 items
var items = Enumerable.Range(1, 150).ToList();

// Validate page size (must be > 0 and <= 500)
var isValidSize = PaginationHelper.IsValidPageSize(25); // true
var isInvalidSize = PaginationHelper.IsValidPageSize(1000); // false

// Validate page number (must be >= 1)
var isValidPage = PaginationHelper.IsValidPageNumber(3); // true
var isInvalidPage = PaginationHelper.IsValidPageNumber(0); // false

// Calculate skip value for offset-based pagination
var skip = PaginationHelper.CalculateSkip(2, 25); // 25 (skips first 25 items)

// Calculate total pages
var totalPages = PaginationHelper.CalculateTotalPages(150, 25); // 6

// Check if page exists
var pageExists = PaginationHelper.PageExists(3, 6); // true
var pageDoesNotExist = PaginationHelper.PageExists(7, 6); // false

// Get next/previous page numbers
var nextPage = PaginationHelper.GetNextPage(3, 6); // 4
var previousPage = PaginationHelper.GetPreviousPage(3); // 2
var noNextPage = PaginationHelper.GetNextPage(6, 6); // -1

// Paginate a collection into pages
var pages = items.Paginate(25); // List<List<int>> with 6 pages

// Get a specific page
var page3 = items.GetPage(3, 25); // List<int> with items 51-75

// Create pagination metadata
var metadata = PaginationHelper.CreateMetadata(3, 25, 150);
Console.WriteLine($"Current: {metadata.CurrentPage}, Total: {metadata.TotalPages}, " +
                $"HasNext: {metadata.HasNextPage}, HasPrev: {metadata.HasPreviousPage}");
```

// ... other utility docs ...