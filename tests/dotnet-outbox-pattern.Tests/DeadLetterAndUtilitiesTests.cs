#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Utilities;
using FluentAssertions;

namespace DotnetOutboxPattern.Tests;

public sealed class DeadLetterTests
{
    private static OutboxMessage CreateFailedMessage() => new()
    {
        Id = Guid.NewGuid(),
        IdempotencyKey = "key-dlq-01",
        AggregateId = "order-99",
        AggregateType = "Order",
        EventType = EventType.Created,
        EventData = "{\"id\":99}",
        EventTypeName = "OrderCreatedEvent",
        Topic = "orders.created",
        PublishAttempts = 5,
        MaxPublishAttempts = 5,
        CreatedAt = DateTime.UtcNow.AddHours(-2),
        LastProcessedAt = DateTime.UtcNow.AddMinutes(-10),
        ErrorMessage = "broker unavailable",
        ErrorStackTrace = "at Publisher.Send()",
        CorrelationId = "corr-123",
        CausationId = "cause-456",
        Metadata = "{\"source\":\"api\"}"
    };

    [Fact]
    public void FromOutboxMessage_CopiesAllCoreFields()
    {
        var source = CreateFailedMessage();

        var dl = DeadLetter.FromOutboxMessage(source);

        dl.OutboxMessageId.Should().Be(source.Id);
        dl.IdempotencyKey.Should().Be(source.IdempotencyKey);
        dl.AggregateId.Should().Be(source.AggregateId);
        dl.AggregateType.Should().Be(source.AggregateType);
        dl.EventType.Should().Be(source.EventType);
        dl.EventData.Should().Be(source.EventData);
        dl.EventTypeName.Should().Be(source.EventTypeName);
        dl.Topic.Should().Be(source.Topic);
        dl.TotalAttempts.Should().Be(source.PublishAttempts);
        dl.ErrorMessage.Should().Be(source.ErrorMessage);
        dl.ErrorStackTrace.Should().Be(source.ErrorStackTrace);
        dl.OriginalCreatedAt.Should().Be(source.CreatedAt);
        dl.CorrelationId.Should().Be(source.CorrelationId);
        dl.CausationId.Should().Be(source.CausationId);
        dl.Metadata.Should().Be(source.Metadata);
    }

    [Fact]
    public void FromOutboxMessage_WithNullErrorMessage_UsesDefaultText()
    {
        var source = CreateFailedMessage();
        source.ErrorMessage = null;

        var dl = DeadLetter.FromOutboxMessage(source);

        dl.ErrorMessage.Should().Be("Unknown error");
    }

    [Fact]
    public void FromOutboxMessage_SetsMovedToDlqAtToNow()
    {
        var before = DateTime.UtcNow;
        var source = CreateFailedMessage();

        var dl = DeadLetter.FromOutboxMessage(source);

        dl.MovedToDlqAt.Should().BeOnOrAfter(before);
        dl.IsReviewed.Should().BeFalse();
        dl.IsRequeued.Should().BeFalse();
    }

