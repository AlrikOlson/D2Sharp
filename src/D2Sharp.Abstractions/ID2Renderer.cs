namespace D2Sharp;

/// <summary>
/// Defines the interface for D2 diagram rendering implementations.
/// All D2Sharp rendering implementations should implement this interface.
/// </summary>
public interface ID2Renderer : IDisposable
{
    /// <summary>
    /// Synchronously renders a D2 diagram script as SVG.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="options">Optional rendering options for customization.</param>
    /// <returns>A <see cref="RenderResult"/> containing either the SVG output or error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="script"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the renderer has been disposed.</exception>
    RenderResult RenderDiagram(string script, RenderOptions? options = null);

    /// <summary>
    /// Asynchronously renders a D2 diagram script as SVG.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="options">Optional rendering options for customization.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="RenderResult"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="script"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the renderer has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<RenderResult> RenderDiagramAsync(string script, RenderOptions? options = null, CancellationToken cancellationToken = default);
}
