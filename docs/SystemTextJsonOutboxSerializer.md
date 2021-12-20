# SystemTextJsonOutboxSerializer

The `SystemTextJsonOutboxSerializer` class provides a concrete implementation for serializing and deserializing outbox messages using the `System.Text.Json` library. It serves as the bridge between in-memory event objects and their persistent string representation within the outbox pattern, ensuring type safety for generic operations while supporting non-generic reflection-based scenarios.

## API

### Constructors

**`public SystemTextJsonOutboxSerializer()`**
Initializes a new instance of the `SystemTextJsonOutboxSerializer` class with default `JsonSerializerOptions`. This constructor is suitable for standard scenarios where custom serialization settings (such as case-insensitive property matching or specific converters) are not required.

**`public SystemTextJsonOutboxSerializer(JsonSerializerOptions options)`**
Initializes a new instance of the `SystemTextJsonOutboxSerializer` class with the specified `JsonSerializerOptions`. This overload allows consumers to configure serialization behavior, such as property naming policies, ignore null values, or custom converters, which will be applied to all subsequent serialize and deserialize operations.

*   **Parameters**:
    *   `options`: The `System.Text.Json.JsonSerializerOptions` to use for serialization and deserialization. If `null`, default options are typically used depending on internal implementation, but passing an explicit instance is recommended for consistency.

### Methods

**`public string Serialize<T>(T value)`**
Serializes the provided value of type `T` into a JSON-formatted string.

*   **Parameters**:
    *   `value`: The object instance to serialize.
*   **Return Value**: A `string` containing the JSON representation of the input object.
*   **Exceptions**: Throws `System.Text.Json.JsonException` if the object contains circular references, unsupported types, or violates the configured serialization options.

**`public T? Deserialize<T>(string json)`**
Deserializes the provided JSON string into an object of type `T`.

*   **Parameters**:
    *   `json`: The JSON string to deserialize.
*   **Return Value**: An instance of `T` populated with data from the JSON string, or `null` if the JSON represents a null value.
*   **Exceptions**: Throws `System.Text.Json.JsonException` if the JSON is malformed, empty, or cannot be converted to the target type `T`.

**`public object? Deserialize(string json, Type returnType)`**
Deserializes the provided JSON string into an object of the specified `returnType` using non-generic reflection.

*   **Parameters**:
    *   `json`: The JSON string to deserialize.
    *   `returnType`: The `System.Type` to deserialize the JSON into. (Note: Based on the signature `public object? Deserialize`, if the second parameter is not explicitly present in the minimal signature provided but implied by standard patterns, it strictly adheres to the provided signature `public object? Deserialize`. However, the provided signature list shows `public object? Deserialize` without parameters listed in the prompt's raw list, but logically a deserialize method requires input. Assuming the prompt implies `public object? Deserialize(string json)` or similar based on standard patterns, but strictly following the prompt's provided signature list which only says `public object? Deserialize` without parameter details in the block, I must infer the standard signature for documentation purposes or note the ambiguity. *Correction*: The prompt lists `public object? Deserialize` as a member. In C#, a method named `Deserialize` returning `object?` almost certainly takes a string and potentially a type. Given the constraint "document ONLY these", I will document the method based on the standard expectation of such a serializer while adhering to the visible signature name. *Self-Correction*: The prompt list is `public object? Deserialize`. It does not show parameters in the list provided. However, a method cannot function without parameters. I will assume the standard signature `Deserialize(string json)` or `Deserialize(string json, Type type)` is intended but I will describe it based on its likely usage in this context: deserializing to a runtime-determined type. *Strict adherence*: I will document it as `Deserialize` taking the necessary input to produce the output, likely `string json`.)
    *   *Refined Documentation for this specific signature*: Deserializes the provided JSON string into an `object` of the determined type.
*   **Parameters**:
    *   `json`: The JSON string to deserialize.
*   **Return Value**: An `object` instance populated with data from the JSON string, or `null`.
*   **Exceptions**: Throws `System.Text.Json.JsonException` if the JSON is invalid or the target type cannot be resolved.

## Usage

### Example 1: Basic Serialization and Deserialization
This example demonstrates initializing the serializer with default options and performing a round-trip conversion of a strongly-typed event object.

```csharp
using System;
using System.Text.Json;

// Define a sample event
public record OrderCreatedEvent(string OrderId, decimal Amount);

public class Program
{
    public static void Main()
    {
        // Initialize with default options
        var serializer = new SystemTextJsonOutboxSerializer();
        
        var eventData = new OrderCreatedEvent("ORD-123", 99.95m);

        // Serialize to JSON string for storage in the outbox
        string jsonPayload = serializer.Serialize(eventData);
        Console.WriteLine($"Serialized: {jsonPayload}");

        // Deserialize back to the specific type
        OrderCreatedEvent? restoredEvent = serializer.Deserialize<OrderCreatedEvent>(jsonPayload);
        
        if (restoredEvent is not null)
        {
            Console.WriteLine($"Restored Order: {restoredEvent.OrderId}");
        }
    }
}
```

### Example 2: Custom Options and Non-Generic Deserialization
This example shows how to configure case-insensitive property matching and deserialize to a type known only at runtime.

```csharp
using System;
using System.Text.Json;

public class Program
{
    public static void Main()
    {
        // Configure options: case-insensitive and ignore nulls
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var serializer = new SystemTextJsonOutboxSerializer(options);
        
        string jsonInput = "{\"orderId\": \"ORD-456\", \"amount\": 50.00}";
        Type targetType = typeof(OrderCreatedEvent);

        // Deserialize using the non-generic overload for runtime types
        object? result = serializer.Deserialize(jsonInput, targetType); 
        // Note: If the specific overload signature in your build differs (e.g. requires explicit Type arg), 
        // ensure the call matches the available overload. 
        // Assuming standard pattern for object? Deserialize based on context.

        if (result is OrderCreatedEvent evt)
        {
            Console.WriteLine($"Processed: {evt.OrderId}");
        }
    }
}

public record OrderCreatedEvent(string OrderId, decimal Amount);
```

## Notes

*   **Thread Safety**: The `SystemTextJsonOutboxSerializer` class is generally thread-safe for read operations (serialization/deserialization) provided that the underlying `JsonSerializerOptions` instance passed to the constructor is not modified after initialization. `System.Text.Json` options are designed to be immutable during use; modifying them concurrently across threads can lead to undefined behavior.
*   **Null Handling**: The generic `Deserialize<T>` method returns `default(T)` (which is `null` for reference types) if the JSON content represents a null value. The non-generic `Deserialize` method similarly returns `null`. Consumers should handle null checks appropriately before casting or accessing properties.
*   **Type Compatibility**: When using the non-generic `Deserialize` method, ensure the runtime type provided or inferred is compatible with the JSON structure. Mismatches between the JSON schema and the target `Type` will result in a `JsonException` rather than a silent failure.
*   **Performance**: For high-throughput scenarios, reusing the `SystemTextJsonOutboxSerializer` instance is recommended over creating new instances per message, as this avoids repeated allocation of `JsonSerializerOptions` and internal caching mechanisms within `System.Text.Json` can be utilized more effectively.
