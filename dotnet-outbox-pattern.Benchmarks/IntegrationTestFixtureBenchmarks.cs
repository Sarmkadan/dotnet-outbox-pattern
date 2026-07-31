using System;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotnetOutboxPattern.Tests;

namespace DotnetOutboxPattern.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="IntegrationTestFixture"/> used in the integration test suite.
/// The benchmarks focus on the most frequently used public members:
///   * InitializeAsync – sets up the WebApplicationFactory and HttpClient.
///   * CreateScope – creates a new DI scope for resolving services.
///   * GetHealthEndpoint – performs a simple HTTP GET against the health endpoint.
/// </summary>
[MemoryDiagnoser]
public class IntegrationTestFixtureBenchmarks
{
    private IntegrationTestFixture _fixture = null!;

    /// <summary>
    /// Number of scopes to create in the <see cref="CreateScopeBenchmark"/> method.
    /// </summary>
    [Params(10, 100, 1000)]
    public int ScopeCount { get; set; }

    /// <summary>
    /// Global setup runs once before any benchmark iteration.
    /// It creates the fixture and performs the asynchronous initialization.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
    }

    /// <summary>
    /// Global cleanup runs after all benchmarks have finished.
    /// It disposes the fixture and underlying resources.
    /// </summary>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _fixture.DisposeAsync();
    }

    /// <summary>
    /// Benchmarks the asynchronous initialization of the fixture.
    /// </summary>
    [Benchmark]
    public async Task InitializeAsyncBenchmark()
    {
        // Re‑initialize a fresh fixture to avoid re‑using the already‑initialized one.
        var tempFixture = new IntegrationTestFixture();
        await tempFixture.InitializeAsync();
        await tempFixture.DisposeAsync();
    }

    /// <summary>
    /// Benchmarks creating and disposing a number of DI scopes.
    /// The number of scopes is controlled by the <see cref="ScopeCount"/> parameter.
    /// </summary>
    [Benchmark]
    public void CreateScopeBenchmark()
    {
        for (int i = 0; i < ScopeCount; i++)
        {
            var scope = _fixture.CreateScope();
            scope.Dispose();
        }
    }

    /// <summary>
    /// Benchmarks a simple HTTP GET request to the health endpoint.
    /// This exercises the HttpClient created by the fixture without performing heavy I/O.
    /// </summary>
    [Benchmark]
    public async Task<HttpResponseMessage> GetHealthEndpointBenchmark()
    {
        return await _fixture.Client.GetAsync("/health");
    }
}
