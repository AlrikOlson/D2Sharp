# D2Sharp

A .NET wrapper for [D2](https://d2lang.com/), the modern diagram scripting language that turns text to diagrams.

## Installation

```bash
dotnet add package D2Sharp
```

## Quick Start

```csharp
using D2Sharp;

var wrapper = new D2Wrapper();
var result = wrapper.RenderDiagram("A -> B -> C");

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

- Full D2 language support
- Async/await with cancellation and timeouts
- Thread-safe, memory-leak protected
- Layout engines: Dagre and ELK
- 300+ themes, sketch mode, styling options
- Works on Windows, macOS, and Linux
- Built-in caching, telemetry, and metrics (v0.3.0+)

## Rendering Options

```csharp
var options = new RenderOptions
{
    Layout = LayoutEngine.Elk,
    ThemeId = 1,
    Sketch = true,
    Pad = 75
};

var result = wrapper.RenderDiagram("server -> database", options);
```

## Async Rendering

```csharp
// With timeout
var result = await wrapper.RenderDiagramAsync(
    script,
    timeout: TimeSpan.FromSeconds(30)
);

// With cancellation
var cts = new CancellationTokenSource();
var result = await wrapper.RenderDiagramAsync(script, cancellationToken: cts.Token);
```

## Error Handling

```csharp
var result = wrapper.RenderDiagram("A -> ");

if (!result.IsSuccess)
{
    var error = result.Error;
    Console.WriteLine($"Line {error.LineNumber}: {error.Message}");
    Console.WriteLine(error.LineContent);
}
```

## Observability (v0.3.0+)

```csharp
var options = new D2WrapperOptions
{
    EnableCaching = true,
    EnableTelemetry = true,
    EnableMetrics = true,
    MaxConcurrentRenders = 10
};

using var wrapper = new D2Wrapper(options);
var result = wrapper.RenderDiagram("A -> B");

Console.WriteLine($"From cache: {result.FromCache}");
Console.WriteLine($"Diagnostic ID: {result.DiagnosticId}");
```

## Requirements

- .NET 8.0 or later

## Documentation

- [GitHub Repository](https://github.com/AlrikOlson/D2Sharp)
- [D2 Language Docs](https://d2lang.com/)
- [Issue Tracker](https://github.com/AlrikOlson/D2Sharp/issues)

## License

MIT License - see LICENSE.txt for details.
