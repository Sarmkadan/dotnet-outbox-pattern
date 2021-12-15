# HealthCheckService

`HealthCheckService` is a monitoring component that tracks the health of message processing in an outbox pattern implementation. It periodically evaluates system state and maintains a list of active health alerts when anomalies such as high failure rates, stuck messages, or dead-lettered items are detected.

## API

### `HealthCheckService`

The primary service class responsible for health monitoring and alert management.

### `IReadOnlyList<HealthAlert> GetActiveAlerts()`

Returns a read-only list of currently active health alerts. Each alert represents a detected anomaly in message processing.

- **Returns**: `IReadOnlyList<HealthAlert>` – A snapshot of active alerts. The list is immutable to prevent external modification of internal state.
- **Throws**: No exceptions are documented for this method.

### `CheckIntervalMs`

Gets or sets the interval, in milliseconds, at which the health check runs.

- **Type**: `int`
- **Default**: Implementation-defined.
- **Remarks**: Changing this value while monitoring is active may cause inconsistent alert timing.

### `HighFailureRateThreshold`

Gets or sets the threshold, as a fraction between 0.0 and 1.0, above which a high failure rate is considered an alert condition.

- **Type**: `double`
- **Default**: Implementation-defined.
- **Remarks**: Must be in the range [0.0, 1.0]. Values outside this range may cause undefined behavior.

### `StuckMessageThreshold`

Gets or sets the maximum allowed duration, in milliseconds, that a message can remain in a processing state without progress before being flagged as stuck.

- **Type**: `int`
- **Default**: Implementation-defined.
- **Remarks**: Must be non-negative. Zero may disable the check.

### `DeadLetterThreshold`

Gets or sets the maximum number of messages allowed in a dead-letter state before triggering an alert.

- **Type**: `int`
- **Default**: Implementation-defined.
- **Remarks**: Must be non-negative. Zero may disable the check.

### `Type`

Gets the type of health alert currently active, if any.

- **Type**: `string`
- **Returns**: The alert type (e.g., `"HighFailureRate"`, `"StuckMessage"`, `"DeadLetter"`) or `null` if no alert is active.
- **Remarks**: This is a derived or cached value based on the most recent alert.

### `Message`

Gets the descriptive message associated with the current health alert, if any.

- **Type**: `string`
- **Returns**: A human-readable message explaining the alert condition or `null` if no alert is active.

### `RaisedAt`

Gets the timestamp when the current health alert was raised.

- **Type**: `DateTime`
- **Returns**: The UTC timestamp of the most recent alert or `default(DateTime)` if no alert is active.

## Usage

### Example 1: Basic Monitoring Setup
