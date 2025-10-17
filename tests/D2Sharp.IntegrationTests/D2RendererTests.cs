using D2Sharp.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace D2Sharp.IntegrationTests;

/// <summary>
/// Tests for D2Renderer - the main user-facing API.
/// D2Renderer is a facade over D2WrapperProcessPool, so these tests focus on
/// the API surface and factory methods rather than comprehensive rendering scenarios
/// (which are covered by D2WrapperTests and the Web stress tests).
/// </summary>
public class D2RendererTests
{
    [Fact]
    public void Constructor_WithDefaultWorkerCount_CreatesInstance()
    {
        // Arrange & Act
        using var renderer = new D2Renderer();

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void Constructor_WithCustomWorkerCount_CreatesInstance()
    {
        // Arrange & Act
        using var renderer = new D2Renderer(workerCount: 5);

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void Constructor_WithInvalidWorkerCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new D2Renderer(workerCount: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new D2Renderer(workerCount: -1));
    }

    [Fact]
    public void Constructor_WithLogger_CreatesInstance()
    {
        // Arrange
        var logger = NullLogger<D2Renderer>.Instance;

        // Act
        using var renderer = new D2Renderer(workerCount: 3, logger);

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void CreateWithPool_CreatesInstanceWithPool()
    {
        // Arrange & Act
        using var renderer = D2Renderer.CreateWithPool(workerCount: 10);

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void CreateWithPool_WithInvalidWorkerCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => D2Renderer.CreateWithPool(workerCount: 0));
    }

    [Fact]
    public void CreateDirect_CreatesInstanceWithDirectMode()
    {
        // Arrange & Act
        using var renderer = D2Renderer.CreateDirect();

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void CreateDirect_WithOptions_CreatesInstanceWithDirectMode()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = false,
            EnableTelemetry = false,
            EnableMetrics = false
        };

        // Act
        using var renderer = D2Renderer.CreateDirect(options);

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void Dispose_DisposesUnderlyingImplementation()
    {
        // Arrange
        var renderer = new D2Renderer();

        // Act
        renderer.Dispose();

        // Assert
        // After disposal, rendering should throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => renderer.RenderDiagram("A -> B"));
    }

    [Fact]
    public void ImplementsID2Renderer_Interface()
    {
        // Arrange & Act
        using var renderer = new D2Renderer();

        // Assert
        Assert.IsAssignableFrom<ID2Renderer>(renderer);
    }

    [Fact]
    public void RenderDiagram_WithNullScript_ThrowsArgumentNullException()
    {
        // Arrange
        using var renderer = new D2Renderer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => renderer.RenderDiagram(null!));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithNullScript_ThrowsArgumentNullException()
    {
        // Arrange
        using var renderer = new D2Renderer();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await renderer.RenderDiagramAsync(null!));
    }

