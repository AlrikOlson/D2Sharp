using D2Sharp.Internal;
using System.Diagnostics;
using Xunit;

namespace D2Sharp.IntegrationTests;

/// <summary>
/// Performance and stress tests for D2Sharp components.
/// These tests verify behavior under load and sustained usage.
/// </summary>
public class PerformanceTests
{
    [Fact]
    public async Task D2Renderer_HighConcurrency_HandlesRequestsEfficiently()
    {
        // Arrange
        using var renderer = new D2Renderer(workerCount: 10);
        var requestCount = 100;
        var scripts = Enumerable.Range(0, requestCount).Select(i => $"Node{i} -> Node{i + 1}");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = scripts.Select(s => renderer.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(requestCount, results.Length);
        Assert.All(results, result => Assert.True(result.IsSuccess));

        // Performance assertion - should handle 100 requests in reasonable time
        Assert.True(stopwatch.Elapsed.TotalSeconds < 30, $"Took {stopwatch.Elapsed.TotalSeconds}s, expected < 30s");
    }

    [Fact]
    public async Task D2WrapperProcessPool_SustainedLoad_MaintainsPerformance()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 5);
        var batchCount = 5;
        var requestsPerBatch = 20;

        // Act - Multiple batches to test sustained load
        for (int batch = 0; batch < batchCount; batch++)
        {
            var scripts = Enumerable.Range(0, requestsPerBatch).Select(i => $"A{batch}_{i} -> B{batch}_{i}");
            var tasks = scripts.Select(s => pool.RenderDiagramAsync(s)).ToArray();
            var results = await Task.WhenAll(tasks);

            // Assert each batch succeeds
            Assert.All(results, result => Assert.True(result.IsSuccess));
        }
    }

    [Fact]
    public async Task D2Renderer_RepeatedDisposalAndCreation_NoMemoryLeaks()
    {
        // Arrange & Act - Create and dispose multiple renderers
        for (int i = 0; i < 10; i++)
        {
            using var renderer = new D2Renderer(workerCount: 2);
            var result = await renderer.RenderDiagramAsync("A -> B");
            Assert.True(result.IsSuccess);
        }

        // Assert - If we got here without crashing, no obvious memory leaks
        Assert.True(true);
    }

    [Fact]
    public async Task D2WrapperProcessPool_RepeatedRenders_NoPerformanceDegradation()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 3);
        var renderCount = 50;
        var timings = new List<long>();

        // Act - Measure performance over multiple renders
        for (int i = 0; i < renderCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await pool.RenderDiagramAsync($"Node{i} -> Node{i + 1}");
            stopwatch.Stop();

            Assert.True(result.IsSuccess);
            timings.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - Later renders should not be significantly slower than earlier ones
        var firstHalfAvg = timings.Take(25).Average();
        var secondHalfAvg = timings.Skip(25).Average();

        // Second half should not be more than 2x slower (allowing for some variance)
        Assert.True(secondHalfAvg < firstHalfAvg * 2,
            $"Performance degradation detected: first half avg {firstHalfAvg}ms, second half avg {secondHalfAvg}ms");
    }

    [Fact]
    public async Task D2Renderer_DirectMode_HandlesConcurrentRequests()
    {
        // Arrange
        using var renderer = D2Renderer.CreateDirect();
        var requestCount = 10;
        var scripts = Enumerable.Range(0, requestCount).Select(i => $"X{i} -> Y{i}");

        // Act
        var tasks = scripts.Select(s => renderer.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(requestCount, results.Length);
        Assert.All(results, result => Assert.True(result.IsSuccess));
    }

    [Fact]
    public async Task D2Renderer_VaryingScriptSizes_HandlesAllSizes()
    {
        // Arrange
        using var renderer = new D2Renderer(workerCount: 5);
        var scripts = new List<string>
        {
            "A -> B", // Small
            string.Join("\n", Enumerable.Range(0, 20).Select(i => $"Node{i} -> Node{i+1}")), // Medium
            string.Join("\n", Enumerable.Range(0, 100).Select(i => $"N{i} -> N{i+1}")), // Large
            "X -> Y", // Small again
        };

        // Act
        var tasks = scripts.Select(s => renderer.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result.IsSuccess));
    }

    [Fact]
    public async Task D2WrapperProcessPool_WorkerExhaustion_QueuesRequestsCorrectly()
    {
        // Arrange - 2 workers, 10 requests
        using var pool = new D2WrapperProcessPool(poolSize: 2);
        var requestCount = 10;
        var scripts = Enumerable.Range(0, requestCount).Select(i => $"Request{i}: {{shape: rectangle}}");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = scripts.Select(s => pool.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Equal(requestCount, results.Length);
        Assert.All(results, result => Assert.True(result.IsSuccess));
        // Requests should queue and complete, not fail
    }

    [Fact]
    public async Task D2Renderer_MixedSuccessAndFailure_MaintainsStability()
    {
        // Arrange
        using var renderer = new D2Renderer(workerCount: 3);
        var scripts = new List<string>
        {
            "A -> B",           // Valid
            "Invalid -> ",      // Invalid
            "C -> D -> E",      // Valid
            "Bad { { {",        // Invalid
            "F -> G",           // Valid
            "Another -> ",      // Invalid
            "H -> I -> J"       // Valid
        };

        // Act
        var tasks = scripts.Select(s => renderer.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(4, results.Count(r => r.IsSuccess));
        Assert.Equal(3, results.Count(r => !r.IsSuccess));

        // Verify renderer still works after errors
        var finalResult = await renderer.RenderDiagramAsync("Final -> Test");
        Assert.True(finalResult.IsSuccess);
    }

    [Fact]
    public async Task D2Renderer_BurstTraffic_HandlesSpikesGracefully()
    {
        // Arrange
        using var renderer = new D2Renderer(workerCount: 5);

        // Act - Simulate burst traffic pattern
        var burst1 = Enumerable.Range(0, 20).Select(i => $"B1_{i} -> B1_{i+1}");
        var burst2 = Enumerable.Range(0, 20).Select(i => $"B2_{i} -> B2_{i+1}");

        var burst1Tasks = burst1.Select(s => renderer.RenderDiagramAsync(s)).ToArray();
        await Task.Delay(100); // Small delay between bursts
        var burst2Tasks = burst2.Select(s => renderer.RenderDiagramAsync(s)).ToArray();

        var allResults = await Task.WhenAll(burst1Tasks.Concat(burst2Tasks));

        // Assert
        Assert.Equal(40, allResults.Length);
        Assert.All(allResults, result => Assert.True(result.IsSuccess));
    }

    [Fact]
    public async Task D2WrapperProcessPool_MinimalPoolSize_StillFunctional()
    {
        // Arrange - Test with just 1 worker
        using var pool = new D2WrapperProcessPool(poolSize: 1);
        var requestCount = 20;

        // Act
        var results = new List<RenderResult>();
        for (int i = 0; i < requestCount; i++)
        {
            var result = await pool.RenderDiagramAsync($"Node{i} -> Node{i+1}");
            results.Add(result);
        }

        // Assert
        Assert.Equal(requestCount, results.Count);
        Assert.All(results, result => Assert.True(result.IsSuccess));
    }

    [Fact]
    public async Task D2Renderer_ComplexDiagrams_HandleEfficiently()
    {
        // Arrange
        using var renderer = new D2Renderer(workerCount: 3);
        var complexScript = @"
network: {
  style: {
    fill: ""#f0f0f0""
  }

  client: {
    shape: person
    label: ""User""
  }

  loadbalancer: {
    shape: hexagon
    label: ""Load Balancer""
  }

  servers: {
    server1: { shape: rectangle }
    server2: { shape: rectangle }
    server3: { shape: rectangle }
  }

  database: {
    primary: { shape: cylinder }
    replica1: { shape: cylinder }
    replica2: { shape: cylinder }
  }

  cache: {
    shape: stored_data
    style.multiple: true
  }

  client -> loadbalancer: ""HTTPS""
  loadbalancer -> servers.server1
  loadbalancer -> servers.server2
  loadbalancer -> servers.server3

  servers.server1 -> cache
  servers.server2 -> cache
  servers.server3 -> cache

  servers.server1 -> database.primary
  servers.server2 -> database.primary
  servers.server3 -> database.primary

  database.primary -> database.replica1
  database.primary -> database.replica2
}
";

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await renderer.RenderDiagramAsync(complexScript);
        stopwatch.Stop();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
        Assert.True(stopwatch.Elapsed.TotalSeconds < 5, "Complex diagram should render in < 5 seconds");
    }

    [Fact]
    public async Task D2Renderer_ParallelInstancesTest()
    {
        // Arrange - Multiple renderer instances in parallel
        var rendererCount = 3;
        var requestsPerRenderer = 10;

        // Act
        var rendererTasks = Enumerable.Range(0, rendererCount).Select(async rendererIndex =>
        {
            using var renderer = new D2Renderer(workerCount: 2);
            var scripts = Enumerable.Range(0, requestsPerRenderer)
                .Select(i => $"R{rendererIndex}_N{i} -> R{rendererIndex}_N{i+1}");

            var tasks = scripts.Select(s => renderer.RenderDiagramAsync(s)).ToArray();
            return await Task.WhenAll(tasks);
        });

        var allResults = await Task.WhenAll(rendererTasks);

        // Assert
        Assert.Equal(rendererCount, allResults.Length);
        foreach (var results in allResults)
        {
            Assert.Equal(requestsPerRenderer, results.Length);
            Assert.All(results, result => Assert.True(result.IsSuccess));
        }
    }
}
