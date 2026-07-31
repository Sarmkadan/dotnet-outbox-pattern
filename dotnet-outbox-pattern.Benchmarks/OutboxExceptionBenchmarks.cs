using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotnetOutboxPattern.Exceptions;

namespace DotnetOutboxPattern.Benchmarks;

[MemoryDiagnoser]
public class OutboxExceptionBenchmarks
{
    private OutboxException _outboxException;
    private string _message;
    private string _errorCode;
    private string _resourceId;

    [Params(10, 100, 1000)]
    public int ExceptionCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _message = "Test exception message";
        _errorCode = "TEST_ERROR";
        _resourceId = "test-resource-id";
        _outboxException = new OutboxException(_message, _errorCode, _resourceId);
    }

    [Benchmark]
    public void CreateOutboxException()
    {
        for (int i = 0; i < ExceptionCount; i++)
        {
            new OutboxException(_message, _errorCode, _resourceId);
        }
    }

    [Benchmark]
    public void CreateMessagePublishingException()
    {
        for (int i = 0; i < ExceptionCount; i++)
        {
            new MessagePublishingException(_message, Guid.NewGuid(), 1);
        }
    }

    [Benchmark]
    public void CreateDeadLetterException()
    {
        for (int i = 0; i < ExceptionCount; i++)
        {
            new DeadLetterException(_message, Guid.NewGuid());
        }
    }

    [Benchmark]
    public void CreateInvalidMessageException()
    {
        for (int i = 0; i < ExceptionCount; i++)
        {
            new InvalidMessageException(_message);
        }
    }
}
