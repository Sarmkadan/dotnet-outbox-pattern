#nullable enable

using DotnetOutboxPattern.Configuration;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace DotnetOutboxPattern.Tests;

/// <summary>
/// Provides a test fixture that sets up an in-memory integration test environment for the Outbox Pattern application.
/// This fixture creates a WebApplicationFactory with an in-memory SQLite database and provides HTTP client access
/// to test the application's API endpoints and services.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    /// <summary>
/// The WebApplicationFactory that creates and manages the test application instance with in-memory configuration.
/// </summary>
private readonly WebApplicationFactory<Program> _factory;
    /// <summary>
/// The shared SQLite connection that keeps the in-memory database alive for the whole fixture.
/// </summary>
private readonly SqliteConnection _connection;
    /// <summary>
/// The HTTP client used to make requests to the application's API endpoints.
/// </summary>
public HttpClient Client { get; private set; } = null!;

    /// <summary>
/// Initializes a new instance of the <see cref="IntegrationTestFixture"/> class.
/// Sets up an in-memory test environment with a WebApplicationFactory configured to use SQLite in-memory database.
/// </summary>
public IntegrationTestFixture()
    {
        // A SQLite in-memory database lives exactly as long as a connection to it stays
        // open, so this "keeper" connection is held open for the whole fixture's lifetime.
        // Every DbContext gets its own connection to the same named, shared-cache database
        // (via the connection string) rather than reusing a single SqliteConnection object -
        // a bare Microsoft.Data.Sqlite connection is not safe to drive concurrently from
        // multiple DbContext instances at once, which showed up as flaky failures under
        // concurrent load.
        var connectionString = $"Data Source=file:{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // The host reads this connection string at startup and refuses to build without it.
                builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);

                builder.ConfigureServices(services =>
                {
                    // Dropping DbContextOptions<T> alone leaves the provider configuration
                    // registered, and EF then sees both SQL Server and SQLite in one container.
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<OutboxDbContext>) ||
                                    d.ServiceType == typeof(DbContextOptions) ||
                                    d.ServiceType == typeof(OutboxDbContext) ||
                                    (d.ServiceType.IsGenericType &&
                                     d.ServiceType.GetGenericArguments().Contains(typeof(OutboxDbContext))))
                        .ToList();

                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<OutboxDbContext>(options =>
                    {
                        options.UseSqlite(connectionString);
                    });

                    using var provider = services.BuildServiceProvider();
                    using var scope = provider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
                    context.Database.EnsureCreated();
                });
            });
    }

    /// <summary>
/// Initializes the test fixture by creating an HTTP client for making API requests.
/// </summary>
/// <returns>A completed task.</returns>
public async Task InitializeAsync()
    {
        Client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    /// <summary>
/// Disposes the HTTP client and WebApplicationFactory when the test fixture is no longer needed.
/// </summary>
/// <returns>A completed task.</returns>
public async Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        await _connection.DisposeAsync();
    }

    /// <summary>
/// Creates a new service scope for resolving scoped services during tests.
/// </summary>
/// <returns>A new <see cref="IServiceScope"/> instance.</returns>
public IServiceScope CreateScope()
    {
        return _factory.Services.CreateScope();
    }
}

