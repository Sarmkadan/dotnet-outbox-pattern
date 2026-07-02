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
public class MessagePublishingServiceBenchmarks : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private IMessagePublishingService? _publishingService;
    private IOutboxService? _outboxService;
    private OutboxDbContext? _context;
    private const int BatchSize = 100;

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
        services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();
        services.AddScoped<IMessagePublisher, NullMessagePublisher>();
        services.AddScoped<IMessagePublishingService, MessagePublishingService>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.Configure<PublishingOptions>(options =>
        {
            options.PublishTimeout = TimeSpan.FromSeconds(30);
        });

        _serviceProvider = services.BuildServiceProvider();
        _publishingService = _serviceProvider.GetRequiredService<IMessagePublishingService>();
        _outboxService = _serviceProvider.GetRequiredService<IOutboxService>();
        _context = _serviceProvider.GetRequiredService<OutboxDbContext>();

        // Initialize database
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        // Pre-populate with test data
        PreloadTestData();
    }

    private void PreloadTestData()
    {
        var messages = new List<OutboxMessage>();
        for (int i = 0; i < BatchSize; i++)
        {
            messages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = Guid.NewGuid().ToString(),
                AggregateId = Guid.NewGuid().ToString(),
                AggregateType = "TestAggregate",
                EventType = EventType.Created,
                EventData = $"{\"Test\":\"Data{i}\"}",
                EventTypeName = "TestEvent",
                Topic = "test.topic",
                PartitionKey = "test-partition",
                State = OutboxMessageState.Pending,
                CreatedAt = DateTime.UtcNow,
                MaxPublishAttempts = 5
            });
        }

        _context.OutboxMessages.AddRange(messages);
        _context.SaveChanges();
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
    public async Task ProcessPendingMessages_Batch100()
    {
        await _publishingService!.ProcessPendingMessagesAsync(BatchSize);
    }

    [Benchmark]
    public async Task ProcessPartition_Batch100()
    {
        await _publishingService!.ProcessPartitionAsync("test-partition", BatchSize);
    }

    [Benchmark]
    public async Task ProcessSingleMessage()
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid().ToString(),
            AggregateId = Guid.NewGuid().ToString(),
            AggregateType = "TestAggregate",
            EventType = EventType.Created,
            EventData = "{\"Test\":\"Data\"}",
            EventTypeName = "TestEvent",
            Topic = "test.topic",
            PartitionKey = "test-partition",
            State = OutboxMessageState.Pending,
            CreatedAt = DateTime.UtcNow,
            MaxPublishAttempts = 5
        };

        await _context!.OutboxMessages.AddAsync(message);
        await _context.SaveChangesAsync();

        await _publishingService!.ProcessSingleMessageAsync(message.Id);
    }
}

/// <summary>
/// Null implementation of IMessagePublisher for benchmarking without external dependencies
/// </summary>
public class NullMessagePublisher : IMessagePublisher
{
    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        // Simulate successful publishing without actual network calls
        return Task.CompletedTask;
    }
}
