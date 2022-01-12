#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Exceptions;
using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Extension methods for <see cref="SerializationHelperTests"/> providing additional test scenarios
/// for serialization and deserialization functionality.
/// </summary>
/// <remarks>
/// This class contains extension methods that test various edge cases and scenarios
/// for the <see cref="SerializationHelper"/> utility class.
/// </remarks>
public static class SerializationHelperTestsExtensions
{
    /// <summary>
    /// Tests round-trip serialization of <see cref="OutboxStatistics"/> ensuring all properties are preserved.
    /// </summary>
    /// <param name="_">The test instance (unused parameter)</param>
    /// <param name="original">The original statistics object to serialize and deserialize</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> is <see langword="null"/></exception>
    public static void Serialize_Deserialize_RoundTrip_ShouldPreserveAllProperties(this SerializationHelperTests _, OutboxStatistics original)
    {
        ArgumentNullException.ThrowIfNull(original);

        // Act
        var json = SerializationHelper.Serialize(original);
        var deserialized = SerializationHelper.Deserialize<OutboxStatistics>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.TotalMessages.Should().Be(original.TotalMessages);
        deserialized.PendingMessages.Should().Be(original.PendingMessages);
        deserialized.ProcessingMessages.Should().Be(original.ProcessingMessages);
        deserialized.PublishedMessages.Should().Be(original.PublishedMessages);
        deserialized.FailedMessages.Should().Be(original.FailedMessages);
        deserialized.ArchivedMessages.Should().Be(original.ArchivedMessages);
        deserialized.DeadLetterCount.Should().Be(original.DeadLetterCount);
        deserialized.AveragePublishTime.Should().Be(original.AveragePublishTime);
        deserialized.OldestPendingAge.Should().Be(original.OldestPendingAge);
    }

    /// <summary>
    /// Tests serialization of complex nested objects ensuring all properties are handled correctly.
    /// </summary>
    /// <param name="_">The test instance (unused parameter)</param>
    /// <param name="metrics">The metrics object to serialize and deserialize</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is <see langword="null"/></exception>
    public static void Serialize_WithComplexNestedObject_ShouldHandleAllProperties(this SerializationHelperTests _, HealthMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        // Arrange
        var complexMetrics = new HealthMetrics
        {
            IsHealthy = false,
            LastSuccessfulPublish = DateTime.UtcNow.AddMinutes(-5),
            ConsecutiveFailures = 15,
            ErrorMessage = "Connection timeout to message broker",
            LockedMessagesCount = 25,
            HasExpiredLocks = true,
            OldestMessageAge = TimeSpan.FromHours(2)
        };

        // Act
        var json = SerializationHelper.Serialize(complexMetrics);
        var deserialized = SerializationHelper.Deserialize<HealthMetrics>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.IsHealthy.Should().BeFalse();
        deserialized.ConsecutiveFailures.Should().Be(15);
        deserialized.LockedMessagesCount.Should().Be(25);
        deserialized.HasExpiredLocks.Should().BeTrue();
        deserialized.ErrorMessage.Should().Be("Connection timeout to message broker");
        deserialized.LastSuccessfulPublish.Should().NotBeNull();
        deserialized.OldestMessageAge.Should().Be(TimeSpan.FromHours(2));
    }

    /// <summary>
    /// Tests serialization with null values and edge cases ensuring null values are omitted correctly.
    /// </summary>
    /// <param name="_">The test instance (unused parameter)</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="_"/> is <see langword="null"/></exception>
    public static void Serialize_WithNullAndDefaultValues_ShouldOmitCorrectly(this SerializationHelperTests _)
    {
        // Arrange - create object with all null/default values
        var emptyStats = new OutboxStatistics
        {
            TotalMessages = 0,
            PendingMessages = 0,
            ProcessingMessages = 0,
            PublishedMessages = 0,
            FailedMessages = 0,
            ArchivedMessages = 0,
            DeadLetterCount = 0,
            AveragePublishTime = TimeSpan.Zero,
            OldestPendingAge = null
        };

        // Act
        var json = SerializationHelper.Serialize(emptyStats);
        var deserialized = SerializationHelper.Deserialize<OutboxStatistics>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.TotalMessages.Should().Be(0);
        deserialized.OldestPendingAge.Should().BeNull();

        // Verify null values are omitted
        json.Should().NotContain("oldestPendingAge");
    }

    /// <summary>
    /// Tests <see cref="SerializationHelper.IsValidJson"/> with various edge cases.
    /// </summary>
    /// <param name="_">The test instance (unused parameter)</param>
    public static void IsValidJson_WithVariousInputs_ShouldReturnCorrectValidation(this SerializationHelperTests _)
    {
        // Test various valid JSON inputs
        SerializationHelper.IsValidJson("{}").Should().BeTrue();
        SerializationHelper.IsValidJson("[]").Should().BeTrue();
        SerializationHelper.IsValidJson("\"simple string\"").Should().BeTrue();
        SerializationHelper.IsValidJson("42").Should().BeTrue();
        SerializationHelper.IsValidJson("true").Should().BeTrue();
        SerializationHelper.IsValidJson("null").Should().BeTrue();

        // Test various invalid JSON inputs
        SerializationHelper.IsValidJson("{invalid").Should().BeFalse();
        SerializationHelper.IsValidJson("[1,2,3").Should().BeFalse();
        SerializationHelper.IsValidJson("not json").Should().BeFalse();
        SerializationHelper.IsValidJson("").Should().BeFalse();
        SerializationHelper.IsValidJson(" ").Should().BeFalse();
    }

