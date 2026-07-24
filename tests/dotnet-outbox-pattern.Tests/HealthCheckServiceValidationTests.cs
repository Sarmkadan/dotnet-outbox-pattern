#nullable enable

using System;
using DotnetOutboxPattern.BackgroundServices;
using FluentAssertions;
using Xunit;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Unit tests for <see cref="HealthCheckServiceValidation"/> static validation class.
/// Tests all three validation patterns: Validate(), IsValid(), and EnsureValid()
/// for both HealthAlert and HealthCheckOptions types.
/// </summary>
public class HealthCheckServiceValidationTests
{
    #region HealthAlert Validation Tests

    [Fact]
    public void Validate_HealthAlert_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        HealthAlert? nullAlert = null;

        // Act
        Action act = () => HealthCheckServiceValidation.Validate(nullAlert);

        // Assert
        act.Should().Throw<ArgumentNullException>("null HealthAlert should throw ArgumentNullException");
    }

    [Fact]
    public void Validate_HealthAlert_WithValidInstance_ReturnsEmptyList()
    {
        // Arrange
        var validAlert = new HealthAlert
        {
            Type = "TestAlert",
            Message = "This is a test alert message",
            RaisedAt = DateTime.UtcNow
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(validAlert);

        // Assert
        errors.Should().BeEmpty("valid HealthAlert should have no validation errors");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_HealthAlert_WithInvalidType_ReturnsError(string? invalidType)
    {
        // Arrange
        var alert = new HealthAlert
        {
            Type = invalidType,
            Message = "Valid message",
            RaisedAt = DateTime.UtcNow
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(alert);

        // Assert
        errors.Should().ContainSingle("invalid Type should produce exactly one error")
            .Which.Should().Be("HealthAlert.Type cannot be null or whitespace");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_HealthAlert_WithInvalidMessage_ReturnsError(string? invalidMessage)
    {
        // Arrange
        var alert = new HealthAlert
        {
            Type = "ValidType",
            Message = invalidMessage,
            RaisedAt = DateTime.UtcNow
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(alert);

        // Assert
        errors.Should().ContainSingle("invalid Message should produce exactly one error")
            .Which.Should().Be("HealthAlert.Message cannot be null or whitespace");
    }

    [Fact]
    public void Validate_HealthAlert_WithDefaultRaisedAt_ReturnsError()
    {
        // Arrange
        var alert = new HealthAlert
        {
            Type = "ValidType",
            Message = "Valid message",
            RaisedAt = default
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(alert);

        // Assert
        errors.Should().ContainSingle("default RaisedAt should produce exactly one error")
            .Which.Should().Be("HealthAlert.RaisedAt cannot be default(DateTime)");
    }

    [Fact]
    public void Validate_HealthAlert_WithFutureRaisedAt_ReturnsError()
    {
        // Arrange
        var alert = new HealthAlert
        {
            Type = "ValidType",
            Message = "Valid message",
            RaisedAt = DateTime.UtcNow.AddMinutes(2)
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(alert);

        // Assert
        errors.Should().ContainSingle("future RaisedAt should produce exactly one error")
            .Which.Should().Be("HealthAlert.RaisedAt cannot be in the future");
    }

    [Fact]
    public void Validate_HealthAlert_WithMultipleInvalidFields_ReturnsMultipleErrors()
    {
        // Arrange
        var alert = new HealthAlert
        {
            Type = null,
            Message = "",
            RaisedAt = default
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(alert);

        // Assert
        errors.Should().HaveCount(3, "multiple invalid fields should produce multiple errors");
        errors.Should().Contain("HealthAlert.Type cannot be null or whitespace");
        errors.Should().Contain("HealthAlert.Message cannot be null or whitespace");
        errors.Should().Contain("HealthAlert.RaisedAt cannot be default(DateTime)");
    }

    [Fact]
    public void IsValid_HealthAlert_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        HealthAlert? nullAlert = null;

        // Act
        Action act = () => HealthCheckServiceValidation.IsValid(nullAlert);

        // Assert
        act.Should().Throw<ArgumentNullException>("null HealthAlert should throw ArgumentNullException");
    }

    [Fact]
    public void IsValid_HealthAlert_WithValidInstance_ReturnsTrue()
    {
        // Arrange
        var validAlert = new HealthAlert
        {
            Type = "TestAlert",
            Message = "This is a test alert message",
            RaisedAt = DateTime.UtcNow
        };

        // Act
        var isValid = HealthCheckServiceValidation.IsValid(validAlert);

        // Assert
        isValid.Should().BeTrue("valid HealthAlert should be considered valid");
    }

    [Fact]
    public void IsValid_HealthAlert_WithInvalidInstance_ReturnsFalse()
    {
        // Arrange
        var invalidAlert = new HealthAlert
        {
            Type = "",
            Message = "",
            RaisedAt = default
        };

        // Act
        var isValid = HealthCheckServiceValidation.IsValid(invalidAlert);

        // Assert
        isValid.Should().BeFalse("invalid HealthAlert should not be considered valid");
    }

    [Fact]
    public void EnsureValid_HealthAlert_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        HealthAlert? nullAlert = null;

        // Act
        Action act = () => HealthCheckServiceValidation.EnsureValid(nullAlert);

        // Assert
        act.Should().Throw<ArgumentNullException>("null HealthAlert should throw ArgumentNullException");
    }

    [Fact]
    public void EnsureValid_HealthAlert_WithValidInstance_DoesNotThrow()
    {
        // Arrange
        var validAlert = new HealthAlert
        {
            Type = "TestAlert",
            Message = "This is a test alert message",
            RaisedAt = DateTime.UtcNow
        };

        // Act
        Action act = () => HealthCheckServiceValidation.EnsureValid(validAlert);

        // Assert
        act.Should().NotThrow("valid HealthAlert should not throw when EnsureValid is called");
    }

    [Fact]
    public void EnsureValid_HealthAlert_WithInvalidInstance_ThrowsArgumentException()
    {
        // Arrange
        var invalidAlert = new HealthAlert
        {
            Type = "",
            Message = "",
            RaisedAt = default
        };

        // Act
        Action act = () => HealthCheckServiceValidation.EnsureValid(invalidAlert);

        // Assert
        act.Should().Throw<ArgumentException>("invalid HealthAlert should throw ArgumentException")
            .WithMessage("*HealthAlert validation failed*");
    }

    #endregion

    #region HealthCheckOptions Validation Tests

    [Fact]
    public void Validate_HealthCheckOptions_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        HealthCheckOptions? nullOptions = null;

        // Act
        Action act = () => HealthCheckServiceValidation.Validate(nullOptions);

        // Assert
        act.Should().Throw<ArgumentNullException>("null HealthCheckOptions should throw ArgumentNullException");
    }

    [Fact]
    public void Validate_HealthCheckOptions_WithValidInstance_ReturnsEmptyList()
    {
        // Arrange
        var validOptions = new HealthCheckOptions
        {
            CheckIntervalMs = 300000, // 5 minutes
            HighFailureRateThreshold = 0.10,
            StuckMessageThreshold = 100,
            DeadLetterThreshold = 50
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(validOptions);

        // Assert
        errors.Should().BeEmpty("valid HealthCheckOptions should have no validation errors");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_HealthCheckOptions_WithNonPositiveCheckInterval_ReturnsError(int invalidInterval)
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            CheckIntervalMs = invalidInterval,
            HighFailureRateThreshold = 0.10,
            StuckMessageThreshold = 100,
            DeadLetterThreshold = 50
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(options);

        // Assert
        errors.Should().ContainSingle("non-positive CheckIntervalMs should produce exactly one error")
            .Which.Should().Be("HealthCheckOptions.CheckIntervalMs must be positive");
    }

    [Fact]
    public void Validate_HealthCheckOptions_WithExcessiveCheckInterval_ReturnsError()
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            CheckIntervalMs = 3600001, // More than 1 hour in milliseconds
            HighFailureRateThreshold = 0.10,
            StuckMessageThreshold = 100,
            DeadLetterThreshold = 50
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(options);

        // Assert
        errors.Should().ContainSingle("excessive CheckIntervalMs should produce exactly one error")
            .Which.Should().Be("HealthCheckOptions.CheckIntervalMs cannot exceed 1 hour");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Validate_HealthCheckOptions_WithInvalidHighFailureRateThreshold_ReturnsError(double invalidThreshold)
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            CheckIntervalMs = 300000,
            HighFailureRateThreshold = invalidThreshold,
            StuckMessageThreshold = 100,
            DeadLetterThreshold = 50
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(options);

        // Assert
        errors.Should().ContainSingle("invalid HighFailureRateThreshold should produce exactly one error")
            .Which.Should().Be("HealthCheckOptions.HighFailureRateThreshold must be between 0 and 1.0");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_HealthCheckOptions_WithNegativeStuckMessageThreshold_ReturnsError(int invalidThreshold)
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            CheckIntervalMs = 300000,
            HighFailureRateThreshold = 0.10,
            StuckMessageThreshold = invalidThreshold,
            DeadLetterThreshold = 50
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(options);

        // Assert
        errors.Should().ContainSingle("negative StuckMessageThreshold should produce exactly one error")
            .Which.Should().Be("HealthCheckOptions.StuckMessageThreshold must be non-negative");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_HealthCheckOptions_WithNegativeDeadLetterThreshold_ReturnsError(int invalidThreshold)
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            CheckIntervalMs = 300000,
            HighFailureRateThreshold = 0.10,
            StuckMessageThreshold = 100,
            DeadLetterThreshold = invalidThreshold
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(options);

        // Assert
        errors.Should().ContainSingle("negative DeadLetterThreshold should produce exactly one error")
            .Which.Should().Be("HealthCheckOptions.DeadLetterThreshold must be non-negative");
    }

    [Fact]
    public void Validate_HealthCheckOptions_WithMultipleInvalidFields_ReturnsMultipleErrors()
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            CheckIntervalMs = 0,
            HighFailureRateThreshold = 1.5,
            StuckMessageThreshold = -10,
            DeadLetterThreshold = -5
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(options);

        // Assert
        errors.Should().HaveCount(4, "multiple invalid fields should produce multiple errors");
        errors.Should().Contain("HealthCheckOptions.CheckIntervalMs must be positive");
        errors.Should().Contain("HealthCheckOptions.HighFailureRateThreshold must be between 0 and 1.0");
        errors.Should().Contain("HealthCheckOptions.StuckMessageThreshold must be non-negative");
        errors.Should().Contain("HealthCheckOptions.DeadLetterThreshold must be non-negative");
    }

    [Fact]
    public void IsValid_HealthCheckOptions_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        HealthCheckOptions? nullOptions = null;

        // Act
        Action act = () => HealthCheckServiceValidation.IsValid(nullOptions);

        // Assert
        act.Should().Throw<ArgumentNullException>("null HealthCheckOptions should throw ArgumentNullException");
    }

    [Fact]
    public void IsValid_HealthCheckOptions_WithValidInstance_ReturnsTrue()
    {
        // Arrange
        var validOptions = new HealthCheckOptions
        {
            CheckIntervalMs = 300000,
            HighFailureRateThreshold = 0.10,
            StuckMessageThreshold = 100,
            DeadLetterThreshold = 50
        };

        // Act
        var isValid = HealthCheckServiceValidation.IsValid(validOptions);

        // Assert
        isValid.Should().BeTrue("valid HealthCheckOptions should be considered valid");
    }

    [Fact]
    public void IsValid_HealthCheckOptions_WithInvalidInstance_ReturnsFalse()
    {
        // Arrange
        var invalidOptions = new HealthCheckOptions
        {
            CheckIntervalMs = 0,
            HighFailureRateThreshold = 1.5,
            StuckMessageThreshold = -10,
            DeadLetterThreshold = -5
        };

        // Act
        var isValid = HealthCheckServiceValidation.IsValid(invalidOptions);

        // Assert
        isValid.Should().BeFalse("invalid HealthCheckOptions should not be considered valid");
    }

    [Fact]
    public void EnsureValid_HealthCheckOptions_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        HealthCheckOptions? nullOptions = null;

        // Act
        Action act = () => HealthCheckServiceValidation.EnsureValid(nullOptions);

        // Assert
        act.Should().Throw<ArgumentNullException>("null HealthCheckOptions should throw ArgumentNullException");
    }

    [Fact]
    public void EnsureValid_HealthCheckOptions_WithValidInstance_DoesNotThrow()
    {
        // Arrange
        var validOptions = new HealthCheckOptions
        {
            CheckIntervalMs = 300000,
            HighFailureRateThreshold = 0.10,
            StuckMessageThreshold = 100,
            DeadLetterThreshold = 50
        };

        // Act
        Action act = () => HealthCheckServiceValidation.EnsureValid(validOptions);

        // Assert
        act.Should().NotThrow("valid HealthCheckOptions should not throw when EnsureValid is called");
    }

    [Fact]
    public void EnsureValid_HealthCheckOptions_WithInvalidInstance_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new HealthCheckOptions
        {
            CheckIntervalMs = 0,
            HighFailureRateThreshold = 1.5,
            StuckMessageThreshold = -10,
            DeadLetterThreshold = -5
        };

        // Act
        Action act = () => HealthCheckServiceValidation.EnsureValid(invalidOptions);

        // Assert
        act.Should().Throw<ArgumentException>("invalid HealthCheckOptions should throw ArgumentException")
            .WithMessage("*HealthCheckOptions validation failed*");
    }

    [Fact]
    public void Validate_HealthCheckOptions_WithBoundaryValues_ReturnsEmptyList()
    {
        // Arrange
        var boundaryOptions = new HealthCheckOptions
        {
            CheckIntervalMs = 1, // Minimum positive value
            HighFailureRateThreshold = 0.0,
            StuckMessageThreshold = 0,
            DeadLetterThreshold = 0
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(boundaryOptions);

        // Assert
        errors.Should().BeEmpty("boundary values should be considered valid");
    }

    [Fact]
    public void Validate_HealthCheckOptions_WithMaximumValidValues_ReturnsEmptyList()
    {
        // Arrange
        var maxValidOptions = new HealthCheckOptions
        {
            CheckIntervalMs = 3600000, // Exactly 1 hour
            HighFailureRateThreshold = 1.0,
            StuckMessageThreshold = int.MaxValue,
            DeadLetterThreshold = int.MaxValue
        };

        // Act
        var errors = HealthCheckServiceValidation.Validate(maxValidOptions);

        // Assert
        errors.Should().BeEmpty("maximum valid values should be considered valid");
    }

    #endregion
}
