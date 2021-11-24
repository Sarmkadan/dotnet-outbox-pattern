# SystemTextJsonOutboxSerializerTests

Unit test class for `SystemTextJsonOutboxSerializer` that verifies correct serialization and deserialization behavior using System.Text.Json with default and custom options.

## API

### `SystemTextJsonOutboxSerializerTests`
Constructor for the test class. Initializes a new instance of the test fixture without shared state between tests.

### `Constructor_WithDefaultOptions_CreatesSerializer`
Verifies that the serializer can be constructed using default `JsonSerializerOptions`.

- **Parameters**: None
- **Return value**: None
- **Throws**: Not applicable

### `Constructor_WithCustomOptions_CreatesSerializer`
Verifies that the serializer can be constructed using custom `JsonSerializerOptions`.

- **Parameters**: None
- **Return value**: None
- **Throws**: Not applicable

### `Constructor_WithNullOptions_ThrowsArgumentNullException`
Verifies that constructing the serializer with `null` options throws an `ArgumentNullException`.

- **Parameters**: None
- **Return value**: None
- **Throws**: `ArgumentNullException`

### `Serialize_WithNullValue_ReturnsNullString`
Verifies that serializing a `null` value returns `null`.

- **Parameters**: None
- **Return value**: `null`
- **Throws**: Not applicable

### `Serialize_WithSimpleObject_ReturnsJsonString`
Verifies that serializing a simple object returns a valid JSON string.

- **Parameters**: None
- **Return value**: Non-null JSON string
- **Throws**: Not applicable

### `Serialize_WithComplexObject_ReturnsValidJson`
Verifies that serializing a complex object returns valid JSON.

- **Parameters**: None
- **Return value**: Valid JSON string
- **Throws**: Not applicable

### `Serialize_WithPrimitiveValue_ReturnsJsonString`
Verifies that serializing a primitive value (e.g., `int`, `bool`) returns a valid JSON string.

- **Parameters**: None
- **Return value**: Non-null JSON string
- **Throws**: Not applicable

### `Serialize_WithStringValue_ReturnsJsonString`
Verifies that serializing a string value returns a valid JSON string.

- **Parameters**: None
- **Return value**: Non-null JSON string
- **Throws**: Not applicable

### `Deserialize_WithNullJson_ReturnsDefault`
Verifies that deserializing `null` JSON returns the default value for the target type.

- **Parameters**: None
- **Return value**: Default value of the target type
- **Throws**: Not applicable

### `Deserialize_WithEmptyString_ReturnsDefault`
Verifies that deserializing an empty string returns the default value for the target type.

- **Parameters**: None
- **Return value**: Default value of the target type
- **Throws**: Not applicable

### `Deserialize_WithWhitespaceString_ReturnsDefault`
Verifies that deserializing a whitespace-only string returns the default value for the target type.

- **Parameters**: None
- **Return value**: Default value of the target type
- **Throws**: Not applicable

### `Deserialize_WithInvalidJson_ReturnsDefault`
Verifies that deserializing invalid JSON returns the default value for the target type.

- **Parameters**: None
- **Return value**: Default value of the target type
- **Throws**: Not applicable

### `Deserialize_WithSimpleObject_ReturnsDeserializedObject`
Verifies that deserializing valid JSON for a simple object returns the expected object.

- **Parameters**: None
- **Return value**: Deserialized object
- **Throws**: Not applicable

### `Deserialize_WithNullString_ReturnsDefault`
Verifies that deserializing a `null` string returns the default value for the target type.

- **Parameters**: None
- **Return value**: Default value of the target type
- **Throws**: Not applicable

### `Deserialize_WithPrimitiveType_ReturnsDeserializedValue`
Verifies that deserializing valid JSON for a primitive type returns the expected value.

- **Parameters**: None
- **Return value**: Deserialized primitive value
- **Throws**: Not applicable

### `Deserialize_WithStringType_ReturnsDeserializedString`
Verifies that deserializing valid JSON for a string type returns the expected string.

- **Parameters**: None
- **Return value**: Deserialized string
- **Throws**: Not applicable

### `Deserialize_WithTypeParameter_ReturnsDeserializedObject`
Verifies that deserializing valid JSON with a specified type parameter returns the expected object.

- **Parameters**: None
- **Return value**: Deserialized object
- **Throws**: Not applicable

### `Deserialize_WithNullJsonAndType_ReturnsNull`
Verifies that deserializing `null` JSON with a type parameter returns `null`.

- **Parameters**: None
- **Return value**: `null`
- **Throws**: Not applicable

### `Deserialize_WithEmptyJsonAndType_ReturnsNull`
Verifies that deserializing an empty string with a type parameter returns `null`.

- **Parameters**: None
- **Return value**: `null`
- **Throws**: Not applicable

## Usage

### Example 1: Basic Serialization and Deserialization
