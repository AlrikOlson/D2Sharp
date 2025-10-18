using Xunit;

namespace D2Sharp.Tests;

/// <summary>
/// Advanced tests for RenderOptions covering all properties and edge cases.
/// </summary>
public class RenderOptionsAdvancedTests
{
    [Fact]
    public void RenderOptions_AllProperties_CanBeSet()
    {
        // Act
        var options = new RenderOptions
        {
            Layout = LayoutEngine.Elk,
            ThemeId = 100,
            DarkThemeId = 200,
            Sketch = true,
            Pad = 150,
            Scale = 2.5,
            Center = true,
            Target = "board1",
            AnimateInterval = 1000,
            ForceAppendix = true
        };

        // Assert
        Assert.Equal(LayoutEngine.Elk, options.Layout);
        Assert.Equal(100, options.ThemeId);
        Assert.Equal(200, options.DarkThemeId);
        Assert.True(options.Sketch);
        Assert.Equal(150, options.Pad);
        Assert.Equal(2.5, options.Scale);
        Assert.True(options.Center);
        Assert.Equal("board1", options.Target);
        Assert.Equal(1000, options.AnimateInterval);
        Assert.True(options.ForceAppendix);
    }

    [Fact]
    public void RenderOptions_DefaultValues_AreNull()
    {
        // Arrange & Act
        var options = new RenderOptions();

        // Assert
        Assert.Null(options.Layout);
        Assert.Null(options.ThemeId);
        Assert.Null(options.DarkThemeId);
        Assert.Null(options.Sketch);
        Assert.Null(options.Pad);
        Assert.Null(options.Scale);
        Assert.Null(options.Center);
        Assert.Null(options.Target);
        Assert.Null(options.AnimateInterval);
        Assert.Null(options.ForceAppendix);
    }

    [Fact]
    public void RenderOptions_WithDagreLayout_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Layout = LayoutEngine.Dagre };

        // Assert
        Assert.Equal(LayoutEngine.Dagre, options.Layout);
    }

    [Fact]
    public void RenderOptions_WithElkLayout_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Layout = LayoutEngine.Elk };

        // Assert
        Assert.Equal(LayoutEngine.Elk, options.Layout);
    }

    [Fact]
    public void RenderOptions_WithZeroThemeId_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { ThemeId = 0 };

        // Assert
        Assert.Equal(0, options.ThemeId);
    }

    [Fact]
    public void RenderOptions_WithHighThemeId_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { ThemeId = 300 };

        // Assert
        Assert.Equal(300, options.ThemeId);
    }

    [Fact]
    public void RenderOptions_WithNegativePad_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Pad = -10 };

        // Assert
        Assert.Equal(-10, options.Pad);
    }

    [Fact]
    public void RenderOptions_WithZeroPad_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Pad = 0 };

        // Assert
        Assert.Equal(0, options.Pad);
    }

    [Fact]
    public void RenderOptions_WithLargePad_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Pad = 1000 };

        // Assert
        Assert.Equal(1000, options.Pad);
    }

    [Fact]
    public void RenderOptions_WithFractionalScale_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Scale = 0.5 };

        // Assert
        Assert.Equal(0.5, options.Scale);
    }

    [Fact]
    public void RenderOptions_WithLargeScale_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Scale = 10.0 };

        // Assert
        Assert.Equal(10.0, options.Scale);
    }

    [Fact]
    public void RenderOptions_WithTargetWildcard_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Target = "*" };

        // Assert
        Assert.Equal("*", options.Target);
    }

    [Fact]
    public void RenderOptions_WithTargetScenario_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Target = "scenario1*" };

        // Assert
        Assert.Equal("scenario1*", options.Target);
    }

    [Fact]
    public void RenderOptions_WithAnimateInterval_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { AnimateInterval = 500 };

        // Assert
        Assert.Equal(500, options.AnimateInterval);
    }

    [Fact]
    public void RenderOptions_WithSketchTrue_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Sketch = true };

        // Assert
        Assert.True(options.Sketch);
    }

    [Fact]
    public void RenderOptions_WithSketchFalse_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Sketch = false };

        // Assert
        Assert.False(options.Sketch);
    }

    [Fact]
    public void RenderOptions_WithCenterTrue_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Center = true };

        // Assert
        Assert.True(options.Center);
    }

    [Fact]
    public void RenderOptions_WithCenterFalse_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { Center = false };

        // Assert
        Assert.False(options.Center);
    }

    [Fact]
    public void RenderOptions_WithForceAppendixTrue_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { ForceAppendix = true };

        // Assert
        Assert.True(options.ForceAppendix);
    }

    [Fact]
    public void RenderOptions_WithForceAppendixFalse_CreatesCorrectly()
    {
        // Arrange & Act
        var options = new RenderOptions { ForceAppendix = false };

        // Assert
        Assert.False(options.ForceAppendix);
    }

    [Fact]
    public void RenderOptions_MultipleInstances_AreIndependent()
    {
        // Arrange & Act
        var options1 = new RenderOptions { ThemeId = 1, Sketch = true };
        var options2 = new RenderOptions { ThemeId = 5, Sketch = false };

        // Assert
        Assert.Equal(1, options1.ThemeId);
        Assert.True(options1.Sketch);
        Assert.Equal(5, options2.ThemeId);
        Assert.False(options2.Sketch);
    }

    [Fact]
    public void LayoutEngine_DagreValue_IsZero()
    {
        // Assert
        Assert.Equal(0, (int)LayoutEngine.Dagre);
    }

    [Fact]
    public void LayoutEngine_ElkValue_IsOne()
    {
        // Assert
        Assert.Equal(1, (int)LayoutEngine.Elk);
    }
}
