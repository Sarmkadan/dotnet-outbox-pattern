# ExportService

## Overview
The `ExportService` provides functionality to export outbox messages in various formats. It supports filtering messages by date range and status, formatting them using registered formatters, and optionally writing the output to a timestamped file.

## Supported Formats
The service supports multiple export formats via registered `IDataFormatter` implementations. Common formats include:
- JSON
- CSV
- XML

## Public Methods

### `ExportAsync(ExportRequest request)`
Exports messages matching the request filters using the requested formatter.
- **Parameters**: `ExportRequest request` (must not be null)
- **Returns**: `Task<ExportResult>` containing the formatted content, metadata, and size.
- **Exceptions**: 
  - `ArgumentNullException` if `request` is null.
  - `InvalidOperationException` if the requested format has no registered formatter.

### `ExportToFileAsync(ExportRequest request)`
Exports messages and writes them to a timestamped file under the `exports` directory.
- **Parameters**: `ExportRequest request` (must not be null)
- **Returns**: `Task<string>` containing the full file path.
- **File Naming Convention**: `outbox_export_YYYYMMDD_HHmmss.{format}`
- **Exceptions**: `ArgumentNullException` if `request` is null.

### `GetSupportedFormats()`
Returns a list of supported export format names based on registered formatters.
- **Returns**: `List<string>`

## Usage Example

