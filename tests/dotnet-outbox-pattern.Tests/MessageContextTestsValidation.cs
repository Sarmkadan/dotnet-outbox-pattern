#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Validation helpers for MessageContextTests to ensure test data validity
/// </summary>
public static class MessageContextTestsValidation
{
	/// <summary>
	/// Validates that a MessageContextTests instance has valid state
	/// </summary>
	/// <param name="value">The MessageContextTests instance to validate</param>
	/// <returns>List of validation problems (empty if valid)</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
	public static IReadOnlyList<string> Validate(this MessageContextTests? value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var problems = new List<string>();

		// Validate correlation ID generation
		ValidateCorrelationIdGeneration(problems);

		// Validate causation ID generation (without activity)
		ValidateCausationIdGeneration(problems);

		// Validate activity creation with message
		ValidateStartActivityWithMessage(problems);

		// Validate activity creation without partition key
		ValidateStartActivityWithoutPartitionKey(problems);

		// Validate service activity creation
		ValidateStartServiceActivity(problems);

		// Validate event recording
		ValidateRecordEvent(problems);

		// Validate null attributes handling
		ValidateRecordEventWithNullAttributes(problems);

		// Validate exception recording
		ValidateRecordException(problems);

		// Validate activity scope
		ValidateActivityScope(problems);

		// Validate activity extensions
		ValidateActivityExtensions(problems);

		return problems.AsReadOnly();
	}

	private static void ValidateCorrelationIdGeneration(ICollection<string> problems)
	{
		var correlationId = MessageContext.GetOrCreateCorrelationId();

		if (string.IsNullOrWhiteSpace(correlationId))
		{
			problems.Add("GetOrCreateCorrelationId returned null or whitespace");
		}
		else if (!Guid.TryParse(correlationId, out _))
		{
			problems.Add($"GetOrCreateCorrelationId returned non-GUID string: {correlationId}");
		}
		else if (correlationId == "00000000-0000-0000-0000-000000000000")
		{
			problems.Add("GetOrCreateCorrelationId returned default GUID");
		}
	}

	private static void ValidateCausationIdGeneration(ICollection<string> problems)
	{
		Activity.Current = null;
		var causationId = MessageContext.GetOrCreateCausationId();

		if (string.IsNullOrWhiteSpace(causationId))
		{
			problems.Add("GetOrCreateCausationId returned null or whitespace");
		}
		else if (!Guid.TryParse(causationId, out _))
		{
			problems.Add($"GetOrCreateCausationId returned non-GUID string: {causationId}");
		}
	}

	private static void ValidateStartActivityWithMessage(ICollection<string> problems)
	{
		var message = new OutboxMessage
		{
			Id = Guid.NewGuid(),
			AggregateId = "test-aggregate",
			Topic = "test.topic",
			EventType = EventType.Created,
			State = OutboxMessageState.Pending,
			CorrelationId = Guid.NewGuid().ToString(),
			PartitionKey = "test-partition"
		};

		using var activity = MessageContext.StartActivity(message, "TestOperation");
		if (activity is null)
		{
			problems.Add("StartActivity with message returned null activity");
		}
	}

	private static void ValidateStartActivityWithoutPartitionKey(ICollection<string> problems)
	{
		var message = new OutboxMessage
		{
			Id = Guid.NewGuid(),
			AggregateId = "test-aggregate",
			Topic = "test.topic",
			EventType = EventType.Created,
			State = OutboxMessageState.Pending,
			CorrelationId = Guid.NewGuid().ToString(),
			PartitionKey = null
		};

		using var activity = MessageContext.StartActivity(message, "TestOperation");
		if (activity is null)
		{
			problems.Add("StartActivity without partition key returned null activity");
		}
	}

	private static void ValidateStartServiceActivity(ICollection<string> problems)
	{
		using var activity = MessageContext.StartServiceActivity("TestService", "TestOperation");
		if (activity is null)
		{
			problems.Add("StartServiceActivity returned null activity");
		}
	}

	private static void ValidateRecordEvent(ICollection<string> problems)
	{
		using var activity = new ActivitySource("Test").StartActivity("TestActivity");
		var attributes = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 123 } };
		MessageContext.RecordEvent("TestEvent", attributes);
	}

	private static void ValidateRecordEventWithNullAttributes(ICollection<string> problems)
	{
		using var activity = new ActivitySource("Test").StartActivity("TestActivity");
		MessageContext.RecordEvent("TestEvent", null);
	}

	private static void ValidateRecordException(ICollection<string> problems)
	{
		using var activity = new ActivitySource("Test").StartActivity("TestActivity");
		var exception = new InvalidOperationException("Test exception");
		MessageContext.RecordException(exception);
	}

	private static void ValidateActivityScope(ICollection<string> problems)
	{
		var activity = new ActivitySource("Test").StartActivity("TestActivity");
		var scope = new ActivityScope(activity);
		scope.Dispose();
	}

	private static void ValidateActivityExtensions(ICollection<string> problems)
	{
		using var activity = new ActivitySource("Test").StartActivity("TestActivity");
		using var scope = activity.UseScope();
	}

	/// <summary>
	/// Checks if a MessageContextTests instance is valid
	/// </summary>
	/// <param name="value">The MessageContextTests instance to check</param>
	/// <returns>True if valid, false otherwise</returns>
	public static bool IsValid(this MessageContextTests? value) =>
		Validate(value).Count == 0;

	/// <summary>
	/// Ensures that a MessageContextTests instance is valid, throwing if not
	/// </summary>
	/// <param name="value">The MessageContextTests instance to validate</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
	/// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid with detailed error messages</exception>
	public static void EnsureValid(this MessageContextTests? value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var problems = Validate(value);
		if (problems.Count > 0)
		{
			throw new ArgumentException(
				"MessageContextTests is not valid:\n" + string.Join("\n", problems),
				nameof(value));
		}
	}
}