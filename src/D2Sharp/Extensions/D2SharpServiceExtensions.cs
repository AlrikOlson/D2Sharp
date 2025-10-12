using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace D2Sharp.Extensions;

/// <summary>
/// Extension methods for registering D2Sharp services with ASP.NET Core dependency injection.
/// </summary>
public static class D2SharpServiceExtensions
{
    /// <summary>
    /// Adds D2Sharp rendering services to the service collection with default configuration (10 workers).
    /// The D2Renderer instance is registered as a singleton for application-wide use.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddD2Sharp();
    /// </code>
    /// Then inject D2Renderer in your controllers or services:
    /// <code>
    /// public class DiagramController : ControllerBase
    /// {
    ///     private readonly D2Renderer _renderer;
    ///
    ///     public DiagramController(D2Renderer renderer)
    ///     {
    ///         _renderer = renderer;
    ///     }
    ///
    ///     [HttpPost("/render")]
    ///     public async Task&lt;IActionResult&gt; Render([FromBody] string script)
    ///     {
    ///         var result = await _renderer.RenderDiagramAsync(script);
    ///         if (result.IsSuccess)
    ///             return Content(result.Svg, "image/svg+xml");
    ///         return BadRequest(result.Error);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddD2Sharp(this IServiceCollection services)
    {
        return services.AddSingleton(sp =>
        {
            var logger = sp.GetService<ILogger<D2Renderer>>();
            return new D2Renderer(workerCount: 10, logger);
        });
    }

    /// <summary>
    /// Adds D2Sharp rendering services to the service collection with custom configuration.
    /// The D2Renderer instance is registered as a singleton for application-wide use.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Action to configure D2Sharp options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <example>
    /// Configure with custom worker count:
    /// <code>
    /// builder.Services.AddD2Sharp(options =>
    /// {
    ///     options.WorkerCount = 15;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddD2Sharp(this IServiceCollection services, Action<D2SharpOptions> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new D2SharpOptions();
        configure(options);

        return services.AddSingleton(sp =>
        {
            var logger = sp.GetService<ILogger<D2Renderer>>();
            return new D2Renderer(workerCount: options.WorkerCount, logger);
        });
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
    ///     options.EnableCaching = true;
    ///     options.MaxConcurrentRenders = 5;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddD2SharpDirect(this IServiceCollection services, Action<D2WrapperOptions>? configureOptions = null)
    {
        return services.AddSingleton(sp =>
        {
            var options = new D2WrapperOptions();
            configureOptions?.Invoke(options);

            var logger = sp.GetService<ILogger<D2Renderer>>();
            return D2Renderer.CreateDirect(options, logger);
        });
    }
}

/// <summary>
/// Configuration options for D2Sharp dependency injection.
/// </summary>
public class D2SharpOptions
{
    /// <summary>
    /// Gets or sets the number of worker processes to use in the pool.
    /// Default: 10. Recommended range: 3-20 depending on expected load.
    /// </summary>
    public int WorkerCount { get; set; } = 10;
}
