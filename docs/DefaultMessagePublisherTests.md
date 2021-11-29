# DefaultMessagePublisherTests

Unit tests for the `DefaultMessagePublisher` class, which implements the `IMessagePublisher` interface to publish outbox messages to a message broker. These tests verify correct behavior under valid, null, and edge-case inputs, including cancellation, logging, and multiple-message scenarios.

## API

### `DefaultMessagePublisherTests`
Constructor for the test class. Initializes test dependencies and mocks required for testing message publishing behavior.

### `Constructor_WithNullLogger_ThrowsArgumentNullException`
Verifies that the `DefaultMessagePublisher` throws an `ArgumentNullException` when constructed with a null `ILogger` instance.

- **Parameters**: None
- **Return value**: None
- **Throws**: `ArgumentNullException` if the logger is null

### `PublishAsync_WithValidMessage_CompletesSuccessfully`
Ensures that publishing a valid outbox message completes without exceptions and returns a successful result.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Does not throw on valid input

### `PublishAsync_WithValidMessage_LogsMessageDetails`
Confirms that publishing a valid message logs expected details such as message ID, event type, and payload.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Does not throw on valid input

### `PublishAsync_WithNullMessage_DoesNotThrow`
Validates that the publisher does not throw when passed a null message, treating it as a no-op.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Does not throw

### `PublishAsync_RespectsCancellationToken`
Checks that the publish operation can be canceled via a `CancellationToken` and propagates cancellation appropriately.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: `OperationCanceledException` if canceled

### `PublishAsync_MultipleMessages_PublishesEach`
Ensures that publishing multiple messages results in each being published exactly once, in order.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Does not throw on valid input

### `PublishAsync_LogsEventType`
Verifies that the event type from the outbox message is logged during publishing.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Does not throw on valid input

### `CreateLoggingPublisher_ReturnsValidPublisher`
Helper method that constructs a `DefaultMessagePublisher` with a test logger and returns it for reuse in other tests.

- **Parameters**: None
- **Return value**: `DefaultMessagePublisher` instance ready for testing
- **Throws**: Does not throw

### `LoggingPublisher_PublishesMessage`
Uses the publisher created by `CreateLoggingPublisher_ReturnsValidPublisher` to verify that a message is successfully published and logged.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation
- **Throws**: Does not throw on valid input

## Usage

### Example 1: Basic Publishing with Logging
