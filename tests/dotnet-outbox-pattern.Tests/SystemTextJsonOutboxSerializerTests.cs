#nullable enable

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using System.Text.Json;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for the SystemTextJsonOutboxSerializer class.
/// </summary>
public sealed class SystemTextJsonOutboxSerializerTests
{
    private readonly SystemTextJsonOutboxSerializer _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonOutboxSerializerTests"/> class.
    /// </summary>
    public SystemTextJsonOutboxSerializerTests()
    {
        _sut = new SystemTextJsonOutboxSerializer();
    }

    /// <summary>
    /// Verifies that the constructor with default options creates a serializer.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultOptions_CreatesSerializer()
    {
        var serializer = new SystemTextJsonOutboxSerializer();
        serializer.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the constructor with custom options creates a serializer.
    /// </summary>
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

    /// <summary>
    /// Verifies that the constructor with null options throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SystemTextJsonOutboxSerializer(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    /// <summary>
    /// Verifies that the Serialize method with a null value returns a null string.
    /// </summary>
    [Fact]
    public void Serialize_WithNullValue_ReturnsNullString()
    {
        var result = _sut.Serialize<object>(null!);
        result.Should().Be("null");
    }

    /// <summary>
    /// Verifies that the Serialize method with a simple object returns a JSON string.
    /// </summary>
    [Fact]
    public void Serialize_WithSimpleObject_ReturnsJsonString()
    {
        var testObject = new { Id = 123, Name = "Test" };
        var result = _sut.Serialize(testObject);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Test");
        result.Should().Contain("123");
    }

    /// <summary>
    /// Verifies that the Serialize method with a complex object returns a valid JSON string.
    /// </summary>
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

    /// <summary>
    /// Verifies that the Serialize method with a primitive value returns a JSON string.
    /// </summary>
    [Fact]
    public void Serialize_WithPrimitiveValue_ReturnsJsonString()
    {
        var result = _sut.Serialize(42);
        result.Should().Be("42");
    }

    /// <summary>
    /// Verifies that the Serialize method with a string value returns a JSON string.
    /// </summary>
    [Fact]
    public void Serialize_WithStringValue_ReturnsJsonString()
    {
        var result = _sut.Serialize("test string");
        result.Should().Be("\"test string\"");
    }

    /// <summary>
    /// Verifies that the Deserialize method with null JSON returns the default value.
    /// </summary>
    [Fact]
    public void Deserialize_WithNullJson_ReturnsDefault()
    {
        var result = _sut.Deserialize<object>(null!);
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method with an empty string returns the default value.
    /// </summary>
    [Fact]
    public void Deserialize_WithEmptyString_ReturnsDefault()
    {
        var result = _sut.Deserialize<object>("");
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method with a whitespace string returns the default value.
    /// </summary>
    [Fact]
    public void Deserialize_WithWhitespaceString_ReturnsDefault()
    {
        var result = _sut.Deserialize<object>(" ");
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method with invalid JSON returns the default value.
    /// </summary>
    [Fact]
    public void Deserialize_WithInvalidJson_ReturnsDefault()
    {
        var result = _sut.Deserialize<object>("{ invalid json }");
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method with a simple object returns the deserialized object.
    /// </summary>
    [Fact]
    public void Deserialize_WithSimpleObject_ReturnsDeserializedObject()
    {
        var json = "{\"id\":42,\"name\":\"Test\"}";
        var result = _sut.Deserialize<TestDto>(json);

        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("Test");
    }

    /// <summary>
    /// Verifies that the Deserialize method with null JSON returns the default value.
    /// </summary>
    [Fact]
    public void Deserialize_WithNullString_ReturnsDefault()
    {
        var result = _sut.Deserialize<TestDto>("null");
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method with a primitive type returns the deserialized value.
    /// </summary>
    [Fact]
    public void Deserialize_WithPrimitiveType_ReturnsDeserializedValue()
    {
        var result = _sut.Deserialize<int>("42");
        result.Should().Be(42);
    }

    /// <summary>
    /// Verifies that the Deserialize method with a string type returns the deserialized string.
    /// </summary>
    [Fact]
    public void Deserialize_WithStringType_ReturnsDeserializedString()
    {
        var result = _sut.Deserialize<string>("\"test\"");
        result.Should().Be("test");
    }

    /// <summary>
    /// Verifies that the Deserialize method with a type parameter returns the deserialized object.
    /// </summary>
    [Fact]
    public void Deserialize_WithTypeParameter_ReturnsDeserializedObject()
    {
        var json = "{\"id\":100,\"name\":\"Product\"}";
        var result = _sut.Deserialize(json, typeof(TestDto)) as TestDto;

        result.Should().NotBeNull();
        result!.Id.Should().Be(100);
        result.Name.Should().Be("Product");
    }

    /// <summary>
    /// Verifies that the Deserialize method with null JSON and a type parameter returns null.
    /// </summary>
    [Fact]
    public void Deserialize_WithNullJsonAndType_ReturnsNull()
    {
        var result = _sut.Deserialize(null!, typeof(TestDto));
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method with an empty string and a type parameter returns null.
    /// </summary>
    [Fact]
    public void Deserialize_WithEmptyJsonAndType_ReturnsNull()
    {
        var result = _sut.Deserialize("", typeof(TestDto));
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method with custom options uses the provided options.
    /// </summary>
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

    /// <summary>
    /// Verifies that the serializer can round-trip a DomainEvent with nested payload.
    /// </summary>
    [Fact]
    public void Serialize_Deserialize_DomainEvent_RoundTrip()
    {
        // Arrange - Create a PublishableEvent wrapping the DomainEvent (as used in actual code)
        var domainEvent = new EntityCreatedEvent
        {
            EntityId = "123",
            EntityType = "User",
            EntityData = new Dictionary<string, object> { { "Name", "John Doe" }, { "Email", "john@example.com" } },
            UserId = "user-42",
            CorrelationId = "corr-123",
            CausationId = "cmd-456"
        };

        var publishableEvent = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "test.topic",
            PartitionKey = "test-partition"
        };

        // Act - Serialize the PublishableEvent (which contains the polymorphic DomainEvent)
        var json = _sut.Serialize(publishableEvent);
        var deserialized = _sut.Deserialize<PublishableEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Event.Should().NotBeNull();
        deserialized.Event.Should().BeOfType<EntityCreatedEvent>();
        var deserializedEvent = deserialized.Event as EntityCreatedEvent;
        deserializedEvent.Should().NotBeNull();
        deserializedEvent!.EntityId.Should().Be(domainEvent.EntityId);
        deserializedEvent.EntityType.Should().Be(domainEvent.EntityType);
        deserializedEvent.UserId.Should().Be(domainEvent.UserId);
        deserializedEvent.CorrelationId.Should().Be(domainEvent.CorrelationId);
        deserializedEvent.CausationId.Should().Be(domainEvent.CausationId);
    }

    /// <summary>
    /// Verifies that the serializer can resolve polymorphic types using JsonDerivedType attributes.
    /// </summary>
    [Fact]
    public void Deserialize_PolymorphicDomainEvent_ResolvesCorrectType()
    {
        // Arrange - Create different types of domain events wrapped in PublishableEvent
        var entityCreated = new EntityCreatedEvent
        {
            EntityId = "created-123",
            EntityType = "Product",
            EntityData = new Dictionary<string, object> { { "Name", "Widget" } }
        };

        var entityUpdated = new EntityUpdatedEvent
        {
            EntityId = "updated-456",
            EntityType = "Product",
            OldData = new Dictionary<string, object> { { "Name", "Old Widget" } },
            NewData = new Dictionary<string, object> { { "Name", "New Widget" } },
            ChangedProperties = new List<string> { "Name" }
        };

        var customEvent = new CustomDomainEvent
        {
            EventName = "SpecialEvent",
            AggregateId = "agg-789",
            AggregateType = "SpecialAggregate",
            Payload = new Dictionary<string, object> { { "Key", "Value" } }
        };

        var notification = new NotificationEvent
        {
            NotificationType = "Email",
            RecipientId = "user-999",
            Subject = "Test Subject",
            Body = "Test Body",
            IsCritical = true
        };

        var publishableCreated = new PublishableEvent { Event = entityCreated, Topic = "test.created" };
        var publishableUpdated = new PublishableEvent { Event = entityUpdated, Topic = "test.updated" };
        var publishableCustom = new PublishableEvent { Event = customEvent, Topic = "test.custom" };
        var publishableNotification = new PublishableEvent { Event = notification, Topic = "test.notification" };

        // Act - Serialize each PublishableEvent
        var createdJson = _sut.Serialize(publishableCreated);
        var updatedJson = _sut.Serialize(publishableUpdated);
        var customJson = _sut.Serialize(publishableCustom);
        var notificationJson = _sut.Serialize(publishableNotification);

        // Assert - Each should deserialize correctly and the Event property should be the correct concrete type
        var deserializedCreated = _sut.Deserialize<PublishableEvent>(createdJson);
        deserializedCreated.Should().NotBeNull();
        deserializedCreated!.Event.Should().BeOfType<EntityCreatedEvent>();
        (deserializedCreated.Event as EntityCreatedEvent)!.EntityId.Should().Be("created-123");

        var deserializedUpdated = _sut.Deserialize<PublishableEvent>(updatedJson);
        deserializedUpdated.Should().NotBeNull();
        deserializedUpdated!.Event.Should().BeOfType<EntityUpdatedEvent>();
        (deserializedUpdated.Event as EntityUpdatedEvent)!.EntityId.Should().Be("updated-456");

        var deserializedCustom = _sut.Deserialize<PublishableEvent>(customJson);
        deserializedCustom.Should().NotBeNull();
        deserializedCustom!.Event.Should().BeOfType<CustomDomainEvent>();
        (deserializedCustom.Event as CustomDomainEvent)!.EventName.Should().Be("SpecialEvent");

        var deserializedNotification = _sut.Deserialize<PublishableEvent>(notificationJson);
        deserializedNotification.Should().NotBeNull();
        deserializedNotification!.Event.Should().BeOfType<NotificationEvent>();
        (deserializedNotification.Event as NotificationEvent)!.Subject.Should().Be("Test Subject");
    }

    /// <summary>
    /// Verifies that the serializer handles null payload correctly.
    /// </summary>
    [Fact]
    public void Serialize_NullPayload_ReturnsNullString()
    {
        // Arrange
        DomainEvent? nullEvent = null;

        // Act
        var result = _sut.Serialize(nullEvent);

        // Assert
        result.Should().Be("null");
    }

    /// <summary>
    /// Verifies that the Deserialize method handles null payload correctly.
    /// </summary>
    [Fact]
    public void Deserialize_NullPayload_ReturnsNull()
    {
        // Act
        var result = _sut.Deserialize<DomainEvent>(null!);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method handles empty string input correctly.
    /// </summary>
    [Fact]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        // Act
        var result = _sut.Deserialize<DomainEvent>("");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method handles malformed JSON gracefully.
    /// </summary>
    [Fact]
    public void Deserialize_MalformedJson_ReturnsNull()
    {
        // Arrange - Malformed JSON
        var malformedJson = "{ this is not valid json";

        // Act
        var result = _sut.Deserialize<DomainEvent>(malformedJson);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method handles malformed JSON with type parameter gracefully.
    /// </summary>
    [Fact]
    public void Deserialize_MalformedJson_WithTypeParameter_ReturnsNull()
    {
        // Arrange - Malformed JSON
        var malformedJson = "not a json at all {{{";

        // Act
        var result = _sut.Deserialize(malformedJson, typeof(DomainEvent));

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Deserialize method handles invalid JSON for the target type gracefully.
    /// </summary>
    [Fact]
    public void Deserialize_InvalidJsonForTargetType_ReturnsNull()
    {
        // Arrange - Truly malformed JSON that will throw JsonException
        var invalidJson = "not valid json {{{";

        // Act - Try to deserialize as PublishableEvent
        var result = _sut.Deserialize<PublishableEvent>(invalidJson);

        // Assert - Should return null rather than throw
        result.Should().BeNull();
    }

    /// <summary>
    /// A test DTO class.
    /// </summary>
    private sealed class TestDto
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }
    }
}
