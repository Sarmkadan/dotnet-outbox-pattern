#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Examples
{
    /// <summary>
    /// Extension methods for BasicEventPublishingExample to demonstrate advanced usage
    /// patterns and provide utility functionality.
    /// </summary>
    public static class BasicEventPublishingExampleExtensions
    {
        /// <summary>
        /// Creates a user registration event with the specified details.
        /// </summary>
        /// <param name="example">The example instance</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="email">The user email address</param>
        /// <param name="fullName">The user's full name</param>
        /// <returns>A configured UserRegisteredEvent instance</returns>
        public static BasicEventPublishingExample.UserRegisteredEvent CreateUserRegisteredEvent(
            this BasicEventPublishingExample example,
            string userId,
            string email,
            string fullName)
        {
            return new BasicEventPublishingExample.UserRegisteredEvent
            {
                UserId = userId,
                Email = email,
                FullName = fullName,
                RegisteredAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates and registers a user with validation and returns the event.
        /// </summary>
        /// <param name="example">The example instance</param>
        /// <param name="userService">The user service</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="email">The user email address</param>
        /// <param name="fullName">The user's full name</param>
        /// <returns>The published outbox message</returns>
        public static async Task<OutboxMessage> RegisterUserWithValidationAsync(
            this BasicEventPublishingExample example,
            UserService userService,
            string userId,
            string email,
            string fullName)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                throw new ArgumentException("Invalid email address", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Full name cannot be null or empty", nameof(fullName));
            }

            // Register the user
            await userService.RegisterUserAsync(userId, email, fullName);

            // Return the outbox message that was created
            // Note: In a real implementation, you would need to retrieve this from the service
            // This is a simplified example showing the pattern
            return new OutboxMessage
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "users.registered",
                EventId = userId,
                Content = $"User registered: {userId}",
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a service provider with all required dependencies for the example.
        /// </summary>
        /// <param name="example">The example instance</param>
        /// <returns>A configured service provider</returns>
        public static IServiceProvider CreateServiceProvider(
            this BasicEventPublishingExample example)
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(configure => configure.AddConsole());

            // Add outbox pattern (mock implementation for demonstration)
            services.AddScoped<IOutboxService>(_ => new MockOutboxService());

            // Add user service
            services.AddScoped<UserService>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Demonstrates batch user registration with a single service provider.
        /// </summary>
        /// <param name="example">The example instance</param>
        /// <param name="users">Collection of user registration data</param>
        public static async Task BatchRegisterUsersAsync(
            this BasicEventPublishingExample example,
            IEnumerable<(string UserId, string Email, string FullName)> users)
        {
            var serviceProvider = example.CreateServiceProvider();
            var userService = serviceProvider.GetRequiredService<UserService>();

            foreach (var user in users)
            {
                await userService.RegisterUserAsync(user.UserId, user.Email, user.FullName);
            }
        }

        // Mock outbox service for demonstration purposes
        private sealed class MockOutboxService : IOutboxService
        {
            public Task<OutboxMessage> PublishEventAsync(
                DomainEvent domainEvent,
                string eventType,
                string? correlationId = null,
                CancellationToken cancellationToken = default)
            {
                var message = new OutboxMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    EventType = eventType,
                    EventId = correlationId ?? Guid.NewGuid().ToString(),
                    Content = System.Text.Json.JsonSerializer.Serialize(domainEvent),
                    CreatedAt = DateTime.UtcNow
                };

                return Task.FromResult(message);
            }
        }
    }

    /// <summary>
    /// Represents an outbox message in the pattern.
    /// </summary>
    public sealed class OutboxMessage
    {
        public string Id { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}