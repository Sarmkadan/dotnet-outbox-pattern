// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Serilog;
using DotnetOutboxPattern.Configuration;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Infrastructure;
using DotnetOutboxPattern.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/outbox-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Outbox Pattern application");

    // Add services
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not configured");

    builder.Services.AddOutboxPattern(connectionString, options =>
    {
        options.MaxRetries = builder.Configuration.GetValue("Outbox:MaxRetries", 5);
        options.RetryPolicy = Enum.Parse<DotnetOutboxPattern.Domain.RetryPolicyType>(
            builder.Configuration["Outbox:RetryPolicy"] ?? "ExponentialBackoff");
        options.DeliveryGuarantee = Enum.Parse<DotnetOutboxPattern.Domain.DeliveryGuarantee>(
            builder.Configuration["Outbox:DeliveryGuarantee"] ?? "AtLeastOnce");
    });

    // Register default message publisher (replace with your implementation)
    builder.Services.AddMessagePublisher<DefaultMessagePublisher>();

    // Register background processor
    var processorOptions = new OutboxProcessorOptions
    {
        Enabled = builder.Configuration.GetValue("Outbox:ProcessorEnabled", true),
        BatchSize = builder.Configuration.GetValue("Outbox:BatchSize", 100),
        DelayBetweenBatches = builder.Configuration.GetValue("Outbox:DelayBetweenBatches", 5000),
        PreservePartitionOrdering = builder.Configuration.GetValue("Outbox:PreservePartitionOrdering", true)
    };

    builder.Services.AddSingleton(processorOptions);
    builder.Services.AddHostedService<OutboxProcessor>();

    // Add controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Outbox Pattern API",
            Version = "v1",
            Description = "Transactional outbox pattern for .NET - guaranteed message delivery",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Vladyslav Zaiets",
                Url = new Uri("https://sarmkadan.com")
            },
            License = new Microsoft.OpenApi.Models.OpenApiLicense
            {
                Name = "MIT"
            }
        });
    });

    var app = builder.Build();

    // Initialize database
    Log.Information("Initializing database");
    await app.Services.InitializeDatabaseAsync();

    // Configure middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

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
    })
    .WithName("Health")
    .WithOpenApi();

    // Outbox statistics endpoint
    app.MapGet("/api/outbox/statistics", async (IOutboxService outboxService) =>
    {
        return await outboxService.GetStatisticsAsync();
    })
    .WithName("GetOutboxStatistics")
    .WithOpenApi();

    // Get message endpoint
    app.MapGet("/api/outbox/messages/{messageId}", async (Guid messageId, IOutboxService outboxService) =>
    {
        var message = await outboxService.GetMessageAsync(messageId);
        return message != null ? Results.Ok(message) : Results.NotFound();
    })
    .WithName("GetMessage")
    .WithOpenApi();

    // Publish event endpoint (for testing)
    app.MapPost("/api/outbox/events", async (DotnetOutboxPattern.Domain.PublishableEvent request, IOutboxService outboxService) =>
    {
        var message = await outboxService.PublishEventAsync(request);
        return Results.Created($"/api/outbox/messages/{message.Id}", message);
    })
    .WithName("PublishEvent")
    .WithOpenApi();

    // Get unreviewed dead letters endpoint
    app.MapGet("/api/deadletters/unreviewed", async (IDeadLetterService dlService) =>
    {
        return await dlService.GetUnreviewedAsync();
    })
    .WithName("GetUnreviewedDeadLetters")
    .WithOpenApi();

    // Review dead letter endpoint
    app.MapPut("/api/deadletters/{deadLetterId}/review", async (Guid deadLetterId, ReviewRequest request, IDeadLetterService dlService) =>
    {
        await dlService.ReviewAsync(deadLetterId, request.Notes);
        return Results.NoContent();
    })
    .WithName("ReviewDeadLetter")
    .WithOpenApi();

    // Requeue dead letter endpoint
    app.MapPut("/api/deadletters/{deadLetterId}/requeue", async (Guid deadLetterId, RequeueRequest request, IDeadLetterService dlService) =>
    {
        await dlService.RequeueAsync(deadLetterId, request.Reason);
        return Results.NoContent();
    })
    .WithName("RequeueDeadLetter")
    .WithOpenApi();

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
public class ReviewRequest
{
    public string Notes { get; set; } = null!;
}

/// <summary>
/// Request model for requeuing dead letters
/// </summary>
public class RequeueRequest
{
    public string Reason { get; set; } = null!;
}
