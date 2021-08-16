// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Services;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// BasicUsage.cs
/// 
/// Demonstrates minimal setup and publication of a message.
/// </summary>
namespace Examples
{
    public class BasicUsage
    {
        public static async Task ExecuteAsync(IServiceProvider serviceProvider)
        {
            // Get the outbox service from DI
            var outboxService = serviceProvider.GetRequiredService<IOutboxService>();

            // Minimal event definition (assuming a class implementing PublishableEvent)
            var myEvent = new { Data = "Hello World" };

            // Publish message
            await outboxService.PublishEventAsync(
                @event: myEvent,
                topic: "my.topic",
                partitionKey: "my-partition");
        }
    }
}
