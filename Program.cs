#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.ComponentModel.DataAnnotations;
using Serilog;
using DotnetOutboxPattern.Configuration;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Outbox Pattern application");

    // Add services
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not configured");

    // Configure Outbox Pattern with IOptions pattern
    builder.Services.Configure<DotnetOutboxPatternOptions>(builder.Configuration.GetSection(DotnetOutboxPatternOptions.SectionName));
    builder.Services.AddOutboxPattern(connectionString);
    builder.Services.AddOutboxPatternPhase2();

    // Register default message publisher (replace with your implementation)
    builder.Services.AddMessagePublisher<DefaultMessagePublisher>();

    // Register background processor with configuration
    builder.Services.AddOptions<OutboxProcessorOptions>()
        .Bind(builder.Configuration.GetSection(DotnetOutboxPatternOptions.SectionName))
        .ValidateDataAnnotations();

    builder.Services.AddSingleton<IOutboxProcessorOptions>(sp =>
        sp.GetRequiredService<IOptions<OutboxProcessorOptions>>().Value);
    builder.Services.AddHostedService<OutboxProcessor>();

    // Add controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    var app = builder.Build();

    // Initialize database
    Log.Information("Initializing database");
    await app.Services.InitializeDatabaseAsync();

    // Configure middleware. Error handling / request logging / rate limiting / perf
    // monitoring were registered in DI but never applied to the pipeline before.
    app.UseOutboxPatternMiddleware();
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    // Expose the Prometheus scrape endpoint (/metrics) - the OpenTelemetry exporter is
    // registered in AddOutboxPatternPhase2 but has no effect without this mapping.
    app.MapPrometheusScrapingEndpoint();

    // Health check endpoint
    app.MapGet("/health", async (IOutboxService outboxService) =>
    {
        var stats = await outboxService.GetStatisticsAsync();
        return new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            statistics = stats
        };
    }).WithName("Health");

    // Note: statistics, message retrieval, event publishing, and dead letter review/requeue
    // are served by the MVC controllers (OutboxMessageController, DeadLetterController) mapped
    // via app.MapControllers() above. Minimal API endpoints for the same routes used to be
    // registered here too, which caused an AmbiguousMatchException (500) on every request to
    // those paths since two endpoints matched the same route template.

    Log.Information("Application started successfully");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Request model for reviewing dead letters
/// </summary>
public sealed class ReviewRequest
{
    /// <summary>
    /// Gets or sets the review notes
    /// </summary>
    [Required]
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Request model for requeuing dead letters
/// </summary>
public sealed class RequeueRequest
{
    /// <summary>
    /// Gets or sets the reason for requeuing
    /// </summary>
    [Required]
    [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
    public string Reason { get; set; } = string.Empty;
}
