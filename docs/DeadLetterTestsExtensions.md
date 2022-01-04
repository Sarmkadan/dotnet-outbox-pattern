# DeadLetterTestsExtensions

`DeadLetterTestsExtensions` provides a set of static utility and extension methods designed to simplify the creation and validation of `DeadLetter` objects within unit test suites for the outbox pattern. These methods facilitate the rapid setup of known error states and provide fluent assertion helpers to verify the status and processing requirements of dead-lettered messages, ensuring consistent test behavior.

## API

### CreateTestDeadLetter()
*   **Purpose**: Constructs and returns a new `DeadLetter` instance initialized with default test-ready values.
*   **Parameters**: None.
*   **Return**: A new `DeadLetter` instance.
*   **Throws**: None.

### CreateTestDeadLetterWithFailure(string errorMessage)
*   **Purpose**: Constructs and returns a new `DeadLetter` instance pre-configured to represent a failed state, incorporating the specified error description.
*   **Parameters**:
    *   `errorMessage` (string): The failure message describing the issue that caused the message to be dead-lettered.
*   **Return**: A new `DeadLetter` instance in an error state.
*   **Throws**: `ArgumentException` if `errorMessage` is null, empty, or whitespace.

### ShouldHaveErrorState(this DeadLetter deadLetter)
*   **Purpose**: Asserts that the provided `DeadLetter` instance is currently in an error state. This is an extension method for fluent assertion usage.
*   **Parameters**:
    *   `deadLetter` (DeadLetter): The instance to validate.
*   **Return**: void.
*   **Throws**: `Xunit.Sdk.XunitException` if the instance is not in an error state.

### ShouldBeReviewed(this DeadLetter deadLetter)
*   **Purpose**: Asserts that the provided `DeadLetter` instance is marked for review. This is an extension method for fluent assertion usage.
*   **Parameters**:
    *   `deadLetter` (DeadLetter): The instance to validate.
*   **Return**: void.
*   **Throws**: `Xunit.Sdk.XunitException` if the instance is not marked for review.

## Usage

```csharp
[Fact]
public void Process_WhenMessageFails_CreatesDeadLetterWithCorrectState()
{
    // Arrange
    var errorMessage = "Database connection timeout";
    
    // Act
    var deadLetter = DeadLetterTestsExtensions.CreateTestDeadLetterWithFailure(errorMessage);
    
    // Assert
    deadLetter.ShouldHaveErrorState();
}
```

```csharp
[Fact]
public void ReviewProcess_WhenFlagged_EnsuresDeadLetterIsMarkedForReview()
{
    // Arrange
    var deadLetter = DeadLetterTestsExtensions.CreateTestDeadLetter();
    // Simulate setting the review flag...
    deadLetter.MarkForReview();
    
    // Act & Assert
    deadLetter.ShouldBeReviewed();
}
```

## Notes

*   **Thread Safety**: All methods within `DeadLetterTestsExtensions` are static and operate purely on the provided instances or return new instances. The class is stateless and thread-safe.
*   **Assertion Framework**: The `Should...` extension methods are designed to work with xUnit. They will throw `Xunit.Sdk.XunitException` when assertions fail, integrating natively with standard .NET test runners.
*   **Null Handling**: The `Should...` extension methods will throw a `NullReferenceException` if the `deadLetter` instance passed is null. Ensure instances are properly initialized before invoking these assertions.