    [Fact]
    public void RenderDiagram_WithValidScript_ReturnsSuccessResult()
    {
        // Arrange
        using var renderer = new D2Renderer();
        var script = "A -> B -> C";

        // Act
        var result = renderer.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithValidScript_ReturnsSuccessResult()
    {
        // Arrange
        using var renderer = new D2Renderer();
        var script = "X -> Y -> Z";

        // Act
        var result = await renderer.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithInvalidScript_ReturnsErrorResult()
    {
        // Arrange
        using var renderer = new D2Renderer();
        var script = "A -> "; // Incomplete

        // Act
        var result = renderer.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithInvalidScript_ReturnsErrorResult()
    {
        // Arrange
        using var renderer = new D2Renderer();
        var script = "invalid { { {";

        // Act
        var result = await renderer.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void RenderDiagram_WithRenderOptions_ReturnsSuccessResult()
    {
        // Arrange
        using var renderer = new D2Renderer();
        var script = "A -> B";
        var options = new RenderOptions
        {
            ThemeId = 1,
            Layout = LayoutEngine.Elk,
            Sketch = true,
            Pad = 50
        };

        // Act
        var result = renderer.RenderDiagram(script, options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithRenderOptions_ReturnsSuccessResult()
    {
        // Arrange
        using var renderer = new D2Renderer();
        var script = "A -> B -> C";
        var options = new RenderOptions
        {
            ThemeId = 5,
            Sketch = false,
            Pad = 100
        };

        // Act
        var result = await renderer.RenderDiagramAsync(script, options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        using var renderer = new D2Renderer();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await renderer.RenderDiagramAsync("A -> B", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RenderDiagramAsync_MultipleConcurrentRequests_AllSucceed()
    {
        // Arrange
        using var renderer = new D2Renderer(workerCount: 5);
        var scripts = Enumerable.Range(0, 10).Select(i => $"Node{i} -> Node{i + 1}");

        // Act
        var tasks = scripts.Select(s => renderer.RenderDiagramAsync(s)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Svg);
        });
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var renderer = new D2Renderer();

        // Act
        renderer.Dispose();
        renderer.Dispose();
        renderer.Dispose();

        // Assert - no exception should be thrown
    }

    [Fact]
    public async Task RenderDiagramAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var renderer = new D2Renderer();
        renderer.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await renderer.RenderDiagramAsync("A -> B"));
    }

    [Fact]
    public void CreateWithPool_WithLogger_CreatesInstance()
    {
        // Arrange
        var logger = NullLogger<D2Renderer>.Instance;

        // Act
        using var renderer = D2Renderer.CreateWithPool(workerCount: 3, logger);

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void CreateDirect_WithLogger_CreatesInstance()
    {
        // Arrange
        var logger = NullLogger<D2Renderer>.Instance;

        // Act
        using var renderer = D2Renderer.CreateDirect(logger: logger);

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void CreateDirect_WithOptionsAndLogger_CreatesInstance()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = true,
            EnableTelemetry = true
        };
        var logger = NullLogger<D2Renderer>.Instance;

        // Act
        using var renderer = D2Renderer.CreateDirect(options, logger);

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void CreateDirect_RenderDiagram_ReturnsSuccessResult()
    {
        // Arrange
        using var renderer = D2Renderer.CreateDirect();
        var script = "A -> B";

        // Act
        var result = renderer.RenderDiagram(script);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task CreateDirect_RenderDiagramAsync_ReturnsSuccessResult()
    {
        // Arrange
        using var renderer = D2Renderer.CreateDirect();
        var script = "X -> Y";

        // Act
        var result = await renderer.RenderDiagramAsync(script);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public void CreateWithPool_RenderDiagram_ReturnsSuccessResult()
    {
        // Arrange
        using var renderer = D2Renderer.CreateWithPool(5);
        var script = "A -> B -> C";

        // Act
        var result = renderer.RenderDiagram(script);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task CreateWithPool_RenderDiagramAsync_ReturnsSuccessResult()
    {
        // Arrange
        using var renderer = D2Renderer.CreateWithPool(3);
        var script = "Node1 -> Node2";

        // Act
        var result = await renderer.RenderDiagramAsync(script);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public void RenderDiagram_SequentialRequests_AllSucceed()
    {
        // Arrange
        using var renderer = new D2Renderer();

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var result = renderer.RenderDiagram($"A{i} -> B{i}");
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Svg);
        }
    }

    [Fact]
    public async Task RenderDiagramAsync_SequentialRequests_AllSucceed()
    {
        // Arrange
        using var renderer = new D2Renderer();

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var result = await renderer.RenderDiagramAsync($"X{i} -> Y{i}");
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Svg);
        }
    }

    [Fact]
    public void Constructor_WithWorkerCountOne_CreatesInstance()
    {
        // Arrange & Act
        using var renderer = new D2Renderer(workerCount: 1);

        // Assert
        Assert.NotNull(renderer);
        var result = renderer.RenderDiagram("A -> B");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CreateWithPool_WithWorkerCountOne_CreatesInstance()
    {
        // Arrange & Act
        using var renderer = D2Renderer.CreateWithPool(workerCount: 1);

        // Assert
        Assert.NotNull(renderer);
        var result = renderer.RenderDiagram("A -> B");
        Assert.True(result.IsSuccess);
    }
}
