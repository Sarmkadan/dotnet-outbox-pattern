// =============================================================================
// Utilities
// =============================================================================

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
var isValidFormat = StringHelper.IsValidFormat("ABC123", "^[A-Z]{3}\\d{3}$"); // true

// Truncate and sanitize strings
var longText = "This is a very long text that needs to be shortened";
var truncated = StringHelper.Truncate(longText, 20); // "This is a very lon..."
var jsonSafe = StringHelper.SanitizeForJson("Line 1\\nLine 2\\tTabbed"); // Escapes special chars

// Convert to URL-friendly formats
var slug = StringHelper.ToSlug("Hello World! This is a Test"); // "hello-world-this-is-a-test"
var kebab = StringHelper.ToKebabCase("PascalCaseString"); // "pascal-case-string"

// Generate random strings and check emptiness
var randomToken = StringHelper.GenerateRandomString(16); // 16-character random string
var isEmpty = StringHelper.IsEmpty(" "); // true
var isEmpty2 = StringHelper.IsEmpty(null); // true

// Join non-empty strings and extract substrings
var joined = StringHelper.JoinNonEmpty("-", "prefix", null, "middle", "", "suffix"); // "prefix-middle-suffix"
var extracted = StringHelper.ExtractBetween("Hello [world] from [C#]", "[", "]"); // "world"
```

// ## StringHelperTestsValidation
// The `StringHelperTestsValidation` class provides validation utilities for testing the `StringHelper` methods.
// It includes validation methods for all `StringHelper` public members that accept string parameters, ensuring test data
// is valid before being passed to the actual methods. This helps maintain consistency between test expectations and actual behavior.

/// <summary>
/// Validation helpers for StringHelper method parameters used in StringHelperTests
/// Provides validation for test data validation
/// </summary>

// Example Usage
```csharp
// Test validation for ComputeSha256Hash method
var hashInput = "my-secret-password";
var hashValidation = StringHelperTestsValidation.Validate(hashInput);
if (StringHelperTestsValidation.IsValid(hashInput))
{
    var hash = StringHelper.StringHelper.ComputeSha256Hash(hashInput);
    Console.WriteLine(hash);
}

// Test validation for IsValidEmail method
var email = "user@example.com";
var emailValidation = StringHelperTestsValidation.Validate(email, true);
if (StringHelperTestsValidation.IsValid(email, true))
{
    var isValid = StringHelper.IsValidEmail(email);
    Console.WriteLine(isValid); // true
}

// Test validation for Truncate method
var longText = "This is a very long text that needs to be shortened";
var truncateValidation = StringHelperTestsValidation.Validate(longText, 20, "This is a very lon...");
if (StringHelperTestsValidation.IsValid(longText, 20, "This is a very lon..."))
{
    var truncated = StringHelper.Truncate(longText, 20);
    Console.WriteLine(truncated);
}

// Test validation for ToSlug method
var slugInput = "Hello World! This is a Test";
var slugValidation = StringHelperTestsValidation.Validate(slugInput, "hello-world-this-is-a-test");
if (StringHelperTestsValidation.IsValid(slugInput, "hello-world-this-is-a-test"))
{
    var slug = StringHelper.ToSlug(slugInput);
    Console.WriteLine(slug); // "hello-world-this-is-a-test"
}

// Test validation for ToKebabCase method
var kebabInput = "PascalCaseString";
var kebabValidation = StringHelperTestsValidation.Validate(kebabInput, "pascal-case-string");
if (StringHelperTestsValidation.IsValid(kebabInput, "pascal-case-string"))
{
    var kebab = StringHelper.ToKebabCase(kebabInput);
    Console.WriteLine(kebab); // "pascal-case-string"
}

// Use EnsureValid to throw exceptions on invalid data
try
{
    StringHelperTestsValidation.EnsureValid(null); // Throws ArgumentNullException
}
catch (ArgumentNullException ex)
{
    Console.WriteLine(ex.Message);
}
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

// ## ErrorResponse
// The `ErrorResponse` class represents a standardized error response structure used throughout the API to communicate
// error conditions to clients. It includes essential fields like error message, error code, timestamp, and optional
// request tracing information for debugging and monitoring purposes.

/// <summary>
/// Standard API error response
/// </summary>

// Example Usage
```csharp
// Create a basic error response
var errorResponse = new ErrorResponse
{
    Message = "Resource not found",
    Code = "NOT_FOUND",
    TraceId = "abc123-def456"
};

// Create a complete error response with all properties
var detailedError = new ErrorResponse
{
    Message = "Database connection timeout occurred",
    Code = "DB_TIMEOUT",
    Timestamp = DateTime.UtcNow,
    TraceId = Guid.NewGuid().ToString()
};

// Log the error response
Console.WriteLine($"Error: {errorResponse.Message} (Code: {errorResponse.Code}) at {errorResponse.Timestamp:o}");

// Use in exception handling
try
{
    // Some operation that might fail
}
catch (Exception ex)
{
    var error = new ErrorResponse
    {
        Message = ex.Message,
        Code = "INTERNAL_ERROR",
        TraceId = Activity.Current?.TraceId.ToString()
    };

    // Return error to client
    return Results.Problem(
        detail: error.Message,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = error.Code,
            ["traceId"] = error.TraceId
        }
    );
}
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

// ## PublishEventRequest
// The `PublishEventRequest` class represents a request to publish events to message brokers or event streams. It contains
// all necessary metadata for event routing, including aggregate information, event data, and optional headers for
// correlation and idempotency.

/// <summary>
/// Request to publish events with routing metadata
/// </summary>

// Example Usage
```csharp
// Create a basic event publish request
var publishRequest = new PublishEventRequest
{
    AggregateId = "order-12345",
    AggregateType = "Order",
    EventType = "OrderCreated",
    EventData = new Dictionary<string, object>
    {
        ["OrderId"] = "order-12345",
        ["CustomerId"] = "customer-67890",
        ["TotalAmount"] = 99.99,
        ["Items"] = new List<object>
        {
            new { ProductId = "prod-001", Quantity = 2, Price = 49.99 },
            new { ProductId = "prod-002", Quantity = 1, Price = 1.99 }
        }
    },
    Topic = "orders.events",
    PartitionKey = "order-12345",
    CorrelationId = Guid.NewGuid().ToString(),
    IdempotencyKey = "idempotency-key-123"
};

