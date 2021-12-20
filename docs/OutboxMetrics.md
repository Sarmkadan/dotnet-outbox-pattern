# OutboxMetrics

Provides a set of OpenTelemetry‑based metrics for monitoring the outbox pattern implementation, including counters for publish errors and dead‑letter messages, and a histogram for measuring message processing duration.

## API

### `public Counter<long> PublishErrorsTotal`
- **Purpose:** Tracks the total number of failed publish attempts to the message broker.
- **Parameters:** None.
- **Return value:** The `Counter<long>` instance; increments are performed via the `Add` method.
- **When it throws:** Does not throw under normal use. If the instance has not been initialized (e.g., accessed before construction), a `NullReferenceException` may occur.

### `public Counter<long> DeadLettersTotal`
- **Purpose:** Tracks the total number of messages that have been moved to the dead‑letter queue after exceeding retry limits.
- **Parameters:** None.
- **Return value:** The `Counter<long>` instance; increments are performed via the `Add` method.
- **When it throws:** Does not throw under normal use. Accessing the field before the object is fully constructed may result in a `NullReferenceException`.

### `public Histogram<double> ProcessingDurationSeconds`
- **Purpose:** Records the duration (in seconds) of successful message processing cycles, enabling latency analysis.
- **Parameters:** None.
- **Return value:** The `Histogram<double>` instance; observations are recorded via the `Record` method.
- **When it throws:** Does not throw under normal use. If accessed prior to initialization, a `NullReferenceException` may arise.

### `public OutboxMetrics()`
- **Purpose:** Creates a new `OutboxMetrics` instance with all metric instruments initialized to their default, no‑op implementations.
- **Parameters:** None.
- **Return value:** A fully constructed `OutboxMetrics` object ready for use.
- **When it throws:** Does not throw any exceptions.

## Usage

```csharp
using OpenTelemetry.Metrics;

// Create the metrics holder (typically done once at application start)
var metrics = new OutboxMetrics();

// Increment the publish error counter when a send operation fails
try
{
    await broker.PublishAsync(message);
}
catch (Exception ex)
{
    metrics.PublishErrorsTotal.Add(1);
    // …handle or log the exception…
}
```

```csharp
using System.Diagnostics;

// Record how long a message took to process
var stopwatch = Stopwatch.StartNew();
try
{
    await ProcessMessageAsync(message);
}
finally
{
    stopwatch.Stop();
    metrics.ProcessingDurationSeconds.Record(stopwatch.Elapsed.TotalSeconds);
}
```

## Notes

- The `Counter<long>` and `Histogram<double>` types supplied by OpenTelemetry are thread‑safe; concurrent calls to `Add` or `Record` from multiple threads are safe without external synchronization.
- If a custom `Meter` or metric exporter is required, the constructor can be extended (not shown here) to accept an `IMeterFactory`; the current parameterless constructor uses a default meter that may produce no‑op metrics unless a provider is configured elsewhere.
- Accessing any of the metric fields before the `OutboxMetrics` instance has been fully constructed will result in a `NullReferenceException`; ensure the object is created before any metric interaction.
- The metrics are intended for long‑running applications; resetting or recreating the `OutboxMetrics` instance will discard previously accumulated values.
