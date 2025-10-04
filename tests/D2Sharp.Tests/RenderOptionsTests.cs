using Xunit;

namespace D2Sharp.Tests;

public class RenderOptionsTests
{
    [Fact]
    public void RenderDiagram_WithTheme_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B";
        var options = new RenderOptions
        {
            ThemeId = 1  // Cool classics theme
        };

        // Act
        var result = wrapper.RenderDiagram(script, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithSketchMode_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B -> C";
        var options = new RenderOptions
        {
            Sketch = true
        };

        // Act
        var result = wrapper.RenderDiagram(script, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithElkLayout_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = @"
A -> B
B -> C
C -> D
";
        var options = new RenderOptions
        {
            Layout = LayoutEngine.Elk
        };

        // Act
        var result = wrapper.RenderDiagram(script, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithPadAndScale_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "x -> y";
        var options = new RenderOptions
        {
            Pad = 50,
            Scale = 0.5
        };

        // Act
        var result = wrapper.RenderDiagram(script, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithCenter_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A: Hello";
        var options = new RenderOptions
        {
            Center = true
        };

        // Act
        var result = wrapper.RenderDiagram(script, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithOptions_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "start -> end";
        var options = new RenderOptions
        {
            ThemeId = 1,  // Cool classics theme
            Sketch = false,
            Pad = 100
        };

        // Act
        var result = await wrapper.RenderDiagramAsync(script, options);

        // Assert
        Assert.NotNull(result);
        if (!result.IsSuccess)
        {
            // Log error details for debugging
            Assert.Fail($"Rendering failed: {result.Error?.Message}");
        }
        Assert.NotNull(result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithTimeout_AndOptions_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B -> C";
        var timeout = TimeSpan.FromSeconds(30);
        var options = new RenderOptions
        {
            Layout = LayoutEngine.Dagre,
            ThemeId = 0
        };

        // Act
        var result = await wrapper.RenderDiagramAsync(script, timeout, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RenderDiagram_WithMultipleOptions_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = @"
direction: right
server: {
  shape: rectangle
}
db: {
  shape: cylinder
}
server -> db
";
        var options = new RenderOptions
        {
            Layout = LayoutEngine.Elk,
            ThemeId = 1,
            Sketch = true,
            Pad = 75,
            Center = true
        };

        // Act
        var result = wrapper.RenderDiagram(script, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }
}
