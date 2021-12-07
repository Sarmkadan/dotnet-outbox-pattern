# SerializationHelperTests

`SerializationHelperTests` is a test class that validates the behavior of the `SerializationHelper` utility and the `EventPublisher` component within the `dotnet-outbox-pattern` project. It ensures that JSON serialization produces camelCase property names, omits null values, supports pretty-printing, correctly validates JSON structure, and reliably deserializes payloads. Additionally, it covers the lifecycle and error-handling semantics of `EventPublisher`, including subscription management, exception isolation, and argument validation.

## API

### SerializationHelper Members

- **`public void Serialize_ProducesCamelCasePropertyNames`**  
  Verifies that serializing an object results in JSON where all property names follow camelCase formatting.  
  *No parameters or return value; asserts the naming convention.*

- **`public void Deserialize_WithValidJson_ReturnsMappedObject`**  
  Confirms that a well-formed JSON string is correctly deserialized into an instance of the expected type with all properties populated.  
  *No parameters or return value; asserts successful mapping.*

- **`public void Serialize_ThenDeserialize_PreservesHealthMetricValues`**  
  Demonstrates a round-trip serialization/deserialization cycle for a health metric object, ensuring that all original values are preserved without loss or corruption.  
  *No parameters or return value; asserts value equality before and after the cycle.*

- **`public void Deserialize_WithInvalidJson_ThrowsSerializationException`**  
  Ensures that attempting to deserialize a malformed or structurally invalid JSON string throws a `SerializationException`.  
  *No parameters or return value; asserts the exception type and condition.*

- **`public void IsValidJson_WithWellFormedObject_ReturnsTrue`**  
  Validates that the `IsValidJson` method returns `true` when given a properly structured JSON object string.  
  *No parameters or return value; asserts the boolean result.*

- **`public void IsValidJson_WithJsonArray_ReturnsTrue`**  
  Validates that `IsValidJson` correctly identifies a valid JSON array as legitimate JSON, returning `true`.  
  *No parameters or return value; asserts the boolean result.*

- **`public void IsValidJson_WithMalformedJson_ReturnsFalse`**  
  Confirms that `IsValidJson` returns `false` when the input string is not syntactically valid JSON.  
  *No parameters or return value; asserts the boolean result.*

- **`public void SerializePretty_ProducesIndentedMultilineOutput`**  
  Checks that the pretty-print serialization mode produces a string containing indentation and newline characters, formatting the JSON across multiple lines.  
  *No parameters or return value; asserts the presence of whitespace formatting.*

- **`public void Serialize_OmitsNullReferenceAndNullableValueProperties`**  
  Ensures that properties with `null` values (both reference types and nullable value types) are excluded from the serialized JSON output.  
  *No parameters or return value; asserts absence of null-valued keys.*

### EventPublisherTests Members

- **`public EventPublisherTests`**  
  Default constructor for the test class. Inherits any test-fixture setup defined in the project’s test infrastructure.

- **`public void Constructor_WithNullLogger_ThrowsArgumentNullException`**  
  Asserts that constructing an `EventPublisher` with a `null` logger argument throws an `ArgumentNullException`.  
  *No parameters or return value; validates constructor guard.*

- **`public async Task Subscribe_WithNullHandler_ThrowsArgumentNullException`**  
  Verifies that calling `Subscribe` with a `null` handler delegate throws an `ArgumentNullException`.  
  *Returns a `Task` representing the asynchronous test operation; asserts the exception.*

- **`public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException`**  
  Confirms that invoking `PublishAsync` with a `null` event argument throws an `ArgumentNullException`.  
  *Returns a `Task`; asserts the exception.*

- **`public async Task PublishAsync_WithNoSubscribers_CompletesWithoutThrowing`**  
  Ensures that publishing an event when no handlers have been subscribed completes successfully without throwing any exception.  
  *Returns a `Task`; asserts successful completion.*

- **`public async Task PublishAsync_WithSubscriber_InvokesHandlerExactlyOnce`**  
  Validates that a subscribed handler is invoked exactly one time when a matching event is published.  
  *Returns a `Task`; asserts invocation count.*

- **`public async Task Dispose_Subscription_StopsDeliveryToRemovedHandler`**  
  Demonstrates that disposing a subscription token prevents the associated handler from receiving subsequent published events.  
  *Returns a `Task`; asserts zero invocations after disposal.*

- **`public async Task PublishAsync_WhenHandlerThrows_DoesNotPropagateExceptionToCaller`**  
  Ensures that if a subscribed handler throws an exception during event processing, the exception is contained and does not propagate to the caller of `PublishAsync`.  
  *Returns a `Task`; asserts that `PublishAsync` completes without throwing.*

## Usage

### Testing Serialization Round-Trip Integrity

```csharp
[Fact]
public void Serialize_ThenDeserialize_PreservesHealthMetricValues()
{
    // Arrange
    var original = new HealthMetric
    {
        Name = "CPU Usage",
        Value = 85.2,
        Timestamp = DateTime.UtcNow
    };

    // Act
    string json = SerializationHelper.Serialize(original);
    HealthMetric restored = SerializationHelper.Deserialize<HealthMetric>(json);

    // Assert
    Assert.Equal(original.Name, restored.Name);
    Assert.Equal(original.Value, restored.Value);
    Assert.Equal(original.Timestamp, restored.Timestamp);
}
```

### Testing EventPublisher Exception Isolation

```csharp
[Fact]
public async Task PublishAsync_WhenHandlerThrows_DoesNotPropagateExceptionToCaller()
{
    // Arrange
    var logger = new NullLogger<EventPublisher>();
    var publisher = new EventPublisher(logger);
    var eventPayload = new OrderPlacedEvent { OrderId = Guid.NewGuid() };

    publisher.Subscribe<OrderPlacedEvent>(e =>
    {
        throw new InvalidOperationException("Simulated handler failure");
    });

    // Act & Assert – no exception should reach the caller
    await publisher.PublishAsync(eventPayload);

    // Test passes if PublishAsync completes without throwing
}
```

## Notes

- **Edge Cases:** The `Deserialize_WithInvalidJson_ThrowsSerializationException` test expects a specific exception type (`SerializationException`) rather than a general `System.Text.Json` exception, indicating a custom abstraction layer. Malformed inputs include truncated strings, mismatched brackets, and invalid literal values.
- **Null Handling:** `Serialize_OmitsNullReferenceAndNullableValueProperties` confirms that the serializer is configured with `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, which prevents null properties from appearing in the output. This behavior is critical for minimizing payload size in outbox message storage.
- **JSON Validation:** `IsValidJson` treats both objects and arrays as valid JSON. The method likely performs syntactic validation only, without schema enforcement.
- **Thread Safety:** The `EventPublisher` tests imply that subscriptions and publications are designed for concurrent scenarios (e.g., `Dispose_Subscription_StopsDeliveryToRemovedHandler` suggests dynamic subscription management). However, the test class itself runs each test method sequentially within the xUnit framework; no explicit thread-safety stress tests are present in these signatures.
- **Exception Isolation:** `PublishAsync_WhenHandlerThrows_DoesNotPropagateExceptionToCaller` documents a fire-and-forget or catch-and-log pattern within `EventPublisher`, ensuring one faulty handler cannot disrupt the publisher or other subscribers. The thrown exception is presumably logged via the injected logger.
