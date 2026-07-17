using System;

namespace DotnetOutboxPattern.Tests
{
    /// <summary>
    /// Extension methods that simplify common test setup and verification for <see cref="OutboxMessageTests"/>.
    /// </summary>
    public static class OutboxMessageTestsExtensions
    {
        /// <summary>
        /// Asserts that a newly created message passes validation without throwing.
        /// </summary>
        /// <param name="test">The test instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="test"/> is <c>null</c>.</exception>
        public static void AssertValidMessage(this OutboxMessageTests test)
        {
            ArgumentNullException.ThrowIfNull(test);
            test.Validate_WithValidMessage_DoesNotThrow();
        }

        /// <summary>
        /// Locks the message and returns the test instance for fluent chaining.
        /// </summary>
        /// <param name="test">The test instance.</param>
        /// <returns>The same <see cref="OutboxMessageTests"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="test"/> is <c>null</c>.</exception>
        public static OutboxMessageTests LockAndReturn(this OutboxMessageTests test)
        {
            ArgumentNullException.ThrowIfNull(test);
            test.Lock_SetsIsLockedTrueAndStateToProcessing();
            return test;
        }

        /// <summary>
        /// Simulates a number of publish failures and returns the test instance.
        /// </summary>
        /// <param name="test">The test instance.</param>
        /// <param name="failureCount">The number of failures to record. Must be non‑negative.</param>
        /// <returns>The same <see cref="OutboxMessageTests"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="test"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="failureCount"/> is negative.</exception>
        public static OutboxMessageTests SimulateFailures(this OutboxMessageTests test, int failureCount)
        {
            ArgumentNullException.ThrowIfNull(test);
            ArgumentOutOfRangeException.ThrowIfNegative(failureCount);
            for (int i = 0; i < failureCount; i++)
            {
                test.RecordFailure_IncrementsPublishAttempts();
            }

            return test;
        }
    }
}
