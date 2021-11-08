#nullable enable

using DotnetOutboxPattern.Services;
using FluentAssertions;
using System.Text.Json;

namespace DotnetOutboxPattern.Tests;

public sealed class SystemTextJsonOutboxSerializerTests
{
    private readonly SystemTextJsonOutboxSerializer _sut;

    public SystemTextJsonOutboxSerializerTests()
    {
        _sut = new SystemTextJsonOutboxSerializer();
    }

    [Fact]
    public void Constructor_WithDefaultOptions_CreatesSerializer()
    {
        var serializer = new SystemTextJsonOutboxSerializer();
        serializer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomOptions_CreatesSerializer()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        var serializer = new SystemTextJsonOutboxSerializer(options);
        serializer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SystemTextJsonOutboxSerializer(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Serialize_WithNullValue_ReturnsNullString()
    {
        var result = _sut.Serialize<object>(null!);
        result.Should().Be("null");
    }

    [Fact]
    public void Serialize_WithSimpleObject_ReturnsJsonString()
    {
        var testObject = new { Id = 123, Name = "Test" };
        var result = _sut.Serialize(testObject);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Test");
        result.Should().Contain("123");
    }

    [Fact]
    public void Serialize_WithComplexObject_ReturnsValidJson()
    {
        var testObject = new
        {
            Id = Guid.NewGuid(),
            Items = new[] { "item1", "item2" },
            Metadata = new Dictionary<string, string> { { "key", "value" } }
        };
        var result = _sut.Serialize(testObject);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(testObject.Id.ToString());
    }

    [Fact]
    public void Serialize_WithPrimitiveValue_ReturnsJsonString()
    {
        var result = _sut.Serialize(42);
        result.Should().Be("42");
    }

    [Fact]
    public void Serialize_WithStringValue_ReturnsJsonString()
    {
        var result = _sut.Serialize("test string");
        result.Should().Be("\"test string\"");
    }

    [Fact]
    public void Deserialize_WithNullJson_ReturnsDefault()
    {
        var result = _sut.Deserialize<object>(null!);
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithEmptyString_ReturnsDefault()
    {
        var result = _sut.Deserialize<object>("");
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithWhitespaceString_ReturnsDefault()
    {
        var result = _sut.Deserialize<object>("   ");
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithInvalidJson_ReturnsDefault()
    {
        var result = _sut.Deserialize<object>("{ invalid json }");
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithSimpleObject_ReturnsDeserializedObject()
    {
        var json = "{\"id\":42,\"name\":\"Test\"}";
        var result = _sut.Deserialize<TestDto>(json);

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public void Deserialize_WithNullString_ReturnsDefault()
    {
        var result = _sut.Deserialize<TestDto>("null");
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithPrimitiveType_ReturnsDeserializedValue()
    {
        var result = _sut.Deserialize<int>("42");
        result.Should().Be(42);
    }

    [Fact]
    public void Deserialize_WithStringType_ReturnsDeserializedString()
    {
        var result = _sut.Deserialize<string>("\"test\"");
        result.Should().Be("test");
    }

    [Fact]
    public void Deserialize_WithTypeParameter_ReturnsDeserializedObject()
    {
        var json = "{\"id\":100,\"name\":\"Product\"}";
        var result = _sut.Deserialize(json, typeof(TestDto)) as TestDto;

        result.Should().NotBeNull();
        result!.Id.Should().Be(100);
        result.Name.Should().Be("Product");
    }

    [Fact]
    public void Deserialize_WithNullJsonAndType_ReturnsNull()
    {
        var result = _sut.Deserialize(null!, typeof(TestDto));
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithEmptyJsonAndType_ReturnsNull()
    {
        var result = _sut.Deserialize("", typeof(TestDto));
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithCustomOptions_UsesProvidedOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var serializer = new SystemTextJsonOutboxSerializer(options);

        var json = "{\"id\":1,\"name\":\"Test\"}";
        var result = serializer.Deserialize<TestDto>(json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test"); // Should work with camelCase if the DTO property matches
    }

    private sealed class TestDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}