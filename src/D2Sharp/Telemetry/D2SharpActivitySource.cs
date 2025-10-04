using System.Diagnostics;

namespace D2Sharp.Telemetry;

/// <summary>
/// Provides distributed tracing capabilities for D2Sharp operations using Activity/ActivitySource.
/// Compatible with OpenTelemetry and Application Insights.
/// </summary>
public static class D2SharpActivitySource
{
    /// <summary>
    /// The name of the Activity Source for D2Sharp operations.
    /// </summary>
    public const string SourceName = "D2Sharp";

    /// <summary>
    /// The current version of the Activity Source.
    /// </summary>
    public const string Version = "0.3.0";

    /// <summary>
    /// The ActivitySource instance for creating Activity spans.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, Version);

    /// <summary>
    /// Tag names used in Activity spans for consistent telemetry.
    /// </summary>
    public static class Tags
    {
        /// <summary>
        /// The length of the D2 script in characters.
        /// </summary>
        public const string ScriptLength = "d2sharp.script.length";

        /// <summary>
        /// The layout engine used (dagre or elk).
        /// </summary>
        public const string LayoutEngine = "d2sharp.layout.engine";

        /// <summary>
        /// The theme ID used for rendering.
        /// </summary>
        public const string ThemeId = "d2sharp.theme.id";

        /// <summary>
        /// Whether sketch mode is enabled.
        /// </summary>
        public const string SketchMode = "d2sharp.sketch.enabled";

        /// <summary>
        /// The diagnostic ID for this render operation.
        /// </summary>
        public const string DiagnosticId = "d2sharp.diagnostic.id";

        /// <summary>
        /// Whether the render result was served from cache.
        /// </summary>
        public const string CacheHit = "d2sharp.cache.hit";

        /// <summary>
        /// The result status (success or error).
        /// </summary>
        public const string ResultStatus = "d2sharp.result.status";

        /// <summary>
        /// The error type if rendering failed.
        /// </summary>
        public const string ErrorType = "d2sharp.error.type";
    }
}
