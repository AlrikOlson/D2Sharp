# d2.Net

d2.Net wraps the D2 diagramming library for .NET, letting you create diagrams with C# in your .NET apps.

## What it does

- Outputs diagrams as SVG
- Offers themes for styling
- Works with ASP.NET Core for web apps
- Includes a console app for quick tests

## Before you start

You need:

- .NET 8.0 SDK or newer
- Go 1.22.2 or newer
- GCC (to compile the Go wrapper)

Check your setup:

```powershell
.\check_prerequisites.ps1
```

## Setting up

1. Clone the repo
2. Build it: `dotnet build`

## How to use it

Quick start:

```csharp
var wrapper = new D2Wrapper(logger);
var script = @"direction: right
A -> B -> C";
var svg = wrapper.RenderDiagram(script);
```

Run the web app:

```
cd src/d2.Net.App
dotnet run
```

## What's in the project

- `src/d2.Net`: Main library
- `src/d2.Net.App`: Web app demo
- `src/d2.Net.ConsoleApp`: Console app for tests
- `src/d2.Net.Diagrams`: Pre-made diagram scripts
- `src/d2.Net/d2wrapper`: Go wrapper code

## Tweaking it

Change themes, padding, and layout:

```csharp
var svg = wrapper.RenderDiagram(script, D2Theme.Aubergine, D2Padding.Medium);
```

## Want to help?

Send a pull request with your improvements!
