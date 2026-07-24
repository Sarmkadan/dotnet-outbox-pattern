# Argument Validation Contract

This document describes the standardized argument validation contract for extension methods in the `DotnetOutboxPattern.Infrastructure` namespace.

## Contract Rules

All public extension methods must follow these validation rules:

### 1. Validate the 'this' parameter

Every extension method must validate its extended instance parameter (the `this` parameter) using `ArgumentNullException.ThrowIfNull()`:

```csharp
public static ReturnType MethodName(this MessageContext context, ...)
{
    ArgumentNullException.ThrowIfNull(context);
    // ... rest of method
}
```

### 2. Validate all reference-type parameters

All reference-type parameters must be validated:
- Use `ArgumentNullException.ThrowIfNull()` for parameters that cannot be null
- Use `ArgumentException.ThrowIfNullOrEmpty()` for string parameters that cannot be empty
- Use appropriate validation for other types (e.g., `ArgumentOutOfRangeException` for numeric ranges)

### 3. Use consistent exception types

For the same category of invalid input (e.g., missing required field), use the same exception type:
- **Missing required parameter**: `ArgumentNullException`
- **Invalid string value**: `ArgumentException` with `ThrowIfNullOrEmpty()`
- **Out of range values**: `ArgumentOutOfRangeException`

### 4. Document exceptions in XML comments

Every exception that can be thrown must be documented in the method's XML documentation:

```xml
/// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
```

## Examples

### MessageContextExtensions

All methods in `MessageContextExtensions` follow this contract:

```csharp
public static ActivityScope StartServiceActivity(this MessageContext context, string serviceName, string operationName)
{
    ArgumentNullException.ThrowIfNull(context);
    ArgumentException.ThrowIfNullOrEmpty(serviceName);
    ArgumentException.ThrowIfNullOrEmpty(operationName);
    // ...
}
```

### OutboxBackoffExtensions

All methods in `OutboxBackoffExtensions` follow this contract:

```csharp
public static OutboxProcessorOptions WithBatchSize(this OutboxProcessorOptions options, int batchSize)
{
    ArgumentNullException.ThrowIfNull(options);
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);
    // ...
}
```

## Rationale

This standardization ensures:
- **Consistency**: All extension methods behave predictably
- **Defensive programming**: Fail fast with clear error messages
- **Testability**: Consistent behavior makes testing easier
- **Maintainability**: Clear contract makes code easier to understand and modify
- **User experience**: Clear error messages help developers use the API correctly

## Migration

When adding new extension methods or modifying existing ones:
1. Add `ArgumentNullException.ThrowIfNull()` for the `this` parameter at the start
2. Validate all other parameters according to their requirements
3. Update XML documentation to include all exceptions
4. Ensure exception types are consistent with similar methods

