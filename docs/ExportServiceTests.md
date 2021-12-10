# ExportServiceTests
The `ExportServiceTests` class is a test suite designed to validate the functionality of the `ExportService` class, which is responsible for exporting data in various formats. This test class ensures that the `ExportService` behaves correctly under different scenarios, including valid and invalid input, and verifies that it interacts correctly with its dependencies, such as formatters and the outbox service.

## API
* `public ExportServiceTests`: The constructor for the `ExportServiceTests` class.
* `public void Constructor_WithNullOutboxService_ThrowsArgumentNullException`: Verifies that the constructor throws an `ArgumentNullException` when the outbox service is null.
* `public void Constructor_WithNullFormatters_ThrowsArgumentNullException`: Verifies that the constructor throws an `ArgumentNullException` when the formatters are null.
* `public void GetSupportedFormats_ReturnsRegisteredFormats`: Tests that the `GetSupportedFormats` method returns the registered formats.
* `public void GetSupportedFormats_ReturnsEmptyList_WhenNoFormatters`: Tests that the `GetSupportedFormats` method returns an empty list when no formatters are registered.
* `public async Task ExportAsync_WithJsonFormat_UsesJsonFormatter`: Verifies that the `ExportAsync` method uses the JSON formatter when the JSON format is specified.
* `public async Task ExportAsync_WithCsvFormat_UsesCsvFormatter`: Verifies that the `ExportAsync` method uses the CSV formatter when the CSV format is specified.
* `public async Task ExportAsync_WithUnsupportedFormat_ThrowsInvalidOperationException`: Tests that the `ExportAsync` method throws an `InvalidOperationException` when an unsupported format is specified.
* `public async Task ExportAsync_WithLowercaseFormat_IsFormatCaseInsensitive`: Verifies that the `ExportAsync` method is case-insensitive when it comes to format specification.
* `public async Task ExportAsync_SetsContentSizeCorrectly`: Tests that the `ExportAsync` method sets the content size correctly.
* `public async Task ExportAsync_SetsExportedAtTimestamp`: Verifies that the `ExportAsync` method sets the exported-at timestamp correctly.
* `public async Task ExportAsync_WhenFormatterThrows_PropagatesException`: Tests that the `ExportAsync` method propagates exceptions thrown by formatters.
* `public async Task ExportToFileAsync_CreatesExportDirectory`: Verifies that the `ExportToFileAsync` method creates the export directory.
* `public async Task ExportAsync_WithEmptyMessageList_ReturnsValidResult`: Tests that the `ExportAsync` method returns a valid result when the message list is empty.

## Usage
The following examples demonstrate how to use the `ExportServiceTests` class:
```csharp
// Example 1: Verifying that the ExportService uses the correct formatter
var exportService = new ExportService(new OutboxService(), new[] { new JsonFormatter() });
var result = await exportService.ExportAsync(new[] { new Message { Content = "Hello World" } }, "json");
Assert.IsTrue(result.IsSuccess);

// Example 2: Testing that the ExportService throws an exception for an unsupported format
var exportService = new ExportService(new OutboxService(), new[] { new JsonFormatter() });
await Assert.ThrowsAsync<InvalidOperationException>(() => exportService.ExportAsync(new[] { new Message { Content = "Hello World" } }, "unsupported"));
```

## Notes
The `ExportServiceTests` class is designed to be thread-safe, as it does not maintain any shared state between tests. However, the tests themselves may not be thread-safe, as they may rely on external dependencies such as the outbox service and formatters. It is recommended to run these tests sequentially to avoid any potential issues. Additionally, the tests assume that the `ExportService` class is correctly implemented and that the formatters and outbox service are functioning as expected. If any of these assumptions are not met, the tests may not accurately reflect the behavior of the `ExportService` class.
