// ... existing content ...

## SystemTextJsonOutboxSerializerTestsExtensions

The `SystemTextJsonOutboxSerializerTestsExtensions` class provides a set of extension methods for testing the `SystemTextJsonOutboxSerializer` class. These methods allow you to easily create test instances with custom JSON options, verify serialization and deserialization, and ensure round-tripping.

### Usage Example

```csharp
var serializer = SystemTextJsonOutboxSerializerTestsExtensions
    .WithCamelCaseOptions(new SystemTextJsonOutboxSerializerTests());

var json = serializer.Serialize(new { Name = "John Doe", Age = 30 });
json.Should().Be("{\"name\":\"John Doe\",\"age\":30}");

var deserialized = serializer.Deserialize<{ Name: string, Age: int }>(json);
deserialized.Should().BeEquivalentTo(new { Name = "John Doe", Age = 30 });

serializer.ShouldContainPropertyName(new { Name = "John Doe" }, json, "name");
serializer.ShouldRoundTrip(new { Name = "John Doe", Age = 30 });
serializer.ShouldSerializeTo(new { Name = "John Doe" }, "{\"name\":\"John Doe\"}");
```

These extension methods simplify the process of testing serialization and deserialization scenarios, making it easier to ensure that your JSON serialization works as expected.

// ... existing content ...
