// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

public class SerializationHelperTests
{
    private class TestPayload
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    [Fact]
    public void Serialize_ValidObject_ReturnsJsonString()
    {
        var payload = new TestPayload { Name = "test", Value = 42 };
        var json = SerializationHelper.Serialize(payload);
        json.Should().Contain("test");
        json.Should().Contain("42");
    }

    [Fact]
    public void Serialize_NullProperties_OmitsNullValues()
    {
        var payload = new TestPayload { Name = "test", Value = 0, Timestamp = null };
        var json = SerializationHelper.Serialize(payload);
        json.Should().NotContain("timestamp");
    }

    [Fact]
    public void Deserialize_ValidJson_ReturnsObject()
    {
        var json = "{\"name\":\"test\",\"value\":42}";
        var result = SerializationHelper.Deserialize<TestPayload>(json);
        result.Should().NotBeNull();
        result.Name.Should().Be("test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsSerializationException()
    {
        var act = () => SerializationHelper.Deserialize<TestPayload>("not json");
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void DefaultOptions_UseCamelCase()
    {
        SerializationHelper.DefaultOptions.PropertyNamingPolicy.Should().Be(System.Text.Json.JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void DefaultOptions_NotIndented()
    {
        SerializationHelper.DefaultOptions.WriteIndented.Should().BeFalse();
    }

    [Fact]
    public void PrettyOptions_IsIndented()
    {
        SerializationHelper.PrettyOptions.WriteIndented.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var original = new TestPayload { Name = "roundtrip", Value = 99, Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var json = SerializationHelper.Serialize(original);
        var restored = SerializationHelper.Deserialize<TestPayload>(json);
        restored.Name.Should().Be(original.Name);
        restored.Value.Should().Be(original.Value);
    }
}
