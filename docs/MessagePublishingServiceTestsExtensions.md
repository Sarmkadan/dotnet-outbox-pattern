# MessagePublishingServiceTestsExtensions

The `MessagePublishingServiceTestsExtensions` class provides a set of static helper methods designed to streamline the unit testing of the `MessagePublishingService` within the `dotnet-outbox-pattern` project. By abstracting common boilerplate code related to mock setup, test data generation, and verification, these extensions facilitate the creation of readable, maintainable, and robust test suites for outbox processing workflows.

## API

### CreateService
Creates and returns an instance of `MessagePublishingService`, initializing it with the necessary mocked dependencies.

### CreateTestMessage
Generates a new `OutboxMessage` object populated with default or specified test values suitable for use in unit tests.

### VerifyPublishCalledOnceWith
Verifies that the `IMessagePublisher.PublishAsync` method was invoked exactly once with the expected parameters during a test execution. Throws an exception if the method was not called, or called with incorrect arguments.

### SetupGetByIdAsync
Configures a provided `Mock<IOutboxRepository>` to return a specified `OutboxMessage` when `GetByIdAsync` is invoked with a given identifier.

### SetupGetPendingMessagesAsync
Configures a provided `Mock<IOutboxRepository>` to return a collection of pending `OutboxMessage` objects when `GetPendingMessagesAsync` is invoked.

### SetupPublishSuccess
Configures a provided `Mock<IMessagePublisher>` to successfully complete the `PublishAsync` method, simulating a successful message delivery.

### SetupUpdateSuccess
Configures a provided `Mock<IOutboxRepository>` to successfully complete the `UpdateAsync` method when called with a valid `OutboxMessage`, simulating a successful state transition in the repository.

## Usage

```csharp
[Fact]
public async Task ProcessPendingMessages_WhenMessagesExist_PublishesSuccessfully()
{
    // Arrange
    var repositoryMock = new Mock<IOutboxRepository>();
    var publisherMock = new Mock<IMessagePublisher>();
    var service = MessagePublishingServiceTestsExtensions.CreateService(repositoryMock, publisherMock);
    
    var messages = new List<OutboxMessage> { MessagePublishingServiceTestsExtensions.CreateTestMessage() };
    
    MessagePublishingServiceTestsExtensions.SetupGetPendingMessagesAsync(repositoryMock, messages);
    MessagePublishingServiceTestsExtensions.SetupPublishSuccess(publisherMock);
    MessagePublishingServiceTestsExtensions.SetupUpdateSuccess(repositoryMock);

    // Act
    await service.ProcessPendingMessagesAsync(CancellationToken.None);

    // Assert
    MessagePublishingServiceTestsExtensions.VerifyPublishCalledOnceWith(publisherMock, messages.First());
}
```

```csharp
[Fact]
public async Task GetMessageById_ReturnsCorrectMessage()
{
    // Arrange
    var repositoryMock = new Mock<IOutboxRepository>();
    var expectedMessage = MessagePublishingServiceTestsExtensions.CreateTestMessage();
    
    MessagePublishingServiceTestsExtensions.SetupGetByIdAsync(repositoryMock, expectedMessage.Id, expectedMessage);

    // Act
    var result = await repositoryMock.Object.GetByIdAsync(expectedMessage.Id);

    // Assert
    Assert.Equal(expectedMessage, result);
}
```

## Notes

*   **Mock Thread-Safety**: These methods operate on `Moq.Mock` objects. While the helper methods themselves are thread-safe, the underlying mock objects and the configured expectations are not inherently thread-safe for concurrent setups. Ensure tests are executed sequentially if shared mocks are utilized across multiple test threads.
*   **Parameter Validation**: The `Setup` methods generally expect non-null mock objects. Passing a `null` mock to these extensions will result in a `NullReferenceException` at runtime.
*   **Verification**: The `VerifyPublishCalledOnceWith` method is strict regarding the number of calls. If the message publisher is invoked multiple times or not at all, the assertion will fail.
