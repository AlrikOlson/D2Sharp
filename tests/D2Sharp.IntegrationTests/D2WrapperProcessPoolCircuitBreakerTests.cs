using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace D2Sharp.IntegrationTests;

/// <summary>
/// Tests for D2WrapperProcessPool circuit breaker functionality.
/// Note: Circuit breaker requires >80% workers crashed, which is difficult to
/// reliably test without killing processes. These tests focus on pool behavior
/// under various error conditions.
/// </summary>
public class D2WrapperProcessPoolCircuitBreakerTests
{
    [Fact]
    public async Task PoolBehavior_WithInvalidSyntax_ReturnsErrors()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 5);

        // Act - Send invalid D2 syntax (missing closing brace)
        var tasks = new List<Task<RenderResult>>();
        for (int i = 0; i < 4; i++)
        {
            tasks.Add(pool.RenderDiagramAsync("container: { style: {"));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Should get parse errors, not crashes
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
        });
    }

    [Fact]
    public async Task PoolBehavior_ConcurrentErrorsAndSuccesses_Handled()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 10);

        // Act - Mix of valid and invalid syntax
        var tasks = new List<Task<RenderResult>>();
        for (int i = 0; i < 20; i++)
        {
            if (i % 4 == 0)
            {
                // Invalid syntax
                tasks.Add(pool.RenderDiagramAsync("container: { {{{"));
            }
            else
            {
                // Valid syntax
                tasks.Add(pool.RenderDiagramAsync($"Node{i} -> Node{i + 1}"));
            }
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Valid requests succeed, invalid fail gracefully
        var successCount = results.Count(r => r.IsSuccess);
        var errorCount = results.Count(r => !r.IsSuccess);

        Assert.True(successCount > 0);
        Assert.True(errorCount > 0);
        Assert.Equal(20, successCount + errorCount);
    }

    [Fact]
    public async Task PoolBehavior_SequentialErrors_AllHandled()
    {
        // Arrange
        using var pool = new D2WrapperProcessPool(poolSize: 5);

        // Act - Sequential invalid requests
        var results = new List<RenderResult>();
        for (int i = 0; i < 10; i++)
        {
            var result = await pool.RenderDiagramAsync("x: {{{");
            results.Add(result);
        }

        // Assert - All return errors, no exceptions
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
        });
    }

    [Fact]
    public async Task PoolDisposal_WithActiveRequests_HandlesGracefully()
    {
        // Arrange
        var pool = new D2WrapperProcessPool(poolSize: 5);

        // Start some requests
        var task1 = pool.RenderDiagramAsync("A -> B");
        var task2 = pool.RenderDiagramAsync("C -> D");

        // Act - Dispose while requests active
        pool.Dispose();

        // Assert - Requests should either complete or throw ObjectDisposedException
        try
        {
            await task1;
            await task2;
        }
        catch (ObjectDisposedException)
        {
            // Expected
        }
    }
}
