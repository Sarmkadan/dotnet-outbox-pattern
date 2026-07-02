using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetOutboxPattern.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class OutboxServiceBenchmarks : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private IOutboxService? _outboxService;
    private OutboxDbContext? _context;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddDbContext<OutboxDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=OutboxBenchmarks;Trusted_Connection=True;MultipleActiveResultSets=true")
        );

        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<IOutboxSerializer, SystemTextJsonOutboxSerializer>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IOutboxService, OutboxService>();

        _serviceProvider = services.BuildServiceProvider();
        _outboxService = _serviceProvider.GetRequiredService<IOutboxService>();
        _context = _serviceProvider.GetRequiredService<OutboxDbContext>();

        // Initialize database
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
        try
        {
            _context?.Database.EnsureDeleted();
        }
        catch
        {
            // Database might already be deleted
        }
        _context?.Dispose();
    }

    [Benchmark]
    public async Task PublishSingleEvent()
    {
        var domainEvent = new EntityCreatedEvent
        {
            EventId = Guid.NewGuid(),
            EntityId = Guid.NewGuid().ToString(),
            EntityType = "TestEntity",
            Timestamp = DateTime.UtcNow
        };

        await _outboxService!.PublishEventAsync(domainEvent, "test.topic");
    }

    [Benchmark]
    public async Task PublishMultipleEvents_Sequential()
    {
        for (int i = 0; i < 10; i++)
        {
            var domainEvent = new EntityCreatedEvent
            {
                EventId = Guid.NewGuid(),
                EntityId = Guid.NewGuid().ToString(),
                EntityType = "TestEntity",
                Timestamp = DateTime.UtcNow
            };

            await _outboxService!.PublishEventAsync(domainEvent, "test.topic");
        }
    }

    [Benchmark]
    public async Task GetStatistics()
    {
        await _outboxService!.GetStatisticsAsync();
    }

    [Benchmark]
    public async Task GetMessageById()
    {
        var messageId = Guid.NewGuid();
        await _outboxService!.GetMessageAsync(messageId);
    }
}
