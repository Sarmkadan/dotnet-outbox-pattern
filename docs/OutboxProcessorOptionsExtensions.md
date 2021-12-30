# OutboxProcessorOptionsExtensions

The `OutboxProcessorOptionsExtensions` static class provides a set of extension methods for the `OutboxProcessorOptions` type. These methods allow you to fluently configure an `OutboxProcessorOptions` instance by enabling or disabling processing, configuring batch behavior, and enabling validation. Each method returns a new `OutboxProcessorOptions` instance with the specified setting applied, leaving the original instance unchanged.

## API

### `Enable`
```csharp
public static OutboxProcessorOptions Enable(this OutboxProcessorOptions options)
```
Returns a new `OutboxProcessorOptions` instance with processing enabled.  
**Parameters:**  
- `options` – The source `OutboxProcessorOptions` instance.  

**Returns:** A new `OutboxProcessorOptions` with the processing flag set to enabled.  

**Throws:** `ArgumentNullException` if `options` is `null`.

### `Disable`
```csharp
public static OutboxProcessorOptions Disable(this OutboxProcessorOptions options)
```
Returns a new `OutboxProcessorOptions` instance with processing disabled.  
**Parameters:**  
- `options` – The source `OutboxProcessorOptions` instance.  

**Returns:** A new `OutboxProcessorOptions` with the processing flag set to disabled.  

**Throws:** `ArgumentNullException` if `options` is `null`.

### `ConfigureBatch`
```csharp
public static OutboxProcessorOptions ConfigureBatch(this OutboxProcessorOptions options, ...)
```
Returns a new `OutboxProcessorOptions` instance with batch processing configured according to the provided parameters. The exact parameters depend on the implementation (e.g., batch size, interval, or a configuration delegate).  
**Parameters:**  
- `options` – The source `OutboxProcessorOptions` instance.  
- Additional parameters as defined by the overload.  

**Returns:** A new `OutboxProcessorOptions` with batch settings applied.  

**Throws:** `ArgumentNullException` if `options` is `null`; may throw `ArgumentException` for invalid batch configuration values.

### `Validate`
```csharp
public static OutboxProcessorOptions Validate(this OutboxProcessorOptions options, ...)
```
Returns a new `OutboxProcessorOptions` instance with validation enabled. The method accepts parameters that define the validation behavior (e.g., a validation delegate or validation options).  
**Parameters:**  
- `options` – The source `OutboxProcessorOptions` instance.  
- Additional parameters as defined by the overload.  

**Returns:** A new `OutboxProcessorOptions` with validation enabled.  

**Throws:** `ArgumentNullException` if `options` is `null`; may throw `ArgumentException` for invalid validation parameters.

## Usage

### Example 1: Enabling processing with batch configuration
```csharp
using OutboxPattern;

var options = new OutboxProcessorOptions()
    .Enable()
    .ConfigureBatch(batchSize: 50, interval: TimeSpan.FromSeconds(5));

// options now represents an enabled processor that processes
// up to 50 messages every 5 seconds.
```

### Example 2: Disabling processing and enabling validation
```csharp
using OutboxPattern;

var options = new OutboxProcessorOptions()
    .Disable()
    .Validate(validationAction: msg => msg.IsValid());

// options represents a disabled processor that will still
// validate outgoing messages when processing is later enabled.
```

## Notes

- All methods are **thread-safe** because they do not modify the original `OutboxProcessorOptions` instance; they return a new instance with the requested settings.  
- If the same `OutboxProcessorOptions` instance is shared across multiple threads, each thread can safely call these extension methods without synchronization.  
- The `ConfigureBatch` and `Validate` methods may have multiple overloads; refer to the IntelliSense documentation or source code for the exact parameter signatures.  
- Passing a `null` `options` argument to any of these methods will throw an `ArgumentNullException`.  
- The returned `OutboxProcessorOptions` instances are immutable with respect to the settings applied by these methods; however, the underlying options class may expose mutable properties that should not be modified after configuration.
