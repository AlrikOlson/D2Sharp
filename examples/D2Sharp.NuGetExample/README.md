# D2Sharp NuGet Package Example

This is a simple console application demonstrating how to use D2Sharp from the published NuGet package.

## Prerequisites

- .NET 8.0 SDK or later
- D2 must be installed and available in PATH ([installation instructions](https://d2lang.com/tour/install))

## Quick Start

1. **Run the example:**
   ```bash
   cd examples/D2Sharp.NuGetExample
   dotnet run
   ```

2. **View the generated diagrams:**
   - Open the generated `.svg` files in your web browser
   - Files: `simple.svg`, `themed.svg`, `complex.svg`, `pool-example.svg`

## What This Example Demonstrates

### Example 1: Simple Diagram
Basic diagram rendering with default options.

### Example 2: Custom Styling
Demonstrates using:
- Dark theme (`ThemeId = 200`)
- Sketch mode (hand-drawn style)
- Custom padding

### Example 3: Complex Layout
Shows a system architecture diagram using:
- ELK layout engine
- Custom scale
- Centered output

### Example 4: Error Handling
Demonstrates how to handle D2 compilation errors with detailed error information.

### Example 5: Worker Pool Configuration
Shows how to configure a custom worker pool for high-concurrency scenarios.

## Using D2Sharp in Your Project

Add the NuGet package to your project:

```bash
dotnet add package D2Sharp
```

Or add it manually to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="D2Sharp" Version="0.4.1" />
</ItemGroup>
```

## Basic Usage

```csharp
using D2Sharp;

// Create a renderer with default settings (3 worker processes)
using var renderer = new D2Renderer();

// Render a diagram
var result = await renderer.RenderDiagramAsync("A -> B -> C");

if (result.IsSuccess)
{
    await File.WriteAllTextAsync("diagram.svg", result.Svg);
    Console.WriteLine("Diagram rendered successfully!");
}
else
{
    Console.WriteLine($"Error: {result.Error?.Message}");
}
```

## Render Options

Customize diagram output with `RenderOptions`:

```csharp
var options = new RenderOptions
{
    Layout = LayoutEngine.Elk,     // or LayoutEngine.Dagre (default)
    ThemeId = 200,                  // Dark theme
    Sketch = true,                  // Hand-drawn style
    Pad = 20,                       // Padding around diagram
    Scale = 1.5,                    // Scale factor
    Center = true                   // Center the diagram
};

var result = await renderer.RenderDiagramAsync(script, options);
```

## Available Themes

- `0` - Neutral Default
- `1` - Neutral Grey
- `100` - Cool Classics
- `200` - Dark Flagship
- `300` - Mixed Berry Blue
- And many more!

## Worker Pool Configuration

For high-concurrency scenarios, configure more workers:

```csharp
// Create renderer with 10 worker processes
using var renderer = new D2Renderer(workerCount: 10);
```

For low-latency but less isolation, use direct mode:

```csharp
// Direct P/Invoke (no process isolation)
using var renderer = D2Renderer.CreateDirect();
```

## Learn More

- [D2Sharp GitHub Repository](https://github.com/AlrikOlson/D2Sharp)
- [D2 Language Documentation](https://d2lang.com)
- [D2Sharp API Documentation](https://github.com/AlrikOlson/D2Sharp#readme)
