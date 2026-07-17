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
var isEmpty = StringHelper.IsEmpty(" "); // true
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