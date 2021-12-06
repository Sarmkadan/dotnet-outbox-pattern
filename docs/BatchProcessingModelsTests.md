# BatchProcessingModelsTests

Unit tests for the batch processing models and options in the `dotnet-outbox-pattern` project. These tests validate the behavior of configuration options, result containers, and summary aggregations used in batch processing scenarios.

## API

### `BatchProcessingOptions_DefaultValues_AreCorrect`
Validates that `BatchProcessingOptions` initializes with expected default values for batch size, timeout, and retry policies.

### `BatchProcessingOptions_CustomValues_AreApplied`
Ensures that custom values provided to `BatchProcessingOptions` are correctly applied to the instance.

### `BatchProcessingOptions_WithMinimalValues_Works`
Confirms that `BatchProcessingOptions` functions correctly when initialized with minimal or boundary values (e.g., zero or one).

### `BatchChunkResult_DefaultConstructor_InitializesProperties`
Verifies that `BatchChunkResult` initializes all properties to default or expected values when using the parameterless constructor.

### `BatchChunkResult_WithValues_SetsPropertiesCorrectly`
Checks that `BatchChunkResult` correctly assigns provided values to its properties during construction.

### `BatchChunkResult_Duration_CalculatesCorrectly`
Validates that the `Duration` property of `BatchChunkResult` is computed accurately based on start and end timestamps.

### `BatchProcessingSummary_DefaultConstructor_InitializesCollections`
Ensures that `BatchProcessingSummary` initializes internal collections (e.g., chunk results) when instantiated via the default constructor.

### `BatchProcessingSummary_WithValues_SetsPropertiesCorrectly`
Confirms that `BatchProcessingSummary` correctly assigns provided values to its properties during construction.

### `BatchProcessingSummary_Duration_CalculatesCorrectly`
Validates that the `Duration` property of `BatchProcessingSummary` is computed accurately based on the earliest start and latest end timestamps across all chunks.

### `BatchProcessingSummary_Accumulate_AddsChunkResults`
Tests that the `Accumulate` method of `BatchProcessingSummary` correctly aggregates multiple `BatchChunkResult` instances into the summary.

## Usage