// Create a request with optional properties
var requestWithOptions = new PublishEventRequest
{
    AggregateId = "user-98765",
    AggregateType = "User",
    EventType = "UserRegistered",
    EventData = new Dictionary<string, object>
    {
        ["UserId"] = "user-98765",
        ["Email"] = "user@example.com",
        ["RegistrationDate"] = DateTime.UtcNow
    },
    Topic = "users.events",
    PartitionKey = "user-98765",
    CorrelationId = Guid.NewGuid().ToString(),
    IdempotencyKey = Guid.NewGuid().ToString()
};

// Create a minimal publish request
var minimalRequest = new PublishEventRequest
{
    AggregateId = "product-789",
    AggregateType = "Product",
    EventType = "ProductCreated",
    Topic = "products.events"
};
```

// ## MetricsController
// The `MetricsController` provides operational metrics and health monitoring for the outbox pattern system. It exposes
// comprehensive monitoring endpoints for system health, performance metrics, error analytics, throughput, latency, and
// resource consumption. This controller is essential for observability and alerting in distributed systems.

/// <summary>
/// Controller for operational metrics and health monitoring
/// </summary>

// Example Usage
```csharp
// Example: Monitoring system health
var healthResponse = await new HttpClient().GetAsync("https://localhost:5001/api/metrics/health");
if (healthResponse.IsSuccessStatusCode)
{
    var healthData = await healthResponse.Content.ReadFromJsonAsync<SystemHealthDto>();
    Console.WriteLine($"System Health: {healthData.Status}");
    Console.WriteLine($"Outbox Status: {healthData.OutboxStatus}");
    Console.WriteLine($"Database Status: {healthData.DatabaseStatus}");
}

// Example: Retrieving performance metrics for the last 24 hours
var performanceResponse = await new HttpClient().GetAsync("https://localhost:5001/api/metrics/performance?period=24h");
if (performanceResponse.IsSuccessStatusCode)
{
    var performanceData = await performanceResponse.Content.ReadFromJsonAsync<PerformanceMetricsDto>();
    Console.WriteLine($"Throughput: {performanceData.ThroughputPerSecond} msg/s");
    Console.WriteLine($"Success Rate: {performanceData.SuccessRate:P}");
    Console.WriteLine($"Error Rate: {performanceData.ErrorRate:P}");
}

// Example: Getting error analytics
var errorsResponse = await new HttpClient().GetAsync("https://localhost:5001/api/metrics/errors?limit=50");
if (errorsResponse.IsSuccessStatusCode)
{
    var errorData = await errorsResponse.Content.ReadFromJsonAsync<ErrorAnalyticsDto>();
    Console.WriteLine($"Total Errors: {errorData.TotalErrors}");
    Console.WriteLine($"Dead Letters: {errorData.DeadLetterCount}");
    foreach (var error in errorData.TopErrors)
    {
        Console.WriteLine($"- {error.ErrorType}: {error.Count} occurrences");
    }
}

// Example: Monitoring throughput by hour
var throughputResponse = await new HttpClient().GetAsync("https://localhost:5001/api/metrics/throughput?granularity=hour");
if (throughputResponse.IsSuccessStatusCode)
{
    var throughputData = await throughputResponse.Content.ReadFromJsonAsync<ThroughputMetricsDto>();
    foreach (var metric in throughputData.Metrics)
    {
        Console.WriteLine($"{metric.Timestamp:yyyy-MM-dd HH:mm}: {metric.Count} messages");
    }
}

// Example: Checking latency percentiles
var latencyResponse = await new HttpClient().GetAsync("https://localhost:5001/api/metrics/latency");
if (latencyResponse.IsSuccessStatusCode)
{
    var latencyData = await latencyResponse.Content.ReadFromJsonAsync<LatencyMetricsDto>();
    Console.WriteLine($"P50 Latency: {latencyData.P50Ms}ms");
    Console.WriteLine($"P95 Latency: {latencyData.P95Ms}ms");
    Console.WriteLine($"P99 Latency: {latencyData.P99Ms}ms");
}

// Example: Getting Prometheus-compatible metrics for monitoring integration
var prometheusResponse = await new HttpClient().GetAsync("https://localhost:5001/api/metrics/prometheus");
if (prometheusResponse.IsSuccessStatusCode)
{
    var prometheusMetrics = await prometheusResponse.Content.ReadAsStringAsync();
    Console.WriteLine(prometheusMetrics);
}

