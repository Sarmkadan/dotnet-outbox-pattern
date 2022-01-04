# SerializationHelperTestsExtensions

SerializationHelperTestsExtensions provides a suite of static extension methods and supporting members designed to standardize serialization and deserialization testing within the dotnet-outbox-pattern library. These utilities assist in validating complex object mapping, handling edge cases such as null or default values, and ensuring the structural integrity of serialized JSON output.

## API

### Methods

*   `Serialize_Deserialize_RoundTrip_ShouldPreserveAllProperties()`
    Performs a full round-trip serialization and deserialization process to verify that all object properties are preserved exactly without loss of data.

*   `Serialize_WithComplexNestedObject_ShouldHandleAllProperties()`
    Validates serialization logic against complex, nested object structures to ensure recursive properties are correctly handled.

*   `Serialize_WithNullAndDefaultValues_ShouldOmitCorrectly()`
    Ensures that null references and fields set to their C# default values are omitted from the serialized JSON output as configured.

*   `IsValidJson_WithVariousInputs_ShouldReturnCorrectValidation()`
    Validates whether provided inputs produce valid JSON structures, returning appropriate validation results based on the input format.

*   `SerializePretty_WithDifferentTypes_ShouldProduceIndentedOutput()`
    Verifies that serialized output is correctly formatted with appropriate indentation for various data types.

*   `Deserialize_WithInvalidJson_ShouldIncludeTypeNameInError()`
    Tests error handling during deserialization, ensuring that exceptions include the specific type name when encountering malformed JSON.

*   `Serialize_WithEnumValues_ShouldPreserveEnumNames()`
    Validates that enum members are serialized to their string representations (names) rather than their underlying numeric values.

*   `Serialize_WithGuidValues_ShouldPreserveGuidFormat()`
    Ensures that `Guid` values are serialized and deserialized while maintaining the correct standard GUID formatting.

*   `Serialize_WithDateTimeValues_ShouldPreserveDateTimeFormat()`
    Validates that `DateTime` values maintain precision and standard format throughout the serialization lifecycle.

### Members

*   `TestStatus Status`
    An enumeration-based member representing the state of the serialization test context.

*   `PriorityLevel Priority`
    An enumeration-based member defining the priority level associated with the test scenario, useful for filtering or categorizing test execution.

## Usage

```csharp
// Example 1: Executing a standard round-trip validation within a test class
public void TestDomainEventSerialization()
{
    // Executes the defined round-trip serialization test logic
    SerializationHelperTestsExtensions.Serialize_Deserialize_RoundTrip_ShouldPreserveAllProperties();
}

// Example 2: Validating enum serialization behavior
public void TestEnumHandling()
{
    // Verifies that the enum serialization logic complies with project standards
    SerializationHelperTestsExtensions.Serialize_WithEnumValues_ShouldPreserveEnumNames();
}
```

## Notes

*   **Thread Safety:** These methods rely on standard, stateless serialization logic. However, ensure that any shared state or global serialization configurations are not being mutated by concurrent tests during the assertion phase.
*   **Environment Assumptions:** These methods assume a standard configured serialization environment consistent with the rest of the dotnet-outbox-pattern library. If custom converters or unconventional serialization options are introduced, test-specific setup may be required.
*   **Performance:** Extensive use of these helpers in large test suites may increase overall test execution time due to the reflection-intensive nature of thorough serialization validation.
