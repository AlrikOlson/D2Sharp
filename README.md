# D2Sharp

[![NuGet](https://img.shields.io/nuget/v/D2Sharp.svg)](https://www.nuget.org/packages/D2Sharp/)

D2Sharp wraps the D2 diagramming library for .NET, allowing you to create diagrams with C# in your .NET applications.

## Features

- Render diagrams as SVG
- Integrate with ASP.NET Core for web applications
- Includes a web demo for quick testing

## Prerequisites for Building

- .NET 8.0 SDK or newer
- Go 1.22.2 or newer
- GCC (for compiling the Go wrapper)

You can check your setup using the provided scripts:

Windows:
```powershell
.\depcheck.ps1
```

Unix-based systems:
```bash
./depcheck.sh
```

## Project Structure

- `src/D2Sharp`: Main library project
- `examples/D2Sharp.Web`: Web demo application
- `src/D2Sharp/d2wrapper`: Go wrapper code

## Setup

1. Clone the repository
2. Build the project: `dotnet build`

## Usage

Basic usage:

```csharp
// Create an instance of D2Wrapper
// You can pass a logger instance if you want to enable logging
var wrapper = new D2Wrapper(logger);

// Define your D2 script as a string
var script = @"direction: right
A -> B -> C";

// Render the diagram
var svg = wrapper.RenderDiagram(script);

// The 'svg' variable now contains the SVG representation of your diagram
// You can save this to a file, display it in a web page, or process it further as needed
```

Running the web demo:

```
cd examples/D2Sharp.Web
dotnet run
```

## Acknowledgements

This project would not be possible without the following open-source projects:

- [D2](https://github.com/terrastruct/d2): The underlying diagramming engine
- [.NET](https://github.com/dotnet/runtime): The runtime and framework
- [Go](https://github.com/golang/go): Used for the native wrapper