// Example: Retrieving active alerts
var alertsResponse = await new HttpClient().GetAsync("https://localhost:5001/api/metrics/alerts");
if (alertsResponse.IsSuccessStatusCode)
{
    var alerts = await alertsResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
    foreach (var alert in alerts)
    {
        Console.WriteLine($"ALERT [{alert.Severity}]: {alert.Message}");
        Console.WriteLine($"  - Type: {alert.Type}");
        Console.WriteLine($"  - Timestamp: {alert.Timestamp}");
    }
}

// Example: Monitoring resource consumption
var resourcesResponse = await new HttpClient().GetAsync("https://localhost:5001/api/metrics/resources");
if (resourcesResponse.IsSuccessStatusCode)
{
    var resourceData = await resourcesResponse.Content.ReadFromJsonAsync<ResourceMetricsDto>();
    Console.WriteLine($"CPU Usage: {resourceData.CpuUsage:P}");
    Console.WriteLine($"Memory Usage: {resourceData.MemoryUsedMb} MB / {resourceData.MemoryTotalMb} MB");
    Console.WriteLine($"Database Connections: {resourceData.DatabaseConnections}");
}
```

// ## ExportController
// The `ExportController` provides API endpoints for exporting outbox messages in various formats (JSON, CSV, XML).
// It enables operators to export message data for analysis, auditing, or integration with external systems.

/// <summary>
/// API controller for exporting outbox messages
/// Supports exporting messages in JSON, CSV, and XML formats with filtering options
/// </summary>

// Example Usage
```csharp
// Initialize HttpClient for API calls
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://localhost:5001");

// 1. Get information about available export options
var infoResponse = await httpClient.GetAsync("/api/export/info");
if (infoResponse.IsSuccessStatusCode)
{
    var exportInfo = await infoResponse.Content.ReadFromJsonAsync<ExportInfo>();
    Console.WriteLine($"Default format: {exportInfo?.DefaultFormat}");
    Console.WriteLine($"Max messages per export: {exportInfo?.MaxMessagesPerExport}");
    Console.WriteLine($"Supported formats: {string.Join(", ", exportInfo?.SupportedFormats ?? new List<string>())}");
    Console.WriteLine($"Filterable fields: {string.Join(", ", exportInfo?.FilterableFields ?? Array.Empty<string>())}");
}

// 2. Get supported export formats
var formatsResponse = await httpClient.GetAsync("/api/export/formats");
if (formatsResponse.IsSuccessStatusCode)
{
    var formats = await formatsResponse.Content.ReadFromJsonAsync<List<string>>();
    Console.WriteLine($"Supported formats: {string.Join(", ", formats ?? new List<string>())}");
}

// 3. Get details about a specific format
var formatDetailsResponse = await httpClient.GetAsync("/api/export/formats/json");
if (formatDetailsResponse.IsSuccessStatusCode)
{
    var formatInfo = await formatDetailsResponse.Content.ReadFromJsonAsync<ExportFormatInfo>();
    Console.WriteLine($"Format: {formatInfo?.Format}");
    Console.WriteLine($"Content-Type: {formatInfo?.ContentType}");
    Console.WriteLine($"Extension: {formatInfo?.Extension}");
    Console.WriteLine($"Description: {formatInfo?.Description}");
}

// 4. Export messages to JSON format (last 24 hours)
var exportRequest = new ExportRequest
{
    Format = "json",
    StartDate = DateTime.UtcNow.AddHours(-24),
    EndDate = DateTime.UtcNow,
    Filter = new ExportFilter
    {
        AggregateId = "order-*",
        Topic = "orders.events"
    }
};

var exportResponse = await httpClient.PostAsync(
    "/api/export/messages",
    new StringContent(
        JsonSerializer.Serialize(exportRequest),
        Encoding.UTF8,
        "application/json"
    )
);

if (exportResponse.IsSuccessStatusCode)
{
    // Save the exported file
    var fileBytes = await exportResponse.Content.ReadAsByteArrayAsync();
    await File.WriteAllBytesAsync("outbox_messages.json", fileBytes);
    Console.WriteLine("Messages exported successfully to outbox_messages.json");
}

// 5. Export messages to CSV format (no filters)
var csvExportRequest = new ExportRequest
{
    Format = "csv",
    StartDate = DateTime.UtcNow.AddDays(-7),
    EndDate = DateTime.UtcNow
};

var csvExportResponse = await httpClient.PostAsync(
    "/api/export/messages",
    new StringContent(
        JsonSerializer.Serialize(csvExportRequest),
        Encoding.UTF8,
        "application/json"
    )
);

if (csvExportResponse.IsSuccessStatusCode)
{
    var csvBytes = await csvExportResponse.Content.ReadAsByteArrayAsync();
    await File.WriteAllBytesAsync("outbox_messages.csv", csvBytes);
    Console.WriteLine("Messages exported successfully to outbox_messages.csv");
}

// 6. Export messages to XML format with specific filters
var xmlExportRequest = new ExportRequest
{
    Format = "xml",
    StartDate = DateTime.UtcNow.AddDays(-30),
    EndDate = DateTime.UtcNow,
    Filter = new ExportFilter
    {
        AggregateType = "Order",
        State = "Processed"
    }
};

