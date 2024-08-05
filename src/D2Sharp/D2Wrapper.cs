using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace D2Sharp;

public partial class D2Wrapper
{
    private readonly ILogger<D2Wrapper> _logger;

    public D2Wrapper(ILogger<D2Wrapper>? logger = null)
    {
        _logger = logger ?? NullLogger<D2Wrapper>.Instance;
    }

    [LibraryImport("d2wrapper", EntryPoint = "RenderDiagram", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr RenderDiagramInternal(string script, out IntPtr errorPtr);

    [LibraryImport("d2wrapper", EntryPoint = "FreeDiagram")]
    private static partial void FreeDiagram(IntPtr ptr);

    public string? RenderDiagram(string script)
    {
        _logger.LogDebug("Calling RenderDiagram with script {Script}", script);
        IntPtr errorPtr;
        var svgPtr = RenderDiagramInternal(script, out errorPtr);

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
