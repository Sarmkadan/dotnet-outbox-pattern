# BatchProcessingOptionsExtensions

The `BatchProcessingOptionsExtensions` class provides a set of static extension methods designed to facilitate the fluent configuration of `BatchProcessingOptions` objects, along with utility methods for calculating chunking parameters and estimating memory consumption. These methods simplify the setup of batch processing workflows, ensuring consistent parameter validation and calculation across the application.

## API

### Configuration Methods
These methods act as extensions to `BatchProcessingOptions`, allowing for fluent configuration. All return the modified `BatchProcessingOptions` instance to support method chaining.

*   `public static BatchProcessingOptions WithTotalBatchSize(this BatchProcessingOptions options, int totalBatchSize)`
    Configures the total number of messages to process in a single batch operation.
*   `public static BatchProcessingOptions WithChunkSize(this BatchProcessingOptions options, int chunkSize)`
    Sets the maximum number of messages to be processed within a single chunk.
*   `public static BatchProcessingOptions WithParallelChunks(this BatchProcessingOptions options, int parallelChunks)`
    Specifies the number of chunks to process in parallel.
*   `public static BatchProcessingOptions AsSequential(this BatchProcessingOptions options)`
    Configures the batch processing to occur sequentially (equivalent to setting parallel chunks to 1).
*   `public static BatchProcessingOptions WithDelayBetweenChunks(this BatchProcessingOptions options, TimeSpan delay)`
    Defines the delay interval between processing consecutive chunks.
*   `public static BatchProcessingOptions StopOnFailure(this BatchProcessingOptions options)`
    Configures the batch processor to terminate execution upon encountering the first failure.

### Utility Methods
These static methods perform calculations based on the provided parameters.

*   `public static int CalculateTotalChunks(int totalBatchSize, int chunkSize)`
    Calculates the total number of chunks required for a given batch size and chunk size.
*   `public static int CalculateChunks(int totalBatchSize, int chunkSize)`
    Calculates the number of chunks; typically functionally identical to `CalculateTotalChunks` depending on implementation.
*   `public static long GetChunkMemoryUsage(...)`
    Calculates the estimated memory usage for a single chunk. Note: This method is overloaded to accept different parameter types for flexible memory estimation.
*   `public static long GetTotalBatchMemoryUsage(...)`
    Calculates the estimated total memory usage for the entire batch. Note: This method is overloaded to accept different parameter types.
*   `public static bool Validate(BatchProcessingOptions options)`
    Validates the configuration within a `BatchProcessingOptions` instance, ensuring parameters (e.g., batch size, chunk size) are within valid ranges. Returns `true` if valid, otherwise `false`.

## Usage

### Fluent Configuration
```csharp
var options = new BatchProcessingOptions()
    .WithTotalBatchSize(1000)
    .WithChunkSize(100)
    .WithParallelChunks(4)
    .WithDelayBetweenChunks(TimeSpan.FromMilliseconds(50))
    .StopOnFailure();

if (BatchProcessingOptionsExtensions.Validate(options))
{
    // Proceed with processing
}
```

### Utility Calculations
```csharp
int totalBatchSize = 5000;
int chunkSize = 250;

int totalChunks = BatchProcessingOptionsExtensions.CalculateTotalChunks(totalBatchSize, chunkSize);
long estimatedMemory = BatchProcessingOptionsExtensions.GetTotalBatchMemoryUsage(totalBatchSize, itemSizeInBytes: 1024);

Console.WriteLine($"Processing {totalChunks} chunks with an estimated memory footprint of {estimatedMemory} bytes.");
```

## Notes

*   **Immutability and Chaining:** The configuration methods return the same `BatchProcessingOptions` instance modified in place. They are designed for fluent chaining.
*   **Thread-Safety:** These methods are stateless and rely on the input parameters. They are thread-safe, assuming the `BatchProcessingOptions` instance is not being concurrently modified by another thread.
*   **Edge Cases:**
    *   `Calculate*` methods may return unexpected values if `chunkSize` is provided as zero or a negative value; ensure input validation is performed before calling these methods if dynamic values are used.
    *   `Validate` should be called after configuring options to ensure that the combination of `TotalBatchSize`, `ChunkSize`, and `ParallelChunks` forms a logically sound execution plan.
    *   Memory estimation methods (`Get*MemoryUsage`) depend on the accuracy of the provided metrics (e.g., `itemSizeInBytes`). These are estimates and should not be used for precise memory allocation.
