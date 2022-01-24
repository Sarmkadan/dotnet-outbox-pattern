#nullable enable

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Dtos;
using DotnetOutboxPattern.Formatters;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Contains unit tests for the <see cref="ExportService"/> class, validating functionality related to data export operations,
/// format support, and error handling.
/// </summary>
public sealed class ExportServiceTests
{
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<ILogger<ExportService>> _loggerMock;
    private readonly List<Mock<IDataFormatter>> _formatterMocks;
    private readonly ExportService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportServiceTests"/> class, setting up mocks for dependencies
    /// and initializing the system under test.
    /// </summary>
    public ExportServiceTests()
    {
        _outboxServiceMock = new Mock<IOutboxService>();
        _loggerMock = new Mock<ILogger<ExportService>>();
        _formatterMocks = new();

        var jsonFormatterMock = new Mock<IDataFormatter>();
        jsonFormatterMock.Setup(f => f.FormatName).Returns("json");
        jsonFormatterMock.Setup(f => f.Format(It.IsAny<List<OutboxMessage>>())).Returns("{\"messages\":[]}");
        jsonFormatterMock.Setup(f => f.ContentType).Returns("application/json");
        _formatterMocks.Add(jsonFormatterMock);

        var csvFormatterMock = new Mock<IDataFormatter>();
        csvFormatterMock.Setup(f => f.FormatName).Returns("csv");
        csvFormatterMock.Setup(f => f.Format(It.IsAny<List<OutboxMessage>>())).Returns("id,topic\n");
        csvFormatterMock.Setup(f => f.ContentType).Returns("text/csv");
        _formatterMocks.Add(csvFormatterMock);

        _sut = new ExportService(
            _outboxServiceMock.Object,
            _formatterMocks.Select(m => m.Object),
            _loggerMock.Object);
    }

