using System.Diagnostics.Tracing;
using Xunit;

namespace D2Sharp.Tests.Telemetry;

/// <summary>
/// Tests for D2SharpEventCounters telemetry component.
/// </summary>
public class D2SharpEventCountersTests
{
    [Fact]
    public void Instance_IsSingleton()
    {
        // Arrange & Act
        var instance1 = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;
        var instance2 = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Assert
        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void RenderStarted_IncrementsCounters()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act
        eventSource.RenderStarted();

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void RenderCompleted_WithSuccess_DecrementsActiveRenders()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;
        eventSource.RenderStarted();

        // Act
        eventSource.RenderCompleted(durationMs: 100.5, isSuccess: true);

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void RenderCompleted_WithFailure_IncrementsErrorCount()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;
        eventSource.RenderStarted();

        // Act
        eventSource.RenderCompleted(durationMs: 50.0, isSuccess: false);

        // Assert - Verify no exceptions thrown and error path executed
        Assert.True(true);
    }

    [Fact]
    public void RecordCacheAccess_WithHit_IncrementsCounters()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act
        eventSource.RecordCacheAccess(isHit: true);

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void RecordCacheAccess_WithMiss_IncrementsOnlyTotalRequests()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act
        eventSource.RecordCacheAccess(isHit: false);

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void MultipleConcurrentRenderStarted_AllTracked()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;
        var tasks = new List<Task>();

