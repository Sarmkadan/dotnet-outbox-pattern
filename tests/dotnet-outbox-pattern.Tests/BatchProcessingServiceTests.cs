#nullable enable

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Tests for the <see cref="BatchProcessingService"/>.
/// </summary>
public sealed class BatchProcessingServiceTests
{
    private readonly Mock<IMessagePublishingService> _publishingServiceMock;
    private readonly Mock<ILogger<BatchProcessingService>> _loggerMock;
    private readonly BatchProcessingOptions _options;
    private readonly BatchProcessingService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProcessingServiceTests"/> class.
    /// </summary>
    public BatchProcessingServiceTests()
    {
        _publishingServiceMock = new Mock<IMessagePublishingService>();
        _loggerMock = new Mock<ILogger<BatchProcessingService>>();
        _options = new BatchProcessingOptions
        {
            TotalBatchSize = 100,
            ChunkSize = 25,
            EnableParallelChunks = false,
            DelayBetweenChunksMs = 0
        };
        _sut = new BatchProcessingService(
            _publishingServiceMock.Object,
            _options,
            _loggerMock.Object);
    }

    /// <summary>
    /// Verifies that the constructor throws an <see cref="ArgumentNullException"/> when the <paramref name="publishingService"/> is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPublishingService_ThrowsArgumentNullException()
    {
        var act = () => new BatchProcessingService(null!, _options, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("publishingService");
    }

    /// <summary>
    /// Verifies that the constructor throws an <see cref="ArgumentNullException"/> when the <paramref name="options"/> is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var act = () => new BatchProcessingService(_publishingServiceMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    /// <summary>
    /// Verifies that the <see cref="ProcessInChunksAsync"/> method divides the messages into chunks when the default size is used.
    /// </summary>
    [Fact]
    public async Task ProcessInChunksAsync_WithDefaultSize_DividesIntoChunks()
    {
        var chunkSize = _options.ChunkSize;
        var totalSize = _options.TotalBatchSize;
        var expectedChunks = (int)Math.Ceiling((double)totalSize / chunkSize);

        _publishingServiceMock
            .Setup(s => s.ProcessPendingMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OutboxProcessingResult { ProcessedCount = chunkSize });

        var result = await _sut.ProcessInChunksAsync();

        result.TotalChunks.Should().Be(expectedChunks);
        result.Success.Should().BeTrue();
        result.StartedAt.Should().NotBe(default(DateTime));
        result.CompletedAt.Should().NotBe(default(DateTime));
    }

    /// <summary>
    /// Verifies that the <see cref="ProcessInChunksAsync"/> method respects the custom total size when specified.
    /// </summary>
    [Fact]
    public async Task ProcessInChunksAsync_WithCustomTotal_RespectsCustomSize()
    {
        var customTotal = 50;
        var chunkSize = _options.ChunkSize;
        var expectedChunks = (int)Math.Ceiling((double)customTotal / chunkSize);

        _publishingServiceMock
            .Setup(s => s.ProcessPendingMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OutboxProcessingResult { ProcessedCount = 20 });

        var result = await _sut.ProcessInChunksAsync(customTotal);

        result.TotalChunks.Should().Be(expectedChunks);
        result.Success.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the <see cref="ProcessInChunksAsync"/> method catches and returns a failure when the publishing service throws an exception.
    /// </summary>
    [Fact]
    public async Task ProcessInChunksAsync_WhenServiceThrows_CatchesAndReturnsFailure()
    {
        _publishingServiceMock
            .Setup(s => s.ProcessPendingMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("processing error"));

        var result = await _sut.ProcessInChunksAsync();

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("processing error");
    }

    /// <summary>
    /// Verifies that the <see cref="ProcessInChunksAsync"/> method tracks the cumulative metrics.
    /// </summary>
    [Fact]
    public async Task ProcessInChunksAsync_TracksCumulativeMetrics()
    {
        _publishingServiceMock
            .Setup(s => s.ProcessPendingMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int size, CancellationToken _) =>
                new OutboxProcessingResult
                {
                    ProcessedCount = Math.Min(size, 10),
                    FailedCount = 2
                });

        var result = await _sut.ProcessInChunksAsync(100);

        result.Success.Should().BeTrue();
        result.TotalProcessed.Should().BeGreaterThan(0);
        result.TotalFailed.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that the <see cref="ProcessScheduledInChunksAsync"/> method delegates to the publishing service.
    /// </summary>
    [Fact]
    public async Task ProcessScheduledInChunksAsync_DelegatesToPublishingService()
    {
        _publishingServiceMock
            .Setup(s => s.ProcessScheduledMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OutboxProcessingResult { ProcessedCount = 20 });

        var result = await _sut.ProcessScheduledInChunksAsync();

        result.Success.Should().BeTrue();
        _publishingServiceMock.Verify(
            s => s.ProcessScheduledMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that the <see cref="ProcessInChunksAsync"/> method creates one chunk when a single message is processed.
    /// </summary>
    [Fact]
    public async Task ProcessInChunksAsync_WithSingleMessage_CreatesOneChunk()
    {
        _publishingServiceMock
            .Setup(s => s.ProcessPendingMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OutboxProcessingResult { ProcessedCount = 1 });

        var result = await _sut.ProcessInChunksAsync(1);

        result.TotalChunks.Should().Be(1);
    }

    /// <summary>
    /// Verifies that the <see cref="ProcessInChunksAsync"/> method sets the duration correctly.
    /// </summary>
    [Fact]
    public async Task ProcessInChunksAsync_SetsDurationCorrectly()
    {
        _publishingServiceMock
            .Setup(s => s.ProcessPendingMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(10);
                return new OutboxProcessingResult { ProcessedCount = 1 };
            });

        var result = await _sut.ProcessInChunksAsync(1);

        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
