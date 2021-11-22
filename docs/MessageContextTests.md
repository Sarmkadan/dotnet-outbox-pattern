# MessageContextTests

`MessageContextTests` is the unit test suite for the `MessageContext` class in the `dotnet-outbox-pattern` project. It validates the behavior of correlation and causation identifier generation, OpenTelemetry activity creation and tagging, event recording, exception handling, and the disposable activity scope pattern. The tests ensure that message context operations produce deterministic, traceable identifiers and correctly propagate telemetry metadata across service boundaries.

## API

### `public void GetOrCreateCorrelationId_ReturnsValidGuidString`
Verifies that `GetOrCreateCorrelationId` returns a non-empty string that can be parsed as a valid GUID. Ensures the correlation identifier is always a properly formatted UUID.

### `public void GetOrCreateCorrelationId_ReturnsDifferentIds`
Confirms that successive calls to `GetOrCreateCorrelationId` produce distinct identifier values. Guards against accidental reuse of correlation identifiers across separate message contexts.

### `public void GetOrCreateCausationId_WithActivity_ReturnsCurrentActivityId`
When an active `Activity` (OpenTelemetry span) exists, asserts that `GetOrCreateCausationId` returns the current activity’s trace identifier. Validates proper chaining of causation to the ambient distributed trace.

### `public void GetOrCreateCausationId_WithoutActivity_ReturnsValidGuidString`
When no ambient activity is present, asserts that `GetOrCreateCausationId` falls back to generating a valid GUID string. Ensures causation identifiers are always available even outside an active trace.

### `public void StartActivity_WithMessage_SetsCorrectTags`
Verifies that starting an activity with a message payload populates the activity tags with the expected metadata (such as message type, identifier, or payload summary). Confirms correct telemetry enrichment for outgoing messages.

### `public void StartActivity_WithoutPartitionKey_DoesNotSetPartitionKeyTag`
When a message lacks a partition key, asserts that the started activity does not contain a partition key tag. Prevents leaking empty or default partition metadata into traces.

### `public void StartServiceActivity_SetsCorrectTags`
Validates that the service-level activity initialization applies the correct service-specific tags (e.g., service name, operation). Ensures inbound service operations are properly annotated in traces.

### `public void RecordEvent_AddsEventToActivity`
Confirms that calling `RecordEvent` attaches a new event to the current activity’s event collection. Verifies that domain events are surfaced in the telemetry span.

### `public void RecordEvent_WithNullAttributes_DoesNotThrow`
Ensures that recording an event with a null attributes collection does not cause an exception. Validates defensive handling of optional event metadata.

### `public void RecordException_SetsExceptionTags`
Asserts that recording an exception sets the standard OpenTelemetry exception tags (`exception.type`, `exception.message`, `exception.stacktrace`) on the current activity. Confirms error telemetry is correctly propagated.

### `public void ActivityScope_DisposesActivity`
Verifies that the `ActivityScope` implementation disposes the underlying activity when its `Dispose` method is called. Ensures spans are properly ended and resources released.

### `public void UseScope_ExtensionMethod_CreatesDisposableScope`
Confirms that the `UseScope` extension method returns an `IDisposable` scope wrapping the current activity. Validates the convenience pattern for scoped activity management.

### `public void ActivityExtensions_UseScope_DisposesCorrectly`
End-to-end test that the scope created by `UseScope` disposes the activity exactly once and does not throw on multiple dispose calls. Ensures the disposable pattern is correctly implemented.

### `public static AndConstraint<StringAssertions> NotBeEmptyCorrelationId`
A custom FluentAssertions extension that asserts a string is a non-empty correlation identifier. Used internally by the test suite to compose readable assertions about correlation ID format and content.

## Usage

```csharp
// Testing correlation ID generation within a service handler
[Fact]
public void Handler_Assigns_Unique_Correlation_Ids()
{
    var context1 = new MessageContext();
    var context2 = new MessageContext();

    var id1 = context1.GetOrCreateCorrelationId();
    var id2 = context2.GetOrCreateCorrelationId();

    id1.Should().NotBeEmptyCorrelationId();
    id2.Should().NotBeEmptyCorrelationId();
    id1.Should().NotBe(id2);
}
```

```csharp
// Verifying activity tags when publishing a message with a partition key
[Fact]
public void Outbox_Publish_Sets_Activity_Tags_With_Partition()
{
    using var scope = MessageContext.UseScope();
    var message = new OutboxMessage("order-123", partitionKey: "region-west");

    MessageContext.StartActivity(message);

    var activity = Activity.Current;
    activity.Should().NotBeNull();
    activity.TagObjects.Should().Contain(t => t.Key == "messaging.partition_key"
                                              && (string)t.Value == "region-west");
}
```

## Notes

- Correlation and causation identifiers are always GUID-based strings; tests assume no external dependency for ID generation and expect purely local, non-colliding values.
- Activity-dependent tests require an active `ActivitySource` listener or a default `Activity` instance. In test environments without a configured OpenTelemetry exporter, the activity may be created but not sampled; tag assertions remain valid regardless of sampling state.
- The `ActivityScope` and `UseScope` tests assume single-threaded execution. `Activity.Current` is ambient state tied to the executing thread or `AsyncLocal` context; parallel test execution may interfere if not isolated per test.
- `RecordEvent_WithNullAttributes_DoesNotThrow` implies that `RecordEvent` internally coalesces null to an empty attribute set. Callers should not rely on null attributes being preserved as-is.
- `NotBeEmptyCorrelationId` is a static assertion helper and does not appear on production types; it is available for reuse in other test classes within the same test assembly.
