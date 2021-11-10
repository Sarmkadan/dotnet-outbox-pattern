# SerializationHelper
The `SerializationHelper` class provides a set of static methods for serializing and deserializing objects to and from JSON strings. It offers various serialization options, including pretty-printing and dynamic deserialization, as well as validation of JSON strings. This helper class is designed to simplify the process of working with JSON data in .NET applications.

## API
* `public static string Serialize<T>(T obj)`: Serializes an object of type `T` to a JSON string. The object to be serialized is passed as a parameter. The method returns the JSON string representation of the object. It throws an exception if the serialization fails.
* `public static T Deserialize<T>(string json)`: Deserializes a JSON string to an object of type `T`. The JSON string to be deserialized is passed as a parameter. The method returns the deserialized object. It throws an exception if the deserialization fails or if the JSON string is invalid.
* `public static object? DeserializeDynamic(string json)`: Dynamically deserializes a JSON string to an object. The JSON string to be deserialized is passed as a parameter. The method returns the deserialized object, or `null` if the deserialization fails.
* `public static string SerializePretty<T>(T obj)`: Serializes an object of type `T` to a pretty-printed JSON string. The object to be serialized is passed as a parameter. The method returns the pretty-printed JSON string representation of the object. It throws an exception if the serialization fails.
* `public static bool IsValidJson(string json)`: Validates whether a given JSON string is valid. The JSON string to be validated is passed as a parameter. The method returns `true` if the JSON string is valid, and `false` otherwise.
* `public override Guid Read()`: Reads a Guid value. This method is not specific to JSON serialization and its purpose is unclear in this context.
* `public override void Write(Guid value)`: Writes a Guid value. This method is not specific to JSON serialization and its purpose is unclear in this context.
* `public override DateTime Read()`: Reads a DateTime value. This method is not specific to JSON serialization and its purpose is unclear in this context.
* `public override void Write(DateTime value)`: Writes a DateTime value. This method is not specific to JSON serialization and its purpose is unclear in this context.

## Usage
The following examples demonstrate how to use the `SerializationHelper` class:
```csharp
// Example 1: Serializing and deserializing a simple object
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

Person person = new Person { Name = "John", Age = 30 };
string json = SerializationHelper.Serialize(person);
Person deserializedPerson = SerializationHelper.Deserialize<Person>(json);

// Example 2: Validating a JSON string
string jsonString = "{\"name\":\"John\",\"age\":30}";
bool isValid = SerializationHelper.IsValidJson(jsonString);
if (isValid)
{
    Console.WriteLine("The JSON string is valid.");
}
else
{
    Console.WriteLine("The JSON string is invalid.");
}
```

## Notes
When using the `SerializationHelper` class, be aware of the following edge cases:
* If the object being serialized contains circular references, the serialization may fail or produce unexpected results.
* If the JSON string being deserialized is malformed or contains invalid data, the deserialization may fail or produce unexpected results.
* The `Read` and `Write` methods for Guid and DateTime values are not specific to JSON serialization and may not be relevant in all use cases.
* The `SerializationHelper` class is designed to be thread-safe, but it is still important to follow best practices for concurrent programming when using this class in a multi-threaded environment.
