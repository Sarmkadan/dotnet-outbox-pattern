# Circuit Breaker

This document describes the `CircuitBreaker` implementation in the `DotnetOutboxPattern.Infrastructure` namespace.

## Overview

The circuit breaker protects downstream message brokers from overload by implementing the Closed/Open/Half-Open pattern. It prevents hammering a downed broker by temporarily stopping requests and allowing time for recovery.

## States

The circuit breaker can be in one of three states:

### Closed
- Normal state where operations are allowed
- Failure count is reset on successful operations
- Transitions to Open when failure count reaches the configured threshold

### Open
- Operations are blocked immediately (returns false/default without executing the action)
- After the configured `OpenDuration` has passed, automatically transitions to HalfOpen
- Transitions to HalfOpen to allow test requests

### HalfOpen
- Allows a limited number of test requests (`HalfOpenTestRequests`)
- On success: increments test success count; if enough successes, transitions to Closed
- On failure: immediately transitions back to Open
- State transitions are checked automatically before each operation attempt

## State Transitions

```
[Closed] --(failure threshold reached)--> [Open]
[Open] --(open duration elapsed)--> [HalfOpen]
[HalfOpen] --(successful test requests)--> [Closed]
[HalfOpen] --(failure)--> [Open]
[Open] --(ForceHalfOpen called)--> [HalfOpen] (for testing)
```

## Public API

### CircuitBreakerOptions

Configuration options for the circuit breaker.

| Property | Description | Default Value |
|----------|-------------|---------------|
| `Enabled` | Whether the circuit breaker is enabled | `true` |
| `FailureThreshold` | Number of consecutive failures before opening the circuit | `5` |
| `OpenDuration` | Duration to keep the circuit open before transitioning to half-open | `1 minute` |
| `HalfOpenTestRequests` | Number of test requests to allow in half-open state | `2` |
| `HalfOpenSuccessDuration` | Duration to wait before transitioning from half-open to closed state after success | `30 seconds` |
| `HalfOpenFailureDuration` | Duration to wait before transitioning from half-open to open state after failure | `10 seconds` |

### CircuitBreaker

Main circuit breaker implementation.

#### Constructor
```csharp
public CircuitBreaker(CircuitBreakerOptions options, ILogger? logger = null)
```
- `options`: Configuration options (cannot be null)
- `logger`: Optional logger for diagnostic events

#### Properties
| Property | Type | Description |
|----------|------|-------------|
| `State` | `CircuitState` | Current state of the circuit (Closed, Open, or HalfOpen) |
| `IsAllowed` | `bool` | Whether the circuit is currently allowing operations (true for Closed and HalfOpen states) |
| `LastException` | `Exception?` | The most recent exception that caused the circuit to open (null if no exception) |

#### Methods
| Method | Description |
|--------|-------------|
| `Task<bool> ExecuteAsync(Func<Task> action)` | Executes an asynchronous action if the circuit allows it. Returns `true` if executed, `false` if circuit is open. |
| `Task<T?> ExecuteAsync<T>(Func<Task<T>> func)` | Executes an asynchronous function if the circuit allows it. Returns the result if successful, `default(T)` if circuit is open. |
| `void RecordSuccess()` | Records a successful operation. Resets failure count in Closed state or increments test success count in HalfOpen state. |
| `void RecordFailure(Exception exception)` | Records a failed operation. Increments failure count and may trigger state transitions. |
| `void Reset()` | Forces the circuit breaker back to Closed state, resetting all counters. |
| `void ForceHalfOpen()` | Forces the circuit into HalfOpen state for testing (only works when currently Open). |
| `void Dispose()` | Disposes the circuit breaker. |

#### Events (logged via ILogger)
- `CircuitBlockedLogMessage`: When an operation is blocked due to open circuit
- `FailureRecordedLogMessage`: When a failure is recorded
- `CircuitOpenedLogMessage`: When circuit opens after reaching failure threshold
- `CircuitClosedLogMessage`: When circuit closes after successful recovery
- `HalfOpenTransitionLogMessage`: When circuit transitions to half-open for testing
- `HalfOpenSuccessLogMessage`: When a test request succeeds in half-open state
- `HalfOpenFailureLogMessage`: When a test request fails in half-open state (circuit reopens)
- `ForcedHalfOpenLogMessage`: When circuit is forced into half-open state for testing

## Usage Example

```csharp
// Configure circuit breaker options
var options = new CircuitBreakerOptions
{
    Enabled = true,
    FailureThreshold = 5,
    OpenDuration = TimeSpan.FromMinutes(1),
    HalfOpenTestRequests = 2,
    HalfOpenSuccessDuration = TimeSpan.FromSeconds(30),
    HalfOpenFailureDuration = TimeSpan.FromSeconds(10)
};

// Create circuit breaker with optional logger
var circuitBreaker = new CircuitBreaker(options, logger);

// Use with an asynchronous operation
bool success = await circuitBreaker.ExecuteAsync(async () =>
{
    // Your operation that might fail (e.g., sending a message to a broker)
    await messageBroker.SendAsync(message);
    return true;
});

// Or with a function that returns a value
var result = await circuitBreaker.ExecuteAsync(async () =>
{
    return await messageBroker.GetMessageAsync(id);
});

// Manual control (if needed)
circuitBreaker.RecordSuccess();
circuitBreaker.RecordFailure(exception);
circuitBreaker.Reset();
circuitBreaker.ForceHalfOpen();
```

## Implementation Details

- Thread-safe: All state modifications are protected by a lock
- Automatic state transitions: State is checked before each operation attempt
- Logging: Comprehensive diagnostic logging via `ILogger` at appropriate levels
- Resource cleanup: Implements `IDisposable` for proper resource management
- Null safety: All parameters are validated for null values