var xmlExportResponse = await httpClient.PostAsync(
    "/api/export/messages",
    new StringContent(
        JsonSerializer.Serialize(xmlExportRequest),
        Encoding.UTF8,
        "application/json"
    )
);

if (xmlExportResponse.IsSuccessStatusCode)
{
    var xmlBytes = await xmlExportResponse.Content.ReadAsByteArrayAsync();
    await File.WriteAllBytesAsync("outbox_messages.xml", xmlBytes);
    Console.WriteLine("Messages exported successfully to outbox_messages.xml");
}
```

// ## WebhookController
// The `WebhookController` manages webhook subscriptions and handles incoming webhook deliveries.
// External systems can subscribe to outbox events via webhooks to receive real-time notifications
// when messages are published to specific topics or aggregates.

/// <summary>
/// Controller for managing webhook subscriptions and handling webhook deliveries
/// </summary>

// Example Usage
```csharp
// Initialize HttpClient for API calls
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://localhost:5001");

// 1. Register a new webhook subscription to receive "OrderCreated" events
var registerRequest = new RegisterWebhookRequest
{
    Url = "https://external-service.com/webhooks/order-events",
    Events = new List<string> { "OrderCreated", "OrderUpdated", "PaymentProcessed" }
};

var subscribeResponse = await httpClient.PostAsJsonAsync("/api/webhooks/subscriptions", registerRequest);
if (subscribeResponse.IsSuccessStatusCode)
{
    var subscription = await subscribeResponse.Content.ReadFromJsonAsync<WebhookSubscriptionDto>();
    Console.WriteLine($"Webhook registered with ID: {subscription.Id}");
    Console.WriteLine($"Events subscribed to: {string.Join(", ", subscription.Events)}");
}

// 2. List all active webhook subscriptions
var subscriptionsResponse = await httpClient.GetAsync("/api/webhooks/subscriptions?active=true");
if (subscribeResponse.IsSuccessStatusCode)
{
    var subscriptions = await subscriptionsResponse.Content.ReadFromJsonAsync<List<WebhookSubscriptionDto>>();
    Console.WriteLine($"Found {subscriptions.Count} active webhook subscriptions");
}

// 3. Get details of a specific webhook subscription
var subscriptionDetailsResponse = await httpClient.GetAsync($"/api/webhooks/subscriptions/{subscription.Id}");
if (subscriptionDetailsResponse.IsSuccessStatusCode)
{
    var details = await subscriptionDetailsResponse.Content.ReadFromJsonAsync<WebhookSubscriptionDto>();
    Console.WriteLine($"Webhook URL: {details.Url}");
    Console.WriteLine($"Status: {details.Status}");
    Console.WriteLine($"Created: {details.CreatedAt}");
}

// 4. Test the webhook to verify it's working
var testResponse = await httpClient.PostAsync($"/api/webhooks/subscriptions/{subscription.Id}/test", null);
if (testResponse.IsSuccessStatusCode)
{
    var testResult = await testResponse.Content.ReadFromJsonAsync<WebhookTestResult>();
    Console.WriteLine($"Test result: {testResult.Status}");
    Console.WriteLine($"Delivery attempts: {testResult.DeliveryAttempts}");
}

// 5. Get delivery history for a webhook
var deliveriesResponse = await httpClient.GetAsync($"/api/webhooks/subscriptions/{subscription.Id}/deliveries?limit=50");
if (deliveriesResponse.IsSuccessStatusCode)
{
    var deliveries = await deliveriesResponse.Content.ReadFromJsonAsync<List<WebhookDeliveryDto>>();
    Console.WriteLine($"Found {deliveries.Count} delivery attempts");
    foreach (var delivery in deliveries.OrderByDescending(d => d.Timestamp))
    {
        Console.WriteLine($"- {delivery.Timestamp:yyyy-MM-dd HH:mm:ss}: Status={delivery.Status}, Response={delivery.ResponseStatus}");
    }
}

// 6. Delete a webhook subscription when no longer needed
var deleteResponse = await httpClient.DeleteAsync($"/api/webhooks/subscriptions/{subscription.Id}");
if (deleteResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Webhook subscription deleted successfully");
}

// External systems receive webhook deliveries at the configured URL with signature verification
// POST https://external-service.com/webhooks/order-events
// Headers: X-Webhook-Signature: <HMAC-SHA256-signature>
// Body: {"eventType":"OrderCreated","aggregateId":"order-12345","data":{...}}
```

// ## OutboxMessageController
// The `OutboxMessageController` is the core API controller for managing outbox messages.
// It provides endpoints for publishing new events, querying existing messages,
// retrying failed operations, archiving old messages, and retrieving outbox statistics.

/// <summary>
/// API controller for managing outbox messages - the primary interface for publishing events
/// </summary>

// Example Usage
```csharp
// Initialize HttpClient for API calls
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://localhost:5001");

// 1. Publish a new event to the outbox
var publishableEvent = new PublishableEvent
{
    Topic = "orders.created",
    EventData = new { OrderId = "order-123", Amount = 99.99 }
};

var publishResponse = await httpClient.PostAsJsonAsync("/api/outbox/events", publishableEvent);
if (publishResponse.IsSuccessStatusCode)
{
    var message = await publishResponse.Content.ReadFromJsonAsync<OutboxMessageDto>();
    Console.WriteLine($"Event published successfully with ID: {message.Id}");
}

