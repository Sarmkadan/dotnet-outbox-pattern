#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Domain;

/// <summary>
/// Provides human-readable display strings for domain enum values.
/// </summary>
public static class EnumDisplayExtensions
{
    /// <summary>
    /// Gets a human-readable display string for an outbox message state.
    /// </summary>
    /// <param name="state">The outbox message state.</param>
    /// <returns>A human-readable representation of <paramref name="state"/>.</returns>
    public static string ToDisplayString(this OutboxMessageState state) => state switch
    {
        OutboxMessageState.Pending => "Pending",
        OutboxMessageState.Processing => "Processing",
        OutboxMessageState.Published => "Published",
        OutboxMessageState.Failed => "Failed",
        OutboxMessageState.Archived => "Archived",
        _ => state.ToString()
    };

    /// <summary>
    /// Gets a human-readable display string for an event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>A human-readable representation of <paramref name="eventType"/>.</returns>
    public static string ToDisplayString(this EventType eventType) => eventType switch
    {
        EventType.Created => "Created",
        EventType.Updated => "Updated",
        EventType.Deleted => "Deleted",
        EventType.Custom => "Custom",
        EventType.Notification => "Notification",
        _ => eventType.ToString()
    };

    /// <summary>
    /// Gets a human-readable display string for a delivery guarantee.
    /// </summary>
    /// <param name="guarantee">The delivery guarantee.</param>
    /// <returns>A human-readable representation of <paramref name="guarantee"/>.</returns>
    public static string ToDisplayString(this DeliveryGuarantee guarantee) => guarantee switch
    {
        DeliveryGuarantee.AtMostOnce => "At Most Once",
        DeliveryGuarantee.AtLeastOnce => "At Least Once",
        DeliveryGuarantee.ExactlyOnce => "Exactly Once",
        _ => guarantee.ToString()
    };

    /// <summary>
    /// Gets a human-readable display string for a retry policy type.
    /// </summary>
    /// <param name="policyType">The retry policy type.</param>
    /// <returns>A human-readable representation of <paramref name="policyType"/>.</returns>
    public static string ToDisplayString(this RetryPolicyType policyType) => policyType switch
    {
        RetryPolicyType.NoRetry => "No Retry",
        RetryPolicyType.FixedInterval => "Fixed Interval",
        RetryPolicyType.ExponentialBackoff => "Exponential Backoff",
        RetryPolicyType.LinearBackoff => "Linear Backoff",
        _ => policyType.ToString()
    };
}
