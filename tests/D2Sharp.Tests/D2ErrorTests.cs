using Xunit;

namespace D2Sharp.Tests;

public class D2ErrorTests
{
    [Fact]
    public void GetHighlightedLineParts_WithNullLineContent_ReturnsEmptyParts()
    {
        // Arrange
        var error = new D2Error
        {
            Message = "Test error",
            LineContent = null,
            Column = 5
        };

        // Act
        var (beforeError, errorPart, afterError) = error.GetHighlightedLineParts();

        // Assert
        Assert.Equal("", beforeError);
        Assert.Equal("", errorPart);
        Assert.Equal("", afterError);
    }

    [Fact]
    public void GetHighlightedLineParts_WithEmptyLineContent_ReturnsEmptyParts()
    {
        // Arrange
        var error = new D2Error
        {
            Message = "Test error",
            LineContent = "",
            Column = 5
        };

        // Act
        var (beforeError, errorPart, afterError) = error.GetHighlightedLineParts();

        // Assert
        Assert.Equal("", beforeError);
        Assert.Equal("", errorPart);
        Assert.Equal("", afterError);
    }

    [Fact]
    public void GetHighlightedLineParts_WithNullColumn_ReturnsFullLineContentInBeforePart()
    {
        // Arrange
        var error = new D2Error
        {
            Message = "Test error",
            LineContent = "A -> B",
            Column = null
        };

        // Act
        var (beforeError, errorPart, afterError) = error.GetHighlightedLineParts();

        // Assert
        Assert.Equal("A -> B", beforeError);
        Assert.Equal("", errorPart);
        Assert.Equal("", afterError);
    }

    [Fact]
    public void GetHighlightedLineParts_WithZeroColumn_ReturnsFullLineContentInBeforePart()
    {
        // Arrange
        var error = new D2Error
        {
            Message = "Test error",
            LineContent = "A -> B",
            Column = 0
        };

        // Act
        var (beforeError, errorPart, afterError) = error.GetHighlightedLineParts();

        // Assert
        Assert.Equal("A -> B", beforeError);
        Assert.Equal("", errorPart);
        Assert.Equal("", afterError);
    }

    [Fact]
    public void GetHighlightedLineParts_WithValidColumn_SplitsLineCorrectly()
    {
        // Arrange
        var error = new D2Error
        {
            Message = "Test error",
            LineContent = "A -> B",
            Column = 3 // Points to '-'
        };

        // Act
        var (beforeError, errorPart, afterError) = error.GetHighlightedLineParts();

        // Assert
        Assert.Equal("A ", beforeError);
        Assert.Equal("-", errorPart);
        Assert.Equal("> B", afterError);
    }

    [Fact]
    public void GetHighlightedLineParts_WithColumnAtStart_SplitsLineCorrectly()
    {
        // Arrange
        var error = new D2Error
        {
            Message = "Test error",
            LineContent = "A -> B",
            Column = 1 // First character
        };

        // Act
        var (beforeError, errorPart, afterError) = error.GetHighlightedLineParts();

        // Assert
        Assert.Equal("", beforeError);
        Assert.Equal("A", errorPart);
        Assert.Equal(" -> B", afterError);
    }

    [Fact]
    public void GetHighlightedLineParts_WithColumnAtEnd_SplitsLineCorrectly()
    {
        // Arrange
        var error = new D2Error
        {
            Message = "Test error",
            LineContent = "A -> B",
            Column = 6 // Last character
        };

        // Act
        var (beforeError, errorPart, afterError) = error.GetHighlightedLineParts();

        // Assert
        Assert.Equal("A -> ", beforeError);
        Assert.Equal("B", errorPart);
        Assert.Equal("", afterError);
    }

    [Fact]
    public void GetHighlightedLineParts_WithColumnBeyondEnd_UseLastCharacter()
    {
        // Arrange
        var error = new D2Error
        {
            Message = "Test error",
            LineContent = "A -> B",
            Column = 100 // Beyond end
        };

        // Act
        var (beforeError, errorPart, afterError) = error.GetHighlightedLineParts();

        // Assert
        Assert.Equal("A -> ", beforeError);
        Assert.Equal("B", errorPart);
        Assert.Equal("", afterError);
    }
}
