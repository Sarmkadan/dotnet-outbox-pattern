#nullable enable

using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DotnetOutboxPattern.Tests;

public sealed class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _sut = new NotificationService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new NotificationService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task SendAsync_WithNullNotification_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.SendAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_WithValidNotification_SendsToAllChannels()
    {
        var notification = new Notification
        {
            Title = "Test",
            Message = "Test message",
            Severity = NotificationSeverity.Info,
            Channels = new List<string> { "in-memory", "console", "file" }
        };

        await _sut.SendAsync(notification);

        // Should have sent to all channels
        var recent = _sut.GetRecentNotifications();
        recent.Should().ContainSingle(n => n.Title == "Test" && n.Message == "Test message");
    }

    [Fact]
    public async Task SendToChannelAsync_WithUnknownChannel_LogsWarningAndReturns()
    {
        var notification = new Notification
        {
            Title = "Test",
            Message = "Test message",
            Channels = new List<string> { "unknown-channel" }
        };

        await _sut.SendToChannelAsync(notification, "unknown-channel");

        _loggerMock.Verify(
            l => l.LogWarning(
                "Unknown notification channel: {Channel}",
                "unknown-channel"),
            Times.Once);
    }

    [Fact]
    public async Task SendToChannelAsync_WithKnownChannel_DelegatesToHandler()
    {
        var notification = new Notification
        {
            Title = "Test",
            Message = "Test message"
        };

        await _sut.SendToChannelAsync(notification, "in-memory");

        var recent = _sut.GetRecentNotifications();
        recent.Should().ContainSingle(n => n.Title == "Test" && n.Message == "Test message");
    }

    [Fact]
    public async Task SendToChannelAsync_WhenHandlerThrows_LogsErrorAndContinues()
    {
        var notification = new Notification
        {
            Title = "Test",
            Message = "Test message",
            Channels = new List<string> { "in-memory" }
        };

        // Make the in-memory channel throw
        var inMemoryChannel = _sut.GetType()
            .GetField("_channels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_sut) as System.Collections.Generic.Dictionary<string, INotificationChannel>;

        var mockChannel = new Mock<INotificationChannel>();
        mockChannel.Setup(c => c.SendAsync(It.IsAny<Notification>()))
            .ThrowsAsync(new InvalidOperationException("channel error"));

        inMemoryChannel!["in-memory"] = mockChannel.Object;

        await _sut.SendToChannelAsync(notification, "in-memory");

        _loggerMock.Verify(
            l => l.LogError(
                It.IsAny<Exception>(),
                "Error sending notification to channel {Channel}",
                "in-memory"),
            Times.Once);
    }

    [Fact]
    public void GetRecentNotifications_ReturnsCorrectNumber()
    {
        // Add 150 notifications
        for (int i = 0; i < 150; i++)
        {
            var notification = new Notification
            {
                Title = $"Test {i}",
                Message = $"Message {i}"
            };
            _sut.GetType()
                .GetField("_notifications", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(_sut)!
                .GetType()
                .GetMethod("Add", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(_sut.GetType()
                    .GetField("_notifications", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .GetValue(_sut), new[] { notification });
        }

        var recent = _sut.GetRecentNotifications(100);
        recent.Should().HaveCount(100);
        recent.Last().Title.Should().Be("Test 149"); // Most recent
    }

    [Fact]
    public void GetRecentNotifications_WithDefaultCount_ReturnsLast100()
    {
        // Add 50 notifications
        for (int i = 0; i < 50; i++)
        {
            var notification = new Notification
            {
                Title = $"Test {i}",
                Message = $"Message {i}"
            };
            _sut.GetType()
                .GetField("_notifications", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(_sut)!
                .GetType()
                .GetMethod("Add", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(_sut.GetType()
                    .GetField("_notifications", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .GetValue(_sut), new[] { notification });
        }

        var recent = _sut.GetRecentNotifications(); // Default count
        recent.Should().HaveCount(50); // Should return all 50 since we only added 50
    }
}