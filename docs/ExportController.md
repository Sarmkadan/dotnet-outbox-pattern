# ExportController

Manages the export of outbox messages into various file formats. It provides endpoints to retrieve supported formats, inspect format-specific metadata, and perform the actual message export operation. The controller exposes format configuration properties that govern how exports are produced, including content types, file extensions, and capacity limits.

## API

### Public Members

#### `ExportController`

Constructor for the controller. Initializes the export service dependencies and format configuration.

#### `public async Task<IActionResult> ExportMessagesAsync`

Executes an asynchronous export of outbox messages based on the current request parameters. Accepts filter criteria and format selection from the request context. Returns an `IActionResult` that typically represents a file download response containing the exported data. Throws when the requested format is not supported or when the underlying export service encounters a processing failure.

#### `public IActionResult GetSupportedFormatsAsync`

Returns a synchronous result containing the list of all supported export formats. The response includes format identifiers, display names, and associated metadata. Does not accept parameters. Throws if format configuration retrieval fails.

#### `public IActionResult GetFormatDetailsAsync`

Provides detailed information about a specific export format. The format identifier is expected to be supplied via the request route or query string. Returns format metadata including content type, file extension, and description. Throws when the specified format is not found in the supported formats collection.

#### `public IActionResult GetExportInfoAsync`

Returns general export capability information, including the maximum number of messages allowed per export operation, the default format, and the list of fields available for filtering. Does not accept parameters. Throws if configuration data is unavailable.

#### `public string Format`

Gets or sets the currently selected export format identifier. This value determines the output format used by `ExportMessagesAsync` when no explicit format is specified in the request.

#### `public string ContentType`

Gets the MIME content type associated with the currently selected format. Used to set the `Content-Type` header on export responses.

#### `public string Extension`

Gets the file extension (including the leading dot) for the currently selected format. Applied when generating the download filename.

#### `public string Description`

Gets a human-readable description of the currently selected export format, suitable for display in user interfaces.

#### `public int MaxMessagesPerExport`

Gets the maximum number of outbox messages that can be included in a single export operation. Requests exceeding this limit are rejected.

#### `public List<string> SupportedFormats`

Gets the complete list of format identifiers that the controller can produce. Each string corresponds to a valid value for the `Format` property.

#### `public string DefaultFormat`

Gets the format identifier used when no explicit format selection is made. This value is always present in `SupportedFormats`.

#### `public string[] FilterableFields`

Gets the array of field names that can be used as filter criteria when calling `ExportMessagesAsync`. These correspond to properties on outbox message entities.

## Usage

### Example 1: Exporting Messages with Explicit Format Selection

```csharp
// Assume controller is injected via dependency injection
public async Task<IActionResult> DownloadOutboxExport(
    ExportController exportController,
    DateTime fromDate,
    string messageType)
{
    // Set the desired format before export
    exportController.Format = "json";

    // Verify the format is supported
    if (!exportController.SupportedFormats.Contains(exportController.Format))
    {
        return new BadRequestObjectResult("Unsupported format requested.");
    }

    // Check message count constraints
    var estimatedCount = await GetMessageCountAsync(fromDate, messageType);
    if (estimatedCount > exportController.MaxMessagesPerExport)
    {
        return new BadRequestObjectResult(
            $"Export exceeds maximum of {exportController.MaxMessagesPerExport} messages.");
    }

    // Perform the export
    var result = await exportController.ExportMessagesAsync();

    // The result is a FileResult with ContentType and Extension already set
    return result;
}
```

### Example 2: Inspecting Format Capabilities Before Export

```csharp
public IActionResult BuildExportConfigurationResponse(ExportController exportController)
{
    var exportInfo = exportController.GetExportInfoAsync();
    var formats = exportController.GetSupportedFormatsAsync();

    var configuration = new
    {
        DefaultFormat = exportController.DefaultFormat,
        MaxMessages = exportController.MaxMessagesPerExport,
        FilterableFields = exportController.FilterableFields,
        AvailableFormats = exportController.SupportedFormats.Select(f =>
        {
            exportController.Format = f;
            return new
            {
                Id = f,
                ContentType = exportController.ContentType,
                Extension = exportController.Extension,
                Description = exportController.Description
            };
        }).ToList()
    };

    return new OkObjectResult(configuration);
}
```

## Notes

- The `Format` property is mutable and affects the behavior of subsequent calls to `ExportMessagesAsync`. Changing `Format` also updates `ContentType`, `Extension`, and `Description` to reflect the newly selected format. In multi-threaded environments, this mutable state means the controller instance should not be shared across concurrent requests without synchronization.
- `ExportMessagesAsync` reads filter parameters from the current HTTP request context. It does not accept them as method arguments, so callers must ensure the request is properly populated before invocation.
- `MaxMessagesPerExport` is a hard limit. The export operation will reject requests that would produce output exceeding this count. Callers should implement client-side validation using this property to provide early feedback.
- `FilterableFields` represents the complete set of supported filter dimensions. Providing filter criteria for fields not in this array may result in ignored filters or validation errors, depending on the underlying service implementation.
- `GetFormatDetailsAsync` expects a format identifier from the request context. If the route or query string does not supply one, or supplies an unrecognized value, the method throws. Callers should validate against `SupportedFormats` before invoking this endpoint.
- The `SupportedFormats` list and `DefaultFormat` are treated as immutable after controller construction. Modifying the list contents at runtime is not supported and may lead to inconsistent state.
