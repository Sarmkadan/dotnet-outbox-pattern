# BatchProcessingModelsTestsExtensions

The `BatchProcessingModelsTestsExtensions` class provides a suite of static factory and assertion methods designed to streamline unit and integration testing of batch processing components within the `dotnet-outbox-pattern` library. These extensions facilitate the rapid creation of model instances and simplify equivalence verification between objects, ensuring consistent test data generation and clear failure reporting.

## API

### Factory Methods

*   **`CreateDefaultOptions()`**
    *   Purpose: Creates a new `BatchProcessingOptions` instance pre-populated with default configuration values.
    *   Return: A `BatchProcessingOptions` object.

*   **`CreateCustomOptions(...)`**
    *   Purpose: Creates a `BatchProcessingOptions` instance with specific property values defined by the caller.
    *   Return: A `BatchProcessingOptions` object.

*   **`CreateDefaultChunkResult()`**
    *   Purpose: Creates a new `BatchChunkResult` instance initialized with default values representing a standard result state.
    *   Return: A `BatchChunkResult` object.

*   **`CreateChunkResult(...)`**
    *   Purpose: Creates a `BatchChunkResult` instance configured with specific property values.
    *   Return: A `BatchChunkResult` object.

*   **`CreateDefaultSummary()`**
    *   Purpose: Creates a new `BatchProcessingSummary` instance initialized with baseline metrics and status values.
    *   Return: A `BatchProcessingSummary` object.

*   **`CreateSummary(...)`**
    *   Purpose: Creates a `BatchProcessingSummary` instance configured with specific metrics or status values.
    *   Return: A `BatchProcessingSummary` object.

### Assertion Methods

*   **`ShouldBeEquivalentTo(this BatchProcessingOptions actual, BatchProcessingOptions expected)`**
    *   Purpose: Asserts that the `actual` options instance is equivalent to the `expected` instance, typically throwing an assertion exception upon failure.
    *   Parameters: `actual` (the object under test), `expected` (the reference object).

*   **`ShouldBeEquivalentTo(this BatchChunkResult actual, BatchChunkResult expected)`**
    *   Purpose: Asserts that the `actual` chunk result is equivalent to the `expected` result, throwing an exception if differences are detected.
    *   Parameters: `actual` (the object under test), `expected` (the reference object).

*   **`ShouldBeEquivalentTo(this BatchProcessingSummary actual, BatchProcessingSummary expected)`**
    *   Purpose: Asserts that the `actual` processing summary is equivalent to the `expected` summary, throwing an exception if differences are detected.
    *   Parameters: `actual` (the object under test), `expected` (the reference object).

## Usage

### Example 1: Initializing Test Data
```csharp
// Create configured options for a test scenario
var options = BatchProcessingModelsTestsExtensions.CreateCustomOptions(
    batchSize: 50,
    maxRetries: 3
);

// Generate a default summary as a baseline
var summary = BatchProcessingModelsTestsExtensions.CreateDefaultSummary();
```

### Example 2: Asserting Batch Results
```csharp
// Act: Process the batch
var actualResult = _service.ProcessBatch(testData);

// Arrange: Define the expected result
var expectedResult = BatchProcessingModelsTestsExtensions.CreateChunkResult(
    processedCount: 10,
    success: true
);

// Assert: Verify equivalence
actualResult.ShouldBeEquivalentTo(expectedResult);
```

## Notes

*   **Edge Cases**: The `ShouldBeEquivalentTo` methods typically perform deep equality checks. If null values are passed as arguments, behavior depends on the underlying assertion framework; ensure both `actual` and `expected` instances are instantiated to avoid unexpected `NullReferenceException` errors unless explicitly tested for null.
*   **Thread Safety**: All methods are implemented as static stateless functions. They are thread-safe and can be invoked concurrently across multiple test threads without risk of race conditions or state corruption.
*   **Compatibility**: These methods are intended solely for testing contexts and should not be included in production build outputs.
