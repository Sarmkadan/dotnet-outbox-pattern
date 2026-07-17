# HealthCheckServiceValidation

Static helper class that provides validation methods for health check related types in the DotnetOutboxPattern.BackgroundServices namespace. It offers three validation patterns for each supported type: a method that returns a list of error messages, a boolean method that indicates whether validation passed, and a method that throws an exception when validation fails.

## API

### Validate(HealthAlert) → IReadOnlyList<string>

Validates a `HealthAlert` instance and returns a read-only list of error messages.

- **Parameters:**
  - `value` – The `HealthAlert` instance to validate. Must not be null.
- **Returns:**
  - An `IReadOnlyList<string>` containing zero or more validation error messages. If the list is empty, the instance is valid.
- **Exceptions:**
  - Throws `ArgumentNullException` if `value` is null.

### IsValid(HealthAlert) → bool

Determines whether a `HealthAlert` instance is valid.

- **Parameters:**
  - `value` – The `HealthAlert` instance to validate. Must not be null.
- **Returns:**
  - `true` if the instance is valid; otherwise `false`.
- **Exceptions:**
  - Throws `ArgumentNullException` if `value` is null.

### EnsureValid(HealthAlert)

Ensures a `HealthAlert` instance is valid, throwing an exception if it is not.

- **Parameters:**
  - `value` – The `HealthAlert` instance to validate. Must not be null.
- **Exceptions:**
  - Throws `ArgumentNullException` if `value` is null.
  - Throws `ArgumentException` with a detailed message listing every validation failure if the instance is invalid.

### Validate(HealthCheckOptions) → IReadOnlyList<string>

Validates a `HealthCheckOptions` instance and returns a read-only list of error messages.

- **Parameters:**
  - `value` – The `HealthCheckOptions` instance to validate. Must not be null.
- **Returns:**
  - An `IReadOnlyList<string>` containing zero or more validation error messages. If the list is empty, the instance is valid.
- **Exceptions:**
  - Throws `ArgumentNullException` if `value` is null.

### IsValid(HealthCheckOptions) → bool

Determines whether a `HealthCheckOptions` instance is valid.

- **Parameters:**
  - `value` – The `HealthCheckOptions` instance to validate. Must not be null.
- **Returns:**
  - `true` if the instance is valid; otherwise `false`.
- **Exceptions:**
  - Throws `ArgumentNullException` if `value` is null.

### EnsureValid(HealthCheckOptions)

Ensures a `HealthCheckOptions` instance is valid, throwing an exception if it is not.

- **Parameters:**
  - `value` – The `HealthCheckOptions` instance to validate. Must not be null.
- **Exceptions:**
  - Throws `ArgumentNullException` if `value` is null.
  - Throws `ArgumentException` with a detailed message listing every validation failure if the instance is invalid.

## Usage

```csharp
// Example 1: Guard clause pattern with EnsureValid
var options = new HealthCheckOptions
{
    CheckIntervalMs = 5000,
    HighFailureRateThreshold = 0.8,
    StuckMessageThreshold = 300,
    DeadLetterThreshold = 10
};

HealthCheckServiceValidation.EnsureValid(options);

// If we reach here, options is guaranteed to be valid
Console.WriteLine("Health check options are valid");
```

```csharp
// Example 2: Collect all validation errors before failing
var alert = new HealthAlert
{
    Type = "OutboxFailure",
    Message = "",
    RaisedAt = default
};

var errors = HealthCheckServiceValidation.Validate(alert);
if (errors.Count > 0)
{
    Console.Error.WriteLine("Alert is invalid:");
    foreach (var error in errors)
    {
        Console.Error.WriteLine($"- {error}");
    }
    return;
}

// Proceed only when alert is valid
Console.WriteLine("Alert is valid");
```

## Notes

- All public methods are static and thread-safe; concurrent calls do not require synchronization.
- Each method validates its argument for null and throws `ArgumentNullException` before performing any other checks.
- The `Validate` methods allocate a new list on every call; prefer `IsValid` or `EnsureValid` when only a boolean result is needed.
- The `EnsureValid` methods throw `ArgumentException` with a multi-line message that lists every validation failure separated by newlines, making it easy to diagnose configuration issues during startup.
- Validation rules are intentionally strict: `HealthAlert.Type` and `HealthAlert.Message` must be non-empty, and `HealthAlert.RaisedAt` must not be the default `DateTime` value.