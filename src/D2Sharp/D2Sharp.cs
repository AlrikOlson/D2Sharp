using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace D2Sharp;

/// <summary>
/// Main entry point for D2Sharp library. Provides a zero-config, "just works" API
/// that uses worker process pool by default for robustness and high-frequency usage.
/// </summary>
/// <remarks>
/// <para>
/// This class intelligently manages D2 rendering with process isolation to prevent
/// crashes from affecting your application. By default, it creates a small pool of
/// worker processes (3 workers) which provides excellent reliability without requiring
/// any configuration.
/// </para>
/// <para>
/// For advanced scenarios, you can customize the number of workers or use alternative
/// implementations via factory methods.
/// </para>
/// <example>
/// Simple usage (recommended):
/// <code>
/// using var renderer = new D2Sharp();
/// var result = await renderer.RenderDiagramAsync("A -> B -> C");
/// if (result.IsSuccess)
/// {
///     Console.WriteLine(result.Svg);
/// }
/// </code>
///
/// Custom worker count:
/// <code>
/// using var renderer = new D2Sharp(workerCount: 10);
/// var result = await renderer.RenderDiagramAsync(script);
/// </code>
///
/// Factory methods for advanced control:
/// <code>
/// // Use worker pool with custom size
/// using var renderer = D2Sharp.CreateWithPool(workerCount: 15);
///
/// // Use direct P/Invoke (less robust, but no worker processes)
/// using var renderer = D2Sharp.CreateDirect();
/// </code>
/// </example>
/// </remarks>
public class D2Sharp : ID2Renderer
{
    private readonly ID2Renderer _implementation;
    private readonly bool _ownsImplementation;

    /// <summary>
    /// Initializes a new instance of the <see cref="D2Sharp"/> class with default worker pool (3 workers).
    /// This is the recommended constructor for most use cases.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public D2Sharp(ILogger<D2Sharp>? logger = null)
        : this(3, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="D2Sharp"/> class with a custom number of workers.
    /// </summary>
    /// <param name="workerCount">Number of worker processes to use. Recommended: 3-15 depending on load.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when workerCount is less than 1.</exception>
    public D2Sharp(int workerCount, ILogger<D2Sharp>? logger = null)
    {
        if (workerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(workerCount), "Worker count must be at least 1");

        // Create logger for the pool if provided
        ILogger<D2WrapperProcessPool>? poolLogger = null;
        if (logger != null)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                // Copy configuration from provided logger (best effort)
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            poolLogger = loggerFactory.CreateLogger<D2WrapperProcessPool>();
        }

        _implementation = new D2WrapperProcessPool(workerCount, poolLogger);
        _ownsImplementation = true;
    }

    /// <summary>
    /// Internal constructor for advanced scenarios where a custom implementation is provided.
    /// </summary>
    /// <param name="implementation">The rendering implementation to use.</param>
    /// <param name="ownsImplementation">Whether this instance owns and should dispose the implementation.</param>
    private D2Sharp(ID2Renderer implementation, bool ownsImplementation = false)
    {
        _implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
        _ownsImplementation = ownsImplementation;
    }

    /// <summary>
    /// Creates a D2Sharp instance using a worker process pool with a custom number of workers.
    /// This is the same as using the constructor but provides a more explicit API.
    /// </summary>
    /// <param name="workerCount">Number of worker processes to use. Default: 10.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>A new D2Sharp instance configured with a worker pool.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when workerCount is less than 1.</exception>
    public static D2Sharp CreateWithPool(int workerCount = 10, ILogger? logger = null)
    {
        if (workerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(workerCount), "Worker count must be at least 1");

        ILogger<D2WrapperProcessPool>? poolLogger = null;
        if (logger != null)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            poolLogger = loggerFactory.CreateLogger<D2WrapperProcessPool>();
        }

        var pool = new D2WrapperProcessPool(workerCount, poolLogger);
        return new D2Sharp(pool, ownsImplementation: true);
    }

    /// <summary>
    /// Creates a D2Sharp instance using direct P/Invoke (no worker processes).
    /// This is less robust than the default worker pool approach and may crash your application
    /// on complex diagrams, but it uses less resources and has lower latency for simple diagrams.
    /// Only use this if you understand the trade-offs.
    /// </summary>
    /// <param name="options">Optional wrapper options for caching, telemetry, and concurrency control.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <returns>A new D2Sharp instance configured for direct P/Invoke rendering.</returns>
    /// <remarks>
    /// Warning: This approach directly invokes the native D2 library without process isolation.
    /// Complex diagrams with deep nesting may cause stack overflow and crash your application.
    /// Use worker pool mode (default) for production scenarios.
    /// </remarks>
    public static D2Sharp CreateDirect(D2WrapperOptions? options = null, ILogger? logger = null)
    {
        ILogger<D2Wrapper>? wrapperLogger = null;
        if (logger != null)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            wrapperLogger = loggerFactory.CreateLogger<D2Wrapper>();
        }

        var wrapper = new D2Wrapper(options, wrapperLogger);
        return new D2Sharp(wrapper, ownsImplementation: true);
    }

    /// <summary>
    /// Synchronously renders a D2 diagram script as SVG.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="options">Optional rendering options for customization.</param>
    /// <returns>A <see cref="RenderResult"/> containing either the SVG output or error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="script"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the wrapper has been disposed.</exception>
    public RenderResult RenderDiagram(string script, RenderOptions? options = null)
    {
        return _implementation.RenderDiagram(script, options);
    }

    /// <summary>
    /// Asynchronously renders a D2 diagram script as SVG.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="options">Optional rendering options for customization.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="RenderResult"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="script"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the wrapper has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    public Task<RenderResult> RenderDiagramAsync(string script, RenderOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _implementation.RenderDiagramAsync(script, options, cancellationToken);
    }

    /// <summary>
    /// Releases all resources used by the <see cref="D2Sharp"/> instance.
    /// </summary>
    public void Dispose()
    {
        if (_ownsImplementation)
        {
            _implementation?.Dispose();
        }
    }
}
