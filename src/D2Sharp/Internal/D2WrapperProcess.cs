using System.Diagnostics;
using System.Runtime.InteropServices;
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
    private StreamReader? _workerStderr;
    private int _restartCount;
    private int _startupFailures;
    private int _renderFailures;
    private string? _lastError;
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

                    var exitCode = _workerProcess.HasExited ? _workerProcess.ExitCode : -1;
                    if (exitCode != 0)
                    {
                        _logger.LogWarning("Previous worker process terminated with exit code {ExitCode}", exitCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error cleaning up old worker process");
                }

                _workerProcess.Dispose();
                _workerProcess = null;
            }

            _restartCount++;
            _logger.LogInformation("Starting worker process (attempt {RestartCount}, startup failures: {StartupFailures}, render failures: {RenderFailures})",
                _restartCount, _startupFailures, _renderFailures);

            // Platform-specific worker startup
            var workerDir = Path.GetDirectoryName(_workerPath + ".dll") ?? Environment.CurrentDirectory;

            // Log diagnostic information
            _logger.LogDebug("Worker directory: {WorkerDir}", workerDir);
            if (Directory.Exists(workerDir))
            {
                var soFiles = Directory.GetFiles(workerDir, "*.so");
                _logger.LogDebug("Worker found {Count} .so files in directory: {Files}",
                    soFiles.Length, string.Join(", ", soFiles.Select(Path.GetFileName)));
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{_workerPath}.dll\"",
                WorkingDirectory = workerDir,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // On Linux/macOS, prepend worker directory to LD_LIBRARY_PATH
            // Note: When UseShellExecute=false, child process inherits parent's environment by default
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var existingLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
                var newLdPath = string.IsNullOrEmpty(existingLdPath)
                    ? workerDir
                    : $"{workerDir}:{existingLdPath}";

                // Set DOTNET_ROOT to help with .NET runtime library resolution
                var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
                if (!string.IsNullOrEmpty(dotnetRoot))
                {
                    startInfo.Environment["DOTNET_ROOT"] = dotnetRoot;
                }

                startInfo.Environment["LD_LIBRARY_PATH"] = newLdPath;
                _logger.LogInformation("Worker starting in: {WorkDir}, LD_LIBRARY_PATH: {LdPath}",
                    workerDir, newLdPath);
            }

            _workerProcess = Process.Start(startInfo);
            if (_workerProcess == null)
            {
                _startupFailures++;
                throw new InvalidOperationException("Failed to start worker process: Process.Start returned null");
            }

            _workerStdin = _workerProcess.StandardInput;
            _workerStdout = _workerProcess.StandardOutput;
            _workerStderr = _workerProcess.StandardError;

            // Start background stderr reader
            _ = Task.Run(async () =>
            {
                try
                {
                    while (_workerStderr != null && !_workerProcess.HasExited)
                    {
                        var line = await _workerStderr.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            _logger.LogWarning("Worker process stderr: {StderrLine}", line);
                            _lastError = line;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Worker stderr reader terminated");
                }
            });

            _logger.LogInformation("Worker process started with PID {ProcessId} on {Platform}",
                _workerProcess.Id, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Unix");
        }
        catch (Exception ex)
        {
            _startupFailures++;
            _logger.LogError(ex, "Failed to start worker process (total startup failures: {StartupFailures})", _startupFailures);
            throw;
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

        // Add timeout to prevent hanging on semaphore wait (45 seconds total)
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(50)); // Extra buffer for lock acquisition

        try
        {
            await _processLock.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout waiting for lock - worker is probably stuck or crashed
            _logger.LogError("Timeout waiting for worker process lock after 50 seconds");
            return new RenderResult
            {
                Error = new D2Error { Message = "Timeout waiting for worker process (system may be overloaded or worker is stuck)" }
            };
        }

        try
        {
            // Check if worker is alive
            if (_workerProcess == null || _workerProcess.HasExited)
            {
                var exitCode = _workerProcess?.ExitCode ?? -1;
                _logger.LogWarning("Worker process died with exit code {ExitCode}, restarting...", exitCode);
                StartWorker();
            }

            // Send request with timeout
            var request = new { Script = script, Options = options };
            var requestJson = JsonSerializer.Serialize(request);

            await _workerStdin!.WriteLineAsync(requestJson);
            await _workerStdin.FlushAsync();

            // Read response with proper timeout and cancellation (45-second timeout)
            using var renderTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            renderTimeoutCts.CancelAfter(TimeSpan.FromSeconds(45));

            string? responseJson = null;
            bool timedOut = false;

            try
            {
                // Use cancellation token so the read operation is actually cancelled on timeout
                responseJson = await _workerStdout!.ReadLineAsync(renderTimeoutCts.Token);
            }
            catch (OperationCanceledException) when (renderTimeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred (not user cancellation)
                timedOut = true;
            }

            // Handle timeout
            if (timedOut)
            {
                _renderFailures++;
                _logger.LogError("Worker process render timed out after 45 seconds (process alive: {ProcessAlive}, render failures: {RenderFailures})",
                    _workerProcess?.HasExited == false, _renderFailures);

                // Restart worker on timeout
                StartWorker();

                var errorMsg = "Render operation timed out after 45 seconds";
                if (!string.IsNullOrEmpty(_lastError))
                {
                    errorMsg += $". Last worker error: {_lastError}";
                }

                return new RenderResult
                {
                    Error = new D2Error { Message = errorMsg }
                };
            }

            // Handle empty response (worker crashed/died)
            if (string.IsNullOrEmpty(responseJson))
            {
                _renderFailures++;
                var processAlive = _workerProcess?.HasExited == false;
                var exitCode = _workerProcess?.HasExited == true ? _workerProcess.ExitCode : (int?)null;

                _logger.LogError("Worker process returned empty response (process alive: {ProcessAlive}, exit code: {ExitCode}, render failures: {RenderFailures})",
                    processAlive, exitCode, _renderFailures);

                // Restart worker on crash
                StartWorker();

                var errorMsg = "Worker process failed to respond";
                if (exitCode.HasValue)
                {
                    errorMsg += $" (exited with code {exitCode})";
                }
                if (!string.IsNullOrEmpty(_lastError))
                {
                    errorMsg += $". Last error: {_lastError}";
                }

                return new RenderResult
                {
                    Error = new D2Error { Message = errorMsg }
                };
            }

            var response = JsonSerializer.Deserialize<WorkerResponse>(responseJson);
            if (response == null)
            {
                _renderFailures++;
                _logger.LogError("Worker process returned invalid JSON: {ResponseJson}", responseJson);
                return new RenderResult
                {
                    Error = new D2Error { Message = "Worker returned invalid response format" }
                };
            }

            if (response.Error != null)
            {
                // This is a D2 compilation error, not a worker failure
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

            // Success - reset failure counter
            if (_renderFailures > 0)
            {
                _logger.LogInformation("Worker process successfully rendered after {RenderFailures} failures", _renderFailures);
                _renderFailures = 0;
            }

            return new RenderResult { Svg = response.Svg };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // User-requested cancellation, not a worker failure
            _logger.LogInformation("Worker process render cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _renderFailures++;
            _logger.LogError(ex, "Error communicating with worker process (render failures: {RenderFailures})", _renderFailures);
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
