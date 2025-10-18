# D2Sharp

A .NET wrapper for [D2](https://d2lang.com/), the modern diagram scripting language that turns text to diagrams.

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

## Features

- **Zero Configuration** - Works out of the box with sensible defaults
- **High Performance** - Built-in worker pool handles concurrent requests efficiently
- **Full D2 Support** - All D2 features: layouts, themes, sketch mode, and more
- **Async/Await** - Task-based async rendering with cancellation support
- **Cross-Platform** - Windows, macOS, and Linux
- **Production Ready** - Battle-tested with comprehensive error handling

## Rendering Options

```csharp
var options = new RenderOptions
{
    Layout = LayoutEngine.Elk,
    ThemeId = 303,  // C4 PlantUML theme
    Sketch = true,
    Pad = 75
};

var result = await renderer.RenderDiagramAsync("server -> database", options);
```

300+ themes available, including theme 303 for C4 diagrams.

## Error Handling

```csharp
var result = await renderer.RenderDiagramAsync("A -> ");  // Invalid

if (!result.IsSuccess)
{
    var error = result.Error;
    Console.WriteLine($"Line {error.LineNumber}: {error.Message}");
    Console.WriteLine($"  {error.LineContent}");
}
```

## ASP.NET Core Integration

```csharp
// In Program.cs - Basic registration (uses 10 workers)
builder.Services.AddD2Sharp();

// With custom configuration
builder.Services.AddD2Sharp(d2 => d2
    .UseProcessPool(pool => pool.WithWorkerCount(15))
    .ConfigureCaching(cache => cache.Enabled = true));

// In your controller
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

## Advanced Configuration

### Custom Worker Count

```csharp
// For high-concurrency scenarios
using var renderer = new D2Renderer(workerCount: 15);
```

### Direct Mode (CLI Tools)

For command-line tools or single-threaded applications:

```csharp
using var renderer = D2Renderer.CreateDirect();
var result = await renderer.RenderDiagramAsync(script);
```

Note: Direct mode is not recommended for web applications with concurrent requests.

## Requirements

- .NET 8.0 or later

## Documentation

- [GitHub Repository](https://github.com/AlrikOlson/D2Sharp)
- [Full Documentation](https://github.com/AlrikOlson/D2Sharp/blob/main/README.md)
- [D2 Language Docs](https://d2lang.com/)
- [Issue Tracker](https://github.com/AlrikOlson/D2Sharp/issues)

## What's New in v0.4.0

- **Unified API**: New `D2Renderer` class with smart defaults
- **Process Isolation**: Worker processes prevent crashes from affecting your app
- **ASP.NET Core DI**: Full dependency injection support with fluent configuration
- **Theme 303 Support**: Upgraded to D2 v0.7.1 for C4 diagram themes
- **100+ New Tests**: Comprehensive test coverage for reliability

## License

MIT License - see LICENSE.txt for details.
