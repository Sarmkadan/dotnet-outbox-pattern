# EnumsTests

`EnumsTests` is a test class that validates the correctness of enum definitions and their associated attributes within the `dotnet-outbox-pattern` project. It ensures that enum values match expected constants, string representations are accurate, and descriptive attributes are properly applied.

## API

### `public void OutboxMessageState_Values_MatchExpected()`

Validates that all values of the `OutboxMessageState` enum match the expected constants defined in the test. This ensures consistency between the enum definition and the project's requirements.

**Parameters:** None
**Return value:** None
**Throws:** Throws if any enum value does not match the expected constant.

---

### `public void EventType_Values_MatchExpected()`

Validates that all values of the `EventType` enum match the expected constants defined in the test. Ensures the enum aligns with the project's event taxonomy.

**Parameters:** None
**Return value:** None
**Throws:** Throws if any enum value does not match the expected constant.

---
### `public void DeliveryGuarantee_Values_MatchExpected()`

Validates that all values of the `DeliveryGuarantee` enum match the expected constants defined in the test. Ensures the enum reflects the project's delivery semantics.

**Parameters:** None
**Return value:** None
**Throws:** Throws if any enum value does not match the expected constant.

---
### `public void RetryPolicyType_Values_MatchExpected()`

Validates that all values of the `RetryPolicyType` enum match the expected constants defined in the test. Ensures the enum aligns with the project's retry configuration.

**Parameters:** None
**Return value:** None
**Throws:** Throws if any enum value does not match the expected constant.

---
### `public void OutboxMessageState_ToString_ReturnsCorrectString()`

Validates that the `ToString()` method of the `OutboxMessageState` enum returns the correct string representation for each value. Ensures human-readable output matches the project's requirements.

**Parameters:** None
**Return value:** None
**Throws:** Throws if any `ToString()` output does not match the expected string.

---
### `public void EventType_ToString_ReturnsCorrectString()`

Validates that the `ToString()` method of the `EventType` enum returns the correct string representation for each value. Ensures human-readable output matches the project's requirements.

**Parameters:** None
**Return value:** None
**Throws:** Throws if any `ToString()` output does not match the expected string.

---
### `public void DeliveryGuarantee_ToString_ReturnsCorrectString()`

Validates that the `ToString()` method of the `DeliveryGuarantee` enum returns the correct string representation for each value. Ensures human-readable output matches the project's requirements.

**Parameters:** None
**Return value:** None
**Throws:** Throws if any `ToString()` output does not match the expected string.

---
### `public void RetryPolicyType_ToString_ReturnsCorrectString()`

Validates that the `ToString()` method of the `RetryPolicyType` enum returns the correct string representation for each value. Ensures human-readable output matches the project's requirements.

**Parameters:** None
**Return value:** None
**Throws:** Throws if any `ToString()` output does not match the expected string.

---
### `public void OutboxMessageState_HasExpectedDescriptionAttributes()`

Validates that the `OutboxMessageState` enum has the expected `[Description]` attributes applied to its values. Ensures descriptive metadata is correctly defined for each enum member.

**Parameters:** None
**Return value:** None
**Throws:** Throws if any value lacks the expected `[Description]` attribute or if the attribute value does not match the expected description.

---
### `public void EventType_HasExpectedValues()`

Validates that the `EventType` enum has the expected values and that they are correctly ordered or scoped as required by the project. Ensures the enum reflects the project's event taxonomy.

**Parameters:** None
**Return value:** None
**Throws:** Throws if the enum does not contain the expected values or if their ordering is incorrect.

## Usage

### Example 1: Validating Enum Values
