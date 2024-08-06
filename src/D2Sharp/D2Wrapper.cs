using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;

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

    public RenderResult RenderDiagram(string script)
    {
        _logger.LogDebug("Calling RenderDiagram with script");

        IntPtr errorPtr;
        var svgPtr = RenderDiagramInternal(script, out errorPtr);

        if (errorPtr != IntPtr.Zero)
        {
            var errorMessage = Marshal.PtrToStringUTF8(errorPtr);
            FreeDiagram(errorPtr);
            _logger.LogError("Diagram rendering failed: {ErrorMessage}", errorMessage);
            return new RenderResult { Error = ParseError(errorMessage, script) };
        }

        if (svgPtr == IntPtr.Zero)
        {
            _logger.LogError("RenderDiagramInternal returned null pointer");
            return new RenderResult { Error = new D2Error { Message = "Rendering failed with null result" } };
        }

        var svg = Marshal.PtrToStringUTF8(svgPtr);
        FreeDiagram(svgPtr);

        _logger.LogDebug("Rendered diagram successfully");
        return new RenderResult { Svg = svg };
    }

    private D2Error ParseError(string errorMessage, string script)
    {
        var error = new D2Error { Message = errorMessage };

        // Match the format: "Compilation error: line:column: specific error message"
        var match = Regex.Match(errorMessage, @"Compilation error: (\d+):(\d+): (.+)");
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out int lineNumber))
            {
                error.LineNumber = lineNumber;
                error.LineContent = GetLineContent(script, lineNumber);
            }
            if (int.TryParse(match.Groups[2].Value, out int column))
            {
                error.Column = column;
            }
            error.Message = match.Groups[3].Value.Trim();
        }

        return error;
    }

    private string GetLineContent(string script, int lineNumber)
    {
        var lines = script.Split('\n');
        if (lineNumber > 0 && lineNumber <= lines.Length)
        {
            return lines[lineNumber - 1];
        }
        return string.Empty;
    }
}

public class RenderResult
{
    public string? Svg { get; set; }
    public D2Error? Error { get; set; }
    public bool IsSuccess => Error == null;
}

public class D2Error
{
    public string Message { get; set; } = "";
    public int? LineNumber { get; set; }
    public int? Column { get; set; }
    public string? LineContent { get; set; }

    public (string beforeError, string errorPart, string afterError) GetHighlightedLineParts()
    {
        if (string.IsNullOrEmpty(LineContent) || !Column.HasValue || Column.Value <= 0)
        {
            return (LineContent ?? "", "", "");
        }

        int highlightIndex = Column.Value - 1;
        if (highlightIndex >= LineContent.Length)
        {
            highlightIndex = LineContent.Length - 1;
        }

        string beforeError = LineContent.Substring(0, highlightIndex);
        string errorPart = LineContent.Substring(highlightIndex, 1);
        string afterError = highlightIndex + 1 < LineContent.Length ? LineContent.Substring(highlightIndex + 1) : "";

        return (beforeError, errorPart, afterError);
    }
}
