using System.Diagnostics.Tracing;

namespace D2Sharp.Internal.Telemetry;

/// <summary>
/// Provides real-time performance metrics for D2Sharp operations using EventCounters.
/// </summary>
[EventSource(Name = "D2Sharp")]
public sealed class D2SharpEventCounters : EventSource
{
    /// <summary>
    /// The singleton instance of the event source.
    /// </summary>
    public static readonly D2SharpEventCounters Instance = new();

    private IncrementingPollingCounter? _rendersCounter;
    private PollingCounter? _activeRendersCounter;
    private EventCounter? _renderDurationCounter;
    private PollingCounter? _cacheHitRateCounter;
    private PollingCounter? _errorRateCounter;

    private long _totalRenders;
    private long _activeRenders;
    private long _totalCacheHits;
    private long _totalCacheRequests;
    private long _totalErrors;

    private D2SharpEventCounters()
    {
    }

    /// <summary>
    /// Records the start of a render operation.
    /// </summary>
    public void RenderStarted()
    {
        Interlocked.Increment(ref _activeRenders);
        Interlocked.Increment(ref _totalRenders);
    }

    /// <summary>
    /// Records the completion of a render operation.
    /// </summary>
    /// <param name="durationMs">The duration of the render operation in milliseconds.</param>
    /// <param name="isSuccess">Whether the render was successful.</param>
    public void RenderCompleted(double durationMs, bool isSuccess)
    {
        Interlocked.Decrement(ref _activeRenders);
        _renderDurationCounter?.WriteMetric(durationMs);

        if (!isSuccess)
        {
            Interlocked.Increment(ref _totalErrors);
        }
    }

    /// <summary>
    /// Records a cache operation.
    /// </summary>
    /// <param name="isHit">Whether the cache lookup was a hit.</param>
    public void RecordCacheAccess(bool isHit)
    {
        Interlocked.Increment(ref _totalCacheRequests);
        if (isHit)
        {
            Interlocked.Increment(ref _totalCacheHits);
        }
    }

    /// <inheritdoc />
    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        if (command.Command == EventCommand.Enable)
        {
            _rendersCounter ??= new IncrementingPollingCounter("renders-total", this, () => _totalRenders)
            {
                DisplayName = "Total Renders",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };

            _activeRendersCounter ??= new PollingCounter("renders-active", this, () => _activeRenders)
            {
                DisplayName = "Active Renders"
            };

            _renderDurationCounter ??= new EventCounter("render-duration-ms", this)
            {
                DisplayName = "Render Duration (ms)",
                DisplayUnits = "ms"
            };

            _cacheHitRateCounter ??= new PollingCounter("cache-hit-rate", this, () =>
            {
                var requests = Interlocked.Read(ref _totalCacheRequests);
                if (requests == 0)
                {
                    return 0;
                }

                var hits = Interlocked.Read(ref _totalCacheHits);
                return (double)hits / requests * 100;
            })
            {
                DisplayName = "Cache Hit Rate",
                DisplayUnits = "%"
            };

            _errorRateCounter ??= new PollingCounter("error-rate", this, () =>
            {
                var total = Interlocked.Read(ref _totalRenders);
                if (total == 0)
                {
                    return 0;
                }

                var errors = Interlocked.Read(ref _totalErrors);
                return (double)errors / total * 100;
            })
            {
                DisplayName = "Error Rate",
                DisplayUnits = "%"
            };
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _rendersCounter?.Dispose();
        _activeRendersCounter?.Dispose();
        _renderDurationCounter?.Dispose();
        _cacheHitRateCounter?.Dispose();
        _errorRateCounter?.Dispose();
        base.Dispose(disposing);
    }
}
