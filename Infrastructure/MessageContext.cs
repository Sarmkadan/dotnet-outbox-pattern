#nullable enable
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
public sealed class MessageContext
{
    private static readonly ActivitySource ActivitySource = new("DotnetOutboxPattern");

    /// <summary>
    /// Header key used to carry the W3C "traceparent" value in <see cref="OutboxMessage.Headers"/>.
    /// </summary>
    public const string TraceParentHeader = "traceparent";

    /// <summary>
    /// Header key used to carry the W3C "tracestate" value in <see cref="OutboxMessage.Headers"/>.
    /// </summary>
    public const string TraceStateHeader = "tracestate";

    /// <summary>
    /// Messaging system tag value used for the standard OpenTelemetry messaging semantic conventions.
    /// </summary>
    private const string MessagingSystem = "outbox";

    /// <summary>
    /// Gets or creates a correlation ID for distributed tracing
    /// </summary>
    public static string GetOrCreateCorrelationId()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Captures the W3C trace-context ("traceparent"/"tracestate") from <see cref="Activity.Current"/>
    /// into the message's headers so it survives being persisted to the outbox store and can later be
    /// used to correlate the consumer's activity with the producer's, at enqueue time.
    /// </summary>
    /// <param name="message">The outbox message being enqueued.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    public static void CaptureTraceContext(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var activity = Activity.Current;
        if (activity is null)
            return;

        message.Headers[TraceParentHeader] = activity.Id ?? activity.Context.ToString();

        if (!string.IsNullOrEmpty(activity.TraceStateString))
            message.Headers[TraceStateHeader] = activity.TraceStateString;
    }

    /// <summary>
    /// Starts a new dispatch activity for the message, linked to the W3C trace-context captured at
    /// enqueue time (if any), so consumers correlate with the originating producer activity. Applies
    /// the standard OpenTelemetry messaging semantic convention tags (messaging.system,
    /// messaging.destination) alongside the outbox-specific tags.
    /// </summary>
    /// <param name="message">The outbox message being dispatched.</param>
    /// <param name="operationName">Name of the dispatch operation being performed.</param>
    /// <returns>The started activity, or <see langword="null"/> if no listener is registered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="operationName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static Activity? StartDispatchActivity(OutboxMessage message, string operationName)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(operationName);

        message.Headers.TryGetValue(TraceStateHeader, out var traceState);
        var hasParent = message.Headers.TryGetValue(TraceParentHeader, out var traceParent)
            && ActivityContext.TryParse(traceParent, traceState, out var parentContext);

        var activity = hasParent
            ? ActivitySource.StartActivity(operationName, ActivityKind.Consumer, parentContext)
            : ActivitySource.StartActivity(operationName, ActivityKind.Consumer);

        if (activity is null)
            return activity;

        activity.SetTag("messaging.system", MessagingSystem);
        activity.SetTag("messaging.destination", message.Topic);
        activity.SetTag("outbox.message_id", message.Id.ToString());
        activity.SetTag("outbox.aggregate_id", message.AggregateId);
        activity.SetTag("outbox.event_type", message.EventType.ToString());
        activity.SetTag("trace.correlation_id", message.CorrelationId);

        return activity;
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

        if (activity is not null)
        {
            // SetTag with a non-string value (Guid, enum, ...) is only visible through
            // TagObjects - Activity.Tags (the string-keyed view most consumers/tests read)
            // silently omits any tag whose value isn't already a string. Stringify explicitly
            // so these tags actually show up wherever the message's other string tags do.
            activity.SetTag("messaging.system", MessagingSystem);
            activity.SetTag("messaging.destination", message.Topic);
            activity.SetTag("outbox.message_id", message.Id.ToString());
            activity.SetTag("outbox.aggregate_id", message.AggregateId);
            activity.SetTag("outbox.topic", message.Topic);
            activity.SetTag("outbox.event_type", message.EventType.ToString());
            activity.SetTag("outbox.state", message.State.ToString());
            activity.SetTag("trace.correlation_id", message.CorrelationId);

            if (!string.IsNullOrEmpty(message.PartitionKey))
                activity.SetTag("outbox.partition_key", message.PartitionKey);

            CaptureTraceContext(message);
        }

        return activity;
    }

    /// <summary>
    /// Creates an activity for an outbox service operation
    /// </summary>
    public static Activity? StartServiceActivity(string serviceName, string operationName)
    {
        var activity = ActivitySource.StartActivity($"{serviceName}.{operationName}");

        if (activity is not null)
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
        var tags = attributes?.ToDictionary(k => k.Key, v => (object?)v.Value) ?? new();
        Activity.Current?.AddEvent(new ActivityEvent(eventName, tags: new ActivityTagsCollection(tags)));
    }

    /// <summary>
    /// Records an exception in the current activity
    /// </summary>
    public static void RecordException(Exception exception)
    {
        if (Activity.Current is not null)
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
public sealed class ActivityScope : IDisposable
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
