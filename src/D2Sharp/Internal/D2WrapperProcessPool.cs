using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace D2Sharp;

/// <summary>
/// Circuit breaker states for the worker pool.
/// </summary>
internal enum CircuitBreakerState
{
    /// <summary>Normal operation - requests are processed</summary>
    Closed,
    /// <summary>Too many failures - requests fail fast</summary>
    Open,
    /// <summary>Testing recovery - limited requests allowed</summary>
    HalfOpen
}

/// <summary>
/// Pool of process-isolated D2 workers for concurrent rendering with crash recovery.
/// Provides better throughput and faster failure recovery than single-worker approach.
/// Includes circuit breaker pattern and automatic background worker recovery.
/// </summary>
public class D2WrapperProcessPool : ID2Renderer
{
    private readonly ILogger<D2WrapperProcessPool> _logger;
    private readonly string _workerPath;
    private readonly int _poolSize;
    private readonly List<WorkerInstance> _workers;
    private readonly SemaphoreSlim _workerSemaphore;
    private readonly Task _healthMonitorTask;
    private readonly CancellationTokenSource _healthMonitorCts;

    // Circuit breaker state
    private CircuitBreakerState _circuitState = CircuitBreakerState.Closed;
    private DateTime _circuitOpenedAt = DateTime.MinValue;
    private readonly TimeSpan _circuitCooldownPeriod = TimeSpan.FromSeconds(10);
    private readonly object _circuitLock = new();

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the D2WrapperProcessPool class.
    /// Creates a pool of worker processes for concurrent D2 rendering with automatic health monitoring.
    /// </summary>
    /// <param name="poolSize">Number of worker processes to maintain. Default is 10.</param>
    /// <param name="logger">Optional logger for process lifecycle events and errors.</param>
    /// <exception cref="FileNotFoundException">Thrown when worker executable is not found.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when poolSize is less than 1.</exception>
    public D2WrapperProcessPool(int poolSize = 10, ILogger<D2WrapperProcessPool>? logger = null)
    {
        if (poolSize < 1)
            throw new ArgumentOutOfRangeException(nameof(poolSize), "Pool size must be at least 1");

        _logger = logger ?? NullLogger<D2WrapperProcessPool>.Instance;
        _poolSize = poolSize;

        // Find worker executable
        var assemblyPath = typeof(D2WrapperProcessPool).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyPath) ?? Environment.CurrentDirectory;
        _workerPath = Path.Combine(assemblyDir, "D2Sharp.Worker");

        if (!File.Exists(_workerPath) && !File.Exists(_workerPath + ".dll"))
        {
            throw new FileNotFoundException($"Worker executable not found at {_workerPath}");
        }

        _workers = new List<WorkerInstance>();
        _workerSemaphore = new SemaphoreSlim(poolSize, poolSize);
        _healthMonitorCts = new CancellationTokenSource();

        // Initialize worker pool
        for (int i = 0; i < poolSize; i++)
        {
            _workers.Add(new WorkerInstance(i, _workerPath, _logger));
        }

        // Start background health monitor
        _healthMonitorTask = Task.Run(() => HealthMonitorLoop(_healthMonitorCts.Token));

