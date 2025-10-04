using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace D2Sharp.Tests;

public class D2WrapperTests
{
    [Fact]
    public void Constructor_WithNullLogger_CreatesInstance()
    {
        // Arrange & Act
        var wrapper = new D2Wrapper(null);

        // Assert
        Assert.NotNull(wrapper);
    }

    [Fact]
    public void Constructor_WithLogger_CreatesInstance()
    {
        // Arrange
        var logger = NullLogger<D2Wrapper>.Instance;

        // Act
        var wrapper = new D2Wrapper(logger);

        // Assert
        Assert.NotNull(wrapper);
    }

    [Fact]
    public void RenderDiagram_WithNullScript_ThrowsArgumentNullException()
    {
        // Arrange
        var wrapper = new D2Wrapper();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => wrapper.RenderDiagram(null!));
    }

    [Fact]
    public void RenderDiagram_WithValidScript_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B";

        // Act
        var result = wrapper.RenderDiagram(script);

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
        var wrapper = new D2Wrapper();
        var script = @"
direction: right
A -> B -> C
D -> E
";

        // Act
        var result = wrapper.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public void RenderDiagram_WithInvalidScript_ReturnsErrorResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> "; // Incomplete connection

        // Act
        var result = wrapper.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Svg);
        Assert.NotNull(result.Error);
        Assert.NotEmpty(result.Error.Message);
    }

    [Fact]
    public void RenderDiagram_WithEmptyScript_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "";

        // Act
        var result = wrapper.RenderDiagram(script);

        // Assert
        Assert.NotNull(result);
        // Empty script should still produce valid (empty) SVG
        Assert.True(result.IsSuccess);
    }
}
