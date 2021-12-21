# IExportService

`IExportService` provides a contract for exporting outbox messages into a structured, serialized representation. It abstracts the mechanics of collecting pending outbox records, transforming them into a chosen format, and delivering the result either as an in-memory payload or as a persistent file. Implementations are expected to handle format-specific serialization, content-type negotiation, and accurate metadata tracking such as message count, byte size, and export timestamp.

## API

### Properties

- **`string Content`**  
  Gets the serialized payload produced by the most recent export operation. The value is `null` or empty until `ExportAsync` completes successfully. The exact structure depends on the selected format.

- **`string Format`**  
  Gets the format identifier used for the export (e.g., `"json"`, `"xml"`, `"csv"`). This value is set before calling `ExportAsync` and determines how `Content` is serialized.

- **`string ContentType`**  
  Gets the MIME content type associated with the exported format (e.g., `"application/json"`). This is derived from the chosen format and is intended for use in HTTP responses or file metadata.

- **`int MessageCount`**  
  Gets the number of outbox messages included in the export. This value is populated after a successful `ExportAsync` call and reflects the actual records serialized.

- **`DateTime ExportedAt`**  
  Gets the UTC timestamp at which the export operation completed. Set immediately after `ExportAsync` finishes, regardless of success or failure.

- **`long ContentSizeBytes`**  
  Gets the size of the serialized `Content` in bytes. This is measured after serialization and can be used for logging, quotas, or Content-Length headers.

### Constructors

- **`ExportService`**  
  The concrete implementation constructor. It is not defined on the interface itself but is listed here as the primary instantiable type. It typically accepts dependencies such as an outbox repository and format serializers.

### Methods

- **`async Task<ExportResult> ExportAsync()`**  
  Executes the export operation asynchronously. It queries pending outbox messages, serializes them according to the configured format, and populates all metadata properties (`Content`, `MessageCount`, `ExportedAt`, `ContentSizeBytes`).  
  **Returns:** An `ExportResult` indicating success or failure, along with any error details.  
  **Throws:** `InvalidOperationException` when no format has been specified prior to calling; `OutboxRepositoryException` (or a derived persistence exception) when the underlying message store is unavailable.

- **`async Task<string> ExportToFileAsync()`**  
  Performs the same export logic as `ExportAsync` but additionally persists the serialized content to a file. The file path is determined by the implementation (often a configured export directory combined with a timestamped filename).  
  **Returns:** The absolute file path where the export was written.  
  **Throws:** `InvalidOperationException` when no format is set; `IOException` or `UnauthorizedAccessException` when file writing fails due to permissions or disk issues; same persistence exceptions as `ExportAsync`.

- **`List<string> GetSupportedFormats()`**  
  Returns the list of format identifiers that the implementation can produce. Typical values include `"json"`, `"xml"`, and `"csv"`. This allows callers to validate format selection before invoking an export operation.  
  **Returns:** A non-null list of supported format strings. An empty list indicates no formats are available.

## Usage

### Example 1: In-Memory JSON Export

```csharp
IExportService exportService = new ExportService(outboxRepository, serializers);

// Validate and set format
var supported = exportService.GetSupportedFormats();
if (!supported.Contains("json"))
    throw new NotSupportedException("JSON export is not available.");

exportService.Format = "json";

ExportResult result = await exportService.ExportAsync();
if (result.Succeeded)
{
    Console.WriteLine($"Exported {exportService.MessageCount} messages " +
                      $"({exportService.ContentSizeBytes} bytes) at {exportService.ExportedAt:O}.");
    Console.WriteLine(exportService.Content);
}
else
{
    Console.WriteLine($"Export failed: {result.Error}");
}
```

### Example 2: File-Based XML Export with Metadata Forwarding

```csharp
IExportService exportService = new ExportService(outboxRepository, serializers);
exportService.Format = "xml";

string filePath;
try
{
    filePath = await exportService.ExportToFileAsync();
}
catch (IOException ex)
{
    // Log and handle disk-full or permission errors
    throw;
}

// Use metadata for an HTTP response or audit record
var response = new
{
    FilePath = filePath,
    Format = exportService.Format,
    ContentType = exportService.ContentType,
    MessageCount = exportService.MessageCount,
    SizeBytes = exportService.ContentSizeBytes,
    ExportedAt = exportService.ExportedAt
};

Console.WriteLine($"Export saved to {filePath}");
```

## Notes

- **Format must be set before export:** Both `ExportAsync` and `ExportToFileAsync` throw `InvalidOperationException` if `Format` is `null` or empty. Always validate with `GetSupportedFormats()` or set a known-good value beforehand.
- **Metadata is overwritten on each call:** Properties such as `Content`, `MessageCount`, `ExportedAt`, and `ContentSizeBytes` reflect only the most recent export. If you need to retain historical values, capture them immediately after the call completes.
- **Thread safety:** The interface is not designed for concurrent use on the same instance. Parallel export operations require separate instances or external synchronization. Calling `ExportAsync` or `ExportToFileAsync` while another export is in flight on the same instance leads to unpredictable metadata state.
- **Empty outbox handling:** When no pending messages exist, `ExportAsync` succeeds with `MessageCount = 0` and `Content` representing an empty collection in the chosen format (e.g., `[]` for JSON). `ContentSizeBytes` will reflect the size of that empty structure, not zero.
- **File overwrite behavior:** `ExportToFileAsync` typically generates a unique filename (often timestamped). If the implementation uses a fixed name, subsequent calls overwrite the previous file without warning.
- **Persistence failures:** Both export methods can fail due to underlying data store unavailability. These exceptions surface before any file I/O occurs, so a failed `ExportToFileAsync` call will not leave a partial or empty file on disk.
