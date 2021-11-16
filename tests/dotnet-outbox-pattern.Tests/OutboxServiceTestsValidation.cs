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
        public static IReadOnlyList<string> Validate(this OutboxServiceTests value)
        {
            var problems = new List<string>();

            if (value is null)
            {
                problems.Add("OutboxServiceTests instance is null.");
                // No further checks are possible because the class only contains methods.
                return problems;
            }

            // The class only contains test methods; there are no mutable state members to validate.
            // If future members (e.g., strings, numbers, dates) are added, additional checks can be placed here.

            return problems;
        }

        /// <summary>
        /// Determines whether the <see cref="OutboxServiceTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test class instance to check.</param>
        /// <returns><c>true</c> if no validation problems are found; otherwise, <c>false</c>.</returns>
        public static bool IsValid(this OutboxServiceTests value) => !value.Validate().Any();

        /// <summary>
        /// Ensures that the <see cref="OutboxServiceTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test class instance to validate.</param>
        /// <exception cref="ArgumentException">Thrown when validation problems are found.</exception>
        public static void EnsureValid(this OutboxServiceTests value)
        {
            var problems = value.Validate();
            if (problems.Any())
            {
                var message = $"OutboxServiceTests validation failed: {string.Join("; ", problems)}";
                throw new ArgumentException(message, nameof(value));
            }
        }
    }
}
