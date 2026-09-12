#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Contains unit tests for the <see cref="OutboxMessageQueryExtensions"/> class.
/// </summary>
public sealed class OutboxMessageQueryExtensionsTests
{
	/// <summary>
	/// Creates a valid <see cref="OutboxMessage"/> instance for testing purposes.
	/// </summary>
	private static OutboxMessage CreateMessage(OutboxMessageState state, DateTime createdAt) => new()
	{
		Id = Guid.NewGuid(),
		IdempotencyKey = "key-001",
		AggregateId = "agg-1",
		AggregateType = "Order",
		EventType = EventType.Created,
		EventData = "{\"orderId\":\"1\"}",
		EventTypeName = "OrderCreatedEvent",
		Topic = "orders.created",
		MaxPublishAttempts = 5,
		State = state,
		CreatedAt = createdAt
	};

	[Fact]
	public void WhereState_WithMatchingState_ReturnsOnlyMatchingMessages()
	{
		var pending = CreateMessage(OutboxMessageState.Pending, DateTime.UtcNow);
		var published = CreateMessage(OutboxMessageState.Published, DateTime.UtcNow);
		var failed = CreateMessage(OutboxMessageState.Failed, DateTime.UtcNow);
		var messages = new[] { pending, published, failed };

		var result = messages.WhereState(OutboxMessageState.Pending).ToList();

		result.Should().ContainSingle().Which.Should().BeSameAs(pending);
	}

	[Fact]
	public void WhereState_WithNoMatchingState_ReturnsEmpty()
	{
		var messages = new[]
		{
			CreateMessage(OutboxMessageState.Published, DateTime.UtcNow),
			CreateMessage(OutboxMessageState.Failed, DateTime.UtcNow)
		};

		var result = messages.WhereState(OutboxMessageState.Archived);

		result.Should().BeEmpty();
	}

	[Fact]
	public void WhereState_WithNullSource_ThrowsArgumentNullException()
	{
		IEnumerable<OutboxMessage>? messages = null;

		var act = () => messages!.WhereState(OutboxMessageState.Pending);

		act.Should().Throw<ArgumentNullException>().WithParameterName("source");
	}

	[Fact]
	public void OrderByCreatedAt_OrdersOldestFirst()
	{
		var oldest = CreateMessage(OutboxMessageState.Pending, new DateTime(2024, 1, 1));
		var middle = CreateMessage(OutboxMessageState.Pending, new DateTime(2024, 6, 1));
		var newest = CreateMessage(OutboxMessageState.Pending, new DateTime(2024, 12, 1));
		var messages = new[] { newest, oldest, middle };

		var result = messages.OrderByCreatedAt().ToList();

		result.Should().ContainInOrder(oldest, middle, newest);
	}

	[Fact]
	public void OrderByCreatedAtDescending_OrdersNewestFirst()
	{
		var oldest = CreateMessage(OutboxMessageState.Pending, new DateTime(2024, 1, 1));
		var middle = CreateMessage(OutboxMessageState.Pending, new DateTime(2024, 6, 1));
		var newest = CreateMessage(OutboxMessageState.Pending, new DateTime(2024, 12, 1));
		var messages = new[] { oldest, middle, newest };

		var result = messages.OrderByCreatedAtDescending().ToList();

		result.Should().ContainInOrder(newest, middle, oldest);
	}

	[Fact]
	public void OrderByCreatedAt_WithNullSource_ThrowsArgumentNullException()
	{
		IEnumerable<OutboxMessage>? messages = null;

		var act = () => messages!.OrderByCreatedAt();

		act.Should().Throw<ArgumentNullException>().WithParameterName("source");
	}
}