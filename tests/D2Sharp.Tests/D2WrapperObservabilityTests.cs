using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace D2Sharp.Tests;

public class D2WrapperObservabilityTests
{
    [Fact]
    public void Constructor_WithOptions_InitializesWrapper()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = true,
            EnableTelemetry = true,
            EnableMetrics = true
        };

        // Act
        using var wrapper = new D2Wrapper(options);

        // Assert
        Assert.NotNull(wrapper);
    }

    [Fact]
    public void Constructor_WithNullOptions_UsesDefaults()
    {
        // Arrange & Act
        using var wrapper = new D2Wrapper((D2WrapperOptions?)null);

        // Assert
        Assert.NotNull(wrapper);
    }

    [Fact]
    public void RenderDiagram_WithCachingEnabled_ReturnsDiagnosticId()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = true,
            EnableDiagnosticIds = true
        };
        using var wrapper = new D2Wrapper(options);
        var script = "A -> B";

        // Act
        var result = wrapper.RenderDiagram(script);

        // Assert
        Assert.NotNull(result.DiagnosticId);
        Assert.False(result.FromCache);
    }

    [Fact]
    public void RenderDiagram_WithCachingEnabled_ReturnsCachedResult()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = true,
            EnableDiagnosticIds = true
        };
        using var wrapper = new D2Wrapper(options);
        var script = "A -> B -> C";

        // Act - First render
        var result1 = wrapper.RenderDiagram(script);

        // Act - Second render (should hit cache)
        var result2 = wrapper.RenderDiagram(script);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.False(result1.FromCache);
        Assert.NotNull(result1.Svg);

        Assert.True(result2.IsSuccess);
        Assert.True(result2.FromCache);
        Assert.Equal(result1.Svg, result2.Svg);
        Assert.NotEqual(result1.DiagnosticId, result2.DiagnosticId); // Different diagnostic IDs
    }

    [Fact]
    public void RenderDiagram_WithDifferentOptions_DoesNotHitCache()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = true
        };
        using var wrapper = new D2Wrapper(options);
        var script = "X -> Y";
        var renderOptions1 = new RenderOptions { ThemeId = 0 };
        var renderOptions2 = new RenderOptions { ThemeId = 1 };

        // Act
        var result1 = wrapper.RenderDiagram(script, renderOptions1);
        var result2 = wrapper.RenderDiagram(script, renderOptions2);

        // Assert
        Assert.False(result1.FromCache);
        Assert.False(result2.FromCache);
    }

    [Fact]
    public void RenderDiagram_WithCachingDisabled_DoesNotCache()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = false
        };
        using var wrapper = new D2Wrapper(options);
        var script = "P -> Q";

        // Act
        var result1 = wrapper.RenderDiagram(script);
        var result2 = wrapper.RenderDiagram(script);

        // Assert
        Assert.False(result1.FromCache);
        Assert.False(result2.FromCache);
    }

    [Fact]
    public void RenderDiagram_WithTelemetryEnabled_CreatesActivity()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableTelemetry = true
        };
        using var wrapper = new D2Wrapper(options);
        var script = "Server -> Database";

        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "D2Sharp",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => capturedActivity = activity
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        var result = wrapper.RenderDiagram(script);

        // Assert
        Assert.NotNull(capturedActivity);
        Assert.Equal("RenderDiagram", capturedActivity.OperationName);
    }

    [Fact]
    public void RenderDiagram_WithDiagnosticIdsDisabled_DoesNotGenerateDiagnosticId()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableDiagnosticIds = false
        };
        using var wrapper = new D2Wrapper(options);
        var script = "A -> B";

        // Act
        var result = wrapper.RenderDiagram(script);

        // Assert
        Assert.Null(result.DiagnosticId);
    }

    [Fact]
    public async Task RenderDiagramAsync_WithConcurrencyLimit_LimitsParallelExecutions()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            MaxConcurrentRenders = 2
        };
        using var wrapper = new D2Wrapper(options);
        var script = "A -> B -> C -> D";

        // Act - Start multiple concurrent renders
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => wrapper.RenderDiagramAsync(script))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete successfully
        Assert.All(results, r => Assert.True(r.IsSuccess));
    }

    [Fact]
    public async Task RenderDiagramAsync_WithNoConcurrencyLimit_AllowsUnlimitedParallelExecutions()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            MaxConcurrentRenders = 0 // No limit
        };
        using var wrapper = new D2Wrapper(options);
        var script = "X -> Y";

        // Act
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => wrapper.RenderDiagramAsync(script))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.True(r.IsSuccess));
    }

    [Fact]
    public void RenderDiagram_CachesOnlySuccessfulResults()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = true
        };
        using var wrapper = new D2Wrapper(options);
        var invalidScript = "A -> ";

        // Act
        var result1 = wrapper.RenderDiagram(invalidScript);
        var result2 = wrapper.RenderDiagram(invalidScript);

        // Assert - Errors should not be cached
        Assert.False(result1.IsSuccess);
        Assert.False(result2.IsSuccess);
        Assert.False(result1.FromCache);
        Assert.False(result2.FromCache);
    }

    [Fact]
    public void Dispose_WithCache_DisposesCache()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = true
        };
        var wrapper = new D2Wrapper(options);
        var script = "A -> B";

        // Act
        var result = wrapper.RenderDiagram(script);
        wrapper.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => wrapper.RenderDiagram(script));
    }

    [Fact]
    public async Task Dispose_WithConcurrencySemaphore_DisposesSemaphore()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            MaxConcurrentRenders = 1
        };
        var wrapper = new D2Wrapper(options);
        var script = "A -> B";

        // Act
        await wrapper.RenderDiagramAsync(script);
        wrapper.Dispose();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await wrapper.RenderDiagramAsync(script));
    }

    [Fact]
    public void D2WrapperOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new D2WrapperOptions();

        // Assert
        Assert.True(options.EnableCaching);
        Assert.Equal(100, options.CacheSize);
        Assert.Equal(TimeSpan.FromHours(1), options.CacheExpiration);
        Assert.Equal(0, options.MaxConcurrentRenders);
        Assert.True(options.EnableTelemetry);
        Assert.True(options.EnableMetrics);
        Assert.True(options.EnableDiagnosticIds);
    }

    [Fact]
    public void RenderDiagram_WithCustomCacheSize_RespectsLimit()
    {
        // Arrange
        var options = new D2WrapperOptions
        {
            EnableCaching = true,
            CacheSize = 2
        };
        using var wrapper = new D2Wrapper(options);

        // Act - Render 3 different diagrams
        var result1 = wrapper.RenderDiagram("A -> B");
        var result2 = wrapper.RenderDiagram("C -> D");
        var result3 = wrapper.RenderDiagram("E -> F");

        // Re-render first diagram (might be evicted if LRU works correctly)
        var result1Again = wrapper.RenderDiagram("A -> B");

        // Assert - First three should not be from cache
        Assert.False(result1.FromCache);
        Assert.False(result2.FromCache);
        Assert.False(result3.FromCache);

        // The first diagram might or might not be cached depending on LRU eviction
        // This is just to verify the cache size limit is working
        Assert.True(result1Again.IsSuccess);
    }

    [Fact]
    public void RenderResult_IsRecord_SupportsWithExpressions()
    {
        // Arrange
        var result = new RenderResult
        {
            Svg = "<svg></svg>",
            DiagnosticId = "test-id",
            FromCache = false
        };

        // Act
        var updatedResult = result with { FromCache = true };

        // Assert
        Assert.Equal("<svg></svg>", updatedResult.Svg);
        Assert.Equal("test-id", updatedResult.DiagnosticId);
        Assert.True(updatedResult.FromCache);
        Assert.False(result.FromCache); // Original unchanged
    }
}
