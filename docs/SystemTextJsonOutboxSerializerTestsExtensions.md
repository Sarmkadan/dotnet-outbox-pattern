# SystemTextJsonOutboxSerializerTestsExtensions

This static class provides extension methods for configuring `SystemTextJsonOutboxSerializer` and validating serialization outcomes within test suites for the `dotnet-outbox-pattern` library. These helpers streamline the setup of custom JSON serialization options and provide fluent assertions for verifying that objects correctly round-trip through the serializer and adhere to expected JSON formatting.

## API

### Configuration Methods

*   **`WithCustomOptions(JsonSerializerOptions options)`**
    Configures the `SystemTextJsonOutboxSerializer` instance using the provided `JsonSerializerOptions`.
    *   **Parameters:** `options` - The `JsonSerializerOptions` instance to apply.
    *   **Returns:** The configured `SystemTextJsonOutboxSerializer` instance.

*   **`WithCamelCaseOptions()`**
    Configures the `SystemTextJsonOutboxSerializer` to use camelCase property naming conventions.
    *   **Returns:** The configured `SystemTextJsonOutboxSerializer` instance.

*   **`WithIndentedOptions()`**
    Configures the `SystemTextJsonOutboxSerializer` to produce indented (pretty-printed) JSON output.
    *   **Returns:** The configured `SystemTextJsonOutboxSerializer` instance.

### Assertion Methods

*   **`ShouldContainPropertyName<T>(SystemTextJsonOutboxSerializer serializer, T obj, string propertyName)`**
    Serializes the provided object and asserts that the resulting JSON contains the specified property name.
    *   **Throws:** An assertion exception (e.g., `Xunit.Sdk.XunitException`) if the property name is not found in the serialized JSON.

*   **`ShouldRoundTrip<T>(SystemTextJsonOutboxSerializer serializer, T obj)`**
    Serializes the provided object and immediately deserializes it back to type `T`. Asserts that the deserialized object is equal to the original object.
    *   **Throws:** An assertion exception if the deserialized object is not equal to the original object, or if serialization/deserialization fails.

*   **`ShouldSerializeTo<T>(SystemTextJsonOutboxSerializer serializer, T obj, string expectedJson)`**
    Serializes the provided object and asserts that the resulting JSON string matches the provided `expectedJson`.
    *   **Throws:** An assertion exception if the serialized JSON does not match `expectedJson`.

## Usage

### Configuring Serializer Options
```csharp
var serializer = new SystemTextJsonOutboxSerializer()
    .WithCamelCaseOptions()
    .WithIndentedOptions();

// Use the configured serializer for testing
```

### Validating Serialization Behavior
```csharp
var serializer = new SystemTextJsonOutboxSerializer();
var myEvent = new DomainEvent { Id = Guid.NewGuid(), Data = "Sample" };

// Verify round-trip integrity
serializer.ShouldRoundTrip(myEvent);

// Verify property inclusion
serializer.ShouldContainPropertyName(myEvent, "Id");
```

## Notes

*   **Thread Safety:** While `SystemTextJsonOutboxSerializer` is generally intended to be used as a transient dependency within test methods, these extension methods modify the options of the serializer instance. If an instance is shared across concurrent tests, results may be unpredictable. It is recommended to create a new serializer instance per test method.
*   **Serialization Rules:** These extensions rely on the underlying `System.Text.Json` behavior. Complex types with circular references or types not supported by the default `JsonSerializer` configuration may cause serialization exceptions.
*   **Assertion Failures:** The assertion methods throw standard unit testing framework exceptions. Ensure that the appropriate testing library (e.g., xUnit, NUnit) is referenced to interpret these exceptions correctly.
