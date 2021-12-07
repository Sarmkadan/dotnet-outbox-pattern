# DeadLetterTests

The `DeadLetterTests` class serves as a test suite within the `dotnet-outbox-pattern` project, dedicated to verifying the correctness of the `DeadLetter` entity's lifecycle and state transitions. It ensures that dead letter messages are correctly instantiated from outbox messages, properly initialized via constructors, and accurately updated when marked as reviewed or requeued, thereby validating the integrity of failure handling logic in the outbox pattern implementation.

## API

### `FromOutboxMessage_CreatesDeadLetterWithCorrectProperties`
Validates that a `DeadLetter` instance created from an existing outbox message inherits the correct properties and maintains data consistency.
*   **Parameters**: None (operates on test fixtures defined within the method).
*   **Return Value**: `void`.
*   **Throws**: Throws an assertion exception if the resulting `DeadLetter` properties do not match the source outbox message or if the creation logic fails.

### `MarkAsReviewed_SetsReviewProperties`
Verifies that invoking the review logic on a `DeadLetter` instance correctly populates review-specific metadata, such as the review timestamp and reviewer identity.
*   **Parameters**: None (operates on test fixtures defined within the method).
*   **Return Value**: `void`.
*   **Throws**: Throws an assertion exception if the review properties remain unset or contain incorrect values after the operation.

### `MarkAsRequeued_SetsRequeueProperties`
Ensures that marking a `DeadLetter` as requeued correctly updates the requeue count and sets the appropriate scheduling or status flags for retry processing.
*   **Parameters**: None (operates on test fixtures defined within the method).
*   **Return Value**: `void`.
*   **Throws**: Throws an assertion exception if the requeue counter is not incremented or if associated requeue metadata is invalid.

### `DeadLetter_DefaultConstructor_InitializesProperties`
Confirms that the parameterless constructor of the `DeadLetter` class initializes all properties to their expected default values, ensuring a clean state for new instances.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Throws an assertion exception if any property deviates from its defined default state.

### `DeadLetter_WithFailureReasonAndSuggestedAction_SetsProperties`
Tests the overloaded constructor that accepts a failure reason and a suggested action, verifying that these specific arguments are correctly assigned to the corresponding properties upon instantiation.
*   **Parameters**: None (arguments are supplied via test fixtures within the method).
*   **Return Value**: `void`.
*   **Throws**: Throws an assertion exception if the `FailureReason` or `SuggestedAction` properties do not match the input values provided to the constructor.

## Usage

The following examples demonstrate how the test methods might be structured using a standard testing framework like xUnit, reflecting the validation of the `DeadLetter` entity.

```csharp
[Fact]
public void FromOutboxMessage_CreatesDeadLetterWithCorrectProperties()
{
    // Arrange
    var outboxMessage = new OutboxMessage 
    { 
        Id = Guid.NewGuid(), 
        Payload = "test-data", 
        CreatedAt = DateTime.UtcNow 
    };

    // Act
    var deadLetter = DeadLetter.FromOutboxMessage(outboxMessage);

    // Assert
    Assert.Equal(outboxMessage.Id, deadLetter.MessageId);
    Assert.Equal(outboxMessage.Payload, deadLetter.Payload);
    Assert.NotNull(deadLetter.FailedAt);
}
```

```csharp
[Fact]
public void MarkAsRequeued_SetsRequeueProperties()
{
    // Arrange
    var deadLetter = new DeadLetter("Connection timeout", "Retry immediately");
    var initialCount = deadLetter.RequeueCount;

    // Act
    deadLetter.MarkAsRequeued();

    // Assert
    Assert.Equal(initialCount + 1, deadLetter.RequeueCount);
    Assert.True(deadLetter.IsScheduledForRetry);
    Assert.NotNull(deadLetter.LastRequeuedAt);
}
```

## Notes

*   **State Dependencies**: Methods such as `MarkAsReviewed_SetsReviewProperties` and `MarkAsRequeued_SetsRequeueProperties` assume the `DeadLetter` instance is in a valid state to undergo these transitions. While the tests verify the successful path, implementations should be checked for guards against invalid state transitions (e.g., requeuing an already permanently failed message).
*   **Thread Safety**: As these tests instantiate objects and modify their state within a single method scope without shared static state, the test class itself is inherently thread-safe for parallel test execution. However, the underlying `DeadLetter` entity being tested should be evaluated separately for thread safety if instances are expected to be mutated concurrently in production code.
*   **Time Sensitivity**: Tests involving timestamps (e.g., `FailedAt`, `LastRequeuedAt`) rely on the system clock. Implementations should ensure that time comparisons allow for minimal execution drift or utilize injectable time providers to maintain deterministic test results.
