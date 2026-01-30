using D2Sharp.Builders;
using D2Sharp.Configuration;
using D2Sharp.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D2Sharp.Extensions;

/// <summary>
/// Extension methods for registering D2Sharp services with ASP.NET Core dependency injection.
/// </summary>
public static class D2SharpServiceExtensions
{
    private const string DefaultRendererName = "Default";

    /// <summary>
    /// Adds D2Sharp rendering services to the service collection with default configuration (10 workers, process pool mode).
    /// Returns a builder for additional configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>A builder for configuring the renderer.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddD2Sharp();
    ///
    /// // Or with configuration:
    /// builder.Services.AddD2Sharp(builder => builder
    ///     .UseProcessPool(pool => pool.WithWorkerCount(15))
    ///     .ConfigureCaching(cache => cache.MaxSize = 200)
    /// );
    /// </code>
    /// </example>
    public static ID2RendererBuilder AddD2Sharp(this IServiceCollection services)
    {
        return services.AddD2Sharp(DefaultRendererName);
    }

    /// <summary>
    /// Adds D2Sharp rendering services to the service collection with configuration via a builder action.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureBuilder">Action to configure the renderer using the builder pattern.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddD2Sharp(this IServiceCollection services, Action<ID2RendererBuilder> configureBuilder)
    {
        if (configureBuilder == null) throw new ArgumentNullException(nameof(configureBuilder));

        var builder = services.AddD2Sharp(DefaultRendererName);
        configureBuilder(builder);
        return services;
    }

    /// <summary>
    /// Adds D2Sharp rendering services with backward-compatible simple configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Action to configure D2Sharp options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddD2Sharp(options =>
    /// {
    ///     options.WorkerCount = 15;
    ///     options.Caching.MaxSize = 200;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddD2Sharp(this IServiceCollection services, Action<D2SharpOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var builder = services.AddD2Sharp(DefaultRendererName);
        configure(((D2RendererBuilder)builder).GetOptions());
        return services;
    }

    /// <summary>
    /// Adds a named D2Sharp renderer instance.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="name">The name of the renderer instance.</param>
    /// <returns>A builder for configuring the renderer.</returns>
    private static ID2RendererBuilder AddD2Sharp(this IServiceCollection services, string name)
    {
        // Create default options - will be captured by closure
        var options = new D2SharpOptions();

        // Register D2Renderer as singleton with captured options
        services.AddSingleton(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            return CreateRenderer(options, loggerFactory);
        });

        // Register as ID2Renderer interface as well
        services.AddSingleton<ID2Renderer>(sp => sp.GetRequiredService<D2Renderer>());

        return new D2RendererBuilder(services, name, options);
    }

    /// <summary>
    /// Adds D2Sharp rendering services using direct P/Invoke (no worker processes).
    /// Use this only if you understand the trade-offs - worker pool mode is recommended for production.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Optional action to configure D2Wrapper options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// Warning: Direct mode may crash your application on complex diagrams with deep nesting.
    /// Only use this for specific scenarios where you need lower latency and can tolerate the risk.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddD2SharpDirect(options =>
    /// {
    ///     options.Caching.Enabled = true;
    ///     options.Concurrency.MaxConcurrentRenders = 5;
    /// });
    /// </code>
    /// </example>
    [Obsolete("Use AddD2Sharp with builder.UseDirect() instead. This method will be removed in a future version.")]
    public static IServiceCollection AddD2SharpDirect(this IServiceCollection services, Action<D2SharpOptions>? configureOptions = null)
    {
        var builder = services.AddD2Sharp(DefaultRendererName);
        var options = ((D2RendererBuilder)builder).GetOptions();
        options.UseProcessPool = false;
        configureOptions?.Invoke(options);

        return services;
    }

    private static D2Renderer CreateRenderer(D2SharpOptions options, ILoggerFactory? loggerFactory)
    {
        if (options.UseProcessPool)
        {
            // Create logger for pool using the app's ILoggerFactory (respects appsettings.json)
            var poolLogger = loggerFactory?.CreateLogger<D2WrapperProcessPool>();
            var pool = new D2WrapperProcessPool(options.WorkerCount, poolLogger, options.CircuitBreaker);
            return new D2Renderer(pool, ownsImplementation: true);
        }
        else
        {
            // Direct mode: create D2Wrapper with options
            var wrapperOptions = ConvertToWrapperOptions(options);
            var wrapperLogger = loggerFactory?.CreateLogger<D2Wrapper>();
            var wrapper = new D2Wrapper(wrapperOptions, wrapperLogger);
            return new D2Renderer(wrapper, ownsImplementation: true);
        }
    }

    private static D2WrapperOptions ConvertToWrapperOptions(D2SharpOptions options)
    {
        return new D2WrapperOptions
        {
            EnableCaching = options.Caching.Enabled,
            CacheSize = options.Caching.MaxSize,
            CacheExpiration = options.Caching.Expiration,
            EnableTelemetry = options.Telemetry.EnableTracing,
            EnableMetrics = options.Telemetry.EnableMetrics,
            EnableDiagnosticIds = options.Telemetry.EnableDiagnosticIds,
            MaxConcurrentRenders = options.Concurrency.MaxConcurrentRenders
        };
    }
}
