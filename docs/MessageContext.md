# MessageContext

The `MessageContext` class provides a centralized mechanism for managing diagnostic and tracing information within message-based workflows. It facilitates correlation, causation, and activity tracking across distributed operations, enabling observability and debugging of message flows. The class integrates with `System.Diagnostics.Activity` to support OpenTelemetry-style tracing and offers scoped lifetime management for contextual data.

## API

### `public static string GetOrCreateCorrelationId`
Gets or initializes the correlation ID for the current logical operation. The correlation ID links related messages across services to represent a single logical transaction.

- **Returns**: The existing correlation ID if present; otherwise, a newly generated GUID-formatted string.
- **Remarks**: The correlation ID is stored in `Activity.Current.Baggage`. If no activity exists, a new one is started implicitly.

### `public static string GetOrCreateCausationId`
Gets or initializes the causation ID for the current message. The causation ID identifies the immediate predecessor message that triggered the current operation.

- **Returns**: The existing causation ID if present; otherwise, a newly generated GUID-formatted string.
- **Remarks**: The causation ID is stored in `Activity.Current.Baggage`. If no activity exists, a new one is started implicitly.

### `public static Activity? StartActivity`
Starts a new `Activity` with a name derived from the calling method's context. The activity inherits baggage and tags from the current activity, if any.

- **Returns**: The newly started `Activity`, or `null` if activity tracking is disabled.
- **Remarks**: The activity name is automatically set to the calling method's name. This method is typically used to trace individual steps within a service.

### `public static Activity? StartServiceActivity`
Starts a new `Activity` with a name formatted as `{ServiceName}.{OperationName}`, where `ServiceName` is inferred from the executing assembly or environment.

- **Returns**: The newly started `Activity`, or `null` if activity tracking is disabled.
- **Remarks**: This method is designed for high-level service operations. The operation name is derived from the calling method's name.

### `public static void RecordEvent`
Records a custom event on the current `Activity` with the specified name and optional key-value tags.

- **Parameters**:
  - `name` (`string`): The name of the event.
  - `tags` (`params KeyValuePair<string, object?>[]`): Optional key-value pairs to attach to the event.
- **Remarks**: If no current `Activity` exists, this method has no effect. Events are typically used to mark significant occurrences within an operation.

### `public static void RecordException`
Records an exception on the current `Activity` with optional tags. The exception details are added as tags to facilitate error tracking.

- **Parameters**:
  - `exception` (`Exception`): The exception to record.
  - `tags` (`params KeyValuePair<string, object?>[]`): Optional key-value pairs to attach to the exception record.
- **Remarks**: If no current `Activity` exists, this method has no effect. The exception's `ToString()` representation is stored under the `exception.message` tag.

### `public ActivityScope`
Creates a new `ActivityScope` instance, which starts an `Activity` and ensures it is disposed (and stopped) when the scope exits.

- **Returns**: An `ActivityScope` instance that implements `IDisposable`.
- **Remarks**: The scope's activity inherits baggage and tags from the current activity, if any. The activity name is set to the calling method's name.

### `public void Dispose`
Disposes the `ActivityScope`, stopping the associated `Activity` and recording its duration.

- **Remarks**: This method is called automatically when the scope exits via `using` statement. Manual disposal is rarely needed.

### `public static ActivityScope UseScope`
Creates a new `ActivityScope` with a custom activity name, typically representing a named operation or service boundary.

- **Parameters**:
  - `activityName` (`string`): The name to assign to the new `Activity`.
- **Returns**: An `ActivityScope` instance that implements `IDisposable`.
- **Remarks**: The scope's activity inherits baggage and tags from the current activity, if any. This method is useful for explicitly naming activities.

## Usage

### Example 1: Tracing a Message Handler
