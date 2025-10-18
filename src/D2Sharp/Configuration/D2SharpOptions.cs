namespace D2Sharp.Configuration;

/// <summary>
/// Configuration options for D2Sharp renderer services.
/// Consolidates all configuration into a single options class following Microsoft's IOptions pattern.
/// </summary>
public class D2SharpOptions
{
    /// <summary>
    /// Gets or sets the number of worker processes to use in the pool.
    /// Default: 10. Recommended range: 3-20 depending on expected load.
    /// Only applies when using process pool mode.
    /// </summary>
    public int WorkerCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to use process pool mode (true) or direct mode (false).
    /// Default: true (process pool for reliability).
    /// </summary>
    public bool UseProcessPool { get; set; } = true;

    /// <summary>
    /// Gets the caching configuration options.
    /// </summary>
    public CachingOptions Caching { get; set; } = new();

    /// <summary>
    /// Gets the telemetry configuration options.
    /// </summary>
    public TelemetryOptions Telemetry { get; set; } = new();

    /// <summary>
    /// Gets the concurrency configuration options.
    /// </summary>
    public ConcurrencyOptions Concurrency { get; set; } = new();

    /// <summary>
    /// Caching configuration for D2Sharp.
    /// </summary>
    public class CachingOptions
    {
        /// <summary>
        /// Gets or sets whether to enable caching of rendered diagrams.
        /// Default: true
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of cached diagrams.
        /// Default: 100
        /// </summary>
        public int MaxSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum time a cached diagram is valid.
        /// Default: 1 hour
        /// </summary>
        public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Telemetry configuration for D2Sharp.
    /// </summary>
    public class TelemetryOptions
    {
        /// <summary>
        /// Gets or sets whether to enable distributed tracing with Activity/OpenTelemetry.
        /// Default: true
        /// </summary>
        public bool EnableTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable EventCounters for real-time metrics.
        /// Default: true
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate diagnostic IDs for each render operation.
        /// Default: true
        /// </summary>
        public bool EnableDiagnosticIds { get; set; } = true;
    }

    /// <summary>
    /// Concurrency configuration for D2Sharp.
    /// </summary>
    public class ConcurrencyOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent render operations.
        /// Set to 0 for unlimited. Default: 0 (unlimited)
        /// Only applies in direct mode (not process pool mode).
        /// </summary>
        public int MaxConcurrentRenders { get; set; } = 0;
    }
}
