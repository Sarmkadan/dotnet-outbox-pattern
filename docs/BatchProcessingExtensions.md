# BatchProcessingExtensions

Extension methods for configuring batch processing behavior in .NET applications, typically used with the Outbox Pattern to process messages in configurable batches.

## API

### `AddBatchProcessing(IServiceCollection services)`

Registers batch processing services with the dependency injection container using default configuration.

- **Parameters**
  - `services` – The `IServiceCollection` instance to configure.
- **Return value**
  - The same `IServiceCollection` instance for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `services` is `null`.

### `AddBatchProcessing(IServiceCollection services, Action<BatchProcessingOptions> configure)`

Registers batch processing services with the dependency injection container and applies custom configuration via `BatchProcessingOptions`.

- **Parameters**
  - `services` – The `IServiceCollection` instance to configure.
  - `configure` – An `Action<BatchProcessingOptions>` delegate to configure batch behavior.
- **Return value**
  - The same `IServiceCollection` instance for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `services` or `configure` is `null`.

### `AddBatchProcessing(IServiceCollection services, int chunkSize)`

Registers batch processing services with a specified chunk size.

- **Parameters**
  - `services` – The `IServiceCollection` instance to configure.
  - `chunkSize` – The number of items to process in each batch.
- **Return value**
  - The same `IServiceCollection` instance for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `services` is `null`.
  - Throws `ArgumentOutOfRangeException` if `chunkSize` is less than or equal to zero.

### `WithChunkSize(this IServiceCollection services, int chunkSize)`

Sets the number of items to process in each batch.

- **Parameters**
  - `services` – The `IServiceCollection` instance to configure.
  - `chunkSize` – The number of items to process in each batch.
- **Return value**
  - The same `IServiceCollection` instance for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `services` is `null`.
  - Throws `ArgumentOutOfRangeException` if `chunkSize` is less than or equal to zero.

### `WithParallelChunks(this IServiceCollection services, int degreeOfParallelism)`

Configures the number of chunks that may be processed concurrently.

- **Parameters**
  - `services` – The `IServiceCollection` instance to configure.
  - `degreeOfParallelism` – The maximum number of chunks to process in parallel.
- **Return value**
  - The same `IServiceCollection` instance for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `services` is `null`.
  - Throws `ArgumentOutOfRangeException` if `degreeOfParallelism` is less than or equal to zero.

### `WithDelayBetweenChunks(this IServiceCollection services, TimeSpan delay)`

Sets a delay to be applied between processing each chunk.

- **Parameters**
  - `services` – The `IServiceCollection` instance to configure.
  - `delay` – The `TimeSpan` to wait between chunks.
- **Return value**
  - The same `IServiceCollection` instance for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `services` is `null`.
  - Throws `ArgumentOutOfRangeException` if `delay` is negative.

### `StopBatchOnChunkFailure(this IServiceCollection services)`

Configures the batch processor to halt processing if any chunk fails.

- **Parameters**
  - `services` – The `IServiceCollection` instance to configure.
- **Return value**
  - The same `IServiceCollection` instance for method chaining.
- **Exceptions**
  - Throws `ArgumentNullException` if `services` is `null`.

## Usage

### Example 1: Basic batch configuration
