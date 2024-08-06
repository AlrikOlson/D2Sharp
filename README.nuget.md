# D2Sharp

D2Sharp is a .NET wrapper for the D2 diagramming library, allowing you to create diagrams programmatically in your .NET applications.

## Installation

You can install D2Sharp via NuGet Package Manager:

```
Install-Package D2Sharp
```

Or via .NET CLI:

```
dotnet add package D2Sharp
```

## Usage

Here's a basic example of how to use D2Sharp:

```csharp
using D2Sharp;
using Microsoft.Extensions.Logging;

// Create a logger (optional)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<D2Wrapper>();

// Create an instance of D2Wrapper
var wrapper = new D2Wrapper(logger);

// Define your D2 script
var script = @"
direction: right
A -> B -> C
";

// Render the diagram
try
{
    string svg = wrapper.RenderDiagram(script);
    // Use the SVG string as needed (e.g., save to file, display in a web page)
}
catch (Exception ex)
{
    Console.WriteLine($"Error rendering diagram: {ex.Message}");
}
```

## Error Handling

The `RenderDiagram` method now returns a `RenderResult` object, which includes both the rendered SVG (if successful) and detailed error information (if rendering failed). Here's how you can use it:

```csharp
var wrapper = new D2Wrapper();
string script = @"
A -> B
B ->  // This line has an error
C -> D
";

var result = wrapper.RenderDiagram(script);

if (result.IsSuccess)
{
    Console.WriteLine("Diagram rendered successfully:");
    Console.WriteLine(result.Svg);
}
else
{
    Console.WriteLine("Error rendering diagram:");
    Console.WriteLine($"Message: {result.Error.Message}");
    if (result.Error.LineNumber.HasValue)
    {
        Console.WriteLine($"Line {result.Error.LineNumber}: {result.Error.LineContent}");
    }
}
```

## Features

- Render D2 diagrams as SVG
- Cross-platform support (Windows, macOS, Linux)
- Integration with .NET logging

## Requirements

- .NET 8.0 or later

## Acknowledgements

D2Sharp is built on top of the following open-source projects:
- [D2](https://github.com/terrastruct/d2): The underlying diagramming engine
- [.NET](https://github.com/dotnet/runtime): The runtime and framework

## Issues and Contributions

For issues, feature requests, or contributions, please visit the [GitHub repository](https://github.com/AlrikOlson/D2Sharp).