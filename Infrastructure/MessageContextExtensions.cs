#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================

using System.Diagnostics;
using DotnetOutboxPattern.Domain;

namespace DotnetOutboxPattern.Infrastructure;

/// <summary>
/// Extension methods for <see cref="MessageContext"/> that provide convenient operations
/// for distributed tracing and message context management in the outbox pattern.
/// All methods delegate to the static <see cref="MessageContext"/> methods with proper validation.
/// </summary>
public static class MessageContextExtensions
{
    /// <summary>
    /// Creates an activity for tracing a message operation with automatic scope disposal.
    /// </summary>
    /// <param name="message">The outbox message to trace.</param>
    /// <param name="operationName">Name of the operation being performed.</param>
    /// <returns>A disposable activity scope for the tracing operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="operationName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static ActivityScope StartActivity(this MessageContext _, OutboxMessage message, string operationName)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(operationName);

        var activity = MessageContext.StartActivity(message, operationName);
        return activity.UseScope();
    }

    /// <summary>
    /// Creates an activity for an outbox service operation with automatic scope disposal.
    /// </summary>
    /// <param name="serviceName">Name of the service performing the operation.</param>
    /// <param name="operationName">Name of the operation being performed.</param>
    /// <returns>A disposable activity scope for the tracing operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="serviceName"/> or <paramref name="operationName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static ActivityScope StartServiceActivity(this MessageContext _, string serviceName, string operationName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentException.ThrowIfNullOrEmpty(operationName);

        var activity = MessageContext.StartServiceActivity(serviceName, operationName);
        return activity.UseScope();
    }

    /// <summary>
    /// Records an event with the specified name and optional attributes.
    /// </summary>
    /// <param name="context">The message context (unused but required for extension method pattern).</param>
    /// <param name="eventName">Name of the event to record.</param>
    /// <param name="attributes">Optional attributes to include with the event. Can be <see langword="null"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static void RecordEvent(this MessageContext context, string eventName, Dictionary<string, object>? attributes = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);

        MessageContext.RecordEvent(eventName, attributes);
    }

    /// <summary>
    /// Records an exception in the current activity.
    /// </summary>
    /// <param name="context">The message context (unused but required for extension method pattern).</param>
    /// <param name="exception">The exception to record. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static void RecordException(this MessageContext context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        MessageContext.RecordException(exception);
    }
}