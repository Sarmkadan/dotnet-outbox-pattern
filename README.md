
## SerializationHelperTestsExtensions

The `SerializationHelperTestsExtensions` class provides extension methods for testing the `SerializationHelper` utility class, ensuring correct serialization and deserialization of complex objects, enums, null values, and DateTime types. It includes scenarios for round-trip validation, invalid JSON handling, and pretty-printed JSON output verification.

### Usage Example

```csharp
using DotnetOutboxPattern.Tests;
using FluentAssertions;

public class OutboxSerializationTests : SerializationHelperTests
{
    [Fact]
    public void Test_RoundTripSerialization_OfOutboxStatistics()
    {
        // Arrange
        var original = new OutboxStatistics
        {
            TotalMessages = 100,
            PendingMessages = 10,
            ProcessingMessages = 5,
            PublishedMessages = 80,
            FailedMessages = 5,
            ArchivedMessages = 0,
            DeadLetterCount = 2,
            AveragePublishTime = TimeSpan.FromSeconds(15),
            OldestPendingAge = TimeSpan.FromHours(2)
        };

        // Act & Assert
        this.Serialize_Deserialize_RoundTrip_ShouldPreserveAllProperties(original);
    }

    [Fact]
    public void Test_EnumSerialization_PreservesNames()
    {
        // Arrange
        var dto = new TestEnumDto
        {
            Status = TestStatus.Active,
            Priority = PriorityLevel.High
        };

        // Act & Assert
        this.Serialize_WithEnumValues_ShouldPreserveEnumNames(dto);
    }
}
```

## MessagePublishingServiceTestsExtensions

The `MessagePublishingServiceTestsExtensions` class provides extension methods for testing the `MessagePublishingService` class. These extension methods facilitate the creation of test instances, verification of publisher calls, and setup of repository mocks.

### Usage Example

```csharp
using DotnetOutboxPattern.Tests;
using Moq;

public class MessagePublishingServiceTests
{
    [Fact]
    public void Test_PublishMessage_ServiceCreatesAndPublishesMessage()
    {
        // Arrange
        var outboxRepoMock = new Mock<IOutboxRepository>();
        var publisherMock = new Mock<IMessagePublisher>();
        var service = MessagePublishingServiceTestsExtensions.CreateService(outboxRepoMock, publisherMock: publisherMock);

        var testMessage = MessagePublishingServiceTestsExtensions.CreateTestMessage(service);

        // Act
        service.PublishMessageAsync(testMessage.Id).Wait();

        // Assert
        publisherMock.VerifyPublishCalledOnceWith(testMessage);
    }
}
