// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Manages distributed tracing context for outbox messages
/// Provides correlation and causation IDs for message flow tracing
/// </summary>
public class MessageContext
{
    private static readonly ActivitySource ActivitySource = new("DotnetOutboxPattern");

    /// <summary>
    /// Gets or creates a correlation ID for distributed tracing
    /// </summary>
    public static string GetOrCreateCorrelationId()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets or creates a causation ID linking to the causative event
    /// </summary>
    public static string GetOrCreateCausationId()
    {
        return Activity.Current?.Id ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Creates an activity for tracing a message operation
    /// </summary>
    public static Activity? StartActivity(OutboxMessage message, string operationName)
    {
        var activity = ActivitySource.StartActivity(operationName);

        if (activity != null)
        {
            activity.SetTag("outbox.message_id", message.Id);
            activity.SetTag("outbox.aggregate_id", message.AggregateId);
            activity.SetTag("outbox.topic", message.Topic);
            activity.SetTag("outbox.event_type", message.EventType);
            activity.SetTag("outbox.state", message.State);
            activity.SetTag("trace.correlation_id", message.CorrelationId);

            if (!string.IsNullOrEmpty(message.PartitionKey))
                activity.SetTag("outbox.partition_key", message.PartitionKey);
        }

        return activity;
    }

    /// <summary>
    /// Creates an activity for an outbox service operation
    /// </summary>
    public static Activity? StartServiceActivity(string serviceName, string operationName)
    {
        var activity = ActivitySource.StartActivity($"{serviceName}.{operationName}");

        if (activity != null)
        {
            activity.SetTag("service", serviceName);
            activity.SetTag("operation", operationName);
        }

        return activity;
    }

    /// <summary>
    /// Adds an event to the current activity for tracking milestones
    /// </summary>
    public static void RecordEvent(string eventName, Dictionary<string, object>? attributes = null)
    {
        Activity.Current?.AddEvent(new ActivityEvent(eventName, tags: new ActivityTagsCollection(attributes ?? new())));
    }

    /// <summary>
    /// Records an exception in the current activity
    /// </summary>
    public static void RecordException(Exception exception)
    {
        if (Activity.Current != null)
        {
            Activity.Current.SetTag("exception.type", exception.GetType().Name);
            Activity.Current.SetTag("exception.message", exception.Message);
            Activity.Current.SetTag("otel.status_code", "ERROR");
        }
    }
}

/// <summary>
/// Scope for automatic activity disposal
/// </summary>
public class ActivityScope : IDisposable
{
    private readonly Activity? _activity;

    public ActivityScope(Activity? activity)
    {
        _activity = activity;
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }
}

/// <summary>
/// Extension methods for creating activity scopes
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Creates a disposable scope for an activity
    /// </summary>
    public static ActivityScope UseScope(this Activity? activity)
    {
        return new ActivityScope(activity);
    }
}
