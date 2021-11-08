#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetOutboxPattern.Configuration;

/// <summary>
/// Interface for Outbox Processor configuration options
/// </summary>
public interface IOutboxProcessorOptions
{
    /// <summary>
    /// Gets whether the processor is enabled
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Gets the batch size
    /// </summary>
    int BatchSize { get; }

    /// <summary>
    /// Gets the delay between batches in milliseconds
    /// </summary>
    int DelayBetweenBatches { get; }

    /// <summary>
    /// Gets whether to preserve partition ordering
    /// </summary>
    bool PreservePartitionOrdering { get; }

    /// <summary>
    /// Gets how often to check for expired locks (milliseconds)
    /// </summary>
    int CheckExpiredLocksInterval { get; }

    /// <summary>
    /// Gets the age threshold (in minutes) beyond which an unprocessed message triggers a warning log
    /// </summary>
    int OldestMessageAgeThresholdMinutes { get; }
}
