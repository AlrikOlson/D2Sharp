using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace D2Sharp;

/// <summary>
/// Process-isolated wrapper for D2 rendering that auto-restarts on crashes.
/// Uses a separate worker process to isolate native code failures.
/// </summary>
public class D2WrapperProcess : IDisposable
{
    private readonly ILogger<D2WrapperProcess> _logger;
    private readonly string _workerPath;
    private readonly SemaphoreSlim _processLock = new(1, 1);
    private Process? _workerProcess;
    private StreamWriter? _workerStdin;
    private StreamReader? _workerStdout;
    private int _restartCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the D2WrapperProcess class.
    /// Starts a worker process for isolated D2 rendering.
    /// </summary>
    /// <param name="logger">Optional logger for process lifecycle events and errors.</param>
    /// <exception cref="FileNotFoundException">Thrown when worker executable is not found.</exception>
    public D2WrapperProcess(ILogger<D2WrapperProcess>? logger = null)
    {
        _logger = logger ?? NullLogger<D2WrapperProcess>.Instance;

        // Find worker executable
        var assemblyPath = typeof(D2WrapperProcess).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyPath) ?? Environment.CurrentDirectory;
        _workerPath = Path.Combine(assemblyDir, "D2Sharp.Worker");

        if (!File.Exists(_workerPath) && !File.Exists(_workerPath + ".dll"))
        {
            throw new FileNotFoundException($"Worker executable not found at {_workerPath}");
        }

        StartWorker();
    }

    private void StartWorker()
    {
        _processLock.Wait();
        try
        {
            // Clean up old process if it exists
            if (_workerProcess != null)
            {
                try
                {
                    if (!_workerProcess.HasExited)
                    {
                        _workerProcess.Kill();
                    }
                }
                catch { /* Ignore */ }

                _workerProcess.Dispose();
                _workerProcess = null;
            }

            _restartCount++;
            _logger.LogInformation("Starting worker process (attempt {RestartCount})", _restartCount);

            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"ulimit -s unlimited && exec dotnet '{_workerPath}.dll'\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _workerProcess = Process.Start(startInfo);
            if (_workerProcess == null)
            {
                throw new InvalidOperationException("Failed to start worker process");
            }

            _workerStdin = _workerProcess.StandardInput;
            _workerStdout = _workerProcess.StandardOutput;

            _logger.LogInformation("Worker process started with PID {ProcessId}", _workerProcess.Id);
        }
        finally
        {
            _processLock.Release();
        }
    }

    /// <summary>
    /// Asynchronously renders a D2 diagram script to SVG using the worker process.
    /// Automatically restarts the worker if it has crashed.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="options">Optional rendering options (theme, layout engine, etc.).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the render result containing SVG or error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when script is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public async Task<RenderResult> RenderDiagramAsync(string script, RenderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(D2WrapperProcess));
        }

        // Add timeout to prevent hanging on semaphore wait
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(35)); // Slightly longer than default render timeout

        try
        {
            await _processLock.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout waiting for lock - worker is probably stuck or crashed
            return new RenderResult
            {
                Error = new D2Error { Message = "Timeout waiting for worker process (system may be overloaded)" }
            };
        }

        try
        {
            // Check if worker is alive
            if (_workerProcess == null || _workerProcess.HasExited)
            {
                _logger.LogWarning("Worker process died, restarting...");
                StartWorker();
            }

            // Send request with timeout
            var request = new { Script = script, Options = options };
            var requestJson = JsonSerializer.Serialize(request);

            await _workerStdin!.WriteLineAsync(requestJson);
            await _workerStdin.FlushAsync();

            // Read response with timeout (5 seconds for response to start arriving)
            var readTask = _workerStdout!.ReadLineAsync();
            var completedTask = await Task.WhenAny(readTask, Task.Delay(5000, timeoutCts.Token));

            string? responseJson = null;
            if (completedTask == readTask)
            {
                responseJson = await readTask;
            }

            if (string.IsNullOrEmpty(responseJson))
            {
                _logger.LogError("Worker process returned empty response or timed out, assuming crash");
                StartWorker();
                return new RenderResult
                {
                    Error = new D2Error { Message = "Worker process crashed or timed out" }
                };
            }

            var response = JsonSerializer.Deserialize<WorkerResponse>(responseJson);
            if (response == null)
            {
                return new RenderResult
                {
                    Error = new D2Error { Message = "Invalid response from worker" }
                };
            }

            if (response.Error != null)
            {
                return new RenderResult
                {
                    Error = new D2Error
                    {
                        Message = response.Error,
                        LineNumber = response.LineNumber,
                        Column = response.Column,
                        LineContent = response.LineContent
                    }
                };
            }

            return new RenderResult { Svg = response.Svg };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error communicating with worker process");
            StartWorker();
            throw;
        }
        finally
        {
            _processLock.Release();
        }
    }

    /// <summary>
    /// Synchronously renders a D2 diagram script to SVG using the worker process.
    /// Automatically restarts the worker if it has crashed.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="options">Optional rendering options (theme, layout engine, etc.).</param>
    /// <returns>The render result containing SVG or error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when script is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    public RenderResult RenderDiagram(string script, RenderOptions? options = null)
    {
        return RenderDiagramAsync(script, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Disposes the D2WrapperProcess and terminates the worker process.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_workerProcess != null)
        {
            try
            {
                if (!_workerProcess.HasExited)
                {
                    _workerProcess.Kill();
                    _workerProcess.WaitForExit(1000);
                }
            }
            catch { /* Ignore */ }

            _workerProcess.Dispose();
        }

        _processLock.Dispose();
    }

    private record WorkerResponse
    {
        public string? Svg { get; init; }
        public string? Error { get; init; }
        public int? LineNumber { get; init; }
        public int? Column { get; init; }
        public string? LineContent { get; init; }
    }
}