// 2. Get a specific message by ID
var messageResponse = await httpClient.GetAsync("/api/outbox/messages/550e8400-e29b-41d4-a716-446655440000");
if (messageResponse.IsSuccessStatusCode)
{
    var message = await messageResponse.Content.ReadFromJsonAsync<OutboxMessageDto>();
    Console.WriteLine($"Message state: {message.State}");
}

// 3. Get messages by aggregate ID
var aggregateResponse = await httpClient.GetAsync("/api/outbox/messages/aggregate/order-123?limit=10");
if (aggregateResponse.IsSuccessStatusCode)
{
    var messages = await aggregateResponse.Content.ReadFromJsonAsync<List<OutboxMessageDto>>();
    Console.WriteLine($"Found {messages.Count} messages for aggregate order-123");
}

// 4. List messages with pagination and state filtering
var listResponse = await httpClient.GetAsync("/api/outbox/messages?state=Published&page=1&pageSize=20");
if (listResponse.IsSuccessStatusCode)
{
    var paginatedResponse = await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<OutboxMessageDto>>();
    Console.WriteLine($"Page {paginatedResponse.Page}, Total Items: {paginatedResponse.TotalItems}");
}

// 5. Retry a failed message
var retryResponse = await httpClient.PostAsync("/api/outbox/messages/550e8400-e29b-41d4-a716-446655440000/retry", null);
if (retryResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Message retry initiated successfully");
}

// 6. Archive published messages older than 30 days
var archiveResponse = await httpClient.PostAsync("/api/outbox/messages/archive?daysOld=30", null);
if (archiveResponse.IsSuccessStatusCode)
{
    var archiveResult = await archiveResponse.Content.ReadFromJsonAsync<ArchiveResult>();
    Console.WriteLine($"Archive status: {archiveResult.Status}");
}

// 7. Get outbox statistics
var statsResponse = await httpClient.GetAsync("/api/outbox/statistics");
if (statsResponse.IsSuccessStatusCode)
{
    var stats = await statsResponse.Content.ReadFromJsonAsync<OutboxStatisticsDto>();
    Console.WriteLine($"Total messages: {stats.TotalMessages}, Failed: {stats.FailedMessages}");
}
```

// ## DeadLetterController
// The `DeadLetterController` provides API endpoints for managing failed messages in the dead letter queue (DLQ).
// It enables operators to review, analyze, requeue, or permanently delete messages that failed to process,
// ensuring system reliability and providing visibility into message processing failures.

/// <summary>
/// API controller for managing dead letter queue operations
/// Provides endpoints for reviewing, requeuing, and analyzing failed messages
/// </summary>

// Example Usage
```csharp
// Initialize HttpClient for API calls
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://localhost:5001");

// 1. Get unreviewed dead letters (messages awaiting operator review)
var unreviewedResponse = await httpClient.GetAsync("/api/deadletters/unreviewed");
if (unreviewedResponse.IsSuccessStatusCode)
{
    var unreviewedDeadLetters = await unreviewedResponse.Content.ReadFromJsonAsync<List<DeadLetter>>();
    Console.WriteLine($"Found {unreviewedDeadLetters?.Count} unreviewed dead letters");
}

// 2. Get all dead letters with pagination
var deadLettersResponse = await httpClient.GetAsync("/api/deadletters?page=1&pageSize=50");
if (deadLettersResponse.IsSuccessStatusCode)
{
    var paginatedResponse = await deadLettersResponse.Content.ReadFromJsonAsync<PaginatedResponse<DeadLetter>>();
    Console.WriteLine($"Page {paginatedResponse?.Page} of {paginatedResponse?.TotalPages} - " +
                     $"Total: {paginatedResponse?.TotalItems} dead letters");
}

// 3. Get a specific dead letter by ID
var specificDeadLetterResponse = await httpClient.GetAsync(
    $"/api/deadletters/{Guid.NewGuid()}");
if (specificDeadLetterResponse.IsSuccessStatusCode)
{
    var deadLetter = await specificDeadLetterResponse.Content.ReadFromJsonAsync<DeadLetter>();
    Console.WriteLine($"Dead letter {deadLetter?.Id}: {deadLetter?.ErrorMessage}");
}

// 4. Review a dead letter (mark as reviewed with notes)
var reviewResponse = await httpClient.PutAsync(
    $"/api/deadletters/{Guid.NewGuid()}/review",
    new StringContent(
        JsonSerializer.Serialize(new ReviewDeadLetterRequest { Notes = "Investigated - transient network issue" }),
        Encoding.UTF8,
        "application/json"));
if (reviewResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Dead letter reviewed successfully");
}

// 5. Requeue a dead letter for retry
var requeueResponse = await httpClient.PostAsync(
    $"/api/deadletters/{Guid.NewGuid()}/requeue",
    new StringContent(
        JsonSerializer.Serialize(new RequeueDeadLetterRequest { Reason = "Temporary network issue resolved" }),
        Encoding.UTF8,
        "application/json"));
if (requeueResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Dead letter requeued for retry");
}

