// dotnet-outbox-pattern.Benchmarks/Benchmarks/OutboxSerializerBenchmarksExtensions.cs
public static class OutboxSerializerBenchmarksExtensions
{
    public static void MeasureSerializationTime(this OutboxSerializerBenchmarks benchmarks, int iterations)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            benchmarks.SerializeEvent();
        }
        stopwatch.Stop();
        Console.WriteLine($"Serialization time: {stopwatch.ElapsedMilliseconds}ms");
    }

    public static void MeasureDeserializationTime(this OutboxSerializerBenchmarks benchmarks, int iterations)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            benchmarks.DeserializeEvent();
        }
        stopwatch.Stop();
        Console.WriteLine($"Deserialization time: {stopwatch.ElapsedMilliseconds}ms");
    }

    public static void MeasureLargeEventSerializationTime(this OutboxSerializerBenchmarks benchmarks, int iterations)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            benchmarks.SerializeLargeEvent();
        }
        stopwatch.Stop();
        Console.WriteLine($"Large event serialization time: {stopwatch.ElapsedMilliseconds}ms");
    }
}
