namespace D2Sharp;

/// <summary>
/// Specifies the layout engine to use for diagram rendering.
/// </summary>
public enum LayoutEngine
{
    /// <summary>
    /// Dagre layout engine (default, faster).
    /// </summary>
    Dagre,

    /// <summary>
    /// ELK (Eclipse Layout Kernel) layout engine (more features, slower).
    /// </summary>
    Elk
}
