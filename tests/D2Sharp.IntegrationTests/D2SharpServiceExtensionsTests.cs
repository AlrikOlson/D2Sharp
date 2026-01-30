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

    [Fact]
    public void CircuitBreakerOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new D2SharpOptions.CircuitBreakerOptions();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(10), options.CooldownPeriod);
        Assert.Equal(0.2, options.OpenThreshold);
        Assert.Equal(0.5, options.CloseThreshold);
    }

    [Fact]
    public void D2SharpOptions_CircuitBreaker_IsNotNull()
    {
        // Arrange & Act
        var options = new D2SharpOptions();

        // Assert
        Assert.NotNull(options.CircuitBreaker);
    }

    [Fact]
    public void D2SharpOptions_CircuitBreaker_CanCustomize()
    {
        // Arrange & Act
        var options = new D2SharpOptions
        {
            CircuitBreaker = new D2SharpOptions.CircuitBreakerOptions
            {
                CooldownPeriod = TimeSpan.FromSeconds(30),
                OpenThreshold = 0.1,
                CloseThreshold = 0.6
            }
        };

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), options.CircuitBreaker.CooldownPeriod);
        Assert.Equal(0.1, options.CircuitBreaker.OpenThreshold);
        Assert.Equal(0.6, options.CircuitBreaker.CloseThreshold);
    }

    [Fact]
    public void AddD2Sharp_WithCircuitBreakerOptions_RegistersWithCustomConfig()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddD2Sharp(options =>
        {
            options.WorkerCount = 5;
            options.CircuitBreaker.CooldownPeriod = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.OpenThreshold = 0.15;
            options.CircuitBreaker.CloseThreshold = 0.7;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var renderer = provider.GetService<D2Renderer>();
        Assert.NotNull(renderer);
        renderer.Dispose();
    }

    [Fact]
    public void AddD2Sharp_WithBuilderCircuitBreakerConfig_RegistersWithCustomConfig()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddD2Sharp(d2 => d2
            .UseProcessPool(pool => pool
                .WithWorkerCount(5)
                .WithCircuitBreakerCooldown(TimeSpan.FromSeconds(15))
                .WithCircuitBreakerThresholds(0.1, 0.8)));
        var provider = services.BuildServiceProvider();

        // Assert
        var renderer = provider.GetService<D2Renderer>();
        Assert.NotNull(renderer);
        renderer.Dispose();
    }

    [Fact]
    public void AddD2Sharp_WithConfigureCircuitBreaker_RegistersWithCustomConfig()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddD2Sharp(d2 => d2
            .UseProcessPool()
            .ConfigureCircuitBreaker(cb =>
            {
                cb.CooldownPeriod = TimeSpan.FromSeconds(20);
                cb.OpenThreshold = 0.25;
                cb.CloseThreshold = 0.6;
            }));
        var provider = services.BuildServiceProvider();

        // Assert
        var renderer = provider.GetService<D2Renderer>();
        Assert.NotNull(renderer);
        renderer.Dispose();
    }

    [Fact]
    public void AddD2Sharp_WithInvalidCircuitBreakerOpenThreshold_ThrowsOnRendererCreation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddD2Sharp(options =>
        {
            options.CircuitBreaker.OpenThreshold = 1.5; // Invalid - must be 0.0-1.0
        });
        var provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetRequiredService<D2Renderer>());
    }

    [Fact]
    public void AddD2Sharp_WithInvalidCircuitBreakerCloseThreshold_ThrowsOnRendererCreation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddD2Sharp(options =>
        {
            options.CircuitBreaker.CloseThreshold = -0.1; // Invalid - must be 0.0-1.0
        });
        var provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetRequiredService<D2Renderer>());
    }

    [Fact]
    public void AddD2Sharp_WithInvalidCircuitBreakerCooldown_ThrowsOnRendererCreation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddD2Sharp(options =>
        {
            options.CircuitBreaker.CooldownPeriod = TimeSpan.Zero; // Invalid - must be positive
        });
        var provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetRequiredService<D2Renderer>());
    }

    [Fact]
    public void ProcessPoolOptions_CircuitBreaker_CanConfigure()
    {
        // Arrange & Act
        var poolOptions = new D2RendererBuilderExtensions.ProcessPoolOptions()
            .WithWorkerCount(8)
            .WithCircuitBreakerCooldown(TimeSpan.FromSeconds(25))
            .WithCircuitBreakerThresholds(0.15, 0.65);

        // Assert
        Assert.Equal(8, poolOptions.WorkerCount);
        Assert.Equal(TimeSpan.FromSeconds(25), poolOptions.CircuitBreaker.CooldownPeriod);
        Assert.Equal(0.15, poolOptions.CircuitBreaker.OpenThreshold);
        Assert.Equal(0.65, poolOptions.CircuitBreaker.CloseThreshold);
    }
}
