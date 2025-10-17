using D2Sharp.Builders;
using D2Sharp.Configuration;
using D2Sharp.Extensions;
using D2Sharp.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using ProcessPoolOptions = D2Sharp.Builders.D2RendererBuilderExtensions.ProcessPoolOptions;

namespace D2Sharp.IntegrationTests;

/// <summary>
/// Tests for D2RendererBuilderExtensions fluent API methods.
/// Ensures builder pattern works correctly with the closure-based options approach.
/// </summary>
public class D2RendererBuilderExtensionsTests
{
    [Fact]
    public void UseProcessPool_WithoutConfigure_SetsUseProcessPoolToTrue()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddD2Sharp();
        builder.UseProcessPool();

        // Assert
        var options = ((D2RendererBuilder)builder).GetOptions();
        Assert.True(options.UseProcessPool);
    }

    [Fact]
    public void UseProcessPool_WithWorkerCount_SetsWorkerCount()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddD2Sharp();
        builder.UseProcessPool(pool => pool.WorkerCount = 15);

        // Assert
        var options = ((D2RendererBuilder)builder).GetOptions();
        Assert.True(options.UseProcessPool);
        Assert.Equal(15, options.WorkerCount);
    }

    [Fact]
    public void UseProcessPool_WithFluentWorkerCount_SetsWorkerCount()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddD2Sharp();
        builder.UseProcessPool(pool => pool.WithWorkerCount(20));

        // Assert
        var options = ((D2RendererBuilder)builder).GetOptions();
        Assert.True(options.UseProcessPool);
        Assert.Equal(20, options.WorkerCount);
    }

    [Fact]
    public void UseDirect_SetsUseProcessPoolToFalse()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddD2Sharp();
        builder.UseDirect();

        // Assert
        var options = ((D2RendererBuilder)builder).GetOptions();
        Assert.False(options.UseProcessPool);
    }

    [Fact]
    public void ConfigureCaching_ModifiesCachingOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddD2Sharp();
        builder.ConfigureCaching(cache =>
        {
            cache.Enabled = true;
            cache.MaxSize = 500;
            cache.Expiration = TimeSpan.FromMinutes(30);
        });

        // Assert
        var options = ((D2RendererBuilder)builder).GetOptions();
        Assert.True(options.Caching.Enabled);
        Assert.Equal(500, options.Caching.MaxSize);
        Assert.Equal(TimeSpan.FromMinutes(30), options.Caching.Expiration);
    }

    [Fact]
    public void ConfigureTelemetry_ModifiesTelemetryOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddD2Sharp();
        builder.ConfigureTelemetry(telemetry =>
        {
            telemetry.EnableTracing = true;
            telemetry.EnableMetrics = true;
            telemetry.EnableDiagnosticIds = true;
        });

        // Assert
        var options = ((D2RendererBuilder)builder).GetOptions();
        Assert.True(options.Telemetry.EnableTracing);
        Assert.True(options.Telemetry.EnableMetrics);
        Assert.True(options.Telemetry.EnableDiagnosticIds);
    }

    [Fact]
    public void ConfigureConcurrency_ModifiesConcurrencyOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddD2Sharp();
        builder.ConfigureConcurrency(concurrency =>
        {
            concurrency.MaxConcurrentRenders = 8;
        });

        // Assert
        var options = ((D2RendererBuilder)builder).GetOptions();
        Assert.Equal(8, options.Concurrency.MaxConcurrentRenders);
    }

    [Fact]
    public void BuilderMethods_CanBeChained()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddD2Sharp(builder => builder
            .UseDirect()  // Use direct mode to avoid needing Worker executable in tests
            .ConfigureCaching(cache => cache.Enabled = true)
            .ConfigureTelemetry(telemetry => telemetry.EnableMetrics = true)
            .ConfigureConcurrency(concurrency => concurrency.MaxConcurrentRenders = 6));

        // Assert
        var provider = services.BuildServiceProvider();
        var renderer = provider.GetService<D2Renderer>();
        Assert.NotNull(renderer);
    }

    [Fact]
    public void DocumentedExample_WorksCorrectly()
    {
        // This is the example from D2SharpServiceExtensions.cs documentation (lines 28-30)
        // Arrange
        var services = new ServiceCollection();

        // Act - Modified to use Direct mode for testing (original example uses ProcessPool)
        services.AddD2Sharp(builder => builder
            .UseDirect()  // Use direct mode to avoid needing Worker executable in tests
            .ConfigureCaching(cache => cache.MaxSize = 200));

        // Assert - Verify it actually works
        var provider = services.BuildServiceProvider();
        var renderer = provider.GetService<D2Renderer>();
        Assert.NotNull(renderer);
    }

    [Fact]
    public void UseProcessPool_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            D2RendererBuilderExtensions.UseProcessPool(null!));
    }

    [Fact]
    public void UseDirect_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            D2RendererBuilderExtensions.UseDirect(null!));
    }

    [Fact]
    public void ConfigureCaching_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            D2RendererBuilderExtensions.ConfigureCaching(null!, _ => { }));
    }

    [Fact]
    public void ConfigureCaching_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddD2Sharp();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.ConfigureCaching(null!));
    }

    [Fact]
    public void ConfigureTelemetry_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            D2RendererBuilderExtensions.ConfigureTelemetry(null!, _ => { }));
    }

    [Fact]
    public void ConfigureTelemetry_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddD2Sharp();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.ConfigureTelemetry(null!));
    }

    [Fact]
    public void ConfigureConcurrency_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            D2RendererBuilderExtensions.ConfigureConcurrency(null!, _ => { }));
    }

    [Fact]
    public void ConfigureConcurrency_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddD2Sharp();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.ConfigureConcurrency(null!));
    }

    [Fact]
    public void ProcessPoolOptions_WithWorkerCount_FluentAPI_ReturnsInstance()
    {
        // Arrange
        var options = new ProcessPoolOptions();

        // Act
        var result = options.WithWorkerCount(25);

        // Assert
        Assert.Same(options, result);
        Assert.Equal(25, options.WorkerCount);
    }

    [Fact]
    public void ProcessPoolOptions_DefaultWorkerCount_IsTen()
    {
        // Arrange & Act
        var options = new ProcessPoolOptions();

        // Assert
        Assert.Equal(10, options.WorkerCount);
    }

    [Fact]
    public void MultipleCallsToSameMethod_LastOneWins()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddD2Sharp();
        builder.UseProcessPool(pool => pool.WorkerCount = 5);
        builder.UseProcessPool(pool => pool.WorkerCount = 15); // This should override

        // Assert
        var options = ((D2RendererBuilder)builder).GetOptions();
        Assert.Equal(15, options.WorkerCount);
    }

    [Fact]
    public void UseProcessPool_ThenUseDirect_DirectModeWins()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddD2Sharp();
        builder.UseProcessPool();
        builder.UseDirect(); // This should override

        // Assert
        var options = ((D2RendererBuilder)builder).GetOptions();
        Assert.False(options.UseProcessPool);
    }
}
