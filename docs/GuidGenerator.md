# GuidGenerator

The `GuidGenerator` static class provides a set of utility methods for creating, parsing, and validating GUIDs, as well as generating common identifier strings used in outbox messaging patterns. It centralizes GUID-related operations to ensure consistent formatting, sequential ordering when needed, and thread-safe generation of correlation IDs, request IDs, and idempotency keys.

## API

### `public static Guid NewGuid`
Returns a new random GUID.  
**Parameters:** None.  
**Returns:** A new `Guid` value.  
**Throws:** Never.

### `public static Guid FromString(string input)`
Parses a string representation of a GUID into a `Guid` value.  
**Parameters:** `input` – The string to parse. Must be a valid GUID format (e.g., "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx").  
**Returns:** A `Guid` parsed from the input.  
**Throws:** `ArgumentNullException` if `input` is `null`; `FormatException` if the string is not a valid GUID.

### `public static Guid FromComponents(int a, short b, short c, byte[] d)`
Creates a GUID from its integer, short, and byte components.  
**Parameters:**  
- `a` – The first 4 bytes as an integer.  
- `b` – The next 2 bytes as a short.  
- `c` – The next 2 bytes as a short.  
- `d` – An 8-byte array for the final 8 bytes.  
**Returns:** A `Guid` constructed from the provided components.  
**Throws:** `ArgumentNullException` if `d` is `null`; `ArgumentException` if `d` does not have exactly 8 elements.

### `public static bool IsValid(string input)`
Determines whether a string is a valid GUID representation.  
**Parameters:** `input` – The string to validate.  
**Returns:** `true` if the string is a valid GUID; otherwise `false`.  
**Throws:** Never (returns `false` for `null` or empty input).

### `public static Guid Parse(string input)`
Parses a string representation of a GUID into a `Guid` value. This method behaves identically to `FromString`.  
**Parameters:** `input` – The string to parse.  
**Returns:** A `Guid` parsed from the input.  
**Throws:** `ArgumentNullException` if `input` is `null`; `FormatException` if the string is not a valid GUID.

### `public static Guid NewSequentialId`
Generates a sequential GUID that can improve index performance in databases. The generated value is unique and monotonically increasing over time.  
**Parameters:** None.  
**Returns:** A new `Guid` with a sequential component.  
**Throws:** Never.

### `public static string GenerateCorrelationId()`
Generates a string suitable for use as a correlation identifier in distributed tracing. The returned value is a GUID formatted as a lowercase hexadecimal string with hyphens.  
**Parameters:** None.  
**Returns:** A 36-character string (e.g., "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx").  
**Throws:** Never.

### `public static string GenerateRequestId()`
Generates a string suitable for use as a request identifier. The returned value is a GUID formatted as a lowercase hexadecimal string with hyphens.  
**Parameters:** None.  
**Returns:** A 36-character string.  
**Throws:** Never.

### `public static string GenerateIdempotencyKey()`
Generates a string suitable for use as an idempotency key. The returned value is a GUID formatted as a lowercase hexadecimal string with hyphens.  
**Parameters:** None.  
**Returns:** A 36-character string.  
**Throws:** Never.

## Usage

### Example 1: Generating identifiers for an outbox message

```csharp
using OutboxPattern;

public class OrderService
{
    public async Task PlaceOrder(Order order)
    {
        var correlationId = GuidGenerator.GenerateCorrelationId();
        var idempotencyKey = GuidGenerator.GenerateIdempotencyKey();
        var messageId = GuidGenerator.NewGuid();

        var outboxMessage = new OutboxMessage
        {
            Id = messageId,
            CorrelationId = correlationId,
            IdempotencyKey = idempotencyKey,
            Payload = Serialize(order)
        };

        await SaveOutboxMessageAsync(outboxMessage);
    }
}
```

### Example 2: Parsing and validating GUIDs from external input

```csharp
using OutboxPattern;

public class RequestHandler
{
    public void ProcessRequest(string requestIdHeader)
    {
        if (!GuidGenerator.IsValid(requestIdHeader))
        {
            throw new InvalidOperationException("Invalid request ID format.");
        }

        var requestId = GuidGenerator.Parse(requestIdHeader);
        var sequentialId = GuidGenerator.NewSequentialId();

        // Use requestId and sequentialId for database operations
    }
}
```

## Notes

- All methods are thread-safe. `NewSequentialId` uses internal synchronization to guarantee uniqueness and ordering across concurrent calls.
- `FromString` and `Parse` are functionally identical; both throw `FormatException` on invalid input. Use `IsValid` to pre-validate strings when exceptions are undesirable.
- `FromComponents` requires the byte array `d` to be exactly 8 elements; passing a different length throws `ArgumentException`.
- The string generators (`GenerateCorrelationId`, `GenerateRequestId`, `GenerateIdempotencyKey`) all return the same format (lowercase GUID with hyphens). They are provided as separate methods for semantic clarity in different contexts.
- `NewSequentialId` is not guaranteed to be globally unique if the system clock is reset or if the process restarts; it is suitable for single‑node scenarios where index performance is critical.