// 6. Get dead letter statistics
var statsResponse = await httpClient.GetAsync("/api/deadletters/statistics");
if (statsResponse.IsSuccessStatusCode)
{
    var stats = await statsResponse.Content.ReadFromJsonAsync<DeadLetterStatistics>();
    Console.WriteLine($"Total: {stats?.TotalDeadLetters}, Unreviewed: {stats?.UnreviewedCount}, " +
                     $"Reviewed: {stats?.ReviewedCount}");
    Console.WriteLine($"Errors by type: {string.Join(", ", stats?.ErrorsByType.Select(kvp => $"{kvp.Key}: {kvp.Value}") ?? Array.Empty<string>())}");
    Console.WriteLine($"Oldest: {stats?.OldestDeadLetter}, Last updated: {stats?.LastUpdated}");
}

// 7. Delete a dead letter permanently
var deleteResponse = await httpClient.DeleteAsync($"/api/deadletters/{Guid.NewGuid()}");
if (deleteResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Dead letter deleted successfully");
}

// 8. Export dead letters to CSV
var exportResponse = await httpClient.PostAsync("/api/deadletters/export",
    new StringContent(
        JsonSerializer.Serialize(new ExportRequest { Format = "csv" }),
        Encoding.UTF8,
        "application/json"));
if (exportResponse.IsSuccessStatusCode)
{
    var csvContent = await exportResponse.Content.ReadAsStringAsync();
    Console.WriteLine(csvContent);
}
```

// ## OutboxBackoffExtensions
// The `OutboxBackoffExtensions` class provides fluent configuration and pure delay calculation for the outbox processor's batch size and idle backoff strategies. It enables configuring exponential backoff, fixed delay, batch sizes, and validation through a clean fluent API.

/// <summary>
/// Fluent configuration and pure delay calculation for the outbox processor's batch size and
/// idle backoff. Kept separate from <see cref="OutboxProcessorOptions"/> so the delay maths can
/// be unit-tested in isolation, without spinning up the background service.
/// </summary>

// Example Usage
```csharp
using System;
using DotnetOutboxPattern.Infrastructure;

// Create a new OutboxProcessorOptions instance
var options = new OutboxProcessorOptions();

// Configure batch size (messages processed per batch)
options.WithBatchSize(50);

// Configure exponential backoff strategy
// Base delay: 100ms, Max delay: 5000ms, Multiplier: 2.0
options.WithExponentialBackoff(
    baseDelayMs: 100,
    maxDelayMs: 5000,
    multiplier: 2.0
);

// Or configure fixed delay (no backoff)
// options.WithFixedDelay(200);

// Validate the configuration
options.ValidateBackoff();

// Compute delay based on consecutive empty batches
// For exponential backoff: delay grows exponentially with each empty batch
var delay1 = options.ComputeDelay(0);   // Base delay (100ms)
var delay2 = options.ComputeDelay(1);   // 100 * 2^1 = 200ms
var delay3 = options.ComputeDelay(2);   // 100 * 2^2 = 400ms
var delay4 = options.ComputeDelay(5);   // 100 * 2^5 = 3200ms (capped at max 5000ms)
var delay5 = options.ComputeDelay(10);  // Still capped at 5000ms

Console.WriteLine($"Base delay: {delay1.TotalMilliseconds}ms");
Console.WriteLine($"After 1 empty batch: {delay2.TotalMilliseconds}ms");
Console.WriteLine($"After 2 empty batches: {delay3.TotalMilliseconds}ms");
Console.WriteLine($"After 5 empty batches: {delay4.TotalMilliseconds}ms");
Console.WriteLine($"After 10 empty batches: {delay5.TotalMilliseconds}ms");

// Use in OutboxProcessor configuration
var processorOptions = new OutboxProcessorOptions()
    .WithBatchSize(25)
    .WithExponentialBackoff(baseDelayMs: 50, maxDelayMs: 10000, multiplier: 1.5)
    .ValidateBackoff();

// Create and configure OutboxProcessor with the options
var processor = new OutboxProcessor(
    dbContext: outboxDbContext,
    options: processorOptions,
    logger: logger
);

// Start the processor
processor.Start();

// Later, when shutting down
processor.Stop();
```

// ## PoisonMessageDeadLetterTests
// The `PoisonMessageDeadLetterTests` class provides end-to-end tests that verify the poison-message path in the outbox pattern. A message that can never be published (due to bad payload, permanently rejecting broker, or other unrecoverable errors) must not be retried forever. After its configured `MaxPublishAttempts` are exhausted, it must leave the hot pending set and land in the dead-letter store so an operator can inspect it, and it must stop being redelivered.

/// <summary>
/// End-to-end tests for the poison-message path: a message that can never be published 
/// (bad payload, permanently rejecting broker, etc.) must not be retried forever. After its 
/// attempts are exhausted it has to leave the hot pending set and land in the dead-letter 
/// store so an operator can inspect it, and it must stop being redelivered.
/// </summary>

// Example Usage
```csharp
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Create an in-memory SQLite database for testing
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

var dbOptions = new DbContextOptionsBuilder<OutboxDbContext>()
  .UseSqlite(connection)
  .Options;

using var context = new OutboxDbContext(dbOptions);
context.Database.EnsureCreated();

// Create a publisher that always fails (poison message scenario)
var failingPublisher = new PoisonMessageDeadLetterTests.AlwaysFailingPublisher();

// Create the publishing service with the failing publisher
var outbox = new OutboxRepository(context);
var dlq = new DeadLetterRepository(context);
var service = new MessagePublishingService(
  outbox,
  dlq,
  failingPublisher,
  NullLogger<MessagePublishingService>.Instance,
  new PublishingOptions { PublishTimeout = TimeSpan.FromSeconds(5) }
);

