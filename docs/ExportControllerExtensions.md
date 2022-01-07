# ExportControllerExtensions

The `ExportControllerExtensions` class is a static utility providing extension methods for ASP.NET Core `ControllerBase` to streamline the export of outbox messages. It encapsulates logic for request creation, format validation, and the execution of asynchronous export operations into various data formats.

## API

### CreateExportRequest
Constructs an `ExportRequest` object based on incoming HTTP request parameters.
*   **Parameters:** `ControllerBase` controller, `ExportRequestOptions` options.
*   **Returns:** An initialized `ExportRequest` instance.

### ExportMessagesAsync
Executes the asynchronous export process for messages based on the provided request parameters.
*   **Parameters:** `ControllerBase` controller, `ExportRequest` request, `CancellationToken` cancellationToken.
*   **Returns:** A `Task<IActionResult>` representing the outcome of the export operation.
*   **Throws:** `OperationCanceledException` if the token is triggered.

### GetFormatDetails
Retrieves configuration details for a specific export format.
*   **Parameters:** `string` format.
*   **Returns:** An `IActionResult` containing the format specification.

### GetExportInfo
Retrieves metadata regarding available export configurations.
*   **Parameters:** `ControllerBase` controller, `ExportRequest` request.
*   **Returns:** An `IActionResult` containing export information.

### IsFormatSupported
Validates if the requested format is supported by the system.
*   **Parameters:** `string` format.
*   **Returns:** `bool` indicating support status.

### GetSupportedFormatsJson
Returns a JSON representation of all supported export formats.
*   **Returns:** A `string` containing the supported formats in JSON format.

### ExportJsonAsync
Handles the asynchronous export of messages to JSON format.
*   **Parameters:** `ControllerBase` controller, `ExportRequest` request, `CancellationToken` cancellationToken.
*   **Returns:** A `Task<IActionResult>` resulting in a JSON response.

### ExportCsvAsync
Handles the asynchronous export of messages to CSV format.
*   **Parameters:** `ControllerBase` controller, `ExportRequest` request, `CancellationToken` cancellationToken.
*   **Returns:** A `Task<IActionResult>` resulting in a CSV file stream response.

## Usage

### Exporting to JSON in a Controller Action

```csharp
[HttpGet("export/json")]
public async Task<IActionResult> ExportJson(ExportRequestOptions options, CancellationToken ct)
{
    var request = this.CreateExportRequest(options);
    return await this.ExportJsonAsync(request, ct);
}
```

### Validating Export Formats

```csharp
[HttpGet("formats/check")]
public IActionResult CheckFormat(string format)
{
    if (!ExportControllerExtensions.IsFormatSupported(format))
    {
        return BadRequest($"Format '{format}' is not supported.");
    }
    
    return ExportControllerExtensions.GetFormatDetails(format);
}
```

## Notes

*   **Thread Safety:** The methods in this class are thread-safe, provided the `ControllerBase` instance is used in the context of the current request lifecycle.
*   **Cancellation:** Asynchronous methods accept a `CancellationToken`. It is recommended to propagate this token from the controller action to ensure long-running export operations can be aborted if the client disconnects or the request is cancelled.
*   **Format Validation:** Always utilize `IsFormatSupported` before attempting specific exports (`ExportJsonAsync` or `ExportCsvAsync`) if the format is determined dynamically by user input to avoid unexpected exceptions.
