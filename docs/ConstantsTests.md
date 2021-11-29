# ConstantsTests

`ConstantsTests` is a unit-test fixture that validates the correctness and consistency of compile-time constant definitions used throughout the outbox-pattern implementation. It ensures that default values, topic names, log property keys, error codes, HTTP header names, and validation thresholds declared in the `OutboxConstants` class match their expected literal values and remain within reasonable bounds.

## API

### `public void OutboxConstants_DefaultValues_MatchExpected`
Verifies that the default constant values exposed by `OutboxConstants` (e.g., polling intervals, batch sizes, retention periods) equal the hard-coded expected literals.  
**Parameters:** none (test method).  
**Return value:** `void`.  
**Throws:** assertion failures via the test framework when any constant deviates from its expected value.

### `public void StandardTopics_ContainsExpectedTopicNames`
Confirms that the collection of standard outbox message topics contains exactly the required set of topic name strings, with no missing or extraneous entries.  
**Parameters:** none.  
**Return value:** `void`.  
**Throws:** assertion failures if the topic set is missing an expected name or includes an unexpected one.

### `public void LogProperties_ContainsExpectedPropertyNames`
Checks that the dictionary or set of structured-logging property keys defined for outbox operations includes every required property name.  
**Parameters:** none.  
**Return value:** `void`.  
**Throws:** assertion failures when a mandatory log property key is absent or an unknown key is present.

### `public void ErrorCodes_ContainsExpectedErrorCodes`
Ensures the error-code constants (e.g., strings or integers representing failure reasons) match the canonical list of expected error codes.  
**Parameters:** none.  
**Return value:** `void`.  
**Throws:** assertion failures if any expected error code is missing or an unexpected code is found.

### `public void HttpHeaders_ContainsExpectedHeaderNames`
Validates that the HTTP header name constants used for outbox-related HTTP communication contain the precise set of required header names.  
**Parameters:** none.  
**Return value:** `void`.  
**Throws:** assertion failures when a required header name is omitted or an extra name is present.

### `public void OutboxConstants_ValidationConstants_AreReasonable`
Asserts that validation-related constants (e.g., maximum retry counts, timeout floors/ceilings, payload size limits) fall within reasonable, pre-defined numeric ranges.  
**Parameters:** none.  
**Return value:** `void`.  
**Throws:** assertion failures if any validation constant lies outside its acceptable range.

## Usage

```csharp
// Running all ConstantsTests together in a CI pipeline
[Test]
public void Run_All_ConstantsTests()
{
    var fixture = new ConstantsTests();

    fixture.OutboxConstants_DefaultValues_MatchExpected();
    fixture.StandardTopics_ContainsExpectedTopicNames();
    fixture.LogProperties_ContainsExpectedPropertyNames();
    fixture.ErrorCodes_ContainsExpectedErrorCodes();
    fixture.HttpHeaders_ContainsExpectedHeaderNames();
    fixture.OutboxConstants_ValidationConstants_AreReasonable();
}
```

```csharp
// Selective re-validation after a constants file change
[Test]
public void Verify_Topics_And_ErrorCodes_After_Update()
{
    var fixture = new ConstantsTests();

    // Only re-run the tests affected by the recent constants revision
    fixture.StandardTopics_ContainsExpectedTopicNames();
    fixture.ErrorCodes_ContainsExpectedErrorCodes();
}
```

## Notes

- All methods are pure assertion checks with no side effects; they are safe to execute in any order and repeatedly.
- The tests assume the constants under inspection are compile-time literals or immutable static readonly fields. If any constant is changed to a runtime-computed value, the corresponding test will break and must be updated.
- Thread-safety is not a concern for this class itself, as each method reads immutable static state and does not mutate shared data. Concurrent test runners may execute all methods in parallel without interference.
- Edge cases: if a constant collection is empty by design, the corresponding test must explicitly expect an empty set; otherwise the test will fail. Validation-range tests (`AreReasonable`) must be adjusted whenever business rules change the acceptable bounds.
