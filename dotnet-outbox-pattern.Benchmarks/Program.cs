using BenchmarkDotNet.Running;
using DotnetOutboxPattern.Benchmarks;

Console.WriteLine("=== .NET Outbox Pattern Benchmarks ===");
Console.WriteLine("Running performance benchmarks for critical operations...");
Console.WriteLine();

// Run all benchmarks
BenchmarkRunner.Run(typeof(OutboxRepositoryBenchmarks).Assembly);
