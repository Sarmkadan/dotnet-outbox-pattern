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
public class BatchProcessingBenchmarks : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private IMessagePublishingService? _publishingService;
    private IOutboxService? _outboxService;
    private OutboxDbContext? _context;
    private const int BatchSize10 = 10;
    private const int BatchSize50 = 50;
    private const int BatchSize100 = 100;
    private const int BatchSize200 = 200;

    [Params(BatchSize10, BatchSize50, BatchSize100, BatchSize200)]
    public int BatchSize { get; set; }

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
        for (int i = 0; i < BatchSize * 2; i++) // Load more than batch size to test batching
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
                PartitionKey = i % 10 == 0 ? "partition-0" : "partition-1",
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
    public async Task ProcessPendingMessages()
    {
        await _publishingService!.ProcessPendingMessagesAsync(BatchSize);
    }

    [Benchmark]
    public async Task ProcessPartitionMessages()
    {
        await _publishingService!.ProcessPartitionAsync("partition-0", BatchSize);
    }
}