// Create a poison message with limited attempts
var poisonMessage = new OutboxMessage
{
  IdempotencyKey = Guid.NewGuid().ToString("N"),
  AggregateId = "agg-poison",
  AggregateType = "PoisonWidget",
  EventType = EventType.Custom,
  EventData = "{\"bad\":true}",
  EventTypeName = "WidgetBroke",
  Topic = "widgets",
  MaxPublishAttempts = 3  // Limited attempts before dead-lettering
};

// Save the poison message
context.OutboxMessages.Add(poisonMessage);
await context.SaveChangesAsync();

// Process messages multiple times (simulating dispatcher ticks)
for (var attempt = 0; attempt < poisonMessage.MaxPublishAttempts; attempt++)
{
  await service.ProcessPendingMessagesAsync(batchSize: 10);
  
  // Requeue the message for next attempt (simulates scheduler requeue)
  var msg = await context.OutboxMessages
    .FirstOrDefaultAsync(x => x.IdempotencyKey == poisonMessage.IdempotencyKey);
  if (msg?.State == OutboxMessageState.Processing)
  {
    msg.State = OutboxMessageState.Pending;
    msg.IsLocked = false;
    msg.LockExpiresAt = null;
    await context.SaveChangesAsync();
  }
}

// Verify the message was attempted exactly MaxPublishAttempts times
Console.WriteLine($"Publisher attempts: {failingPublisher.Attempts}"); // Should be 3

// Verify the message is now in Failed state and dead-lettered
var persisted = await outbox.GetByIdempotencyKeyAsync(poisonMessage.IdempotencyKey);
Console.WriteLine($"Message state: {persisted?.State}"); // Should be Failed
Console.WriteLine($"Publish attempts: {persisted?.PublishAttempts}"); // Should be 3

// Verify it's in the dead-letter store
var deadLetter = await dlq.GetByOutboxMessageIdAsync(persisted?.Id ?? Guid.Empty);
Console.WriteLine($"Dead letter exists: {deadLetter != null}"); // Should be true
Console.WriteLine($"Dead letter topic: {deadLetter?.Topic}"); // Should be "widgets"

// Verify no pending messages remain
var pending = await outbox.GetPendingMessagesAsync(10);
Console.WriteLine($"Pending messages: {pending.Count}"); // Should be 0

// Cleanup
connection.Dispose();
```

// ## OutboxBackoffOptionsTests
// The `OutboxBackoffOptionsTests` class provides comprehensive unit tests for the configurable batch size and idle backoff options on `OutboxProcessorOptions` and the helper methods in `OutboxBackoffExtensions`. These tests verify the fluent configuration API, validation logic, and delay calculation algorithms work correctly under various scenarios including edge cases and error conditions.

/// <summary>
/// Tests for the configurable batch-size and idle-backoff options on
/// <see cref="OutboxProcessorOptions"/> and the helpers in
/// <see cref="OutboxBackoffExtensions"/>: the fluent builders, the aggregate validation, and
/// the pure delay calculation.
/// </summary>

// Example Usage
```csharp
using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;

// Test batch size configuration
var options = new OutboxProcessorOptions().WithBatchSize(250);
options.BatchSize.Should().Be(250);

// Test exponential backoff configuration
var exponentialOptions = new OutboxProcessorOptions()
    .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 30000, multiplier: 3.0);
exponentialOptions.BackoffStrategy.Should().Be(BackoffStrategy.Exponential);
exponentialOptions.DelayBetweenBatches.Should().Be(1000);
exponentialOptions.MaxDelayBetweenBatches.Should().Be(30000);
exponentialOptions.BackoffMultiplier.Should().Be(3.0);

// Test fixed delay configuration
var fixedOptions = new OutboxProcessorOptions().WithFixedDelay(2500);
fixedOptions.BackoffStrategy.Should().Be(BackoffStrategy.Fixed);
fixedOptions.DelayBetweenBatches.Should().Be(2500);

// Test validation - should return same instance when valid
var validOptions = new OutboxProcessorOptions();
validOptions.ValidateBackoff().Should().BeSameAs(validOptions);

// Test delay computation with exponential backoff
var backoffOptions = new OutboxProcessorOptions()
    .WithExponentialBackoff(baseDelayMs: 1000, maxDelayMs: 60000, multiplier: 2.0);

// Base delay (no empty batches)
var delay0 = backoffOptions.ComputeDelay(0);
// After 1 empty batch: 1000 * 2^1 = 2000ms
var delay1 = backoffOptions.ComputeDelay(1);
// After 2 empty batches: 1000 * 2^2 = 4000ms
var delay2 = backoffOptions.ComputeDelay(2);
// Capped at max delay
var delay20 = backoffOptions.ComputeDelay(20); // Still 60000ms (capped)

Console.WriteLine($"Base delay: {delay0.TotalMilliseconds}ms");
Console.WriteLine($"After 1 empty batch: {delay1.TotalMilliseconds}ms");
Console.WriteLine($"After 2 empty batches: {delay2.TotalMilliseconds}ms");
Console.WriteLine($"After 20 empty batches: {delay20.TotalMilliseconds}ms (capped)");

// Test error cases - these will throw exceptions
try {
    new OutboxProcessorOptions().WithBatchSize(-1);
    throw new Exception("Should have thrown");
} catch (ArgumentOutOfRangeException) { }

