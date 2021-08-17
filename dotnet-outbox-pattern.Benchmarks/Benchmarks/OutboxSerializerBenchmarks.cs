using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;

namespace DotnetOutboxPattern.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class OutboxSerializerBenchmarks
{
    private readonly SystemTextJsonOutboxSerializer _serializer = new();
    private readonly PublishableEvent _publishableEvent;

    public OutboxSerializerBenchmarks()
    {
        var domainEvent = new EntityCreatedEvent
        {
            EventId = Guid.NewGuid(),
            EntityId = Guid.NewGuid().ToString(),
            EntityType = "TestEntity",
            Timestamp = DateTime.UtcNow
        };

        _publishableEvent = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "test.topic",
            PartitionKey = "test-partition"
        };
    }

    [Benchmark]
    public string SerializeEvent()
    {
        return _serializer.Serialize(_publishableEvent);
    }

    [Benchmark]
    public PublishableEvent DeserializeEvent()
    {
        var json = _serializer.Serialize(_publishableEvent);
        return _serializer.Deserialize<PublishableEvent>(json);
    }

    [Benchmark]
    public string SerializeLargeEvent()
    {
        var largeEvent = new EntityCreatedEvent
        {
            EventId = Guid.NewGuid(),
            EntityId = Guid.NewGuid().ToString(),
            EntityType = new string('A', 1000), // Large string
            Timestamp = DateTime.UtcNow
        };

        var publishable = new PublishableEvent
        {
            Event = largeEvent,
            Topic = "test.topic",
            PartitionKey = "test-partition"
        };

        return _serializer.Serialize(publishable);
    }
}
