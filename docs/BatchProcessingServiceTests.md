# BatchProcessingServiceTests

The `BatchProcessingServiceTests` class contains unit tests for the `BatchProcessingService` component of the `dotnet-outbox-pattern` project. It validates chunking logic, error handling, metric tracking, and delegation to the publishing service. The tests are written using an asynchronous test framework (e.g., xUnit) and rely on mocked dependencies to isolate the service under test.

## API

### `public BatchProcessingServiceTests()`
Initializes a new instance of the test class. Sets up the necessary mock objects for the publishing service and options, and creates the `BatchProcessingService` instance used by all test methods.  
**Parameters:** None.  
**Return value:** None.  
**Exceptions:** None.

### `public void Constructor_WithNullPublishingService_ThrowsArgumentNullException`
Verifies that the `BatchProcessingService` constructor throws an `ArgumentNullException` when the publishing service argument is `null`.  
**Parameters:** None.  
**Return value:** None.  
**Exceptions:** None (the test itself does not throw; it asserts that the constructor throws).

### `public void Constructor_WithNullOptions_ThrowsArgumentNullException`
Verifies that the `BatchProcessingService` constructor throws an `ArgumentNullException` when the options argument is `null`.  
**Parameters:** None.  
**Return value:** None.  
**Exceptions:** None.

### `public async Task ProcessInChunksAsync_WithDefaultSize_DividesIntoChunks`
Tests that `ProcessInChunksAsync` splits a collection of messages into chunks of the default size (as defined in the options). Asserts that the publishing service is called the expected number of times with the correct chunk sizes.  
**Parameters:** None.  
**Return value:** `Task`.  
**Exceptions:** None.

### `public async Task ProcessInChunksAsync_WithCustomTotal_RespectsCustomSize`
Tests that `ProcessInChunksAsync` respects a custom chunk size passed via options or method parameter. Verifies that the number of chunks and the size of each chunk match the custom value.  
**Parameters:** None.  
**Return value:** `Task`.  
**Exceptions:** None.

### `public async Task ProcessInChunksAsync_WhenServiceThrows_CatchesAndReturnsFailure`
Tests that when the publishing service throws an exception during processing, `ProcessInChunksAsync` catches the exception and returns a failure result (e.g., a `BatchResult` with error information) instead of propagating the exception.  
**Parameters:** None.  
**Return value:** `Task`.  
**Exceptions:** None.

### `public async Task ProcessInChunksAsync_TracksCumulativeMetrics`
Verifies that `ProcessInChunksAsync` correctly accumulates metrics (e.g., total processed count, total errors) across multiple chunks and exposes them in the returned result.  
**Parameters:** None.  
**Return value:** `Task`.  
**Exceptions:** None.

### `public async Task ProcessScheduledInChunksAsync_DelegatesToPublishingService`
Tests that `ProcessScheduledInChunksAsync` correctly delegates the processing of scheduled messages to the publishing service, respecting the same chunking logic as `ProcessInChunksAsync`.  
**Parameters:** None.  
**Return value:** `Task`.  
**Exceptions:** None.

### `public async Task ProcessInChunksAsync_WithSingleMessage_CreatesOneChunk`
Tests that when only one message is provided, `ProcessInChunksAsync` creates a single chunk and calls the publishing service exactly once.  
**Parameters:** None.  
**Return value:** `Task`.  
**Exceptions:** None.

### `public async Task ProcessInChunksAsync_SetsDurationCorrectly`
Verifies that `ProcessInChunksAsync` records the total processing duration in the result, reflecting the elapsed time from start to completion.  
**Parameters:** None.  
**Return value:** `Task`.  
**Exceptions:** None.

## Usage

The following examples demonstrate how the `BatchProcessingService` is used in a typical outbox processing pipeline. These scenarios are covered by the tests above.

**Example 1: Processing a batch of outbox messages with default chunk size**

```csharp
var options = new BatchProcessingOptions { ChunkSize = 10 };
var publisher = new OutboxPublisher();
var service = new BatchProcessingService(publisher, options);

var messages = Enumerable.Range(1, 25).Select(i => new OutboxMessage { Id = i }).ToList();
BatchResult result = await service.ProcessInChunksAsync(messages);

Console.WriteLine($"Processed: {result.ProcessedCount}, Errors: {result.ErrorCount}, Duration: {result.Duration}");
```

**Example 2: Handling a failure during chunk processing**

```csharp
var options = new BatchProcessingOptions { ChunkSize = 5 };
var failingPublisher = new FailingPublisher(); // throws on third message
var service = new BatchProcessingService(failingPublisher, options);

var messages = Enumerable.Range(1, 10).Select(i => new OutboxMessage { Id = i }).ToList();
BatchResult result = await service.ProcessInChunksAsync(messages);

if (!result.IsSuccess)
{
    Console.WriteLine($"Processing failed after {result.ProcessedCount} messages. Error: {result.ErrorMessage}");
}
```

## Notes

- **Edge cases:** The tests cover single-message batches, custom chunk sizes, and service failures. An empty message collection is not explicitly tested in the listed methods; callers should ensure the service handles zero messages gracefully (e.g., returning a success result with zero processed count).
- **Thread safety:** The `BatchProcessingService` itself is not guaranteed to be thread-safe. The tests run sequentially and do not verify concurrent access. In production, the service should be used from a single processing loop or protected by synchronization if accessed from multiple threads.
- **Metric accuracy:** The `TracksCumulativeMetrics` and `SetsDurationCorrectly` tests assume that the service uses a high-resolution timer and accumulates counts atomically. Any changes to metric collection logic should preserve these invariants.
- **Dependency injection:** The tests use mocked dependencies. In a real application, the publishing service and options are typically resolved through dependency injection. The constructor argument validation tests ensure that null dependencies are rejected early.
