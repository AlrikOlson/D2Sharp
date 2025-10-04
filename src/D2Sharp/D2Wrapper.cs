using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using D2Sharp.Caching;
using D2Sharp.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;

namespace D2Sharp;

/// <summary>
/// Provides functionality to render D2 diagrams as SVG.
/// </summary>
public partial class D2Wrapper : IDisposable
{
    private readonly ILogger<D2Wrapper> _logger;
    private readonly D2WrapperOptions _options;
    private readonly RenderCache? _cache;
    private readonly SemaphoreSlim? _concurrencySemaphore;
    private int _disposed; // 0 = false, 1 = true (thread-safe via Interlocked)

    private const int MaxScriptLength = 10_000_000; // 10MB character limit
    private static readonly TimeSpan MinTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxTimeout = TimeSpan.FromMinutes(10);

    [GeneratedRegex(@"Compilation error: (\d+):(\d+): (.+)", RegexOptions.Compiled)]
    private static partial Regex CompilationErrorRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="D2Wrapper"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public D2Wrapper(ILogger<D2Wrapper>? logger = null)
        : this(null, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="D2Wrapper"/> class with configuration options.
    /// </summary>
    /// <param name="options">Configuration options for caching, telemetry, and concurrency control.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public D2Wrapper(D2WrapperOptions? options, ILogger<D2Wrapper>? logger = null)
    {
        _logger = logger ?? NullLogger<D2Wrapper>.Instance;
        _options = options ?? new D2WrapperOptions();

        // Initialize cache if enabled
        if (_options.EnableCaching)
        {
            _cache = new RenderCache(_options);
            _logger.LogDebug("Render cache initialized with size {CacheSize}", _options.CacheSize);
        }

        // Initialize concurrency semaphore if max concurrent renders is set
        if (_options.MaxConcurrentRenders > 0)
        {
            _concurrencySemaphore = new SemaphoreSlim(_options.MaxConcurrentRenders, _options.MaxConcurrentRenders);
            _logger.LogDebug("Concurrency limit set to {MaxConcurrentRenders}", _options.MaxConcurrentRenders);
        }
    }

    [LibraryImport("d2wrapper", EntryPoint = "RenderDiagram", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr RenderDiagramInternal(string script, string optionsJson, out IntPtr errorPtr);

    [LibraryImport("d2wrapper", EntryPoint = "FreeDiagram")]
    private static partial void FreeDiagram(IntPtr ptr);

    /// <summary>
    /// Renders a D2 diagram script as SVG.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="options">Optional rendering options for customization.</param>
    /// <returns>A <see cref="RenderResult"/> containing either the SVG output or error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="script"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when script exceeds maximum length.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the wrapper has been disposed.</exception>
    /// <exception cref="DllNotFoundException">Thrown when the native d2wrapper library cannot be found.</exception>
    /// <exception cref="EntryPointNotFoundException">Thrown when required functions are missing from the native library.</exception>
    public RenderResult RenderDiagram(string script, RenderOptions? options = null)
    {
        ThrowIfDisposed();

        if (script == null)
        {
            throw new ArgumentNullException(nameof(script));
        }

        if (script.Length > MaxScriptLength)
        {
            throw new ArgumentException($"Script exceeds maximum length of {MaxScriptLength} characters", nameof(script));
        }

        // Generate diagnostic ID if enabled
        var diagnosticId = _options.EnableDiagnosticIds
            ? Activity.Current?.Id ?? Guid.NewGuid().ToString("N")
            : null;

        // Start Activity span for distributed tracing
        using var activity = _options.EnableTelemetry
            ? D2SharpActivitySource.Source.StartActivity("RenderDiagram")
            : null;

        if (activity != null)
        {
            activity.SetTag(D2SharpActivitySource.Tags.ScriptLength, script.Length);
            activity.SetTag(D2SharpActivitySource.Tags.LayoutEngine, options?.Layout?.ToString() ?? "dagre");
            activity.SetTag(D2SharpActivitySource.Tags.ThemeId, options?.ThemeId);
            activity.SetTag(D2SharpActivitySource.Tags.SketchMode, options?.Sketch ?? false);
            if (diagnosticId != null)
            {
                activity.SetTag(D2SharpActivitySource.Tags.DiagnosticId, diagnosticId);
            }
        }

        _logger.LogDebug("Calling RenderDiagram with script (DiagnosticId: {DiagnosticId})", diagnosticId);

        // Check cache before rendering
        if (_options.EnableCaching && _cache?.TryGet(script, options, out var cachedResult) == true)
        {
            if (_options.EnableMetrics)
            {
                D2SharpEventCounters.Instance.RecordCacheAccess(true);
            }

            activity?.SetTag(D2SharpActivitySource.Tags.CacheHit, true);
            activity?.SetTag(D2SharpActivitySource.Tags.ResultStatus, "success");

            _logger.LogDebug("Cache hit for diagram (DiagnosticId: {DiagnosticId})", diagnosticId);

            return cachedResult! with { DiagnosticId = diagnosticId, FromCache = true };
        }

        if (_options.EnableCaching)
        {
            if (_options.EnableMetrics)
            {
                D2SharpEventCounters.Instance.RecordCacheAccess(false);
            }

            activity?.SetTag(D2SharpActivitySource.Tags.CacheHit, false);
        }

        // Record metrics
        if (_options.EnableMetrics)
        {
            D2SharpEventCounters.Instance.RenderStarted();
        }

        var sw = Stopwatch.StartNew();
        RenderResult? result = null;

        try
        {
            result = RenderDiagramCore(script, options, diagnosticId);

            // Store successful result in cache
            if (_options.EnableCaching && result.IsSuccess && _cache != null)
            {
                _cache.Set(script, options, result, _options.CacheExpiration);
                _logger.LogDebug("Stored result in cache (DiagnosticId: {DiagnosticId})", diagnosticId);
            }

            return result;
        }
        finally
        {
            sw.Stop();
            if (_options.EnableMetrics)
            {
                D2SharpEventCounters.Instance.RenderCompleted(sw.Elapsed.TotalMilliseconds, result?.IsSuccess ?? false);
            }

            if (activity != null)
            {
                activity.SetTag(D2SharpActivitySource.Tags.ResultStatus, result?.IsSuccess == true ? "success" : "error");
                if (result?.Error != null)
                {
                    activity.SetTag(D2SharpActivitySource.Tags.ErrorType, "compilation_error");
                }
            }
        }
    }

    private RenderResult RenderDiagramCore(string script, RenderOptions? options, string? diagnosticId)
    {
        // Serialize options to JSON
        string optionsJson = SerializeOptions(options);

        IntPtr errorPtr = IntPtr.Zero;
        IntPtr svgPtr = IntPtr.Zero;

        try
        {
            try
            {
                svgPtr = RenderDiagramInternal(script, optionsJson, out errorPtr);
            }
            catch (DllNotFoundException ex)
            {
                _logger.LogError(ex, "Native d2wrapper library not found");
                throw new DllNotFoundException(
                    "The d2wrapper native library could not be found. Ensure the library is built and in the correct location.", ex);
            }
            catch (EntryPointNotFoundException ex)
            {
                _logger.LogError(ex, "Required entry point not found in d2wrapper library");
                throw new EntryPointNotFoundException(
                    "Required function not found in d2wrapper library. The library version may be incompatible.", ex);
            }

            if (errorPtr != IntPtr.Zero)
            {
                var errorMessage = Marshal.PtrToStringUTF8(errorPtr);
                if (errorMessage == null)
                {
                    _logger.LogError("Failed to read error message from native library");
                    return new RenderResult
                    {
                        Error = new D2Error { Message = "Unknown error from native library" },
                        DiagnosticId = diagnosticId
                    };
                }

                _logger.LogError("Diagram rendering failed: {ErrorMessage} (DiagnosticId: {DiagnosticId})", errorMessage, diagnosticId);
                return new RenderResult
                {
                    Error = ParseError(errorMessage, script),
                    DiagnosticId = diagnosticId
                };
            }

            if (svgPtr == IntPtr.Zero)
            {
                _logger.LogError("RenderDiagramInternal returned null pointer");
                return new RenderResult
                {
                    Error = new D2Error { Message = "Rendering failed with null result" },
                    DiagnosticId = diagnosticId
                };
            }

            var svg = Marshal.PtrToStringUTF8(svgPtr);
            if (svg == null)
            {
                _logger.LogError("Failed to read SVG from native library");
                return new RenderResult
                {
                    Error = new D2Error { Message = "Failed to read SVG output" },
                    DiagnosticId = diagnosticId
                };
            }

            _logger.LogDebug("Rendered diagram successfully (DiagnosticId: {DiagnosticId})", diagnosticId);
            return new RenderResult
            {
                Svg = svg,
                DiagnosticId = diagnosticId,
                FromCache = false
            };
        }
        finally
        {
            if (errorPtr != IntPtr.Zero)
            {
                try { FreeDiagram(errorPtr); }
                catch { /* Ignore cleanup errors */ }
            }
            if (svgPtr != IntPtr.Zero)
            {
                try { FreeDiagram(svgPtr); }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    /// <summary>
    /// Asynchronously renders a D2 diagram script as SVG.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="options">Optional rendering options for customization.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="RenderResult"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="script"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when script exceeds maximum length.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the wrapper has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    public async Task<RenderResult> RenderDiagramAsync(string script, RenderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (script == null)
        {
            throw new ArgumentNullException(nameof(script));
        }

        if (script.Length > MaxScriptLength)
        {
            throw new ArgumentException($"Script exceeds maximum length of {MaxScriptLength} characters", nameof(script));
        }

        // Apply concurrency limit if configured
        if (_concurrencySemaphore != null)
        {
            await _concurrencySemaphore.WaitAsync(cancellationToken);
            try
            {
                return await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return RenderDiagram(script, options);
                }, cancellationToken);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RenderDiagram(script, options);
        }, cancellationToken);
    }

    /// <summary>
    /// Asynchronously renders a D2 diagram script as SVG with a timeout.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="timeout">The maximum time to wait for rendering to complete.</param>
    /// <param name="options">Optional rendering options for customization.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="RenderResult"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="script"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when script exceeds maximum length or timeout is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeout is less than minimum or greater than maximum.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the wrapper has been disposed.</exception>
    /// <exception cref="TimeoutException">Thrown when the rendering operation exceeds the specified timeout.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    public async Task<RenderResult> RenderDiagramAsync(string script, TimeSpan timeout, RenderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (script == null)
        {
            throw new ArgumentNullException(nameof(script));
        }

        if (script.Length > MaxScriptLength)
        {
            throw new ArgumentException($"Script exceeds maximum length of {MaxScriptLength} characters", nameof(script));
        }

        if (timeout < MinTimeout)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), $"Timeout must be at least {MinTimeout.TotalMilliseconds}ms");
        }

