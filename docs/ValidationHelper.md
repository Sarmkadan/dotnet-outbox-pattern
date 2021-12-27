# ValidationHelper

A utility class providing static methods and fluent-style extension methods for validating arguments and conditions in .NET applications. It supports common validation scenarios such as null checks, empty checks, range validation, length constraints, and custom predicate-based validation. The API is designed for both immediate validation with exceptions and fluent chaining to build validation contexts before throwing.

## API

### `public static void ValidateNotEmpty(string value, string paramName)`

Validates that the provided string is not null or empty. Throws an `ArgumentException` if the string is null or empty.

- **Parameters**:
  - `value`: The string to validate.
  - `paramName`: The name of the parameter being validated, used in the exception message.
- **Throws**: `ArgumentException` if `value` is null or empty.

---

### `public static void ValidateNotNull<T>(T value, string paramName)`

Validates that the provided value is not null. Throws an `ArgumentNullException` if the value is null.

- **Parameters**:
  - `value`: The value to validate.
  - `paramName`: The name of the parameter being validated, used in the exception message.
- **Throws**: `ArgumentNullException` if `value` is null.

---

### `public static void ValidatePositive(long value, string paramName)`

Validates that the provided long value is positive (greater than zero). Throws an `ArgumentOutOfRangeException` if the value is less than or equal to zero.

- **Parameters**:
  - `value`: The value to validate.
  - `paramName`: The name of the parameter being validated, used in the exception message.
- **Throws**: `ArgumentOutOfRangeException` if `value` <= 0.

---

### `public static void ValidateRange(long value, long min, long max, string paramName)`

Validates that the provided long value lies within the specified inclusive range. Throws an `ArgumentOutOfRangeException` if the value is outside the range.

- **Parameters**:
  - `value`: The value to validate.
  - `min`: The minimum allowed value (inclusive).
  - `max`: The maximum allowed value (inclusive).
  - `paramName`: The name of the parameter being validated, used in the exception message.
- **Throws**: `ArgumentOutOfRangeException` if `value` < `min` or `value` > `max`.

---
### `public static void ValidateLength(string value, int minLength, int maxLength, string paramName)`

Validates that the length of the provided string lies within the specified inclusive range. Throws an `ArgumentException` if the string is null or its length is outside the range.

- **Parameters**:
  - `value`: The string whose length is to be validated.
  - `minLength`: The minimum allowed length (inclusive).
  - `maxLength`: The maximum allowed length (inclusive).
  - `paramName`: The name of the parameter being validated, used in the exception message.
- **Throws**: `ArgumentException` if `value` is null or `value.Length` < `minLength` or `value.Length` > `maxLength`.

---
### `public static void ValidateAny<T>(IEnumerable<T> values, Func<T, bool> predicate, string paramName)`

Validates that at least one element in the provided collection satisfies the given predicate. Throws an `ArgumentException` if no element satisfies the predicate or if the collection is null.

- **Parameters**:
  - `values`: The collection to validate.
  - `predicate`: The function to test each element.
  - `paramName`: The name of the parameter being validated, used in the exception message.
- **Throws**: `ArgumentException` if `values` is null or no element satisfies `predicate`.

---
### `public static void ValidateAll<T>(IEnumerable<T> values, Func<T, bool> predicate, string paramName)`

Validates that every element in the provided collection satisfies the given predicate. Throws an `ArgumentException` if any element fails the predicate or if the collection is null.

- **Parameters**:
  - `values`: The collection to validate.
  - `predicate`: The function to test each element.
  - `paramName`: The name of the parameter being validated, used in the exception message.
- **Throws**: `ArgumentException` if `values` is null or any element fails `predicate`.

---
### `public static void ValidateEqual<T>(T actual, T expected, string paramName)`

Validates that the provided actual value equals the expected value. Throws an `ArgumentException` if they are not equal.

- **Parameters**:
  - `actual`: The value to validate.
  - `expected`: The expected value.
  - `paramName`: The name of the parameter being validated, used in the exception message.
- **Throws**: `ArgumentException` if `actual` does not equal `expected`.

---
### `public static void ValidateCondition(bool condition, string paramName, string message)`

Validates that the provided condition is true. Throws an `ArgumentException` with the specified message if the condition is false.

- **Parameters**:
  - `condition`: The condition to validate.
  - `paramName`: The name of the parameter being validated, used in the exception message.
  - `message`: The message to include in the exception if the condition is false.
- **Throws**: `ArgumentException` if `condition` is false.

---
### `public static ValidationContext<T> Validate<T>(T value, string paramName)`

Begins a fluent validation context for the given value and parameter name. This context allows chaining of validation rules before throwing.

- **Parameters**:
  - `value`: The value to validate.
  - `paramName`: The name of the parameter being validated.
- **Returns**: A `ValidationContext<T>` instance for fluent validation.

---
### `public ValidationContext`

Base context for fluent validation. Provides common validation continuation methods.

---
### `public ValidationContext<T> NotNull`

Extends the current validation context to assert that the value is not null. Returns a new context for further chaining.

- **Returns**: A `ValidationContext<T>` for fluent continuation.

---
### `public ValidationContext<T> NotEmpty`

Extends the current validation context to assert that the string value is not null or empty. Returns a new context for further chaining.

- **Returns**: A `ValidationContext<T>` for fluent continuation.

---
### `public ValidationContext<T> MinLength(int minLength)`

Extends the current validation context to assert that the string value has a minimum length. Returns a new context for further chaining.

- **Parameters**:
  - `minLength`: The minimum allowed length (inclusive).
- **Returns**: A `ValidationContext<T>` for fluent continuation.

---
### `public ValidationContext<T> MaxLength(int maxLength)`

Extends the current validation context to assert that the string value has a maximum length. Returns a new context for further chaining.

- **Parameters**:
  - `maxLength`: The maximum allowed length (inclusive).
- **Returns**: A `ValidationContext<T>` for fluent continuation.

---
### `public ValidationContext<T> Condition(Func<T, bool> predicate, string message)`

Extends the current validation context to assert that the value satisfies the given predicate. Returns a new context for further chaining.

- **Parameters**:
  - `predicate`: The function to test the value.
  - `message`: The message to include in the exception if the predicate returns false.
- **Returns**: A `ValidationContext<T>` for fluent continuation.

---
### `public void ThrowIfInvalid()`

Throws an `InvalidOperationException` if any validation rule in the context has failed. This finalizes the validation chain and raises an exception if applicable.

- **Throws**: `InvalidOperationException` if any validation rule failed.

## Usage
