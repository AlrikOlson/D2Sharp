# D2Sharp

A .NET wrapper for [D2](https://d2lang.com/), the modern diagram scripting language that turns text to diagrams.

[![NuGet](https://img.shields.io/nuget/v/D2Sharp.svg)](https://www.nuget.org/packages/D2Sharp/)
[![codecov](https://codecov.io/gh/AlrikOlson/D2Sharp/branch/main/graph/badge.svg)](https://codecov.io/gh/AlrikOlson/D2Sharp)
[![License](https://img.shields.io/github/license/AlrikOlson/D2Sharp)](LICENSE.txt)

## Features

- **Full D2 Support** - Render D2 diagrams
- **Async/Await** - Task-based async rendering with cancellation and timeout support
- **Thread-Safe** - Thread-safe with memory leak protection and error handling
- **Flexible Rendering** - Layout engines (Dagre/ELK), themes, sketch mode
- **Cross-Platform** - Works on Windows, macOS, and Linux
- **Type-Safe** - Documented API with XML docs and nullable reference types
- **Observability** - Built-in telemetry, metrics, and caching (v0.3.0+)

## Installation

```bash
dotnet add package D2Sharp
```

## Quick Start

### Basic Rendering

```csharp
using D2Sharp;

var wrapper = new D2Wrapper();
var result = wrapper.RenderDiagram("A -> B -> C");

if (result.IsSuccess)
{
    Console.WriteLine(result.Svg);
    // Save to file
    File.WriteAllText("diagram.svg", result.Svg);
}
else
{
    Console.WriteLine($"Error: {result.Error?.Message}");
}
```

### Async Rendering

```csharp
using D2Sharp;

var wrapper = new D2Wrapper();

// With cancellation token
var cts = new CancellationTokenSource();
var result = await wrapper.RenderDiagramAsync("x -> y", cancellationToken: cts.Token);

// With timeout (30 seconds)
var result = await wrapper.RenderDiagramAsync(
    "A -> B",
    timeout: TimeSpan.FromSeconds(30)
);
```

## Rendering Options

Customize rendering with `RenderOptions`:

### Themes

300+ themes available:

```csharp
var options = new RenderOptions
{
    ThemeId = 1  // Cool classics theme
};

var result = wrapper.RenderDiagram("server -> database", options);
```

[View all D2 themes →](https://github.com/terrastruct/d2/tree/master/d2themes)

### Layout Engines

```csharp
var options = new RenderOptions
{
    Layout = LayoutEngine.Elk  // or LayoutEngine.Dagre (default)
};

var result = wrapper.RenderDiagram(@"
    A -> B
    B -> C
    C -> D
", options);
```

- **Dagre** - Faster, simpler layouts (default)
- **ELK** - More complex layouts with additional features

### Sketch Mode

Create hand-drawn style diagrams:

```csharp
var options = new RenderOptions
{
    Sketch = true
};

var result = wrapper.RenderDiagram("idea -> prototype -> product", options);
```

### Visual Customization

```csharp
var options = new RenderOptions
{
    Pad = 50,        // Padding around diagram (default: 100)
    Scale = 0.5,     // Scale factor (0.5 = half size)
    Center = true    // Center in viewbox
};

var result = wrapper.RenderDiagram("start -> end", options);
```

### Combined Options

```csharp
var options = new RenderOptions
{
    Layout = LayoutEngine.Elk,
    ThemeId = 1,
    Sketch = true,
    Pad = 75,
    Center = true
};

var result = wrapper.RenderDiagram(@"
    server: Web Server {
        shape: rectangle
    }
    db: Database {
        shape: cylinder
    }
    server -> db: queries
", options);
```

## Error Handling

Error information includes:

```csharp
var result = wrapper.RenderDiagram("A -> ");  // Invalid script

if (!result.IsSuccess)
{
    var error = result.Error;
    Console.WriteLine($"Message: {error.Message}");
    Console.WriteLine($"Line: {error.LineNumber}");
    Console.WriteLine($"Column: {error.Column}");
    Console.WriteLine($"Line Content: {error.LineContent}");

    // Get highlighted error parts
    var parts = error.GetHighlightedLineParts();
    Console.WriteLine($"Before: {parts.beforeError}");
    Console.WriteLine($"Error: {parts.errorPart}");
    Console.WriteLine($"After: {parts.afterError}");
}
```

## Logging

Works with `Microsoft.Extensions.Logging`:

```csharp
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

var logger = loggerFactory.CreateLogger<D2Wrapper>();
var wrapper = new D2Wrapper(logger);

var result = wrapper.RenderDiagram("A -> B");
```

## Resource Management

Implements `IDisposable`:

```csharp
using var wrapper = new D2Wrapper();
var result = wrapper.RenderDiagram("A -> B");

// Resources automatically cleaned up when leaving scope
```

## Advanced Usage

### Dark Theme Support

```csharp
var options = new RenderOptions
{
    ThemeId = 0,          // Light theme
    DarkThemeId = 200     // Dark theme (when client is in dark mode)
};
```

### Timeout Protection

```csharp
// Set maximum rendering time
var result = await wrapper.RenderDiagramAsync(
    complexScript,
    timeout: TimeSpan.FromSeconds(15)
);
```

Timeout bounds:
- Minimum: 100ms
- Maximum: 10 minutes

### Input Validation

Automatic validation:
- Maximum script length: 10MB
- Timeout ranges
- Disposed state

```csharp
try
{
    var result = wrapper.RenderDiagram(veryLongScript);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Script too long: {ex.Message}");
}
```

## D2 Language Reference

Full D2 language syntax:

```d2
# Shapes and connections
server -> client: HTTPS

# Containers
network: {
  router
  switch
  router -> switch
}

# Styling
server.style.fill: "#4CAF50"
client.shape: person

# Direction
direction: right

# And much more...
```

[Learn D2 syntax →](https://d2lang.com/tour/intro)

## Observability & Diagnostics (v0.3.0+)

Built-in caching, distributed tracing, and real-time metrics.

### Quick Start with Observability

```csharp
using D2Sharp;

// Configure observability features
var options = new D2WrapperOptions
{
    EnableCaching = true,              // Enable response caching (default: true)
    CacheSize = 100,                   // Max cache entries (default: 100)
    CacheExpiration = TimeSpan.FromHours(1),
    MaxConcurrentRenders = 10,         // Limit concurrent renders (0 = unlimited)
    EnableTelemetry = true,            // Enable Activity/spans (default: true)
    EnableMetrics = true,              // Enable EventCounters (default: true)
    EnableDiagnosticIds = true         // Generate diagnostic IDs (default: true)
};

using var wrapper = new D2Wrapper(options);
var result = wrapper.RenderDiagram("A -> B -> C");

Console.WriteLine($"Diagnostic ID: {result.DiagnosticId}");
Console.WriteLine($"From cache: {result.FromCache}");
```

### Automatic Caching

Caches successful renders based on script content and options:

```csharp
var wrapper = new D2Wrapper(new D2WrapperOptions { EnableCaching = true });

// First render - executes D2 engine
var result1 = wrapper.RenderDiagram("server -> database");
Console.WriteLine($"From cache: {result1.FromCache}"); // False

// Second render - served from cache
var result2 = wrapper.RenderDiagram("server -> database");
Console.WriteLine($"From cache: {result2.FromCache}"); // True
Console.WriteLine($"Same SVG: {result1.Svg == result2.Svg}"); // True
```

Cache keys are based on:
- Script content (SHA256 hash)
- Layout engine
- Theme IDs
- Sketch mode
- Padding, scale, and center options

### Concurrency Control

Limit concurrent render operations to prevent resource exhaustion:

```csharp
var options = new D2WrapperOptions
{
    MaxConcurrentRenders = 5  // Max 5 concurrent renders
};

using var wrapper = new D2Wrapper(options);

// These will be automatically throttled to 5 concurrent executions
var tasks = Enumerable.Range(0, 20)
    .Select(i => wrapper.RenderDiagramAsync($"Task {i} -> Result {i}"))
    .ToArray();

var results = await Task.WhenAll(tasks);
```

### Distributed Tracing

Works with OpenTelemetry and Application Insights:

```csharp
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

// Configure OpenTelemetry
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("D2Sharp")
    .AddConsoleExporter()
    .Build();

var wrapper = new D2Wrapper(new D2WrapperOptions { EnableTelemetry = true });
var result = wrapper.RenderDiagram("A -> B");

// Activity tags automatically include:
// - d2sharp.script.length: Script character count
// - d2sharp.layout.engine: Layout engine (dagre/elk)
// - d2sharp.theme.id: Theme ID
// - d2sharp.sketch.enabled: Sketch mode flag
// - d2sharp.diagnostic.id: Diagnostic correlation ID
// - d2sharp.cache.hit: Whether result was from cache
// - d2sharp.result.status: success/error
// - d2sharp.error.type: Error type if failed
```

### Real-Time Metrics

Monitor performance with EventCounters:

```bash
# View real-time metrics with dotnet-counters
dotnet-counters monitor -n YourApp --counters D2Sharp

# Available metrics:
# - renders-total: Total renders per second
# - renders-active: Currently active render operations
# - render-duration-ms: Average render duration (ms)
# - cache-hit-rate: Cache hit percentage
# - error-rate: Error percentage
```

### Diagnostic IDs

Every render gets a unique diagnostic ID for log correlation:

```csharp
var wrapper = new D2Wrapper(new D2WrapperOptions { EnableDiagnosticIds = true });
var result = wrapper.RenderDiagram("A -> B");

// Use diagnostic ID for log correlation
Console.WriteLine($"Render completed: {result.DiagnosticId}");
// Output: Render completed: 7e3f5a9c2b1d4e8f...

// Diagnostic IDs are unique per render, even for cache hits
var result2 = wrapper.RenderDiagram("A -> B");
Console.WriteLine($"Cache hit: {result2.DiagnosticId}"); // Different ID
```

### Minimal Configuration

For development or when you don't need observability features:

```csharp
var options = new D2WrapperOptions
{
    EnableCaching = false,
    EnableTelemetry = false,
    EnableMetrics = false,
    EnableDiagnosticIds = false
};

using var wrapper = new D2Wrapper(options);
// Behaves like the original wrapper with minimal overhead
```

## Performance

- **Thread-safe**: Safe for concurrent use across multiple threads
- **Memory efficient**: Automatic cleanup with try-finally patterns, minimal allocations
- **Async-first**: Non-blocking async API with cancellation support
- **Optimized**: GeneratedRegex for fast error parsing, zero-allocation patterns

### Test Coverage

- **Line Coverage**: 82.6%
- **Branch Coverage**: 75.5%
- **Method Coverage**: 97.4%

Coverage reports are automatically generated on every commit and available on [Codecov](https://codecov.io/gh/AlrikOlson/D2Sharp).

### Performance Benchmarks

Performance benchmarks are available in the `benchmarks/` directory using BenchmarkDotNet.

Run benchmarks:
```bash
cd benchmarks/D2Sharp.Benchmarks
dotnet run -c Release
```

**Typical Performance** (Apple Silicon M-series, .NET 8.0):
- Simple diagrams (A -> B): ~30-50ms
- Complex diagrams (10-20 nodes): ~100-200ms
- Very complex diagrams (50+ nodes): ~300-500ms

Performance varies based on diagram complexity, layout engine (Dagre vs ELK), and hardware.

## Building from Source

### Prerequisites

- .NET 8.0 SDK or newer
- Go 1.22+ or newer
- GCC (for compiling the Go wrapper)

Check your setup:

**Windows:**
```powershell
.\depcheck.ps1
```

**Unix-based systems:**
```bash
./depcheck.sh
```

### Build

```bash
dotnet build
```

## Project Structure

- `src/D2Sharp` - Main library project
- `src/D2Sharp/d2wrapper` - Go wrapper code
- `examples/D2Sharp.Web` - Web demo application
- `tests/D2Sharp.Tests` - Unit and integration tests

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE.txt](LICENSE.txt) for details.

## Acknowledgments

- [D2](https://github.com/terrastruct/d2) - The modern diagram scripting language
- Built with .NET 8.0

## Links

- [D2 Documentation](https://d2lang.com/)
- [D2 Themes Gallery](https://github.com/terrastruct/d2/tree/master/d2themes)
- [D2 Playground](https://play.d2lang.com/)
- [Report Issues](https://github.com/AlrikOlson/D2Sharp/issues)
