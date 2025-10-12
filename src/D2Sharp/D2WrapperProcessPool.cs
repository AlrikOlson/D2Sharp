using System.Collections.Concurrent;
using System.Diagnostics;
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
        private int _restartCount;
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
                _logger.LogInformation("Starting worker {WorkerId} (attempt {RestartCount})", Id, _restartCount);

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

                _process = Process.Start(startInfo);
                if (_process == null)
                {
                    throw new InvalidOperationException($"Failed to start worker {Id}");
                }

                _stdin = _process.StandardInput;
                _stdout = _process.StandardOutput;

                _logger.LogInformation("Worker {WorkerId} started with PID {ProcessId}", Id, _process.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start worker {WorkerId}", Id);
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
                }
                catch { /* Ignore */ }

                _process.Dispose();
                _process = null;
            }

            _stdin = null;
            _stdout = null;
        }

        public async Task<RenderResult> RenderAsync(string script, RenderOptions? options, CancellationToken cancellationToken)
        {
            await _processLock.WaitAsync(cancellationToken);
            try
            {
                // Check if process is alive
                if (_process == null || _process.HasExited)
                {
                    _logger.LogWarning("Worker {WorkerId} died, restarting...", Id);
                    Stop();
                    Start();
                }

                // Send request
                var request = new { Script = script, Options = options };
                var requestJson = JsonSerializer.Serialize(request);

                await _stdin!.WriteLineAsync(requestJson);
                await _stdin.FlushAsync();

                // Read response with timeout (matches render timeout)
                var readTask = _stdout!.ReadLineAsync();
                var timeoutTask = Task.Delay(35000, cancellationToken); // 35 seconds (slightly more than 30s render timeout)
                var completedTask = await Task.WhenAny(readTask, timeoutTask);

                string? responseJson = null;
                if (completedTask == readTask)
                {
                    responseJson = await readTask;
                }

                if (string.IsNullOrEmpty(responseJson))
                {
                    _logger.LogError("Worker {WorkerId} returned empty response or timed out", Id);
                    HasCrashed = true;
                    return new RenderResult
                    {
                        Error = new D2Error { Message = "Worker crashed or timed out" }
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
                _logger.LogError(ex, "Error communicating with worker {WorkerId}", Id);
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
