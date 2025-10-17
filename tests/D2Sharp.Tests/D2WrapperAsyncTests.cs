using D2Sharp.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace D2Sharp.Tests;

public class D2WrapperAsyncTests
{
    [Fact]
    public async Task RenderDiagramAsync_WithNullScript_ThrowsArgumentNullException()
    {
        // Arrange
        var wrapper = new D2Wrapper();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => wrapper.RenderDiagramAsync(null!));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithValidScript_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B";

        // Act
        var result = await wrapper.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithComplexScript_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = @"
direction: right
A -> B -> C
D -> E -> F
A -> E
";

        // Act
        var result = await wrapper.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Svg);
        Assert.Contains("<svg", result.Svg);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithInvalidScript_ReturnsErrorResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> "; // Incomplete connection

        // Act
        var result = await wrapper.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Svg);
        Assert.NotNull(result.Error);
        Assert.NotEmpty(result.Error.Message);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B";
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            wrapper.RenderDiagramAsync(script, options: null, cts.Token));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithTimeout_CompletesWithinTimeout()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B";
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var result = await wrapper.RenderDiagramAsync(script, timeout);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithTimeoutBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B";
        var timeout = TimeSpan.FromMilliseconds(50); // Below minimum of 100ms

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            wrapper.RenderDiagramAsync(script, timeout));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithTimeoutAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B";
        var timeout = TimeSpan.FromMinutes(11); // Above maximum of 10 minutes

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            wrapper.RenderDiagramAsync(script, timeout));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithTimeoutAndCancellation_RespectsCancellation()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B";
        var timeout = TimeSpan.FromSeconds(30);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            wrapper.RenderDiagramAsync(script, timeout, options: null, cts.Token));
    }

    [Fact]
    public async Task RenderDiagramAsync_MultipleCallsConcurrently_AllSucceed()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "A -> B -> C";
        var tasks = new List<Task<RenderResult>>();

        // Act - Create 10 concurrent tasks
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(wrapper.RenderDiagramAsync(script));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Svg);
            Assert.Contains("<svg", result.Svg);
        }
    }

    [Fact]
    public void Dispose_CalledOnce_DoesNotThrow()
    {
        // Arrange
        var wrapper = new D2Wrapper();

        // Act & Assert - Should not throw
        wrapper.Dispose();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var wrapper = new D2Wrapper();

        // Act & Assert - Should not throw on multiple calls
        wrapper.Dispose();
        wrapper.Dispose();
        wrapper.Dispose();
    }

    [Fact]
    public void RenderDiagram_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        wrapper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => wrapper.RenderDiagram("A -> B"));
    }

    [Fact]
    public async Task RenderDiagramAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        wrapper.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            wrapper.RenderDiagramAsync("A -> B"));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithTimeoutAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        wrapper.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            wrapper.RenderDiagramAsync("A -> B", TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithEmptyScript_ReturnsSuccessResult()
    {
        // Arrange
        var wrapper = new D2Wrapper();
        var script = "";

        // Act
        var result = await wrapper.RenderDiagramAsync(script);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
    }
}