try {
    new OutboxProcessorOptions()
        .WithExponentialBackoff(baseDelayMs: 5000, maxDelayMs: 1000);
    throw new Exception("Should have thrown");
} catch (ArgumentOutOfRangeException) { }
```

// ## OutboxExceptionExtensions
// The `OutboxExceptionExtensions` class provides extension methods for `OutboxException` and its derived types.
// It offers utilities for determining retryability, formatting error messages, extracting diagnostic information,
// adding context to exceptions, and identifying critical failures that should not be retried.

/// <summary>
/// Extension methods for <see cref="OutboxException"/> and its derived types
/// </summary>

// Example Usage
```csharp
using System;
using DotnetOutboxPattern.Exceptions;

// Create a sample exception
var exception = new MessagePublishingException(
    "Failed to publish message to broker",
    Guid.NewGuid(),
    attemptNumber: 1
);

// Check if the exception is retryable
bool isRetryable = exception.IsRetryable();
Console.WriteLine($"Is retryable: {isRetryable}"); // true

// Get a formatted error message with error code and resource ID
var formattedMessage = exception.GetFormattedErrorMessage();
Console.WriteLine(formattedMessage);
// Output: [MSG_PUBLISH_FAIL] Failed to publish message to broker | Resource: <message-guid>

// Get diagnostic information about the exception
var diagnostics = exception.GetDiagnosticInfo();
foreach (var kvp in diagnostics)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
// Output includes: ErrorCode, Message, ExceptionType, ResourceId, MessageId, AttemptNumber, etc.

// Add additional context to an exception
var contextualException = exception.WithContext("Additional context about the failure");
Console.WriteLine(contextualException.Message);
// Output: "Failed to publish message to broker | Additional context about the failure"

// Check if the exception is critical (should not be retried)
bool isCritical = exception.IsCritical();
Console.WriteLine($"Is critical: {isCritical}"); // false

// Example with a critical exception
var criticalException = new ValidationException("Invalid message format");
isCritical = criticalException.IsCritical();
Console.WriteLine($"Is critical: {isCritical}"); // true
```

// ## DefaultMessagePublisherExtensions
// Extension methods that enhance `DefaultMessagePublisher` with logging, batch publishing, and retry capabilities.
// They simplify common publishing scenarios such as bulk operations, parallel publishing with throttling, and resilient retries for transient failures.

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger<DefaultMessagePublisher> logger = loggerFactory.CreateLogger<DefaultMessagePublisher>();

// Create a publisher instance with a logger
var publisher = logger.WithLogger();

// Prepare some outbox messages
var messages = new List<OutboxMessage>
{
    new OutboxMessage(),
    new OutboxMessage()
};

// Publish the batch sequentially
await publisher.PublishBatchAsync(messages);

// Publish the batch in parallel with a max degree of parallelism
await publisher.PublishBatchAsync(messages, maxDegreeOfParallelism: 4);

// Publish a single message with retry logic
await publisher.PublishWithRetryAsync(messages[0], maxRetries: 5, retryDelay: TimeSpan.FromMilliseconds(200));

// Wrap the publisher with a logging decorator
IMessagePublisher loggingPublisher = publisher.WithLoggingDecorator(logger);
await loggingPublisher.PublishAsync(messages[0], CancellationToken.None);
```

// ## OutboxEndToEndTests
// The `OutboxEndToEndTests` class provides end-to-end tests that exercise the outbox pattern against a real SQLite database through actual repositories and services. These tests verify the core guarantees of the outbox pattern: durability through process crashes and at-least-once delivery with consumer-side deduplication.

/// <summary>
/// End-to-end tests for the outbox pattern that verify durability and at-least-once delivery guarantees
/// </summary>

// Example Usage
```csharp
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Create a test fixture with in-memory SQLite database
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

var dbOptions = new DbContextOptionsBuilder<OutboxDbContext>()
    .UseSqlite(connection)
    .Options;

using var context = new OutboxDbContext(dbOptions);
context.Database.EnsureCreated();

// Create a recording publisher to track deliveries
var publisher = new RecordingPublisher();

// Create a process scope (simulates a fresh application process)
using var process = new ProcessScope(dbOptions, publisher, new PublishingOptions());

// Create and save a message within a transaction
await using var tx = await process.Context.Database.BeginTransactionAsync();
var message = new OutboxMessage
{
    IdempotencyKey = Guid.NewGuid().ToString("N"),
    AggregateId = "order-123",
    AggregateType = "Order",
    EventType = EventType.Created,
    EventData = "{\"orderId\":123}",
    EventTypeName = "OrderCreated",
    Topic = "orders",
    MaxPublishAttempts = 3
};
process.Context.OutboxMessages.Add(message);
await process.Context.SaveChangesAsync();
await tx.CommitAsync();

// Process pending messages (simulates the outbox processor running)
await process.PublishingService.ProcessPendingMessagesAsync(batchSize: 10);

// Verify the message was delivered
publisher.Deliveries.Should().ContainSingle();

// Verify the message state
var persisted = await process.OutboxRepository.GetByIdempotencyKeyAsync(message.IdempotencyKey);
persisted.Should().NotBeNull();
persisted.State.Should().Be(OutboxMessageState.Published);

// Cleanup
connection.Dispose();
```
