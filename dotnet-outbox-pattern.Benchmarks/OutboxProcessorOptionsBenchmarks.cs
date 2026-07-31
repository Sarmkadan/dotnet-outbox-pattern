using BenchmarkDotNet.Attributes;
using DotnetOutboxPattern.Infrastructure;

namespace DotnetOutboxPattern.Benchmarks;

[MemoryDiagnoser]
public class OutboxProcessorOptionsBenchmarks
{
    private OutboxProcessorOptions _validOptions;
    private OutboxProcessorOptions _invalidOptions;

    [Params(0, 1, 5, 10)]
    public int EmptyBatches { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _validOptions = new OutboxProcessorOptions
        {
            Enabled = true,
            BatchSize = 100,
            DelayBetweenBatches = 5000,
            BackoffStrategy = BackoffStrategy.Exponential,
            BackoffMultiplier = 2.0,
            MaxDelayBetweenBatches = 60000
        };
        
        _invalidOptions = new OutboxProcessorOptions
        {
            Enabled = true,
            BatchSize = 0, // Invalid: must be > 0
            DelayBetweenBatches = 5000
        };
    }

    [Benchmark]
    public bool IsValidValid()
    {
        return _validOptions.IsValid();
    }

    [Benchmark]
    public bool IsValidInvalid()
    {
        return _invalidOptions.IsValid();
    }

    [Benchmark]
    public OutboxProcessorOptions ValidateMethod()
    {
        return _validOptions.Validate();
    }

    [Benchmark]
    public TimeSpan ComputeDelayExponential()
    {
        return _validOptions.ComputeDelay(EmptyBatches);
    }
}
