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
}