        _logger.LogInformation("Worker pool initialized with {PoolSize} workers and automatic health monitoring", poolSize);
    }

    /// <summary>
    /// Asynchronously renders a D2 diagram script to SVG using an available worker from the pool.
    /// Automatically restarts workers if they crash. Requests queue naturally when all workers are busy.
    /// Circuit breaker provides fail-fast protection when workers are actually crashed.
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
            throw new ObjectDisposedException(nameof(D2WrapperProcessPool));
        }

        // Check circuit breaker state
        lock (_circuitLock)
        {
            if (_circuitState == CircuitBreakerState.Open)
            {
                // Check if cooldown period has elapsed
                if (DateTime.UtcNow - _circuitOpenedAt >= _circuitCooldownPeriod)
                {
                    _circuitState = CircuitBreakerState.HalfOpen;
                    _logger.LogInformation("Circuit breaker entering half-open state, testing recovery");
                }
                else
                {
                    return new RenderResult
                    {
                        Error = new D2Error { Message = "Service temporarily unavailable - all workers are crashed. Please try again in a few seconds." }
                    };
                }
            }
        }

        // Wait for an available worker (requests will queue naturally)
        // No artificial timeout - rely on caller's cancellation token
        await _workerSemaphore.WaitAsync(cancellationToken);

        WorkerInstance? worker = null;
        try
        {
            // Find an available healthy worker
            worker = _workers.FirstOrDefault(w => !w.IsBusy && !w.HasCrashed);

            if (worker == null)
            {
                // All healthy workers busy, return overload error
                // (background health monitor will restart crashed workers)
                return new RenderResult
                {
                    Error = new D2Error { Message = "All healthy workers busy, request rejected (system recovering from failures)" }
                };
            }

            worker.IsBusy = true;

            // Send render request with timeout
            var renderTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            renderTimeoutCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second render timeout

            var result = await worker.RenderAsync(script, options, renderTimeoutCts.Token);

            // On successful render in half-open state, close the circuit
            if (result.IsSuccess)
            {
                lock (_circuitLock)
                {
                    if (_circuitState == CircuitBreakerState.HalfOpen)
                    {
                        _circuitState = CircuitBreakerState.Closed;
                        _logger.LogInformation("Circuit breaker closed - workers have recovered");
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering with worker {WorkerId}", worker?.Id ?? -1);

            if (worker != null)
            {
                worker.MarkCrashed();
            }

            return new RenderResult
            {
                Error = new D2Error { Message = $"Worker error: {ex.Message}" }
            };
        }
        finally
        {
            if (worker != null)
            {
                worker.IsBusy = false;
            }
            _workerSemaphore.Release();
        }
    }

    /// <summary>
    /// Synchronously renders a D2 diagram script to SVG using an available worker from the pool.
    /// </summary>
    /// <param name="script">The D2 diagram script to render.</param>
    /// <param name="options">Optional rendering options (theme, layout engine, etc.).</param>
    /// <returns>The render result containing SVG or error information.</returns>
    public RenderResult RenderDiagram(string script, RenderOptions? options = null)
    {
        return RenderDiagramAsync(script, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Background task that monitors worker health and automatically restarts crashed workers.
    /// </summary>
    private async Task HealthMonitorLoop(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health monitor started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                // Check worker health and restart crashed workers
                var crashedWorkers = _workers.Where(w => w.HasCrashed && !w.IsBusy).ToList();

                foreach (var worker in crashedWorkers)
                {
                    try
                    {
                        _logger.LogWarning("Health monitor restarting crashed worker {WorkerId}", worker.Id);
                        await Task.Run(() => worker.Restart(), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Health monitor failed to restart worker {WorkerId}", worker.Id);
                    }
                }

                // Check circuit breaker state
                var healthyWorkers = _workers.Count(w => !w.HasCrashed);
                var healthPercentage = (double)healthyWorkers / _poolSize;

                lock (_circuitLock)
                {
                    if (_circuitState == CircuitBreakerState.Closed && healthPercentage < 0.2)
                    {
                        // Less than 20% healthy - open circuit
                        _circuitState = CircuitBreakerState.Open;
                        _circuitOpenedAt = DateTime.UtcNow;
                        _logger.LogError("Circuit breaker opened - only {HealthyCount}/{TotalCount} workers healthy", healthyWorkers, _poolSize);
                    }
                    else if (_circuitState == CircuitBreakerState.HalfOpen && healthPercentage > 0.5)
                    {
                        // More than 50% healthy - close circuit
                        _circuitState = CircuitBreakerState.Closed;
                        _logger.LogInformation("Circuit breaker closed - {HealthyCount}/{TotalCount} workers healthy", healthyWorkers, _poolSize);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health monitor loop");
            }
        }

        _logger.LogInformation("Health monitor stopped");
    }

    /// <summary>
    /// Disposes the process pool and terminates all worker processes.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Stop health monitor
        _healthMonitorCts.Cancel();
        try
        {
            _healthMonitorTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch { /* Ignore */ }

        foreach (var worker in _workers)
        {
            worker.Dispose();
        }

        _workerSemaphore.Dispose();
        _healthMonitorCts.Dispose();

        _logger.LogInformation("Worker pool disposed");
    }

    private class WorkerInstance : IDisposable
    {
        public int Id { get; }
        public bool IsBusy { get; set; }
        public bool HasCrashed { get; private set; }

        private readonly string _workerPath;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _processLock = new(1, 1);
        private Process? _process;
        private StreamWriter? _stdin;
        private StreamReader? _stdout;
        private StreamReader? _stderr;
        private int _restartCount;
        private int _startupFailures;
        private int _renderFailures;
        private string? _lastError;
        private bool _disposed;

        public WorkerInstance(int id, string workerPath, ILogger logger)
        {
            Id = id;
            _workerPath = workerPath;
            _logger = logger;
            Start();
        }

        public void MarkCrashed()
        {
            HasCrashed = true;
        }

        public void Restart()
        {
            _processLock.Wait();
            try
            {
                Stop();
                Start();
                HasCrashed = false;
            }
            finally
            {
                _processLock.Release();
            }
        }

        private void Start()
        {
            try
            {
                _restartCount++;
                _logger.LogInformation("Starting worker {WorkerId} (attempt {RestartCount}, startup failures: {StartupFailures}, render failures: {RenderFailures})",
                    Id, _restartCount, _startupFailures, _renderFailures);

                // Platform-specific worker startup
                var workerDir = Path.GetDirectoryName(_workerPath + ".dll") ?? Environment.CurrentDirectory;

                // Log diagnostic information
                _logger.LogDebug("Worker {WorkerId} directory: {WorkerDir}", Id, workerDir);
                if (Directory.Exists(workerDir))
                {
                    var soFiles = Directory.GetFiles(workerDir, "*.so");
                    _logger.LogDebug("Worker {WorkerId} found {Count} .so files in directory: {Files}",
                        Id, soFiles.Length, string.Join(", ", soFiles.Select(Path.GetFileName)));
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
                    _logger.LogInformation("Worker {WorkerId} starting in: {WorkDir}, LD_LIBRARY_PATH: {LdPath}",
                        Id, workerDir, newLdPath);
                }

                _process = Process.Start(startInfo);
                if (_process == null)
                {
                    _startupFailures++;
                    throw new InvalidOperationException($"Failed to start worker {Id}: Process.Start returned null");
                }

                _stdin = _process.StandardInput;
                _stdout = _process.StandardOutput;
                _stderr = _process.StandardError;

                // Start background stderr reader
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (_stderr != null && !_process.HasExited)
                        {
                            var line = await _stderr.ReadLineAsync();
                            if (!string.IsNullOrEmpty(line))
                            {
                                _logger.LogWarning("Worker {WorkerId} stderr: {StderrLine}", Id, line);
                                _lastError = line;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Worker {WorkerId} stderr reader terminated", Id);
                    }
                });

                _logger.LogInformation("Worker {WorkerId} started with PID {ProcessId} on {Platform}",
                    Id, _process.Id, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Unix");
            }
            catch (Exception ex)
            {
                _startupFailures++;
                _logger.LogError(ex, "Failed to start worker {WorkerId} (total startup failures: {StartupFailures})", Id, _startupFailures);
                HasCrashed = true;
                throw;
            }
        }

        private void Stop()
        {
            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                        _process.WaitForExit(1000);
                    }

                    var exitCode = _process.HasExited ? _process.ExitCode : -1;
                    if (exitCode != 0)
                    {
                        _logger.LogWarning("Worker {WorkerId} terminated with exit code {ExitCode}", Id, exitCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error stopping worker {WorkerId}", Id);
                }

                _process.Dispose();
                _process = null;
            }

            _stdin = null;
            _stdout = null;
            _stderr = null;
        }

        public async Task<RenderResult> RenderAsync(string script, RenderOptions? options, CancellationToken cancellationToken)
        {
            await _processLock.WaitAsync(cancellationToken);
            try
            {
                // Check if process is alive
                if (_process == null || _process.HasExited)
                {
                    var exitCode = _process?.ExitCode ?? -1;
                    _logger.LogWarning("Worker {WorkerId} died with exit code {ExitCode}, restarting...", Id, exitCode);
                    Stop();
                    Start();
                }

                // Send request
                var request = new { Script = script, Options = options };
                var requestJson = JsonSerializer.Serialize(request);

                await _stdin!.WriteLineAsync(requestJson);
                await _stdin.FlushAsync();

                // Read response with proper timeout and cancellation
                // Use 45-second timeout (30s render + 15s buffer)
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(45));

                string? responseJson = null;
                bool timedOut = false;

                try
                {
                    // Use cancellation token so the read operation is actually cancelled on timeout
                    responseJson = await _stdout!.ReadLineAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    // Timeout occurred (not user cancellation)
                    timedOut = true;
                }

                // Handle timeout
                if (timedOut)
                {
                    _renderFailures++;
                    _logger.LogError("Worker {WorkerId} render timed out after 45 seconds (process alive: {ProcessAlive}, render failures: {RenderFailures})",
                        Id, _process?.HasExited == false, _renderFailures);

                    HasCrashed = true;

                    var errorMsg = $"Render operation timed out after 45 seconds (worker {Id})";
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
                    var processAlive = _process?.HasExited == false;
                    var exitCode = _process?.HasExited == true ? _process.ExitCode : (int?)null;

                    _logger.LogError("Worker {WorkerId} returned empty response (process alive: {ProcessAlive}, exit code: {ExitCode}, render failures: {RenderFailures})",
                        Id, processAlive, exitCode, _renderFailures);

                    HasCrashed = true;

                    var errorMsg = $"Worker process {Id} failed to respond";
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
                    _logger.LogError("Worker {WorkerId} returned invalid JSON: {ResponseJson}", Id, responseJson);
                    return new RenderResult
                    {
                        Error = new D2Error { Message = $"Worker {Id} returned invalid response format" }
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
                    _logger.LogInformation("Worker {WorkerId} successfully rendered after {RenderFailures} failures", Id, _renderFailures);
                    _renderFailures = 0;
                }

                return new RenderResult { Svg = response.Svg };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // User-requested cancellation, not a worker failure
                _logger.LogInformation("Worker {WorkerId} render cancelled by user", Id);
                throw;
            }
            catch (Exception ex)
            {
                _renderFailures++;
                _logger.LogError(ex, "Error communicating with worker {WorkerId} (render failures: {RenderFailures})", Id, _renderFailures);
                HasCrashed = true;
                throw;
            }
            finally
            {
                _processLock.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
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
}
