using D2Sharp.Builders;
using D2Sharp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using D2Sharp.Extensions;

namespace D2Sharp.IntegrationTests;

/// <summary>
/// Tests for D2Sharp DI extension methods focusing on registration and resolution.
/// Rendering tests are avoided to prevent flaky worker startup issues.
/// </summary>
public class D2SharpServiceExtensionsTests
{
    [Fact]
    public void AddD2Sharp_WithoutOptions_RegistersSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddD2Sharp();
        var provider = services.BuildServiceProvider();

        // Assert
        var renderer = provider.GetService<D2Renderer>();
        Assert.NotNull(renderer);
        renderer.Dispose();
    }

    [Fact]
    public void AddD2Sharp_WithOptions_RegistersWithCustomWorkerCount()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddD2Sharp(options =>
        {
            options.WorkerCount = 15;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var renderer = provider.GetService<D2Renderer>();
        Assert.NotNull(renderer);
        renderer.Dispose();
    }

    [Fact]
    public void AddD2Sharp_MultipleCalls_UsesSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddD2Sharp();
        var provider = services.BuildServiceProvider();

        // Act
        var renderer1 = provider.GetRequiredService<D2Renderer>();
        var renderer2 = provider.GetRequiredService<D2Renderer>();

        // Assert
        Assert.Same(renderer1, renderer2);
        renderer1.Dispose();
    }

    [Fact]
    public void AddD2Sharp_WithLogger_UsesLoggerFromDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddD2Sharp();
        var provider = services.BuildServiceProvider();

        // Act
        using var renderer = provider.GetRequiredService<D2Renderer>();

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void AddD2SharpDirect_RegistersDirectMode()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddD2Sharp(d2 => d2.UseDirect());
        var provider = services.BuildServiceProvider();

        // Assert
        using var renderer = provider.GetService<D2Renderer>();
        Assert.NotNull(renderer);
    }

    [Fact]
    public void AddD2SharpDirect_WithOptions_UsesCustomOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddD2Sharp(d2 => d2
            .UseDirect()
            .ConfigureCaching(cache => cache.Enabled = false)
            .ConfigureTelemetry(telemetry =>
            {
                telemetry.EnableTracing = false;
                telemetry.EnableMetrics = false;
            }));
        var provider = services.BuildServiceProvider();

        // Assert
        using var renderer = provider.GetRequiredService<D2Renderer>();
        Assert.NotNull(renderer);
    }

    [Fact]
    public void AddD2Sharp_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddD2Sharp((Action<D2SharpOptions>)null!));
    }

    [Fact]
    public void AddD2Sharp_WithInvalidWorkerCount_ThrowsOnRendererCreation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddD2Sharp(options =>
        {
            options.WorkerCount = 0; // Invalid
        });
        var provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetRequiredService<D2Renderer>());
    }

    [Fact]
    public void D2SharpOptions_DefaultWorkerCount_IsTen()
    {
        // Arrange & Act
        var options = new D2SharpOptions();

        // Assert
        Assert.Equal(10, options.WorkerCount);
    }

    [Fact]
    public void D2SharpOptions_CanSetCustomWorkerCount()
    {
        // Arrange & Act
        var options = new D2SharpOptions
        {
            WorkerCount = 20
        };

        // Assert
        Assert.Equal(20, options.WorkerCount);
    }
}
