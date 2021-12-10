# NotificationServiceTests

Unit tests for the `NotificationService` class, verifying behavior around sending notifications to registered channels, error handling, and retrieval of recent notifications. Focuses on validating argument validation, channel routing, and resilience under failure conditions.

## API

### `NotificationServiceTests()`

Constructor for the test class. Initializes the test fixture, including in-memory channel registrations and notification store used by the service under test.

### `void Constructor_WithNullLogger_ThrowsArgumentNullException()`

Verifies that the `NotificationService` throws an `ArgumentNullException` when a null logger is provided during construction. Ensures the service enforces non-null logger dependencies.

### `async Task SendAsync_WithNullNotification_ThrowsArgumentNullException()`

Ensures that calling `SendAsync` with a null notification results in an `ArgumentNullException`. Validates input validation at the service boundary.

### `async Task SendAsync_WithValidNotification_SendsToAllChannels()`

Confirms that a valid notification is dispatched to all registered channels. Validates that the service correctly routes notifications to every subscribed channel.

### `async Task SendToChannelAsync_WithUnknownChannel_LogsWarningAndReturns()`

Tests behavior when attempting to send a notification to a channel that has not been registered. Expects a warning-level log entry and silent return without throwing.

### `async Task SendToChannelAsync_WithKnownChannel_DelegatesToHandler()`

Validates that sending a notification to a registered channel results in the appropriate handler being invoked. Ensures channel routing and delegation work as expected.

### `async Task SendToChannelAsync_WhenHandlerThrows_LogsErrorAndContinues()`

Ensures that if a channel handler throws an exception during processing, the error is logged at error level and processing continues for other channels. Validates resilience and fault isolation.

### `void GetRecentNotifications_ReturnsCorrectNumber()`

Confirms that retrieving recent notifications returns the exact number requested. Validates the retrieval logic and count accuracy.

### `void GetRecentNotifications_WithDefaultCount_ReturnsLast100()`

Ensures that when no count is specified, the method returns the last 100 notifications. Validates default behavior and pagination logic.

## Usage
