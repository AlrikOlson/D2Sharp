namespace D2Sharp.Internal;

/// <summary>
/// Configuration options for D2Wrapper behavior.
/// </summary>
public class D2WrapperOptions
{
    /// <summary>
    /// Gets or sets whether to enable caching of rendered diagrams.
    /// Default: true
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of cached diagrams.
    /// Default: 100
    /// </summary>
    public int CacheSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum time a cached diagram is valid.
    /// Default: 1 hour
    /// </summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the maximum number of concurrent render operations.
    /// Set to 0 for unlimited. Default: 0 (unlimited)
    /// </summary>
    public int MaxConcurrentRenders { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to enable distributed tracing with Activity/OpenTelemetry.
    /// Default: true
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

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