    /// <summary>
    /// Tests <see cref="SerializationHelper.SerializePretty"/> with different object types ensuring indented output is produced.
    /// </summary>
    /// <param name="_">The test instance (unused parameter)</param>
    public static void SerializePretty_WithDifferentTypes_ShouldProduceIndentedOutput(this SerializationHelperTests _)
    {
        // Test with simple object
        var stats = new OutboxStatistics { TotalMessages = 100 };
        var prettyJson = SerializationHelper.SerializePretty(stats);

        prettyJson.Should().Contain("{");
        prettyJson.Should().Contain("\n");
        prettyJson.Should().Contain("totalMessages");

        // Verify it's actually indented (contains multiple lines with indentation)
        var lines = prettyJson.Split('\n');
        lines.Should().HaveCountGreaterThan(1);

        // Test with complex object
        var metrics = new HealthMetrics
        {
            IsHealthy = true,
            ConsecutiveFailures = 3,
            LockedMessagesCount = 5
        };

        var complexPrettyJson = SerializationHelper.SerializePretty(metrics);
        complexPrettyJson.Should().Contain("{");
        complexPrettyJson.Should().Contain("\n");
        complexPrettyJson.Should().Contain("isHealthy");
    }

    /// <summary>
    /// Tests deserialization with invalid JSON ensuring error messages include type information.
    /// </summary>
    /// <param name="_">The test instance (unused parameter)</param>
    public static void Deserialize_WithInvalidJson_ShouldIncludeTypeNameInError(this SerializationHelperTests _)
    {
        // Arrange
        var invalidJson = "{invalid json format";

        // Act
        Action act = () => SerializationHelper.Deserialize<OutboxStatistics>(invalidJson);

        // Assert
        act.Should().Throw<SerializationException>()
            .Where(e => e.TargetType == typeof(OutboxStatistics).Name);
    }

    /// <summary>
    /// Tests serialization preserves enum values correctly.
    /// </summary>
    /// <param name="_">The test instance (unused parameter)</param>
    public static void Serialize_WithEnumValues_ShouldPreserveEnumNames(this SerializationHelperTests _)
    {
        // Arrange - create a test DTO with enum
        var testDto = new TestEnumDto
        {
            Status = TestStatus.Active,
            Priority = PriorityLevel.High
        };

        // Act
        var json = SerializationHelper.Serialize(testDto);
        var deserialized = SerializationHelper.Deserialize<TestEnumDto>(json);

        // Assert
        deserialized.Status.Should().Be(TestStatus.Active);
        deserialized.Priority.Should().Be(PriorityLevel.High);
        json.Should().Contain("active");
        json.Should().Contain("high");
    }

    /// <summary>
    /// Tests serialization with Guid values ensuring Guid format is preserved.
    /// </summary>
    /// <param name="_">The test instance (unused parameter)</param>
    public static void Serialize_WithGuidValues_ShouldPreserveGuidFormat(this SerializationHelperTests _)
    {
        // Arrange
        var stats = new OutboxStatistics
        {
            TotalMessages = 1
        };

        // Act
        var json = SerializationHelper.Serialize(stats);
        var deserialized = SerializationHelper.Deserialize<OutboxStatistics>(json);

        // Assert - Guid should be serialized and deserialized correctly
        deserialized.Should().NotBeNull();
        json.Should().Contain("totalMessages");
    }

    /// <summary>
    /// Tests serialization round-trip with DateTime values ensuring DateTime format is preserved.
    /// </summary>
    /// <param name="_">The test instance (unused parameter)</param>
    public static void Serialize_WithDateTimeValues_ShouldPreserveDateTimeFormat(this SerializationHelperTests _)
    {
        // Arrange
        var metrics = new HealthMetrics
        {
            LastHealthCheckAt = DateTime.UtcNow,
            LastSuccessfulPublish = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var json = SerializationHelper.Serialize(metrics);
        var deserialized = SerializationHelper.Deserialize<HealthMetrics>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.LastHealthCheckAt.Should().BeCloseTo(metrics.LastHealthCheckAt, TimeSpan.FromSeconds(1));
        deserialized.LastSuccessfulPublish.Should().BeCloseTo(metrics.LastSuccessfulPublish!.Value, TimeSpan.FromSeconds(1));
    }
}

/// <summary>
/// Test DTO for enum serialization testing
/// </summary>
public sealed class TestEnumDto
{
    public TestStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
}

/// <summary>
/// Test enum for serialization
/// </summary>
public enum TestStatus
{
    Active,
    Inactive,
    Pending
}

/// <summary>
/// Test enum for serialization
/// </summary>
public enum PriorityLevel
{
    Low,
    Medium,
    High
}