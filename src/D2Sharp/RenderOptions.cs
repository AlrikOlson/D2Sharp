namespace D2Sharp;

/// <summary>
/// Options for customizing D2 diagram rendering.
/// </summary>
public class RenderOptions
{
    /// <summary>
    /// Gets the layout engine to use for diagram rendering.
    /// </summary>
    public LayoutEngine? Layout { get; init; }

    /// <summary>
    /// Gets the theme ID to use for diagram styling (0-300+).
    /// See: https://github.com/terrastruct/d2/tree/master/d2themes
    /// </summary>
    public int? ThemeId { get; init; }

    /// <summary>
    /// Gets the theme ID to use when the client is in dark mode.
    /// </summary>
    public int? DarkThemeId { get; init; }

    /// <summary>
    /// Gets a value indicating whether to enable sketch mode (hand-drawn style).
    /// </summary>
    public bool? Sketch { get; init; }

    /// <summary>
    /// Gets the padding in pixels around the rendered diagram (default: 100).
    /// </summary>
    public int? Pad { get; init; }

    /// <summary>
    /// Gets the scale factor for the output.
    /// E.g., 0.5 to halve the default size, 2.0 to double it.
    /// Setting to 1.0 turns off SVG fitting to screen.
    /// </summary>
    public double? Scale { get; init; }

    /// <summary>
    /// Gets a value indicating whether to center the SVG in the containing viewbox.
    /// </summary>
    public bool? Center { get; init; }

    /// <summary>
    /// Gets the target board to render. If target ends with '*', it will be rendered
    /// with all of its scenarios, steps, and layers. Pass '*' to render all.
    /// Requires AnimateInterval > 0 for multi-board outputs.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Gets the animation interval in milliseconds for multi-board diagrams.
    /// If set, multiple boards are packaged as 1 SVG which transitions through each board.
    /// </summary>
    public int? AnimateInterval { get; init; }

    /// <summary>
    /// Gets a value indicating whether to force an appendix for tooltips and links.
    /// </summary>
    public bool? ForceAppendix { get; init; }
}
