# IntegrationTestFixture

A test fixture that provides a pre-configured `HttpClient` and service dependencies for integration testing the outbox pattern implementation. It initializes a test host with in-memory infrastructure, manages test state cleanup, and exposes helper methods for exercising the outbox endpoints and workflows.

## API

### `public HttpClient Client`

Provides an `HttpClient` pre-configured to target the test host. The client includes base address and default headers suitable for integration tests. Ownership of the client is retained by the fixture; dispose the fixture to release associated resources.

### `public IntegrationTestFixture()`

Constructs a new test fixture. Initializes in-memory infrastructure, configures services, and prepares the test host. Does not start the host; callers must invoke `InitializeAsync` to begin test execution.

### `public async Task InitializeAsync()`

Starts the test host and ensures all dependencies are ready for testing. Must be called before any test interactions. Throws if host startup fails or required services are unavailable.

### `public async Task DisposeAsync()`

Stops the test host and releases all resources allocated during initialization. Idempotent; safe to call multiple times. Ensures no background processing remains after tests complete.

### `public IServiceScope CreateScope()`

Creates a new `IServiceScope` tied to the test host’s service provider. Use this scope to resolve scoped services (e.g., `DbContext`, `OutboxProcessor`) within a test. The caller is responsible for disposing the scope.

### `public OutboxEndToEndIntegrationTests`

A nested test class containing end-to-end integration tests for the outbox pattern. Not intended for direct instantiation; use the fixture to run these tests.

### `public async Task Health_Endpoint_ReturnsOk()`

Verifies that the health check endpoint returns HTTP 200 OK. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the health endpoint is unreachable or returns a non-success status.

### `public async Task PublishEvent_WithValidEvent_CreatesOutboxMessage()`

Publishes a valid event to the outbox and asserts that a corresponding outbox message is created in the database. No parameters. Returns a `Task` representing the asynchronous operation. Throws if publishing fails or the message is not persisted.

### `public async Task GetMessage_WithValidId_ReturnsMessage()`

Retrieves an outbox message by its identifier and asserts that the response contains the expected message data. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the message does not exist or the request fails.

### `public async Task GetStatistics_ReturnsOutboxStatistics()`

Fetches outbox statistics (e.g., counts of pending, processed, failed messages) and asserts the response is valid. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the statistics endpoint is unavailable or returns invalid data.

### `public async Task PublishEvent_WithDuplicateIdempotencyKey_ReturnsExistingMessage()`

Publishes an event with a previously used idempotency key and asserts that the system returns the existing message instead of creating a duplicate. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the key is not recognized or the response is incorrect.

### `public async Task PublishEvent_CreatesMessageInPendingState()`

Publishes an event and asserts that the resulting outbox message is created in the `Pending` state. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the message is not created or its state is incorrect.

### `public async Task RetryFailedMessage_ResetsMessageState()`

Selects a failed outbox message, invokes the retry mechanism, and asserts that the message state is reset to `Pending`. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the message cannot be retried or the state is not updated.

### `public async Task GetUnreviewedDeadLetters_ReturnsDeadLetters()`

Queries the dead-letter queue for unreviewed messages and asserts that the response contains the expected entries. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the query fails or no messages are returned.

### `public async Task DeadLetterWorkflow_MovesFailedMessageToDlq()`

Forces a message into a failed state, triggers the dead-letter workflow, and asserts that the message is moved to the dead-letter queue. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the workflow does not execute or the message is not relocated.

### `public async Task ReviewDeadLetter_MarksAsReviewed()`

Retrieves a dead-letter message and asserts that marking it as reviewed updates its status accordingly. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the message cannot be found or the update fails.

### `public async Task MessageStatistics_TracksPublishedMessages()`

Publishes a set of messages and asserts that the statistics endpoint reflects the correct counts for published, pending, and processed messages. No parameters. Returns a `Task` representing the asynchronous operation. Throws if statistics are inconsistent or the endpoint is unavailable.

### `public async Task ConcurrentPublish_HandlesMultipleEventsSimultaneously()`

Publishes multiple events concurrently and asserts that all messages are created without duplication or race conditions. No parameters. Returns a `Task` representing the asynchronous operation. Throws if any publish operation fails or messages are missing.

### `public async Task GetMessage_WithInvalidId_ReturnsNotFound()`

Attempts to retrieve an outbox message using a non-existent identifier and asserts that the response is HTTP 404 Not Found. No parameters. Returns a `Task` representing the asynchronous operation. Throws if the endpoint returns an unexpected status.

### `public ConcurrencyIntegrationTests`

A nested test class containing concurrency-focused integration tests. Not intended for direct instantiation; use the fixture to run these tests.

## Usage

### Example 1: Basic end-to-end test
