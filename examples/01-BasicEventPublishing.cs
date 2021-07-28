// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Example 1: Basic Event Publishing
///
/// Demonstrates how to publish a simple domain event using the outbox pattern.
/// This is the most common use case - create an event, publish it atomically
/// with your domain changes.
/// </summary>

namespace Examples
{
    public class BasicEventPublishingExample
    {
        // Define your domain event
        public class UserRegisteredEvent : DomainEvent
        {
            public string UserId { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public DateTime RegisteredAt { get; set; }
        }

        public class UserService
        {
            private readonly IOutboxService _outboxService;
            private readonly ILogger<UserService> _logger;

            public UserService(IOutboxService outboxService, ILogger<UserService> logger)
            {
                _outboxService = outboxService;
                _logger = logger;
            }

            /// <summary>
            /// Registers a user and publishes a domain event.
            /// Both operations happen atomically - either both succeed or both fail.
            /// </summary>
            public async Task RegisterUserAsync(string userId, string email, string fullName)
            {
                _logger.LogInformation("Registering user: {UserId} ({Email})", userId, email);

                // In a real application, you would also save the user to your domain database
                // using the same DbContext to ensure atomicity:
                //
                // var user = new User { Id = userId, Email = email, FullName = fullName };
                // dbContext.Users.Add(user);
                // await dbContext.SaveChangesAsync();

                // Create the domain event
                var userEvent = new UserRegisteredEvent
                {
                    UserId = userId,
                    Email = email,
                    FullName = fullName,
                    RegisteredAt = DateTime.UtcNow
                };

                // Publish the event
                // This stores it in the OutboxMessages table atomically
                var outboxMessage = await _outboxService.PublishEventAsync(
                    userEvent,
                    "users.registered",
                    userId);

                _logger.LogInformation(
                    "User event published with ID: {MessageId}",
                    outboxMessage.Id);
            }
        }

        public static async Task Main(string[] args)
        {
            var services = new ServiceCollection();

            // Add your services
            // services.AddOutboxPattern("your-connection-string");
            // services.AddMessagePublisher<YourPublisher>();
            // services.AddScoped<UserService>();

            // Example usage:
            // var provider = services.BuildServiceProvider();
            // var userService = provider.GetRequiredService<UserService>();
            // await userService.RegisterUserAsync("USER-123", "alice@example.com", "Alice Johnson");

            Console.WriteLine("Example: Basic Event Publishing");
            Console.WriteLine("See UserService.RegisterUserAsync() for implementation details");

            await Task.CompletedTask;
        }
    }
}
