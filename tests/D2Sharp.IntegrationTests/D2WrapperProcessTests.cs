using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace D2Sharp.IntegrationTests;

public class D2WrapperProcessTests
{
    [Fact]
    public void Constructor_WithNullLogger_CreatesInstance()
    {
        // Act
        using var process = new D2WrapperProcess(null);

        // Assert
        Assert.NotNull(process);
    }

    [Fact]
    public void Constructor_WithLogger_CreatesInstance()
    {
        // Arrange
        var logger = NullLogger<D2WrapperProcess>.Instance;

        // Act
        using var process = new D2WrapperProcess(logger);

        // Assert
        Assert.NotNull(process);
    }

    [Fact]
    public void RenderDiagram_WithNullScript_ThrowsArgumentNullException()
    {
        // Arrange
        using var process = new D2WrapperProcess();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => process.RenderDiagram(null!));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithNullScript_ThrowsArgumentNullException()
    {
        // Arrange
        using var process = new D2WrapperProcess();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => process.RenderDiagramAsync(null!));
    }

    [Fact]
    public void RenderDiagram_WithSimpleScript_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "A -> B";

        // Act
        var result = process.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithSimpleScript_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "A -> B -> C";

        // Act
        var result = await process.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithComplexScript_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = @"
direction: right
A -> B -> C
D -> E
F: {
  shape: rectangle
  G -> H
}
";

        // Act
        var result = process.RenderDiagram(script);

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
        using var process = new D2WrapperProcess();
        var script = "A -> "; // Incomplete connection

        // Act
        var result = process.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Svg);
        Assert.NotNull(result.Error);
        Assert.NotEmpty(result.Error.Message);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithInvalidScript_ReturnsErrorResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "invalid { { { syntax";

        // Act
        var result = await process.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Svg);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void RenderDiagram_WithEmptyScript_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "";

        // Act
        var result = process.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        // Empty script should still produce valid SVG
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RenderDiagram_WithWhitespaceOnlyScript_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "   \n\t\r\n   ";

        // Act
        var result = process.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RenderDiagram_WithDifferentThemes_ReturnsSuccessResults()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "A -> B";
        var themeIds = new[] { 0, 1, 5, 100 };

        foreach (var themeId in themeIds)
        {
            var options = new RenderOptions { ThemeId = themeId };

            // Act
            var result = process.RenderDiagram(script, options);

            // Assert
            Assert.True(result.IsSuccess, $"ThemeId '{themeId}' should succeed");
            Assert.NotNull(result.Svg);
        }
    }

    [Fact]
    public async Task RenderDiagramAsync_WithDifferentLayoutEngines_ReturnsSuccessResults()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "A -> B -> C\nD -> E";
        var layouts = new[] { LayoutEngine.Dagre, LayoutEngine.Elk };

        foreach (var layout in layouts)
        {
            var options = new RenderOptions { Layout = layout };

            // Act
            var result = await process.RenderDiagramAsync(script, options);

            // Assert
            Assert.True(result.IsSuccess, $"Layout engine '{layout}' should succeed");
            Assert.NotNull(result.Svg);
        }
    }

    [Fact]
    public async Task RenderDiagramAsync_MultipleConcurrentRequests_AllSucceed()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var scripts = new[]
        {
            "A -> B",
            "C -> D -> E",
            "F: { shape: rectangle }",
            "G -> H -> I -> J"
        };

        // Act
        var tasks = scripts.Select(s => process.RenderDiagramAsync(s)).ToArray();
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
        var process = new D2WrapperProcess();

        // Act
        process.Dispose();
        process.Dispose();
        process.Dispose();

        // Assert - no exception should be thrown
    }

    [Fact]
    public void RenderDiagram_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var process = new D2WrapperProcess();
        process.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => process.RenderDiagram("A -> B"));
    }

    [Fact]
    public async Task RenderDiagramAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var process = new D2WrapperProcess();
        process.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => process.RenderDiagramAsync("A -> B"));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            process.RenderDiagramAsync("A -> B", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithLargeScript_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();

        // Create a large script with many nodes
        var nodes = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            nodes.AppendLine($"Node{i} -> Node{i + 1}");
        }
        var script = nodes.ToString();

        // Act
        var result = await process.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithUnicodeCharacters_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "用户 -> 系统 -> データベース";

        // Act
        var result = process.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithLabeledNodes_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "A: Hello World -> B: Test Node";

        // Act
        var result = await process.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_SequentialRequests_AllSucceed()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var requestCount = 10;

        // Act & Assert
        for (int i = 0; i < requestCount; i++)
        {
            var result = await process.RenderDiagramAsync($"A{i} -> B{i}");
            Assert.True(result.IsSuccess, $"Request {i} should succeed");
            Assert.NotNull(result.Svg);
        }
    }

    [Fact]
    public void RenderDiagram_WithAllRenderOptions_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "A -> B";
        var options = new RenderOptions
        {
            ThemeId = 1,
            Layout = LayoutEngine.Elk,
            Sketch = true,
            Pad = 50
        };

        // Act
        var result = process.RenderDiagram(script, options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithSketchOption_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "A -> B -> C";
        var options = new RenderOptions { Sketch = true };

        // Act
        var result = await process.RenderDiagramAsync(script, options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithPadOption_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = "A -> B";
        var options = new RenderOptions { Pad = 100 };

        // Act
        var result = await process.RenderDiagramAsync(script, options);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithNestedShapes_ReturnsSuccessResult()
    {
        // Arrange
        using var process = new D2WrapperProcess();
        var script = @"
container: {
  shape: rectangle
  subcontainer: {
    shape: circle
    A -> B
  }
}
";

        // Act
        var result = process.RenderDiagram(script);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }
}
