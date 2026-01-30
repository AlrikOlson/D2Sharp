using D2Sharp.Configuration;

namespace D2Sharp.Builders;

/// <summary>
/// Extension methods for configuring <see cref="ID2RendererBuilder"/>.
/// Provides fluent API for building D2 renderer configurations.
/// </summary>
public static class D2RendererBuilderExtensions
{
    /// <summary>
    /// Configures the renderer to use a process pool for rendering with optional worker count configuration.
    /// </summary>
    /// <param name="builder">The renderer builder.</param>
    /// <param name="configure">Optional action to configure pool options.</param>
    /// <returns>The builder for chaining.</returns>
    public static ID2RendererBuilder UseProcessPool(this ID2RendererBuilder builder, Action<ProcessPoolOptions>? configure = null)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        var options = ((D2RendererBuilder)builder).GetOptions();
        options.UseProcessPool = true;

        if (configure != null)
        {
            var poolOptions = new ProcessPoolOptions();
            configure(poolOptions);
            options.WorkerCount = poolOptions.WorkerCount;
            options.CircuitBreaker = poolOptions.CircuitBreaker;
        }

        return builder;
    }

    /// <summary>
    /// Configures the renderer to use direct P/Invoke rendering (no worker processes).
    /// Warning: Direct mode may crash your application on complex diagrams with deep nesting.
    /// </summary>
    /// <param name="builder">The renderer builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static ID2RendererBuilder UseDirect(this ID2RendererBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        var options = ((D2RendererBuilder)builder).GetOptions();
        options.UseProcessPool = false;

        return builder;
    }

    /// <summary>
    /// Configures caching options for the renderer.
    /// </summary>
    /// <param name="builder">The renderer builder.</param>
    /// <param name="configure">Action to configure caching options.</param>
    /// <returns>The builder for chaining.</returns>
    public static ID2RendererBuilder ConfigureCaching(this ID2RendererBuilder builder, Action<D2SharpOptions.CachingOptions> configure)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = ((D2RendererBuilder)builder).GetOptions();
        configure(options.Caching);

        return builder;
    }

    /// <summary>
    /// Configures telemetry options for the renderer.
    /// </summary>
    /// <param name="builder">The renderer builder.</param>
    /// <param name="configure">Action to configure telemetry options.</param>
    /// <returns>The builder for chaining.</returns>
    public static ID2RendererBuilder ConfigureTelemetry(this ID2RendererBuilder builder, Action<D2SharpOptions.TelemetryOptions> configure)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = ((D2RendererBuilder)builder).GetOptions();
        configure(options.Telemetry);

        return builder;
    }

    /// <summary>
    /// Configures concurrency options for the renderer (direct mode only).
    /// </summary>
    /// <param name="builder">The renderer builder.</param>
    /// <param name="configure">Action to configure concurrency options.</param>
    /// <returns>The builder for chaining.</returns>
    public static ID2RendererBuilder ConfigureConcurrency(this ID2RendererBuilder builder, Action<D2SharpOptions.ConcurrencyOptions> configure)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = ((D2RendererBuilder)builder).GetOptions();
        configure(options.Concurrency);

        return builder;
    }

    /// <summary>
    /// Configures circuit breaker options for the renderer (process pool mode only).
    /// </summary>
    /// <param name="builder">The renderer builder.</param>
    /// <param name="configure">Action to configure circuit breaker options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddD2Sharp(d2 => d2
    ///     .UseProcessPool()
    ///     .ConfigureCircuitBreaker(cb =>
    ///     {
    ///         cb.CooldownPeriod = TimeSpan.FromSeconds(30);
    ///         cb.OpenThreshold = 0.1;  // Open when less than 10% healthy
    ///         cb.CloseThreshold = 0.6; // Close when more than 60% healthy
    ///     }));
    /// </code>
    /// </example>
    public static ID2RendererBuilder ConfigureCircuitBreaker(this ID2RendererBuilder builder, Action<D2SharpOptions.CircuitBreakerOptions> configure)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = ((D2RendererBuilder)builder).GetOptions();
        configure(options.CircuitBreaker);

        return builder;
    }

    /// <summary>
    /// Options for configuring process pool behavior.
    /// </summary>
    public class ProcessPoolOptions
    {
        /// <summary>
        /// Gets or sets the number of worker processes to use.
        /// Default: 10
        /// </summary>
        public int WorkerCount { get; set; } = 10;

        /// <summary>
        /// Gets the circuit breaker configuration.
        /// </summary>
        public D2SharpOptions.CircuitBreakerOptions CircuitBreaker { get; set; } = new();

        /// <summary>
        /// Sets the number of worker processes.
        /// </summary>
        /// <param name="count">Number of workers.</param>
        /// <returns>This instance for chaining.</returns>
        public ProcessPoolOptions WithWorkerCount(int count)
        {
            WorkerCount = count;
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker cooldown period.
        /// </summary>
        /// <param name="cooldownPeriod">Time the circuit breaker stays open before testing recovery.</param>
        /// <returns>This instance for chaining.</returns>
        public ProcessPoolOptions WithCircuitBreakerCooldown(TimeSpan cooldownPeriod)
        {
            CircuitBreaker.CooldownPeriod = cooldownPeriod;
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker thresholds.
        /// </summary>
        /// <param name="openThreshold">Health percentage below which the circuit opens (0.0-1.0). Default: 0.2 (20%)</param>
        /// <param name="closeThreshold">Health percentage above which the circuit closes (0.0-1.0). Default: 0.5 (50%)</param>
        /// <returns>This instance for chaining.</returns>
        public ProcessPoolOptions WithCircuitBreakerThresholds(double openThreshold, double closeThreshold)
        {
            CircuitBreaker.OpenThreshold = openThreshold;
            CircuitBreaker.CloseThreshold = closeThreshold;
            return this;
        }
    }
}
