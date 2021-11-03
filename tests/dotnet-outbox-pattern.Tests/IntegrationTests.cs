#nullable enable

using DotnetOutboxPattern.Configuration;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace DotnetOutboxPattern.Tests;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    public HttpClient Client { get; private set; } = null!;

    public IntegrationTestFixture()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OutboxDbContext>));
                    if (descriptor is not null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<OutboxDbContext>(options =>
                    {
                        options.UseSqlite("Data Source=:memory:");
                    });

                    var provider = services.BuildServiceProvider();
                    using var scope = provider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
                    context.Database.EnsureCreated();
                });
            });
    }

    public async Task InitializeAsync()
    {
        Client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

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

        deadLetter.IsReviewed.Should().BeTrue();
        deadLetter.ReviewNotes.Should().Be("reviewed and confirmed");
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
        using var scope = _fixture.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var threadCount = 5;
        var eventsPerThread = 20;
        var totalExpected = threadCount * eventsPerThread;

        var tasks = Enumerable.Range(0, threadCount)
            .Select(threadId =>
                Task.Run(async () =>
                {
                    for (int i = 0; i < eventsPerThread; i++)
                    {
                        await outboxService.PublishEventAsync(new PublishableEvent
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

        var stats = await outboxService.GetStatisticsAsync();
        stats.TotalMessages.Should().BeGreaterThanOrEqualTo(totalExpected);
    }
}
