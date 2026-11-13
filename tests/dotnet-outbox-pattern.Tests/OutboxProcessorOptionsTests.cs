using System;
using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Exceptions;
using Xunit;

namespace DotnetOutboxPattern.Tests
{
    public class OutboxProcessorOptionsTests
    {
        [Fact]
        public void DefaultOptions_ShouldBeValid()
        {
            // Arrange
            var options = new OutboxProcessorOptions();

            // Act
            var isValid = options.IsValid();
            var validated = options.Validate();

            // Assert
            Assert.True(isValid);
            Assert.Same(options, validated);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void BatchSize_ShouldBePositive(int batchSize)
        {
            // Arrange
            var options = new OutboxProcessorOptions
            {
                BatchSize = batchSize,
                DelayBetweenBatches = 5000
            };

            // Act
            var isValid = options.IsValid();

            // Assert
            Assert.False(isValid);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [Fact]
        public void DelayBetweenBatches_Negative_IsInvalid()
        {
            // Arrange
            var options = new OutboxProcessorOptions
            {
                BatchSize = 10,
                DelayBetweenBatches = -10
            };

            // Act
            var isValid = options.IsValid();

            // Assert
            Assert.False(isValid);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        }

        [Fact]
        public void DelayBetweenBatches_Zero_IsValid()
        {
            // Arrange - a zero base delay is a legitimate configuration (see ConfigureBatch,
            // WithFixedDelay and ValidateBackoff, which all accept it)
            var options = new OutboxProcessorOptions
            {
                BatchSize = 10,
                DelayBetweenBatches = 0
            };

            // Act
            var isValid = options.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void BoundaryValues_ShouldBeValid()
        {
            // Arrange
            var options = new OutboxProcessorOptions
            {
                BatchSize = 1,                     // minimum valid batch size
                DelayBetweenBatches = 1,           // minimum valid delay
                BackoffMultiplier = 1.0,           // minimum valid multiplier
                MaxDelayBetweenBatches = 60000,    // typical maximum delay
                BackoffStrategy = BackoffStrategy.Exponential,
                Enabled = true
            };

            // Act
            var isValid = options.IsValid();
            var validated = options.Validate();

            // Assert
            Assert.True(isValid);
            Assert.Same(options, validated);
        }

        [Fact]
        public void ComputeDelay_WithZeroEmptyBatches_ShouldReturnPositiveTimeSpan()
        {
            // Arrange
            var options = new OutboxProcessorOptions
            {
                BatchSize = 10,
                DelayBetweenBatches = 5000,
                BackoffStrategy = BackoffStrategy.Exponential,
                BackoffMultiplier = 2.0,
                MaxDelayBetweenBatches = 60000
            };

            // Act
            var delay = options.ComputeDelay(0);

            // Assert
            Assert.True(delay > TimeSpan.Zero);
        }
    }
}
