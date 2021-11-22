# DeadLetterAdditionalTests

`DeadLetterAdditionalTests` is a unit test class that validates the behavior of the `DeadLetter` entity and its factory method `FromOutboxMessage`. It ensures that property mappings, default value handling, and state-transition methods (`MarkAsReviewed`, `MarkAsRequeued`) behave correctly under various input conditions, including null, empty, and whitespace strings.

## API

### public void FromOutboxMessage_WithAllProperties_CopiesCorrectly
Verifies that `DeadLetter.FromOutboxMessage` copies every relevant property from a fully populated `OutboxMessage` to the resulting `DeadLetter` instance.  
**Purpose:** Ensures complete and accurate property transfer when all source fields contain non-null, non-empty values.  
**Parameters:** None (test method).  
**Returns:** void.  
**Throws:** Assertion failures if any property does not match the expected value.

### public void FromOutboxMessage_WithNullErrorMessage_UsesDefaultErrorMessage
Confirms that when the source `OutboxMessage` has a `null` error message, `DeadLetter.FromOutboxMessage` assigns a predefined default error message string instead of propagating `null`.  
**Purpose:** Guards against null error messages in the dead-letter record.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if the resulting `ErrorMessage` is not the expected default.

### public void FromOutboxMessage_WithNullErrorStackTrace_CopiesNull
Validates that a `null` error stack trace on the source `OutboxMessage` is faithfully copied as `null` to the `DeadLetter`, without replacement or transformation.  
**Purpose:** Ensures nullable reference types are handled correctly for stack trace data.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if the stack trace is not `null`.

### public void MarkAsReviewed_WithEmptyNotes_SetsReviewedProperties
Tests that calling `MarkAsReviewed` with an empty string for `notes` correctly transitions the `DeadLetter` to a reviewed state, setting the reviewed timestamp and storing the empty notes.  
**Purpose:** Ensures empty notes are accepted and do not cause validation errors.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if reviewed properties are not set as expected.

### public void MarkAsReviewed_WithWhitespaceNotes_TrimsNotes
Verifies that when `MarkAsReviewed` receives a notes string consisting only of whitespace, the stored notes are trimmed (typically to an empty string).  
**Purpose:** Prevents accidental storage of meaningless whitespace-only notes.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if the trimmed result is not empty or if reviewed state is incorrect.

### public void MarkAsRequeued_WithEmptyReason_SetsRequeueProperties
Confirms that `MarkAsRequeued` with an empty reason string successfully transitions the `DeadLetter` to a requeued state, recording the requeue timestamp and the empty reason.  
**Purpose:** Ensures empty reasons are valid input for the requeue operation.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if requeue properties are not correctly applied.

### public void MarkAsRequeued_WithWhitespaceReason_TrimsReason
Validates that a whitespace-only reason passed to `MarkAsRequeued` is trimmed before storage, preventing meaningless whitespace from persisting in the reason field.  
**Purpose:** Maintains data cleanliness for requeue reasons.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if the reason is not trimmed to an empty string or if the requeue state is not set.

### public void DeadLetter_WithAllProperties_InitializesCorrectly
Tests the `DeadLetter` constructor (or a creation helper) that accepts all properties, ensuring every supplied value is correctly assigned to the corresponding property.  
**Purpose:** Validates full initialization without property loss or transposition.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if any property deviates from the input value.

### public void DeadLetter_DefaultConstructor_GeneratesNewGuid
Verifies that instantiating `DeadLetter` using its parameterless constructor automatically generates a new, non-empty `Guid` for its identifier.  
**Purpose:** Guarantees every dead-letter record has a unique identity even when created without explicit ID assignment.  
**Parameters:** None.  
**Returns:** void.  
**Throws:** Assertion failures if the generated `Guid` is `Guid.Empty`.

## Usage

### Example 1: Creating a DeadLetter from an OutboxMessage and marking it as reviewed
```csharp
// Arrange an OutboxMessage with a processing error
var outboxMessage = new OutboxMessage
{
    Id = Guid.NewGuid(),
    MessageType = "OrderPlaced",
    Payload = "{ \"orderId\": 123 }",
    ErrorMessage = "Database timeout",
    ErrorStackTrace = "at OrderProcessor.Handle() ...",
    OccurredOn = DateTime.UtcNow.AddMinutes(-5)
};

// Act — create DeadLetter via factory method
DeadLetter deadLetter = DeadLetter.FromOutboxMessage(outboxMessage);

// Mark as reviewed with meaningful notes
deadLetter.MarkAsReviewed("Timeout resolved; message will be re-processed manually.");

// Assert state
// deadLetter.IsReviewed == true
// deadLetter.ReviewedAt != null
// deadLetter.Notes == "Timeout resolved; message will be re-processed manually."
```

### Example 2: Handling null error details and requeuing with whitespace reason
```csharp
// Arrange an OutboxMessage with null error details
var outboxMessage = new OutboxMessage
{
    Id = Guid.NewGuid(),
    MessageType = "InventoryUpdated",
    Payload = "{ \"sku\": \"ABC-001\" }",
    ErrorMessage = null,
    ErrorStackTrace = null,
    OccurredOn = DateTime.UtcNow
};

// Act — factory method applies defaults for null ErrorMessage
DeadLetter deadLetter = DeadLetter.FromOutboxMessage(outboxMessage);

// Requeue with a reason that contains leading/trailing whitespace
deadLetter.MarkAsRequeued("   ");

// Assert
// deadLetter.ErrorMessage == "An unknown error occurred" (or similar default)
// deadLetter.ErrorStackTrace == null
// deadLetter.IsRequeued == true
// deadLetter.RequeueReason == "" (whitespace trimmed)
```

## Notes

- **Null vs. Empty Semantics:** `FromOutboxMessage` distinguishes between `null` error messages (replaced with a default) and `null` stack traces (preserved as `null`). Consumers should not rely on `ErrorMessage` ever being `null` after creation.
- **Whitespace Trimming:** Both `MarkAsReviewed` and `MarkAsRequeued` trim whitespace-only input. Passing `"   "` is functionally equivalent to passing `""`. This prevents database pollution with invisible characters.
- **Default Constructor Identity:** The parameterless constructor guarantees a unique `Guid` per instance. This is critical for persistence scenarios where identity must be assigned immediately upon instantiation.
- **Thread Safety:** These test methods document the expected behavior of the `DeadLetter` entity. The entity itself is not inherently thread-safe; concurrent calls to `MarkAsReviewed` or `MarkAsRequeued` on the same instance from multiple threads may lead to race conditions. Synchronization should be applied externally if shared instances are used across threads.
- **Test Isolation:** Each test method is independent and does not mutate shared state. They are designed to run in any order without side effects.
