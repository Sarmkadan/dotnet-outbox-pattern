
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
```

## BatchProcessingModelsTestsExtensions

The `BatchProcessingModelsTestsExtensions` class provides extension methods for testing batch processing models, including `BatchProcessingOptions`, `BatchChunkResult`, and `BatchProcessingSummary`. These extension methods facilitate the creation of test instances and verification of their properties.

### Usage Example

```csharp
using DotnetOutboxPattern.Tests;
using FluentAssertions;

public class BatchProcessingModelsTests
{
    [Fact]
    public void Test_CreateDefaultOptions_ReturnsOptionsWithDefaultValues()
    {
        // Arrange & Act
        var options = BatchProcessingModelsTestsExtensions.CreateDefaultOptions(this);

        // Assert
        options.Should().NotBeNull();
        options.TotalBatchSize.Should().Be(0);
        options.ChunkSize.Should().Be(0);
        options.MaxParallelChunks.Should().Be(0);
        options.EnableParallelChunks.Should().BeFalse();
        options.DelayBetweenChunksMs.Should().Be(0);
        options.StopOnChunkFailure.Should().BeFalse();
    }

    [Fact]
    public void Test_CreateChunkResult_ReturnsChunkResultWithSpecifiedValues()
    {
        // Arrange & Act
        var chunkResult = BatchProcessingModelsTestsExtensions.CreateChunkResult(
            this,
            chunkIndex: 1,
            success: true,
            processedCount: 10,
            failedCount: 0,
            errorMessage: null);

        // Assert
        chunkResult.Should().NotBeNull();
        chunkResult.ChunkIndex.Should().Be(1);
        chunkResult.Success.Should().Be(true);
        chunkResult.ProcessedCount.Should().Be(10);
        chunkResult.FailedCount.Should().Be(0);
        chunkResult.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Test_ShouldBeEquivalentTo_Options_AreEqual()
    {
        // Arrange
        var expected = BatchProcessingModelsTestsExtensions.CreateDefaultOptions(this);
        var actual = BatchProcessingModelsTestsExtensions.CreateDefaultOptions(this);

        // Act & Assert
        BatchProcessingModelsTestsExtensions.ShouldBeEquivalentTo(this, expected, actual);
    }
}
```

## BatchProcessingOptionsExtensions

The `BatchProcessingOptionsExtensions` class provides extension methods for configuring and validating `BatchProcessingOptions`. These extension methods allow for fluent configuration of batch processing settings, calculation of chunk sizes, and estimation of memory usage.

### Usage Example

```csharp
using DotnetOutboxPattern.Domain;

public class BatchProcessingExample
{
    public void ConfigureBatchProcessingOptions()
    {
        var options = new BatchProcessingOptions();

        options = options
            .WithTotalBatchSize(1000)
            .WithChunkSize(100)
            .WithParallelChunks(4)
            .WithDelayBetweenChunks(500)
            .StopOnFailure();

        var totalChunks = options.CalculateTotalChunks();

        var chunkMemoryUsage = options.GetChunkMemoryUsage();
        var totalBatchMemoryUsage = options.GetTotalBatchMemoryUsage();

        var isValid = options.Validate();
    }
}
