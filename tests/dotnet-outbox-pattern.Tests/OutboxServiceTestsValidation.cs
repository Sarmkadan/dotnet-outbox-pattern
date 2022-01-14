using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetOutboxPattern.Tests
{
    /// <summary>
    /// Validation helpers for <see cref="OutboxServiceTests"/>.
    /// </summary>
    public static class OutboxServiceTestsValidation
    {
        /// <summary>
        /// Validates the <see cref="OutboxServiceTests"/> instance and returns a list of human‑readable problems.
        /// </summary>
        /// <param name="value">The test class instance to validate.</param>
        /// <returns>A read‑only list of validation error messages. Empty if the instance is valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> Validate(this OutboxServiceTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // The class only contains test methods; there are no mutable state members to validate.
            // If future members (e.g., strings, numbers, dates) are added, additional checks can be placed here.

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the <see cref="OutboxServiceTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test class instance to check.</param>
        /// <returns><c>true</c> if no validation problems are found; otherwise, <c>false</c>.</returns>
        public static bool IsValid(this OutboxServiceTests value) => value.Validate().Count == 0;

        /// <summary>
        /// Ensures that the <see cref="OutboxServiceTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test class instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when validation problems are found.</exception>
        public static void EnsureValid(this OutboxServiceTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                var message = $"OutboxServiceTests validation failed: {string.Join("; ", problems)}";
                throw new ArgumentException(message, nameof(value));
            }
        }
    }
}