public sealed class OutboxEndToEndIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public OutboxEndToEndIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_Endpoint_ReturnsOk()
    {
        var response = await _fixture.Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task PublishEvent_WithValidEvent_CreatesOutboxMessage()
    {
        var publishEvent = new PublishableEvent
        {
            Event = new EntityCreatedEvent { EntityId = "test-1", EntityType = "Order" },
            Topic = "orders.created",
            MaxAttempts = 3
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/outbox/events", publishEvent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMessage_WithValidId_ReturnsMessage()
    {
        using var scope = _fixture.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var publishEvent = new PublishableEvent
        {
            Event = new EntityCreatedEvent { EntityId = "test-2", EntityType = "Product" },
            Topic = "products.created"
        };

        var message = await outboxService.PublishEventAsync(publishEvent);

        var response = await _fixture.Client.GetAsync($"/api/outbox/messages/{message.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStatistics_ReturnsOutboxStatistics()
    {
        var response = await _fixture.Client.GetAsync("/api/outbox/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalMessages");
    }

    [Fact]
    public async Task PublishEvent_WithDuplicateIdempotencyKey_ReturnsExistingMessage()
    {
        var eventId = Guid.NewGuid();
        using var scope = _fixture.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var publishEvent1 = new PublishableEvent
        {
            Event = new EntityCreatedEvent
            {
                EventId = eventId,
                EntityId = "test-3",
                EntityType = "Invoice"
            },
            Topic = "invoices.created"
        };

        var message1 = await outboxService.PublishEventAsync(publishEvent1);

        var publishEvent2 = new PublishableEvent
        {
            Event = new EntityCreatedEvent
            {
                EventId = eventId,
                EntityId = "test-3",
                EntityType = "Invoice"
            },
            Topic = "invoices.created"
        };

        var message2 = await outboxService.PublishEventAsync(publishEvent2);

        message1.Id.Should().Be(message2.Id);
    }

    [Fact]
    public async Task PublishEvent_CreatesMessageInPendingState()
    {
        using var scope = _fixture.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var publishEvent = new PublishableEvent
        {
            Event = new EntityCreatedEvent { EntityId = "test-4", EntityType = "Order" },
            Topic = "orders.created"
        };

        var message = await outboxService.PublishEventAsync(publishEvent);

        message.State.Should().Be(OutboxMessageState.Pending);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RetryFailedMessage_ResetsMessageState()
    {
        using var scope = _fixture.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var publishEvent = new PublishableEvent
        {
            Event = new EntityCreatedEvent { EntityId = "test-5", EntityType = "Order" },
            Topic = "orders.created"
        };

        var message = await outboxService.PublishEventAsync(publishEvent);
        message.State = OutboxMessageState.Failed;
        message.PublishAttempts = 5;
        message.ErrorMessage = "connection timeout";

        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        await repo.UpdateAsync(message);

        var success = await outboxService.RetryFailedMessageAsync(message.Id);

        success.Should().BeTrue();
        var updated = await repo.GetByIdAsync(message.Id);
        updated!.State.Should().Be(OutboxMessageState.Pending);
        updated.PublishAttempts.Should().Be(0);
        updated.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetUnreviewedDeadLetters_ReturnsDeadLetters()
    {
        var response = await _fixture.Client.GetAsync("/api/deadletters/unreviewed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("[");
    }

    [Fact]
    public async Task DeadLetterWorkflow_MovesFailedMessageToDlq()
    {
        using var scope = _fixture.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var dlService = scope.ServiceProvider.GetRequiredService<IDeadLetterService>();

        var publishEvent = new PublishableEvent
        {
            Event = new EntityCreatedEvent { EntityId = "test-6", EntityType = "Order" },
            Topic = "orders.created"
        };

        var message = await outboxService.PublishEventAsync(publishEvent);
        message.State = OutboxMessageState.Failed;
        message.PublishAttempts = 5;
        message.MaxPublishAttempts = 5;
        message.ErrorMessage = "broker unavailable";

        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        await repo.UpdateAsync(message);

        var deadLetter = await dlService.MoveToDlqAsync(message);

        deadLetter.Should().NotBeNull();
        deadLetter.OutboxMessageId.Should().Be(message.Id);
        deadLetter.Topic.Should().Be(message.Topic);
        deadLetter.IsReviewed.Should().BeFalse();
    }

    [Fact]
    public async Task ReviewDeadLetter_MarksAsReviewed()
    {
        using var scope = _fixture.CreateScope();
        var dlService = scope.ServiceProvider.GetRequiredService<IDeadLetterService>();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var publishEvent = new PublishableEvent
        {
            Event = new EntityCreatedEvent { EntityId = "test-7", EntityType = "Order" },
            Topic = "orders.created"
        };

        var message = await outboxService.PublishEventAsync(publishEvent);
        message.State = OutboxMessageState.Failed;
        message.ErrorMessage = "error";

        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        await repo.UpdateAsync(message);

        var deadLetter = await dlService.MoveToDlqAsync(message);

        await dlService.ReviewAsync(deadLetter.Id, "reviewed and confirmed");

        var dlRepo = scope.ServiceProvider.GetRequiredService<IDeadLetterRepository>();
        var updated = await dlRepo.GetByIdAsync(deadLetter.Id);

        updated!.IsReviewed.Should().BeTrue();
        updated.ReviewNotes.Should().Be("reviewed and confirmed");
    }

    [Fact]
    public async Task MessageStatistics_TracksPublishedMessages()
    {
        using var scope = _fixture.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        for (int i = 0; i < 3; i++)
        {
            var publishEvent = new PublishableEvent
            {
                Event = new EntityCreatedEvent { EntityId = $"test-{i}", EntityType = "Order" },
                Topic = "orders.created"
            };
            await outboxService.PublishEventAsync(publishEvent);
        }

        var stats = await outboxService.GetStatisticsAsync();

        stats.TotalMessages.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ConcurrentPublish_HandlesMultipleEventsSimultaneously()
    {
        using var scope = _fixture.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var tasks = Enumerable.Range(0, 10)
            .Select(i => outboxService.PublishEventAsync(new PublishableEvent
            {
                Event = new EntityCreatedEvent
                {
                    EventId = Guid.NewGuid(),
                    EntityId = $"test-{i}",
                    EntityType = "Order"
                },
                Topic = "orders.created"
            }))
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(10);
        results.Select(r => r.Id).Distinct().Should().HaveCount(10);
    }

    [Fact]
    public async Task GetMessage_WithInvalidId_ReturnsNotFound()
    {
        var invalidId = Guid.NewGuid();
        var response = await _fixture.Client.GetAsync($"/api/outbox/messages/{invalidId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public sealed class ConcurrencyIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ConcurrencyIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MultipleThreads_PublishSimultaneously_NoDataLoss()
    {
        var threadCount = 5;
        var eventsPerThread = 20;
        var totalExpected = threadCount * eventsPerThread;

        // Each concurrent worker needs its own DI scope (and therefore its own
        // DbContext instance) - a DbContext is not thread-safe and cannot be shared
        // across parallel operations.
        var tasks = Enumerable.Range(0, threadCount)
            .Select(threadId =>
                Task.Run(async () =>
                {
                    using var workerScope = _fixture.CreateScope();
                    var workerOutboxService = workerScope.ServiceProvider.GetRequiredService<IOutboxService>();

                    for (int i = 0; i < eventsPerThread; i++)
                    {
                        await workerOutboxService.PublishEventAsync(new PublishableEvent
                        {
                            Event = new EntityCreatedEvent
                            {
                                EventId = Guid.NewGuid(),
                                EntityId = $"test-{threadId}-{i}",
                                EntityType = "Order"
                            },
                            Topic = "orders.created"
                        });
                    }
                }))
            .ToList();

        await Task.WhenAll(tasks);

        using var scope = _fixture.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var stats = await outboxService.GetStatisticsAsync();
        stats.TotalMessages.Should().BeGreaterThanOrEqualTo(totalExpected);
    }
}
