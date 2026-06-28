// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

/// <summary>
/// IntegrationExample.cs
/// 
/// Shows how to wire up the outbox pattern into an ASP.NET Core application.
/// </summary>
namespace Examples
{
    public static class IntegrationExample
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Register the Outbox Pattern
            services.AddOutboxPattern(connectionString);

            // Additional custom services can be added here
            // services.AddMessagePublisher<MyCustomPublisher>();
        }
    }
}
