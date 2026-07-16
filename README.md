// ... existing content ...
// =============================================================================
// Utilities
// =============================================================================

// ... other utility docs ...

// ## RetryHelper
// The `RetryHelper` utility provides reusable retry logic for handling transient failures with configurable retry strategies.
// It supports exponential backoff, fixed delay, linear backoff, and jittered backoff patterns to handle network timeouts,
// database deadlocks, and other temporary failures in distributed systems.

/// <summary>
/// Retry helper utilities for transient error handling
/// </summary>

// Example Usage
```csharp
// Example transient operation that might fail
async Task<string> FetchDataFromApi()
{
    // Simulate API call that might fail
    return await Task.FromResult("API Response Data");
}

// Example transient operation that might fail
async Task<string> SaveToDatabase()
{
    // Simulate database operation that might fail
    return await Task.FromResult("Database Save Successful");
}

// Use exponential backoff retry for API calls
var apiResult = await RetryHelper.ExecuteWithExponentialBackoffAsync(
    async () => await FetchDataFromApi(),
    maxRetries: 5,
    initialDelayMs: 200,
    backoffMultiplier: 2.0
);
Console.WriteLine(apiResult);

// Use fixed delay retry for database operations
var dbResult = await RetryHelper.ExecuteWithFixedDelayAsync(
    async () => await SaveToDatabase(),
    maxRetries: 3,
    delayMs: 1000
);
Console.WriteLine(dbResult);

// Use linear backoff retry
var linearResult = await RetryHelper.ExecuteWithLinearBackoffAsync(
    async () => await FetchDataFromApi(),
    maxRetries: 4,
    initialDelayMs: 100,
    delayIncrementMs: 150
);
Console.WriteLine(linearResult);

// Use jittered backoff retry to prevent thundering herd
var jitteredResult = await RetryHelper.ExecuteWithJitteredBackoffAsync(
    async () => await FetchDataFromApi(),
    maxRetries: 5,
    initialDelayMs: 100
);
Console.WriteLine(jitteredResult);

// Check if an exception is transient
try
{
    await FetchDataFromApi();
}
catch (Exception ex)
{
    if (RetryHelper.IsTransientError(ex))
    {
        Console.WriteLine("Transient error detected, safe to retry");
    }
}

// Create a reusable retry policy
var policy = RetryHelper.CreatePolicy(
    maxRetries: 5,
    strategy: RetryStrategy.ExponentialBackoff,
    initialDelayMs: 200
);

// Execute with the policy
var policyResult = await policy.ExecuteAsync(async () => await FetchDataFromApi());
Console.WriteLine(policyResult);

// Access policy properties
Console.WriteLine($"Max retries: {policy.MaxRetries}");
Console.WriteLine($"Strategy: {policy.Strategy}");
Console.WriteLine($"Initial delay: {policy.InitialDelayMs}ms");
Console.WriteLine($"Max delay: {policy.MaxDelayMs}ms");
Console.WriteLine($"Backoff multiplier: {policy.BackoffMultiplier}");
```

// ## StringHelper
// The `StringHelper` utility provides a set of static methods for common string operations used throughout the outbox pattern,
// including validation, hashing, formatting, and transformation. This utility helps ensure consistent handling
// of string data across the application.

/// <summary>
/// String helper utilities
/// </summary>

// Example Usage
```csharp
// Generate a secure hash of a string
var passwordHash = StringHelper.ComputeSha256Hash("my-secret-password");
Console.WriteLine(passwordHash); // Outputs a base64-encoded SHA256 hash

// Validate different string formats
var isValidEmail = StringHelper.IsValidEmail("user@example.com"); // true
var isValidGuid = StringHelper.IsValidGuid("550e8400-e29b-41d4-a716-446655440000"); // true
var isValidFormat = StringHelper.IsValidFormat("ABC123", "^[A-Z]{3}\d{3}$"); // true

// Truncate and sanitize strings
var longText = "This is a very long text that needs to be shortened";
var truncated = StringHelper.Truncate(longText, 20); // "This is a very lon..."
var jsonSafe = StringHelper.SanitizeForJson("Line 1\nLine 2\tTabbed"); // Escapes special chars

// Convert to URL-friendly formats
var slug = StringHelper.ToSlug("Hello World! This is a Test"); // "hello-world-this-is-a-test"
var kebab = StringHelper.ToKebabCase("PascalCaseString"); // "pascal-case-string"

// Generate random strings and check emptiness
var randomToken = StringHelper.GenerateRandomString(16); // 16-character random string
var isEmpty = StringHelper.IsEmpty("   "); // true
var isEmpty2 = StringHelper.IsEmpty(null); // true

// Join non-empty strings and extract substrings
var joined = StringHelper.JoinNonEmpty("-", "prefix", null, "middle", "", "suffix"); // "prefix-middle-suffix"
var extracted = StringHelper.ExtractBetween("Hello [world] from [C#]", "[", "]"); // "world"
```

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
