using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetOutboxPattern.Data;
using DotnetOutboxPattern.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetOutboxPattern.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class OutboxRepositoryBenchmarks : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private OutboxDbContext? _context;
    private IOutboxRepository? _repository;
    private const int BatchSize = 100;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddDbContext<OutboxDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=OutboxBenchmarks;Trusted_Connection=True;MultipleActiveResultSets=true")
        );

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<OutboxDbContext>();
        _repository = _serviceProvider.GetRequiredService<IOutboxRepository>();

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

    /// <summary>
    /// Disposes resources held by the benchmark
    /// </summary>
    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark]
    public async Task AddSingleMessage()
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

        await _repository!.AddAsync(message);
    }

    [Benchmark]
    public async Task GetPendingMessages_Batch100()
    {
        await _repository!.GetPendingMessagesAsync(BatchSize);
    }

    [Benchmark]
    public async Task GetPendingMessagesByPartition_Batch100()
    {
        await _repository!.GetPendingByPartitionAsync("test-partition", BatchSize);
    }

    [Benchmark]
    public async Task GetPendingMessages_WithLockCheck()
    {
        await _repository!.GetPendingMessagesAsync(BatchSize);
    }

    [Benchmark]
    public async Task GetStatistics()
    {
        await _repository!.GetStatisticsAsync();
    }

    [Benchmark]
    public async Task GetPendingCount()
    {
        await _repository!.GetPendingCountAsync();
    }
}
