namespace D2Sharp;

/// <summary>
/// Represents the result of a D2 diagram rendering operation.
/// </summary>
public record RenderResult
{
    /// <summary>
    /// Gets the rendered SVG output. Null if rendering failed.
    /// </summary>
    public string? Svg { get; init; }

    /// <summary>
    /// Gets error information if rendering failed. Null if successful.
    /// </summary>
    public D2Error? Error { get; init; }

    /// <summary>
    /// Gets the diagnostic ID for this render operation. Useful for correlating logs and telemetry.
    /// </summary>
    public string? DiagnosticId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this result was served from cache.
    /// </summary>
    public bool FromCache { get; init; }

    /// <summary>
    /// Gets a value indicating whether the rendering was successful.
    /// </summary>
    public bool IsSuccess => Error == null;
}
