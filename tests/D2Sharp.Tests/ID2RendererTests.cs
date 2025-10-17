using D2Sharp.Internal;
using Xunit;

namespace D2Sharp.Tests;

/// <summary>
/// Tests to verify that all implementations of ID2Renderer conform to the interface contract.
/// This focuses on D2Wrapper (direct P/Invoke). Process pool implementations (D2WrapperProcessPool
/// and D2Renderer) have their own dedicated test files to avoid flaky timing-dependent tests.
/// </summary>
public class ID2RendererTests
{
    /// <summary>
    /// Provides test data: each implementation of ID2Renderer that needs to be tested.
    /// Only includes D2Wrapper to avoid flaky process startup timing issues.
    /// </summary>
    public static IEnumerable<object[]> GetRendererImplementations()
    {
        yield return new object[] { "D2Wrapper", () => new D2Wrapper() };
        // Note: D2WrapperProcessPool and D2Renderer use worker processes that need warmup time,
        // causing flaky tests. They have dedicated test files instead.
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public void AllImplementations_ImplementID2Renderer(string name, Func<ID2Renderer> factory)
    {
        // Arrange & Act
        using var renderer = factory();

        // Assert
        Assert.IsAssignableFrom<ID2Renderer>(renderer);
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public void RenderDiagram_WithValidScript_ReturnsSuccess(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();
        var script = "A -> B";

        // Act
        var result = renderer.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"{name} should return success");
        Assert.Null(result.Error);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public void RenderDiagram_WithNullScript_ThrowsArgumentNullException(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => renderer.RenderDiagram(null!));
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public void RenderDiagram_WithInvalidScript_ReturnsError(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();
        var script = "A -> "; // Incomplete connection

        // Act
        var result = renderer.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess, $"{name} should return error for invalid script");
        Assert.Null(result.Svg);
        Assert.NotNull(result.Error);
        Assert.NotEmpty(result.Error.Message);
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public void RenderDiagram_WithRenderOptions_ReturnsSuccess(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();
        var script = "A -> B -> C";
        var options = new RenderOptions
        {
            ThemeId = 1,
            Layout = LayoutEngine.Dagre
        };

        // Act
        var result = renderer.RenderDiagram(script, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"{name} should handle render options");
        Assert.NotNull(result.Svg);
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public async Task RenderDiagramAsync_WithValidScript_ReturnsSuccess(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();
        var script = "A -> B -> C";

        // Act
        var result = await renderer.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"{name} async should return success");
        Assert.Null(result.Error);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public async Task RenderDiagramAsync_WithNullScript_ThrowsArgumentNullException(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await renderer.RenderDiagramAsync(null!));
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public async Task RenderDiagramAsync_WithInvalidScript_ReturnsError(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();
        var script = "A -> "; // Incomplete connection

        // Act
        var result = await renderer.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess, $"{name} async should return error for invalid script");
        Assert.Null(result.Svg);
        Assert.NotNull(result.Error);
        Assert.NotEmpty(result.Error.Message);
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public void Dispose_AfterDisposal_ThrowsObjectDisposedException(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        var renderer = factory();
        renderer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => renderer.RenderDiagram("A -> B"));
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public void AllImplementations_AreDisposable(string name, Func<ID2Renderer> factory)
    {
        // Arrange & Act
        var renderer = factory();

        // Assert
        Assert.IsAssignableFrom<IDisposable>(renderer);

        // Cleanup
        renderer.Dispose();
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public void RenderDiagram_MultipleSequentialCalls_AllSucceed(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();
        var scripts = new[]
        {
            "A -> B",
            "X -> Y -> Z",
            "foo: {shape: rectangle}",
        };

        // Act & Assert
        foreach (var script in scripts)
        {
            var result = renderer.RenderDiagram(script);
            Assert.True(result.IsSuccess, $"{name} should handle sequential calls");
            Assert.NotNull(result.Svg);
        }
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public async Task RenderDiagramAsync_MultipleSequentialCalls_AllSucceed(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();
        var scripts = new[]
        {
            "A -> B",
            "X -> Y -> Z",
            "foo: {shape: rectangle}",
        };

        // Act & Assert
        foreach (var script in scripts)
        {
            var result = await renderer.RenderDiagramAsync(script);
            Assert.True(result.IsSuccess, $"{name} async should handle sequential calls");
            Assert.NotNull(result.Svg);
        }
    }

    [Theory]
    [MemberData(nameof(GetRendererImplementations))]
    public async Task RenderDiagramAsync_MultipleConcurrentCalls_AllSucceed(string name, Func<ID2Renderer> factory)
    {
        // Arrange
        using var renderer = factory();
        var script = "A -> B -> C";

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => renderer.RenderDiagramAsync(script))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result =>
        {
            Assert.True(result.IsSuccess, $"{name} should handle concurrent calls");
            Assert.NotNull(result.Svg);
        });
    }
}