    /// <summary>
    /// Tests that the constructor throws an <see cref="ArgumentNullException"/> when the provided outbox service is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOutboxService_ThrowsArgumentNullException()
    {
        var act = () => new ExportService(null!, _formatterMocks.Select(m => m.Object), _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("outboxService");
    }

    /// <summary>
    /// Tests that the constructor throws an <see cref="ArgumentNullException"/> when the provided formatters are null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFormatters_ThrowsArgumentNullException()
    {
        var act = () => new ExportService(_outboxServiceMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("formatters");
    }

    /// <summary>
    /// Tests that <see cref="ExportService.GetSupportedFormats"/> returns all registered format names.
    /// </summary>
    [Fact]
    public void GetSupportedFormats_ReturnsRegisteredFormats()
    {
        var formats = _sut.GetSupportedFormats();

        formats.Should().Contain("json");
        formats.Should().Contain("csv");
    }

    /// <summary>
    /// Tests that <see cref="ExportService.ExportAsync"/> correctly uses the JSON formatter when requested.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WithJsonFormat_UsesJsonFormatter()
    {
        var request = new ExportRequest { Format = "json" };
        var stats = new OutboxStatistics { TotalMessages = 5, PublishedMessages = 3 };

        _outboxServiceMock
            .Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await _sut.ExportAsync(request);

        result.Should().NotBeNull();
        result.Format.Should().Be("json");
        result.ContentType.Should().Be("application/json");
    }

    /// <summary>
    /// Tests that <see cref="ExportService.ExportAsync"/> correctly uses the CSV formatter when requested.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WithCsvFormat_UsesCsvFormatter()
    {
        var request = new ExportRequest { Format = "csv" };
        var stats = new OutboxStatistics { TotalMessages = 10, PublishedMessages = 8 };

        _outboxServiceMock
            .Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await _sut.ExportAsync(request);

        result.Should().NotBeNull();
        result.Format.Should().Be("csv");
        result.ContentType.Should().Be("text/csv");
    }

    /// <summary>
    /// Tests that <see cref="ExportService.ExportAsync"/> throws an <see cref="InvalidOperationException"/> when an unsupported format is requested.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WithUnsupportedFormat_ThrowsInvalidOperationException()
    {
        var request = new ExportRequest { Format = "yaml" };

        var act = async () => await _sut.ExportAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unsupported export format*");
    }

    /// <summary>
    /// Tests that <see cref="ExportService.ExportAsync"/> handles format strings case-insensitively.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WithLowercaseFormat_IsFormatCaseInsensitive()
    {
        var request = new ExportRequest { Format = "JSON" };
        var stats = new OutboxStatistics { TotalMessages = 1 };

        _outboxServiceMock
            .Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await _sut.ExportAsync(request);

        result.Format.Should().Be("json");
    }

    /// <summary>
    /// Tests that <see cref="ExportService.ExportAsync"/> correctly calculates and sets the content size.
    /// </summary>
    [Fact]
    public async Task ExportAsync_SetsContentSizeCorrectly()
    {
        var request = new ExportRequest { Format = "json" };
        var stats = new OutboxStatistics { TotalMessages = 5 };

        _outboxServiceMock
            .Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await _sut.ExportAsync(request);

        result.ContentSizeBytes.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that <see cref="ExportService.ExportAsync"/> sets the exported timestamp correctly.
    /// </summary>
    [Fact]
    public async Task ExportAsync_SetsExportedAtTimestamp()
    {
        var before = DateTime.UtcNow;
        var request = new ExportRequest { Format = "json" };
        var stats = new OutboxStatistics { TotalMessages = 1 };

        _outboxServiceMock
            .Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await _sut.ExportAsync(request);

        result.ExportedAt.Should().BeOnOrAfter(before);
        result.ExportedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Tests that <see cref="ExportService.ExportAsync"/> propagates exceptions thrown by the formatter.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WhenFormatterThrows_PropagatesException()
    {
        var request = new ExportRequest { Format = "json" };
        var stats = new OutboxStatistics { TotalMessages = 5 };

        _outboxServiceMock
            .Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        _formatterMocks[0].Setup(f => f.Format(It.IsAny<List<OutboxMessage>>()))
            .Throws(new InvalidOperationException("formatter error"));

        var act = async () => await _sut.ExportAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that <see cref="ExportService.ExportToFileAsync"/> correctly creates the export file path in the directory.
    /// </summary>
    [Fact]
    public async Task ExportToFileAsync_CreatesExportDirectory()
    {
        var request = new ExportRequest { Format = "json" };
        var stats = new OutboxStatistics { TotalMessages = 1 };

        _outboxServiceMock
            .Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        _formatterMocks[0].Setup(f => f.Format(It.IsAny<List<OutboxMessage>>()))
            .Returns("test content");
        _formatterMocks[0].Setup(f => f.ContentType).Returns("application/json");

        var result = await _sut.ExportToFileAsync(request);

        result.Should().Contain("exports/");
        result.Should().Contain(".json");
    }

    /// <summary>
    /// Tests that <see cref="ExportService.GetSupportedFormats"/> returns an empty list when no formatters are registered.
    /// </summary>
    [Fact]
    public void GetSupportedFormats_ReturnsEmptyList_WhenNoFormatters()
    {
        var emptyFormatterList = new List<IDataFormatter>();
        var exportService = new ExportService(
            _outboxServiceMock.Object,
            emptyFormatterList,
            _loggerMock.Object);

        var formats = exportService.GetSupportedFormats();

        formats.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ExportService.ExportAsync"/> returns a valid result even when the message list is empty.
    /// </summary>
    [Fact]
    public async Task ExportAsync_WithEmptyMessageList_ReturnsValidResult()
    {
        var request = new ExportRequest { Format = "json" };
        var stats = new OutboxStatistics { TotalMessages = 0 };

        _outboxServiceMock
            .Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        var result = await _sut.ExportAsync(request);

        result.Should().NotBeNull();
        result.MessageCount.Should().Be(0);
        result.Content.Should().NotBeNullOrEmpty();
    }
}
