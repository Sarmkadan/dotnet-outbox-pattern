# ModelsTests

Unit test class for validating the default and custom initialization of configuration and result models used by the outbox pattern processor. Each test verifies that constructors initialize properties correctly, custom values are applied as expected, and calculated properties return accurate results under various conditions.

## API

### `OutboxProcessingResult_DefaultConstructor_InitializesCollections`
Ensures that a new `OutboxProcessingResult` instance initializes all internal collection properties as empty rather than null. No parameters are required. This test asserts that `ProcessedMessages`, `FailedMessages`, and `RetriedMessages` are all empty collections immediately after construction.

### `OutboxProcessingResult_Duration_ReturnsCorrectTimeSpan`
Validates that the `Duration` property of `OutboxProcessingResult` returns a `TimeSpan` reflecting the elapsed time between the start and end of processing. No parameters are required. The test verifies that the value is non-negative and matches the expected duration based on the test context.

### `OutboxProcessorConfig_DefaultValues_AreCorrect`
Confirms that a newly created `OutboxProcessorConfig` instance assigns default values to all configuration properties. No parameters are required. The test asserts that properties such as `MaxConcurrentProcessors`, `BatchSize`, `PollingInterval`, and `EnableAutoAck` are set to their documented defaults.

### `OutboxProcessorConfig_CustomValues_AreApplied`
Checks that custom values provided to the `OutboxProcessorConfig` constructor or property setters are correctly applied to the instance. No parameters are required. The test constructs the config with non-default values and asserts that each property retains the specified value after initialization.

### `OutboxStatistics_DefaultValues_AreCorrect`
Ensures that a new `OutboxStatistics` instance initializes all counters and metrics to zero. No parameters are required. The test verifies that `TotalMessages`, `SuccessfulMessages`, `FailedMessages`, and `ProcessingTime` are all zero upon construction.

### `OutboxStatistics_SuccessRate_CalculatesCorrectly`
Validates that the `SuccessRate` property of `OutboxStatistics` computes the correct percentage based on the ratio of successful messages to total messages processed. No parameters are required. The test asserts that the returned value is accurate and within the expected range (0 to 100).

### `OutboxStatistics_SuccessRate_WithZeroTotal_ReturnsZero`
Confirms that the `SuccessRate` property returns zero when no messages have been processed (i.e., `TotalMessages` is zero). No parameters are required. The test asserts that division by zero is handled gracefully and the result is zero to avoid undefined behavior.

### `PublishingOptions_DefaultValues_AreCorrect`
Verifies that a new `PublishingOptions` instance initializes all optional publishing parameters with sensible defaults. No parameters are required. The test asserts that properties such as `MaxRetryAttempts`, `RetryDelay`, `EnableImmediateRetry`, and `Timeout` are set to their documented defaults.

### `PublishingOptions_CustomValues_AreApplied`
Ensures that custom values provided to `PublishingOptions` are correctly applied and retained. No parameters are required. The test constructs the options with non-default values and asserts that each property reflects the intended configuration after initialization.

### `HealthMetrics_DefaultValues_AreCorrect`
Checks that a new `HealthMetrics` instance initializes all internal counters and gauges to zero or default states. No parameters are required. The test asserts that `MessagesProcessed`, `ProcessingErrors`, `AverageProcessingTime`, and `LastSuccessTimestamp` are initialized appropriately.

### `HealthMetrics_UpdateProperties_WorksCorrectly`
Validates that the `Update` method of `HealthMetrics` correctly increments counters and updates gauges based on processing events. No parameters are required. The test simulates message processing and error scenarios, then asserts that all metrics are updated as expected.

## Usage
