using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace d2.Net;

public enum D2Theme
{
    GrapeSoda,
    Aubergine,
    ButteredToast,
    // Add more themes as needed
}

public enum D2Padding
{
    None,
    Small,
    Medium,
    Large,
    // Add more padding options as needed
}

public partial class D2Wrapper
{
    private readonly ILogger<D2Wrapper> _logger;

    public D2Wrapper(ILogger<D2Wrapper>? logger = null)
    {
        _logger = logger ?? NullLogger<D2Wrapper>.Instance;
    }

    [LibraryImport("d2wrapper.dll", EntryPoint = "RenderDiagram", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr RenderDiagramInternal(string script, int themeID, int pad, out IntPtr errorPtr);

    [LibraryImport("d2wrapper.dll", EntryPoint = "FreeDiagram")]
    private static partial void FreeDiagram(IntPtr ptr);

    public string? RenderDiagram(string script, D2Theme theme = D2Theme.ButteredToast, D2Padding padding = D2Padding.Large)
    {
        _logger.LogDebug("Calling RenderDiagram with script {Script}", script);
        IntPtr errorPtr;
        var svgPtr = RenderDiagramInternal(script, (int)theme, (int)padding, out errorPtr);

        if (errorPtr != IntPtr.Zero)
        {
            var errorMessage = Marshal.PtrToStringUTF8(errorPtr);
            FreeDiagram(errorPtr);
            _logger.LogError("Diagram rendering failed: {ErrorMessage}", errorMessage);
            throw new Exception($"Diagram rendering failed: {errorMessage}");
        }

        if (svgPtr == IntPtr.Zero)
        {
            _logger.LogError("RenderDiagramInternal returned null pointer");
            return null;
        }

        var svg = Marshal.PtrToStringUTF8(svgPtr);
        FreeDiagram(svgPtr);

        _logger.LogDebug("Rendered diagram");
        return svg;
    }
}
