# D2Sharp

A .NET wrapper for [D2](https://d2lang.com/), the modern diagram scripting language that turns text to diagrams.

[![NuGet](https://img.shields.io/nuget/v/D2Sharp.svg)](https://www.nuget.org/packages/D2Sharp/)
[![codecov](https://codecov.io/gh/AlrikOlson/D2Sharp/branch/main/graph/badge.svg)](https://codecov.io/gh/AlrikOlson/D2Sharp)
[![License](https://img.shields.io/github/license/AlrikOlson/D2Sharp)](LICENSE.txt)

## Features

- **Zero Configuration** - Works out of the box with sensible defaults
- **High Performance** - Built-in worker pool handles concurrent requests efficiently
- **Full D2 Support** - All D2 features: layouts, themes, sketch mode, and more
- **Async/Await** - Task-based async rendering with cancellation support
- **Cross-Platform** - Windows, macOS, and Linux
- **Production Ready** - Battle-tested with comprehensive error handling

## Installation

```bash
dotnet add package D2Sharp
```

## Quick Start

```csharp
using D2Sharp;

// Create a renderer (uses 3 worker processes by default)
using var renderer = new D2Renderer();

// Render a diagram
var result = await renderer.RenderDiagramAsync("A -> B -> C");

if (result.IsSuccess)
{
    File.WriteAllText("diagram.svg", result.Svg);
}
else
{
    Console.WriteLine($"Error: {result.Error?.Message}");
}
```

That's it! D2Sharp handles all the complexity for you.

## Basic Usage

### Simple Diagrams

```csharp
using var renderer = new D2Renderer();

var script = @"
server -> database: queries
database -> cache: reads
";

var result = await renderer.RenderDiagramAsync(script);
```

### Error Handling

```csharp
var result = await renderer.RenderDiagramAsync("A -> ");  // Invalid

if (!result.IsSuccess)
{
    var error = result.Error;
    Console.WriteLine($"Line {error.LineNumber}: {error.Message}");
    Console.WriteLine($"  {error.LineContent}");
}
```

### Synchronous Rendering

```csharp
var result = renderer.RenderDiagram("A -> B");  // Blocks until complete
```

### Cancellation & Timeouts

```csharp
// With cancellation token
var cts = new CancellationTokenSource();
var result = await renderer.RenderDiagramAsync(script, cancellationToken: cts.Token);

// The renderer handles timeouts automatically - diagrams that take too long
// will be cancelled and return an error result
```

## Customization

### Themes

```csharp
var options = new RenderOptions
{
    ThemeId = 1  // Cool classics theme
};

var result = await renderer.RenderDiagramAsync("server -> database", options);
```

300+ themes available: [View all D2 themes →](https://github.com/terrastruct/d2/tree/master/d2themes)

### Layout Engines

```csharp
var options = new RenderOptions
{
    Layout = LayoutEngine.Elk  // or LayoutEngine.Dagre (default)
};
```

- **Dagre** - Fast, clean layouts (default)
- **ELK** - More complex layouts with additional features

### Sketch Mode

```csharp
var options = new RenderOptions
{
    Sketch = true  // Hand-drawn style
};
```

### Visual Options

```csharp
var options = new RenderOptions
{
    Pad = 50,        // Padding (default: 100)
    Scale = 0.5,     // Scale factor
    Center = true    // Center in viewbox
};
```

### Combined Example

```csharp
var options = new RenderOptions
{
    Layout = LayoutEngine.Elk,
    ThemeId = 1,
    Sketch = true,
    Pad = 75
};

var result = await renderer.RenderDiagramAsync(complexDiagram, options);
```

## Advanced Configuration

### Custom Worker Count

```csharp
// For high-concurrency scenarios
using var renderer = new D2Renderer(workerCount: 15);
```

The default (3 workers) is optimal for most use cases.

### ASP.NET Core Integration

```csharp
// In Program.cs
builder.Services.AddD2Sharp();  // Registers as singleton with 10 workers

// In your controller/endpoint
public class DiagramController : ControllerBase
{
    private readonly D2Renderer _renderer;

    public DiagramController(D2Renderer renderer)
    {
        _renderer = renderer;
    }

    [HttpPost]
    public async Task<IActionResult> Render([FromBody] string script)
    {
        var result = await _renderer.RenderDiagramAsync(script);
        return result.IsSuccess
            ? Content(result.Svg, "image/svg+xml")
            : BadRequest(result.Error);
    }
}
```

### Direct Mode (CLI Tools)

For command-line tools or single-threaded applications where you don't need worker processes:

```csharp
using var renderer = D2Renderer.CreateDirect();
var result = await renderer.RenderDiagramAsync(script);
```

Note: Direct mode is not recommended for web applications with concurrent requests.

## D2 Language Reference

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
```

[Learn D2 syntax →](https://d2lang.com/tour/intro)

## Performance

D2Sharp is optimized for production workloads:

- **Concurrent requests**: Efficiently handled via worker pool
- **Memory efficient**: Automatic cleanup and minimal allocations
- **Fast**: Simple diagrams render in ~30-50ms

Typical performance (on Apple Silicon):
- Simple diagrams: 30-50ms
- Complex diagrams: 100-200ms
- Very complex: 300-500ms

## Building from Source

### Prerequisites

- .NET 8.0 SDK
- Go 1.22+
- GCC

Check dependencies:

```bash
# Windows
.\depcheck.ps1

# Unix/Linux/macOS
./depcheck.sh
```

### Build

```bash
dotnet build
```

## Project Structure

- `src/D2Sharp` - Main library
- `examples/D2Sharp.Web` - Web demo application
- `tests/D2Sharp.Tests` - Test suite

## Additional Documentation

- [Observability & Telemetry](OBSERVABILITY.md) - Advanced monitoring features
- [Architecture](ARCHITECTURE.md) - Technical implementation details
- [Contributing](CONTRIBUTING.md) - Contribution guidelines

## Links

- [D2 Documentation](https://d2lang.com/)
- [D2 Themes Gallery](https://github.com/terrastruct/d2/tree/master/d2themes)
- [D2 Playground](https://play.d2lang.com/)
- [Report Issues](https://github.com/AlrikOlson/D2Sharp/issues)

## License

MIT License - see [LICENSE.txt](LICENSE.txt) for details.

## Acknowledgments

- [D2](https://github.com/terrastruct/d2) - The diagram scripting language
- Built with .NET 8.0
