# D2Sharp

A production-ready .NET wrapper for [D2](https://d2lang.com/), the modern diagram scripting language that turns text to diagrams.

[![NuGet](https://img.shields.io/nuget/v/D2Sharp.svg)](https://www.nuget.org/packages/D2Sharp/)
[![codecov](https://codecov.io/gh/AlrikOlson/D2Sharp/branch/main/graph/badge.svg)](https://codecov.io/gh/AlrikOlson/D2Sharp)
[![License](https://img.shields.io/github/license/AlrikOlson/D2Sharp)](LICENSE.txt)

## Features

✅ **Full D2 Support** - Render D2 diagrams with complete feature support
✅ **Async/Await** - Task-based async rendering with cancellation and timeout support
✅ **Production Ready** - Thread-safe, memory-leak protected, with comprehensive error handling
✅ **Flexible Rendering** - Choose layout engines (Dagre/ELK), themes, sketch mode, and more
✅ **Cross-Platform** - Works on Windows, macOS, and Linux
✅ **Type-Safe** - Fully documented API with XML docs and nullable reference types
✅ **Observability** - Built-in telemetry, metrics, and caching infrastructure (v0.3.0+)

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

D2Sharp supports extensive customization through `RenderOptions`:

### Themes

Choose from 300+ built-in themes:

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
- **ELK** - More sophisticated layouts with advanced features

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

D2Sharp provides detailed error information:

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

D2Sharp supports `Microsoft.Extensions.Logging`:

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

D2Sharp implements `IDisposable`:

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

D2Sharp automatically validates:
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

D2Sharp supports the full D2 language syntax:

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

D2Sharp includes production-grade observability infrastructure for monitoring, tracing, and performance optimization.

### Telemetry & Distributed Tracing

Built-in support for OpenTelemetry and Application Insights:

```csharp
// Activity/spans are automatically created with D2SharpActivitySource
// Compatible with OpenTelemetry, Application Insights, and Azure Monitor

// Activity tags include:
// - d2sharp.script.length
// - d2sharp.layout.engine
// - d2sharp.theme.id
// - d2sharp.sketch.enabled
// - d2sharp.diagnostic.id
// - d2sharp.cache.hit
// - d2sharp.result.status
```

### Real-Time Metrics

Monitor performance with EventCounters:

```bash
# View real-time metrics with dotnet-counters
dotnet-counters monitor -n YourApp --counters D2Sharp

# Available metrics:
# - renders-total: Total renders per second
# - renders-active: Currently active render operations
# - render-duration-ms: Average render duration
# - cache-hit-rate: Cache hit percentage
# - error-rate: Error percentage
```

### Diagnostic IDs

Each render operation can include a diagnostic ID for log correlation:

```csharp
var result = wrapper.RenderDiagram("A -> B");
Console.WriteLine($"Diagnostic ID: {result.DiagnosticId}");
Console.WriteLine($"From cache: {result.FromCache}");
```

### Configuration

Configure caching, concurrency, and telemetry:

```csharp
var options = new D2WrapperOptions
{
    EnableCaching = true,
    CacheSize = 100,
    CacheExpiration = TimeSpan.FromHours(1),
    MaxConcurrentRenders = 10,
    EnableTelemetry = true,
    EnableMetrics = true
};

// Note: Full integration in Phase 5b (coming soon)
```

**Note:** Phase 5a (v0.3.0-alpha.1) provides the observability infrastructure. Full integration into D2Wrapper will be available in Phase 5b.

## Performance

D2Sharp is designed for production performance:

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
- Built with ❤️ using .NET 8.0

## Links

- [D2 Documentation](https://d2lang.com/)
- [D2 Themes Gallery](https://github.com/terrastruct/d2/tree/master/d2themes)
- [D2 Playground](https://play.d2lang.com/)
- [Report Issues](https://github.com/AlrikOlson/D2Sharp/issues)
