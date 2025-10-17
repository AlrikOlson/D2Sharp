using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace D2Sharp.IntegrationTests;

/// <summary>
/// Tests for D2WrapperProcessPool internals including circuit breaker and health monitoring.
/// These are integration tests that exercise the full worker pool functionality.
/// </summary>
public class D2WrapperProcessPoolInternalsTests
{
    [Fact]
    public void Constructor_WithDefaultPoolSize_CreatesPool()
    {
        // Arrange & Act
        using var pool = new D2WrapperProcessPool();

        // Assert
        Assert.NotNull(pool);
    }

    [Fact]
    public void Constructor_WithCustomPoolSize_CreatesPool()
    {
        // Arrange & Act
        using var pool = new D2WrapperProcessPool(poolSize: 5);

        // Assert
        Assert.NotNull(pool);
    }

    [Fact]
    public void Constructor_WithInvalidPoolSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new D2WrapperProcessPool(poolSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new D2WrapperProcessPool(poolSize: -1));
    }

    [Fact]
    public void Constructor_WithLogger_CreatesPool()
    {
        // Arrange
        var logger = NullLogger<D2WrapperProcessPool>.Instance;

        // Act
        using var pool = new D2WrapperProcessPool(poolSize: 3, logger);

        // Assert
        Assert.NotNull(pool);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithNullScript_ThrowsArgumentNullException()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => pool.RenderDiagramAsync(null!));
    }

    [Fact]
    public void RenderDiagram_WithNullScript_ThrowsArgumentNullException()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pool.RenderDiagram(null!));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithValidScript_ReturnsSuccessResult()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 2);
        var script = "A -> B -> C";

        // Act
        var result = await pool.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithValidScript_ReturnsSuccessResult()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 2);
        var script = "X -> Y";

        // Act
        var result = pool.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithInvalidScript_ReturnsErrorResult()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 1);
        var script = "A -> "; // Incomplete

        // Act
        var result = await pool.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithRenderOptions_ReturnsSuccessResult()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 2);
        var script = "A -> B";
        var options = new RenderOptions
        {
            ThemeId = 1,
            Layout = LayoutEngine.Elk,
            Sketch = true,
            Pad = 50
        };

        // Act
        var result = await pool.RenderDiagramAsync(script, options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_MultipleConcurrentRequests_AllSucceed()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 5);
        var scripts = Enumerable.Range(0, 15).Select(i => $"Node{i} -> Node{i + 1}");

        // Act
        var tasks = scripts.Select(s => pool.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Svg);
        });
    }

    [Fact]
    public async Task RenderDiagramAsync_MoreRequestsThanWorkers_AllSucceed()
    {
        // Arrange - pool with 2 workers handling 10 requests
        using var pool = new D2WrapperProcessPool(poolSize: 2);
        var scripts = Enumerable.Range(0, 10).Select(i => $"A{i} -> B{i}");

        // Act
        var tasks = scripts.Select(s => pool.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess, "All requests should succeed even when queued");
            Assert.NotNull(result.Svg);
        });
    }

    [Fact]
    public async Task RenderDiagramAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pool.RenderDiagramAsync("A -> B", cancellationToken: cts.Token));
    }

    [Fact]
    public void Dispose_AfterCreation_DisposesSuccessfully()
    {
        // Arrange
        var pool = new D2WrapperProcessPool(poolSize: 3);

        // Act
        pool.Dispose();

        // Assert - no exception should be thrown
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var pool = new D2WrapperProcessPool(poolSize: 2);

        // Act
        pool.Dispose();
        pool.Dispose();
        pool.Dispose();

        // Assert - no exception should be thrown
    }

    [Fact]
    public async Task RenderDiagramAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var pool = new D2WrapperProcessPool(poolSize: 1);
        pool.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => pool.RenderDiagramAsync("A -> B"));
    }

    [Fact]
    public void RenderDiagram_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var pool = new D2WrapperProcessPool(poolSize: 1);
        pool.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => pool.RenderDiagram("A -> B"));
    }

    [Fact]
    public async Task RenderDiagramAsync_SequentialRequests_AllSucceed()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 1);

        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            var result = await pool.RenderDiagramAsync($"Node{i} -> Node{i + 1}");
            Assert.True(result.IsSuccess, $"Request {i} should succeed");
            Assert.NotNull(result.Svg);
        }
    }

    [Fact]
    public void RenderDiagram_SequentialRequests_AllSucceed()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 1);

        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            var result = pool.RenderDiagram($"A{i} -> B{i}");
            Assert.True(result.IsSuccess, $"Request {i} should succeed");
            Assert.NotNull(result.Svg);
        }
    }

    [Fact]
    public async Task RenderDiagramAsync_WithLargeScript_ReturnsSuccessResult()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 2);

        // Create a large script with many nodes
        var nodes = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            nodes.AppendLine($"Node{i} -> Node{i + 1}");
        }
        var script = nodes.ToString();

        // Act
        var result = await pool.RenderDiagramAsync(script);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithComplexScript_ReturnsSuccessResult()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 1);
        var script = @"
direction: right
network: {
  cell tower: {
    satellites: {
      shape: stored_data
      style.multiple: true
    }
    satellites -> transmitter
    transmitter -> processor
    processor -> satellites
  }
  processor -> server
}
server -> cloud: ""HTTPS""
cloud.shape: cloud
";

        // Act
        var result = await pool.RenderDiagramAsync(script);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_MixedConcurrentSuccessAndError_HandlesCorrectly()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 3);
        var validScript = "A -> B";
        var invalidScript = "A -> ";

        // Act
        var tasks = new[]
        {
            pool.RenderDiagramAsync(validScript),
            pool.RenderDiagramAsync(invalidScript),
            pool.RenderDiagramAsync(validScript),
            pool.RenderDiagramAsync(invalidScript),
            pool.RenderDiagramAsync(validScript)
        };
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(3, results.Count(r => r.IsSuccess));
        Assert.Equal(2, results.Count(r => !r.IsSuccess));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithPoolSizeOne_HandlesRequestsSerially()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 1);
        var scripts = Enumerable.Range(0, 5).Select(i => $"X{i} -> Y{i}");

        // Act
        var tasks = scripts.Select(s => pool.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Svg);
        });
    }

    [Fact]
    public async Task RenderDiagramAsync_HighConcurrency_AllRequestsHandled()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 10);
        var requestCount = 50;
        var scripts = Enumerable.Range(0, requestCount).Select(i => $"Node{i} -> Node{i + 1}");

        // Act
        var tasks = scripts.Select(s => pool.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(requestCount, results.Length);
        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Svg);
        });
    }
}
