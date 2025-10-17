using D2Sharp.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace D2Sharp.IntegrationTests;

/// <summary>
/// Tests for D2WrapperProcessPool error handling scenarios.
/// </summary>
public class D2WrapperProcessPoolErrorHandlingTests
{
    [Fact]
    public async Task PoolSaturation_HandlesGracefully()
    {
        // Arrange - Create very small pool
        using var pool = new D2WrapperProcessPool(poolSize: 2);

        // Act - Send more requests than pool size (will queue)
        var tasks = new List<Task<RenderResult>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(pool.RenderDiagramAsync($"Node{i} -> Node{i + 1}"));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete (queuing works)
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
        });
    }

    [Fact]
    public async Task InvalidSyntax_ReturnsErrorResult()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 3);

        // Act - Send invalid syntax
        var result = await pool.RenderDiagramAsync("container: { style: {{{");

        // Assert - Should get error result
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.NotEmpty(result.Error.Message);
    }

    [Fact]
    public async Task MultipleErrors_PoolContinuesFunctioning()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 5);

        // Act - Send multiple invalid requests
        var errorTasks = new List<Task<RenderResult>>();
        for (int i = 0; i < 3; i++)
        {
            errorTasks.Add(pool.RenderDiagramAsync("invalid: {{{"));
        }

        await Task.WhenAll(errorTasks);

        // Pool should still work for valid requests
        var validResult = await pool.RenderDiagramAsync("ValidA -> ValidB");

        // Assert - Pool continues functioning
        Assert.NotNull(validResult);
        Assert.True(validResult.IsSuccess);
    }

    [Fact]
    public async Task MultipleSequentialErrors_HandledGracefully()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 5);

        // Act - Send multiple invalid requests sequentially
        var results = new List<RenderResult>();
        for (int i = 0; i < 10; i++)
        {
            var result = await pool.RenderDiagramAsync("x: { y: {{{");
            results.Add(result);
        }

        // Assert - All should return error results (not throw exceptions)
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
        });
    }

    [Fact]
    public async Task MixedValidAndInvalidRequests_HandledCorrectly()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 10);

        // Act - Mix of valid and invalid requests
        var tasks = new List<Task<RenderResult>>();
        for (int i = 0; i < 20; i++)
        {
            if (i % 3 == 0)
            {
                // Invalid syntax
                tasks.Add(pool.RenderDiagramAsync("container: {{{"));
            }
            else
            {
                // Valid syntax
                tasks.Add(pool.RenderDiagramAsync($"Node{i} -> Node{i + 1}"));
            }
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Valid requests should succeed, invalid should fail
        var successCount = results.Count(r => r.IsSuccess);
        var errorCount = results.Count(r => !r.IsSuccess);

        Assert.True(successCount > 0, "At least some valid requests should succeed");
        Assert.True(errorCount > 0, "Invalid requests should return errors");
    }

    [Fact]
    public async Task PoolWithErrors_ContinuesServing()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 8);

        // Act - Mix errors with valid requests
        var errorTasks = new List<Task<RenderResult>>();
        for (int i = 0; i < 6; i++)
        {
            errorTasks.Add(pool.RenderDiagramAsync("invalid: {{{"));
        }

        await Task.WhenAll(errorTasks);

        // Give system a moment
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Pool should continue serving valid requests
        var validResults = new List<RenderResult>();
        for (int i = 0; i < 5; i++)
        {
            var result = await pool.RenderDiagramAsync($"Node{i} -> Node{i + 1}");
            validResults.Add(result);
        }

        // Assert - Valid requests succeed
        Assert.All(validResults, result =>
        {
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        });
    }

    [Fact]
    public void Constructor_WithInvalidPoolSize_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new D2WrapperProcessPool(poolSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new D2WrapperProcessPool(poolSize: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new D2WrapperProcessPool(poolSize: -10));
    }
}
