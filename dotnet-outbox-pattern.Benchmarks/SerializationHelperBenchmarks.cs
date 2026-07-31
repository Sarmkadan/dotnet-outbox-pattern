#nullable enable
using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotnetOutboxPattern.Infrastructure;

namespace DotnetOutboxPattern.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="SerializationHelper"/> utility class.
/// Covers the most frequently used public methods with realistic payload sizes.
/// </summary>
[MemoryDiagnoser]
public class SerializationHelperBenchmarks
{
    /// <summary>
    /// Number of items to serialize / deserialize.
    /// </summary>
    [Params(10, 100, 1000)]
    public int Size { get; set; }

    private List<TestModel> _models = null!;
    private string _jsonArray = null!;
    private string _prettyJsonArray = null!;
    private Type _targetType = typeof(List<TestModel>);

    /// <summary>
    /// Simple POCO used for serialization tests.
    /// </summary>
    private sealed class TestModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public DateTime Timestamp { get; set; }
        public StatusEnum Status { get; set; }

        public enum StatusEnum
        {
            Pending,
            Processed,
            Failed
        }
    }

    /// <summary>
    /// Prepare test data before any benchmark runs.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        var rnd = new Random(42);
        _models = new List<TestModel>(Size);
        for (int i = 0; i < Size; i++)
        {
            _models.Add(new TestModel
            {
                Id = Guid.NewGuid(),
                Name = $"Item-{i}",
                Timestamp = DateTime.UtcNow.AddSeconds(rnd.Next(-1000, 1000)),
                Status = (TestModel.StatusEnum)rnd.Next(0, 3)
            });
        }

        // Pre‑serialize once so that deserialization benchmarks have a stable input.
        _jsonArray = SerializationHelper.Serialize(_models);
        _prettyJsonArray = SerializationHelper.SerializePretty(_models);
    }

    /// <summary>
    /// Benchmark the regular (compact) serialization of a list of objects.
    /// </summary>
    [Benchmark]
    public string Serialize()
    {
        return SerializationHelper.Serialize(_models);
    }

    /// <summary>
    /// Benchmark the pretty‑printed serialization of a list of objects.
    /// </summary>
    [Benchmark]
    public string SerializePretty()
    {
        return SerializationHelper.SerializePretty(_models);
    }

    /// <summary>
    /// Benchmark deserialization of a JSON array back into a strongly typed list.
    /// </summary>
    [Benchmark]
    public List<TestModel> Deserialize()
    {
        return SerializationHelper.Deserialize<List<TestModel>>(_jsonArray);
    }

    /// <summary>
    /// Benchmark deserialization using the dynamic overload (object? return).
    /// </summary>
    [Benchmark]
    public object? DeserializeDynamic()
    {
        return SerializationHelper.DeserializeDynamic(_jsonArray, _targetType);
    }

    /// <summary>
    /// Benchmark the JSON validation helper.
    /// </summary>
    [Benchmark]
    public bool IsValidJson()
    {
        return SerializationHelper.IsValidJson(_jsonArray);
    }
}