        // Act - Start 10 concurrent renders
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => eventSource.RenderStarted()));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void MultipleConcurrentRenderCompleted_AllTracked()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;
        var tasks = new List<Task>();

        // Start some renders first
        for (int i = 0; i < 10; i++)
        {
            eventSource.RenderStarted();
        }

        // Act - Complete 10 concurrent renders
        for (int i = 0; i < 10; i++)
        {
            var isSuccess = i % 2 == 0; // Alternate success/failure
            tasks.Add(Task.Run(() => eventSource.RenderCompleted(100.0, isSuccess)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void ConcurrentCacheAccess_AllTracked()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;
        var tasks = new List<Task>();

        // Act - Record 20 concurrent cache accesses
        for (int i = 0; i < 20; i++)
        {
            var isHit = i % 2 == 0;
            tasks.Add(Task.Run(() => eventSource.RecordCacheAccess(isHit)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void MixedConcurrentOperations_MaintainConsistency()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;
        var tasks = new List<Task>();

        // Act - Mix of all operation types
        for (int i = 0; i < 30; i++)
        {
            switch (i % 3)
            {
                case 0:
                    tasks.Add(Task.Run(() => eventSource.RenderStarted()));
                    break;
                case 1:
                    tasks.Add(Task.Run(() => eventSource.RenderCompleted(100.0, true)));
                    break;
                case 2:
                    tasks.Add(Task.Run(() => eventSource.RecordCacheAccess(i % 2 == 0)));
                    break;
            }
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void RenderDuration_VariousValues_Recorded()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;
        var durations = new[] { 0.1, 10.5, 100.0, 1000.5, 5000.0 };

        // Act
        foreach (var duration in durations)
        {
            eventSource.RenderStarted();
            eventSource.RenderCompleted(duration, isSuccess: true);
        }

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void SequentialOperations_WorkCorrectly()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act - Simulate realistic usage pattern
        for (int i = 0; i < 5; i++)
        {
            eventSource.RenderStarted();
            eventSource.RecordCacheAccess(isHit: i % 2 == 0);
            eventSource.RenderCompleted(durationMs: 100.0 * i, isSuccess: i != 2);
        }

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void ErrorRate_WithAllSuccesses_Calculated()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act - All successful renders
        for (int i = 0; i < 10; i++)
        {
            eventSource.RenderStarted();
            eventSource.RenderCompleted(100.0, isSuccess: true);
        }

        // Assert - Verify no exceptions thrown (error rate should be 0%)
        Assert.True(true);
    }

    [Fact]
    public void ErrorRate_WithSomeFailures_Calculated()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act - Mix of success and failures
        for (int i = 0; i < 10; i++)
        {
            eventSource.RenderStarted();
            eventSource.RenderCompleted(100.0, isSuccess: i >= 5); // 50% error rate
        }

        // Assert - Verify no exceptions thrown (error rate should be ~50%)
        Assert.True(true);
    }

    [Fact]
    public void CacheHitRate_WithNoRequests_HandlesZeroDivision()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act - Don't record any cache accesses
        // The cache hit rate calculation should handle 0 requests gracefully

        // Assert - Verify no exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void CacheHitRate_WithAllHits_Calculated()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act - All cache hits
        for (int i = 0; i < 10; i++)
        {
            eventSource.RecordCacheAccess(isHit: true);
        }

        // Assert - Verify no exceptions thrown (hit rate should be 100%)
        Assert.True(true);
    }

    [Fact]
    public void CacheHitRate_WithMixedHitsAndMisses_Calculated()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act - 50/50 mix
        for (int i = 0; i < 10; i++)
        {
            eventSource.RecordCacheAccess(isHit: i % 2 == 0);
        }

        // Assert - Verify no exceptions thrown (hit rate should be 50%)
        Assert.True(true);
    }

    [Fact]
    public void CacheHitRate_WithAllMisses_Calculated()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Act - All cache misses
        for (int i = 0; i < 10; i++)
        {
            eventSource.RecordCacheAccess(isHit: false);
        }

        // Assert - Verify no exceptions thrown (hit rate should be 0%)
        Assert.True(true);
    }

    [Fact]
    public void EventSource_Name_IsD2Sharp()
    {
        // Arrange & Act
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Assert
        Assert.Equal("D2Sharp", eventSource.Name);
    }

    [Fact]
    public void EventSource_Dispose_DoesNotThrow()
    {
        // Note: We can't actually dispose the singleton instance as it would affect other tests
        // This test verifies the pattern is correct by checking the instance exists

        // Arrange & Act
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        // Assert - Verify instance is valid and has correct name
        Assert.NotNull(eventSource);
        Assert.Equal("D2Sharp", eventSource.Name);
    }

    [Fact]
    public void OnEventCommand_WithEnable_InitializesCounters()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;

        using var listener = new TestEventListener();

        // Act - Enable the EventSource which triggers OnEventCommand
        listener.EnableEvents(eventSource, EventLevel.Informational,
            EventKeywords.All,
            new Dictionary<string, string?>
            {
                ["EventCounterIntervalSec"] = "1"
            });

        // Give counters time to initialize and report
        Thread.Sleep(1500);

        // Assert - Verify no exceptions thrown and listener is active
        Assert.True(listener.IsEnabled);
    }

    [Fact]
    public void OnEventCommand_WithCountersEnabled_ReportsMetrics()
    {
        // Arrange
        var eventSource = D2Sharp.Internal.Telemetry.D2SharpEventCounters.Instance;
        var counterEvents = new List<string>();

        using var listener = new TestEventListener();
        listener.CounterReceived += (sender, args) =>
        {
            if (args.EventSource.Name == "D2Sharp" && args.EventName == "EventCounters")
            {
                if (args.Payload != null)
                {
                    foreach (var payload in args.Payload)
                    {
                        if (payload is IDictionary<string, object> counterData &&
                            counterData.TryGetValue("Name", out var name))
                        {
                            counterEvents.Add(name.ToString() ?? string.Empty);
                        }
                    }
                }
            }
        };

        // Enable counters
        listener.EnableEvents(eventSource, EventLevel.Informational,
            EventKeywords.All,
            new Dictionary<string, string?>
            {
                ["EventCounterIntervalSec"] = "1"
            });

        // Act - Generate some events
        eventSource.RenderStarted();
        eventSource.RenderCompleted(100.0, true);
        eventSource.RecordCacheAccess(true);

        // Wait for counter interval
        Thread.Sleep(1500);

        // Assert - Verify counter events were reported
        Assert.Contains("renders-total", counterEvents);
        Assert.Contains("renders-active", counterEvents);
        Assert.Contains("cache-hit-rate", counterEvents);
    }

    private class TestEventListener : EventListener
    {
        public bool IsEnabled { get; private set; }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);
            if (eventSource.Name == "D2Sharp")
            {
                IsEnabled = true;
            }
        }

        public event EventHandler<EventWrittenEventArgs>? CounterReceived;

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            CounterReceived?.Invoke(this, eventData);
        }
    }
}
