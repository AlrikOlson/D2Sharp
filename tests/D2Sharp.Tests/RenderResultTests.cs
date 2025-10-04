using Xunit;

namespace D2Sharp.Tests;

public class RenderResultTests
{
    [Fact]
    public void IsSuccess_WithNoError_ReturnsTrue()
    {
        // Arrange
        var result = new RenderResult
        {
            Svg = "<svg></svg>",
            Error = null
        };

        // Act & Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_WithError_ReturnsFalse()
    {
        // Arrange
        var result = new RenderResult
        {
            Svg = null,
            Error = new D2Error { Message = "Test error" }
        };

        // Act & Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_WithBothSvgAndError_ReturnsFalse()
    {
        // Arrange
        var result = new RenderResult
        {
            Svg = "<svg></svg>",
            Error = new D2Error { Message = "Test error" }
        };

        // Act & Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_WithNullSvgAndNoError_ReturnsTrue()
    {
        // Arrange
        var result = new RenderResult
        {
            Svg = null,
            Error = null
        };

        // Act & Assert
        Assert.True(result.IsSuccess);
    }
}
