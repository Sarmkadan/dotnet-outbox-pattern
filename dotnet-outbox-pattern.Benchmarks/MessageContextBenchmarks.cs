using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using DotnetOutboxPattern.Domain;
using DotnetOutboxPattern.Infrastructure;

namespace DotnetOutboxPattern.Benchmarks
{
    [MemoryDiagnoser]
    public class MessageContextBenchmarks
    {
        [Params(10, 100, 1000)]
        public int MessageCount;

        private List<OutboxMessage> _messages;
        private Activity _parentActivity;

        [GlobalSetup]
        public void Setup()
        {
            _messages = new List<OutboxMessage>(MessageCount);
            for (int i = 0; i < MessageCount; i++)
            {
                var msg = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    AggregateId = Guid.NewGuid(),
                    EventType = typeof(string),
                    Topic = "test-topic",
                    State = OutboxMessageState.Pending,
                    CorrelationId = Guid.NewGuid().ToString(),
                    Headers = new Dictionary<string, string>()
                };
                _messages.Add(msg);
            }

            // Create a parent activity to provide a trace context for dispatch activities
            _parentActivity = ActivitySource.StartActivity("parent");
            Activity.Current = _parentActivity;
        }

        [Benchmark]
        public void CaptureTraceContext()
        {
            foreach (var msg in _messages)
            {
                MessageContext.CaptureTraceContext(msg);
            }
        }

        [Benchmark]
        public void StartDispatchActivity()
        {
            foreach (var msg in _messages)
            {
                using var activity = MessageContext.StartDispatchActivity(msg, "Dispatch");
                activity?.Dispose();
            }
        }

        [Benchmark]
        public void StartActivity()
        {
            foreach (var msg in _messages)
            {
                using var activity = MessageContext.StartActivity(msg, "Process");
                activity?.Dispose();
            }
        }

        [Benchmark]
        public void RecordEvent()
        {
            foreach (var msg in _messages)
            {
                MessageContext.RecordEvent("EventOccurred", new Dictionary<string, object> { { "key", "value" } });
            }
        }

        [Benchmark]
        public void RecordException()
        {
            var ex = new Exception("Test exception");
            foreach (var msg in _messages)
            {
                MessageContext.RecordException(ex);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _parentActivity?.Dispose();
        }
    }
}