    [Fact]
    public void MarkAsReviewed_SetsIsReviewedAndNotes()
    {
        var dl = DeadLetter.FromOutboxMessage(CreateFailedMessage());
        var before = DateTime.UtcNow;

        dl.MarkAsReviewed("investigated — broker was down");

        dl.IsReviewed.Should().BeTrue();
        dl.ReviewNotes.Should().Be("investigated — broker was down");
        dl.ReviewedAt.Should().NotBeNull();
        dl.ReviewedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MarkAsRequeued_SetsIsRequeuedAndReason()
    {
        var dl = DeadLetter.FromOutboxMessage(CreateFailedMessage());
        var before = DateTime.UtcNow;

        dl.MarkAsRequeued("manual retry after infrastructure fix");

        dl.IsRequeued.Should().BeTrue();
        dl.RequeueReason.Should().Be("manual retry after infrastructure fix");
        dl.RequeuedAt.Should().NotBeNull();
        dl.RequeuedAt.Should().BeOnOrAfter(before);
    }
}

public sealed class StringHelperTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("Order Created Event", "order-created-event")]
    [InlineData("  spaces  ", "spaces")]
    [InlineData("Hello---World", "hello-world")]
    public void ToSlug_ConvertsToLowercaseKebab(string input, string expected)
    {
        var result = StringHelper.ToSlug(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("OutboxMessage", "outbox-message")]
    [InlineData("DeadLetterService", "dead-letter-service")]
    [InlineData("Id", "id")]
    public void ToKebabCase_ConvertsPascalCaseToKebab(string input, string expected)
    {
        var result = StringHelper.ToKebabCase(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void Truncate_WhenExceedsLimit_AppendsEllipsis()
    {
        var result = StringHelper.Truncate("Hello World", 8);
        result.Should().Be("Hello...");
        result.Length.Should().Be(8);
    }

    [Fact]
    public void Truncate_WhenWithinLimit_ReturnsOriginal()
    {
        var result = StringHelper.Truncate("Hi", 10);
        result.Should().Be("Hi");
    }

    [Fact]
    public void Truncate_WithNullOrEmpty_ReturnsEmpty()
    {
        StringHelper.Truncate(null, 10).Should().BeEmpty();
        StringHelper.Truncate("", 10).Should().BeEmpty();
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("bad-email", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_MatchesExpected(string? email, bool expected)
    {
        StringHelper.IsValidEmail(email).Should().Be(expected);
    }

    [Fact]
    public void IsValidGuid_WithValidGuid_ReturnsTrue()
    {
        var guid = Guid.NewGuid().ToString();
        StringHelper.IsValidGuid(guid).Should().BeTrue();
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValidGuid_WithInvalidInput_ReturnsFalse(string? value)
    {
        StringHelper.IsValidGuid(value).Should().BeFalse();
    }

    [Fact]
    public void ExtractBetween_WithMatchingDelimiters_ReturnsSubstring()
    {
        var result = StringHelper.ExtractBetween("Hello [World] Foo", "[", "]");
        result.Should().Be("World");
    }

    [Fact]
    public void ExtractBetween_WhenStartDelimiterMissing_ReturnsEmpty()
    {
        var result = StringHelper.ExtractBetween("Hello World", "[", "]");
        result.Should().BeEmpty();
    }

    [Fact]
    public void JoinNonEmpty_FiltersNullAndEmptyValues()
    {
        var result = StringHelper.JoinNonEmpty(", ", "a", null, "  ", "b");
        result.Should().Be("a, b");
    }

    [Fact]
    public void SanitizeForJson_EscapesSpecialCharacters()
    {
        var result = StringHelper.SanitizeForJson("say \"hello\"\nnewline");
        result.Should().Contain("\\\"");
        result.Should().Contain("\\n");
    }
}

public sealed class PaginationHelperTests
{
    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(2, 20, 20)]
    [InlineData(3, 10, 20)]
    public void CalculateSkip_ReturnsCorrectOffset(int page, int pageSize, int expectedSkip)
    {
        PaginationHelper.CalculateSkip(page, pageSize).Should().Be(expectedSkip);
    }

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(101, 10, 11)]
    [InlineData(1, 10, 1)]
    [InlineData(0, 10, 0)]
    public void CalculateTotalPages_CeilsCorrectly(int totalItems, int pageSize, int expected)
    {
        PaginationHelper.CalculateTotalPages(totalItems, pageSize).Should().Be(expected);
    }

    [Fact]
    public void CalculateTotalPages_WithZeroPageSize_ReturnsZero()
    {
        PaginationHelper.CalculateTotalPages(50, 0).Should().Be(0);
    }

    [Fact]
    public void IsValidPageSize_WithinBounds_ReturnsTrue()
    {
        PaginationHelper.IsValidPageSize(50).Should().BeTrue();
        PaginationHelper.IsValidPageSize(1).Should().BeTrue();
        PaginationHelper.IsValidPageSize(500).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public void IsValidPageSize_OutOfBounds_ReturnsFalse(int pageSize)
    {
        PaginationHelper.IsValidPageSize(pageSize).Should().BeFalse();
    }

    [Fact]
    public void GetNextPage_WhenNotLastPage_ReturnsIncremented()
    {
        PaginationHelper.GetNextPage(2, 5).Should().Be(3);
    }

    [Fact]
    public void GetNextPage_WhenLastPage_ReturnsMinusOne()
    {
        PaginationHelper.GetNextPage(5, 5).Should().Be(-1);
    }

    [Fact]
    public void GetPreviousPage_WhenNotFirstPage_ReturnsDecremented()
    {
        PaginationHelper.GetPreviousPage(3).Should().Be(2);
    }

    [Fact]
    public void GetPreviousPage_WhenFirstPage_ReturnsMinusOne()
    {
        PaginationHelper.GetPreviousPage(1).Should().Be(-1);
    }

    [Fact]
    public void CreateMetadata_SetsHasNextAndPreviousCorrectly()
    {
        var meta = PaginationHelper.CreateMetadata(page: 2, pageSize: 10, totalItems: 50);

        meta.CurrentPage.Should().Be(2);
        meta.TotalPages.Should().Be(5);
        meta.HasNextPage.Should().BeTrue();
        meta.HasPreviousPage.Should().BeTrue();
        meta.NextPage.Should().Be(3);
        meta.PreviousPage.Should().Be(1);
    }

    [Fact]
    public void CreateMetadata_OnFirstPage_HasNoPreviousPage()
    {
        var meta = PaginationHelper.CreateMetadata(page: 1, pageSize: 10, totalItems: 30);

        meta.HasPreviousPage.Should().BeFalse();
        meta.HasNextPage.Should().BeTrue();
    }
}

public sealed class CollectionExtensionsTests
{
    [Fact]
    public void Chunk_DividesIntoCorrectGroups()
    {
        var items = Enumerable.Range(1, 10).ToList();

        var chunks = DotnetOutboxPattern.Utilities.CollectionExtensions.Chunk(items, 3);

        chunks.Should().HaveCount(4);
        chunks[0].Should().Equal(1, 2, 3);
        chunks[3].Should().Equal(10);
    }

    [Fact]
    public void Chunk_WithZeroSize_ThrowsArgumentException()
    {
        var items = new List<int> { 1, 2 };
        var act = () => DotnetOutboxPattern.Utilities.CollectionExtensions.Chunk(items, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveWhere_RemovesMatchingElements()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };

        var count = list.RemoveWhere(x => x % 2 == 0);

        count.Should().Be(2);
        list.Should().Equal(1, 3, 5);
    }

    [Fact]
    public void FindDuplicates_ReturnsOnlyDuplicatedItems()
    {
        var items = new List<string> { "a", "b", "a", "c", "b" };

        var duplicates = items.FindDuplicates(x => x).ToList();

        duplicates.Should().HaveCount(4);
        duplicates.Should().Contain("a");
        duplicates.Should().Contain("b");
        duplicates.Should().NotContain("c");
    }

    [Fact]
    public void IsNullOrEmpty_WithNull_ReturnsTrue()
    {
        IEnumerable<int>? nullSource = null;
        nullSource.IsNullOrEmpty().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithEmptyList_ReturnsTrue()
    {
        new List<string>().IsNullOrEmpty().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithItems_ReturnsFalse()
    {
        new List<int> { 1 }.IsNullOrEmpty().Should().BeFalse();
    }

    [Fact]
    public void SafeGetAt_WithValidIndex_ReturnsElement()
    {
        var list = new List<string> { "x", "y", "z" };
        list.SafeGetAt(1).Should().Be("y");
    }

    [Fact]
    public void SafeGetAt_WithOutOfBoundsIndex_ReturnsDefault()
    {
        var list = new List<string> { "x" };
        list.SafeGetAt(99, "fallback").Should().Be("fallback");
    }

    [Fact]
    public void GetNext_ReturnsElementAfterGiven()
    {
        var list = new List<int> { 10, 20, 30 };
        list.GetNext(20).Should().Be(30);
    }

    [Fact]
    public void GetNext_WhenLastElement_ReturnsDefault()
    {
        var list = new List<int> { 10, 20, 30 };
        list.GetNext(30).Should().Be(default);
    }

    [Fact]
    public void Merge_CombinesMultipleSources()
    {
        var a = new[] { 1, 2 };
        var b = new[] { 3, 4 };
        var c = new[] { 5 };

        var result = DotnetOutboxPattern.Utilities.CollectionExtensions.Merge(a, b, c).ToList();

        result.Should().Equal(1, 2, 3, 4, 5);
    }
}

public sealed class ValidationHelperTests
{
    [Fact]
    public void ValidateNotEmpty_WithNullValue_ThrowsArgumentException()
    {
        var act = () => ValidationHelper.ValidateNotEmpty(null, "param");
        act.Should().Throw<ArgumentException>().WithParameterName("param");
    }

    [Fact]
    public void ValidateNotEmpty_WithWhitespace_ThrowsArgumentException()
    {
        var act = () => ValidationHelper.ValidateNotEmpty("   ", "param");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateNotEmpty_WithValue_DoesNotThrow()
    {
        var act = () => ValidationHelper.ValidateNotEmpty("value", "param");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatePositive_WithZero_ThrowsArgumentException()
    {
        var act = () => ValidationHelper.ValidatePositive(0, "count");
        act.Should().Throw<ArgumentException>().WithParameterName("count");
    }

    [Fact]
    public void ValidatePositive_WithPositiveValue_DoesNotThrow()
    {
        var act = () => ValidationHelper.ValidatePositive(1, "count");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(5, 1, 10)]
    [InlineData(1, 1, 10)]
    [InlineData(10, 1, 10)]
    public void ValidateRange_WithinBounds_DoesNotThrow(int value, int min, int max)
    {
        var act = () => ValidationHelper.ValidateRange(value, min, max, "val");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(11, 1, 10)]
    public void ValidateRange_OutOfBounds_ThrowsArgumentException(int value, int min, int max)
    {
        var act = () => ValidationHelper.ValidateRange(value, min, max, "val");
        act.Should().Throw<ArgumentException>().WithParameterName("val");
    }

    [Fact]
    public void ValidateCondition_WhenFalse_ThrowsArgumentException()
    {
        var act = () => ValidationHelper.ValidateCondition(false, "condition failed");
        act.Should().Throw<ArgumentException>().WithMessage("condition failed");
    }

    [Fact]
    public void ValidationContext_WithMultipleErrors_ThrowsAllOnInvalid()
    {
        var context = ValidationHelper.Validate("test-obj")
            .Condition(false, "error one")
            .Condition(false, "error two");

        context.IsValid.Should().BeFalse();
        context.Errors.Should().HaveCount(2);

        var act = () => context.ThrowIfInvalid();
        act.Should().Throw<ArgumentException>().WithMessage("*error one*error two*");
    }

    [Fact]
    public void ValidateNotNull_WithNullObject_ThrowsArgumentNullException()
    {
        object? obj = null;
        var act = () => ValidationHelper.ValidateNotNull(obj, "myParam");
        act.Should().Throw<ArgumentNullException>().WithParameterName("myParam");
    }
}