        if (timeout > MaxTimeout)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), $"Timeout must not exceed {MaxTimeout.TotalMinutes} minutes");
        }

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await RenderDiagramAsync(script, options, linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Diagram rendering timed out after {Timeout}", timeout);
            throw new TimeoutException($"Diagram rendering timed out after {timeout.TotalSeconds} seconds");
        }
    }

    private static string SerializeOptions(RenderOptions? options)
    {
        if (options == null)
        {
            return "null";
        }

        var jsonOptions = new
        {
            layout = options.Layout?.ToString().ToLowerInvariant(),
            themeId = options.ThemeId,
            darkThemeId = options.DarkThemeId,
            sketch = options.Sketch,
            pad = options.Pad,
            scale = options.Scale,
            center = options.Center,
            target = options.Target,
            animateInterval = options.AnimateInterval,
            forceAppendix = options.ForceAppendix
        };

        return JsonSerializer.Serialize(jsonOptions, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private D2Error ParseError(string errorMessage, string script)
    {
        // Match the format: "Compilation error: line:column: specific error message"
        var match = CompilationErrorRegex().Match(errorMessage);

        if (!match.Success)
        {
            return new D2Error { Message = errorMessage };
        }

        int? lineNumber = null;
        string? lineContent = null;
        int? column = null;
        string message = errorMessage;

        if (int.TryParse(match.Groups[1].Value, out int parsedLineNumber))
        {
            lineNumber = parsedLineNumber;
            lineContent = GetLineContent(script, parsedLineNumber);
        }

        if (int.TryParse(match.Groups[2].Value, out int parsedColumn))
        {
            column = parsedColumn;
        }

        message = match.Groups[3].Value.Trim();

        return new D2Error
        {
            Message = message,
            LineNumber = lineNumber,
            Column = column,
            LineContent = lineContent
        };
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

    private void ThrowIfDisposed()
    {
        if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="D2Wrapper"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="D2Wrapper"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            if (disposing)
            {
                _cache?.Dispose();
                _concurrencySemaphore?.Dispose();
                _logger.LogDebug("D2Wrapper disposed");
            }

            // Dispose unmanaged resources here if needed in the future
        }
    }
}

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

/// <summary>
/// Represents detailed error information from a failed D2 diagram rendering.
/// </summary>
public class D2Error
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; init; } = "";

    /// <summary>
    /// Gets the line number where the error occurred, if available.
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Gets the column number where the error occurred, if available.
    /// </summary>
    public int? Column { get; init; }

    /// <summary>
    /// Gets the content of the line where the error occurred, if available.
    /// </summary>
    public string? LineContent { get; init; }

    /// <summary>
    /// Gets the line content split into parts before, at, and after the error position for highlighting.
    /// </summary>
    /// <returns>A tuple containing the text before the error, the error character, and the text after the error.</returns>
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
