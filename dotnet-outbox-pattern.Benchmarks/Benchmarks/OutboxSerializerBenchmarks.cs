using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Services;

namespace DotnetOutboxPattern.Benchmarks;

/// <summary>
/// Benchmark class for OutboxSerializer.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class OutboxSerializerBenchmarks
{
    private readonly SystemTextJsonOutboxSerializer _serializer = new();
    private readonly PublishableEvent _publishableEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxSerializerBenchmarks"/> class.
    /// </summary>
    public OutboxSerializerBenchmarks()
    {
        var domainEvent = new EntityCreatedEvent
        {
            EventId = Guid.NewGuid(),
            EntityId = Guid.NewGuid().ToString(),
            EntityType = "TestEntity",
            OccurredAt = DateTime.UtcNow
        };

        _publishableEvent = new PublishableEvent
        {
            Event = domainEvent,
            Topic = "test.topic",
            PartitionKey = "test-partition"
        };
    }

    /// <summary>
    /// Benchmarks the serialization of a <see cref="PublishableEvent"/>.
    /// </summary>
    [Benchmark]
    public string SerializeEvent()
    {
        return _serializer.Serialize(_publishableEvent);
    }

    /// <summary>
    /// Benchmarks the deserialization of a <see cref="PublishableEvent"/>.
    /// </summary>
    /// <returns>The deserialized <see cref="PublishableEvent"/>.</returns>
    [Benchmark]
    public PublishableEvent DeserializeEvent()
    {
        var json = _serializer.Serialize(_publishableEvent);
        return _serializer.Deserialize<PublishableEvent>(json);
    }

    /// <summary>
    /// Benchmarks the serialization of a large <see cref="PublishableEvent"/>.
    /// </summary>
    /// <returns>The serialized <see cref="PublishableEvent"/>.</returns>
    [Benchmark]
    public string SerializeLargeEvent()
    {
        var largeEvent = new EntityCreatedEvent
        {
            EventId = Guid.NewGuid(),
            EntityId = Guid.NewGuid().ToString(),
            EntityType = new string('A', 1000), // Large string
            OccurredAt = DateTime.UtcNow
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
