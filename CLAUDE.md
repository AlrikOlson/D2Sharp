# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

D2Sharp is a .NET wrapper for D2 (d2lang.com), the modern diagram scripting language. It renders D2 diagram scripts as SVG with themes, layout engines, async patterns, and observability features.

**Key Technologies:**
- .NET 8.0 (C# library)
- Go 1.22+ (native wrapper using D2 library)
- P/Invoke for C#-Go interop
- Cross-platform (Windows, Linux, macOS)

## Build & Development Commands

### Prerequisites Check
```bash
# Unix/Linux/macOS
./depcheck.sh

# Windows
.\depcheck.ps1
```

### Build
```bash
# Build entire solution
dotnet build

# Build specific configuration
dotnet build --configuration Release
```

The build process automatically compiles the Go wrapper (d2wrapper.dll/.so/.dylib) before building the C# project via MSBuild targets.

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Run specific test
dotnet test --filter "FullyQualifiedName~D2WrapperTests.RenderDiagram_WithValidScript_ReturnsSuccess"
```

**Coverage Requirements:** Minimum 80% line coverage (enforced in CI)

### Benchmarks
```bash
cd benchmarks/D2Sharp.Benchmarks
dotnet run -c Release
```

### Web Demo
```bash
cd examples/D2Sharp.Web
dotnet run
```

## Architecture

### Core Components

**D2Wrapper (src/D2Sharp/D2Wrapper.cs)**
- Main entry point for rendering D2 diagrams
- Marshals data between C# and Go via P/Invoke
- Thread-safe with IDisposable pattern
- Supports both sync (`RenderDiagram`) and async (`RenderDiagramAsync`) rendering
- Integrated observability: caching, telemetry, metrics, concurrency control

**Go Wrapper (src/D2Sharp/d2wrapper/d2wrapper.go)**
- CGo bridge to D2 library
- Exports `RenderDiagram` and `FreeDiagram` functions
- Handles D2 compilation and SVG rendering
- Memory management via C.CString/C.free

### Key Design Patterns

**P/Invoke Interop:**
```csharp
[LibraryImport("d2wrapper", EntryPoint = "RenderDiagram", StringMarshalling = StringMarshalling.Utf8)]
private static partial IntPtr RenderDiagramInternal(string script, string optionsJson, out IntPtr errorPtr);
```

**Memory Safety:**
- Always use try-finally blocks when working with native pointers
- Call `FreeDiagram(ptr)` to release Go-allocated memory
- Check for IntPtr.Zero before marshaling strings

**Configuration System:**
- `D2WrapperOptions` - configures caching, telemetry, concurrency
- `RenderOptions` - configures diagram rendering (themes, layout, sketch mode)
- Both use nullable properties for optional configuration

**Observability Integration:**
- Caching: `RenderCache` with SHA256-based keys
- Telemetry: `D2SharpActivitySource` for distributed tracing
- Metrics: `D2SharpEventCounters` for real-time monitoring
- Diagnostic IDs for log correlation

### Data Flow

1. **RenderDiagram** receives D2 script + optional RenderOptions
2. Options serialized to JSON via `SerializeOptions`
3. Cache checked if enabled (`RenderCache.TryGet`)
4. On cache miss: `RenderDiagramInternal` P/Invoke call to Go
5. Go wrapper compiles D2 script and renders SVG
6. Result marshaled back to C# as `RenderResult`
7. Success results cached for future requests
8. Activity spans and metrics recorded throughout

### Error Handling

**Error Parsing:**
- Go errors returned via out parameter as C string
- Regex pattern: `"Compilation error: (\d+):(\d+): (.+)"`
- Parsed into `D2Error` with line number, column, and message
- Line content extracted from original script for context

**Native Library Errors:**
- `DllNotFoundException`: d2wrapper library not found
- `EntryPointNotFoundException`: incompatible library version

## Project Structure

```
src/D2Sharp/           # Main library
├── D2Wrapper.cs       # Core wrapper class
├── RenderOptions.cs   # Rendering configuration
├── D2WrapperOptions.cs # Wrapper configuration
├── Caching/           # Cache implementation
├── Telemetry/         # Observability components
└── d2wrapper/         # Go native wrapper
    ├── d2wrapper.go   # Go implementation
    ├── build.ps1      # Build script
    ├── go.mod         # Go dependencies
    └── go.sum

tests/D2Sharp.Tests/   # Unit & integration tests
├── D2WrapperTests.cs
├── D2ErrorTests.cs
└── D2WrapperObservabilityTests.cs

examples/D2Sharp.Web/  # Web demo application
benchmarks/D2Sharp.Benchmarks/  # Performance benchmarks
```

## Common Development Tasks

### Adding New RenderOptions Properties

1. Add property to `RenderOptions` class (C#)
2. Add corresponding field to `RenderOptionsJSON` struct (Go)
3. Update `SerializeOptions` method to include new property
4. Update Go wrapper to apply the option
5. Add unit tests for the new option
6. Update XML documentation

### Modifying Native Library

1. Edit `src/D2Sharp/d2wrapper/d2wrapper.go`
2. Build manually: `cd src/D2Sharp/d2wrapper && go build -buildmode=c-shared -o d2wrapper.[dll|so|dylib] .`
3. Copy built library to test output directory
4. Run tests to validate changes
5. CI will automatically build for all platforms

### Working with Observability Features

**Enable features via D2WrapperOptions:**
```csharp
var options = new D2WrapperOptions
{
    EnableCaching = true,
    EnableTelemetry = true,
    EnableMetrics = true,
    EnableDiagnosticIds = true,
    MaxConcurrentRenders = 10
};
```

**Cache invalidation:** Cache keys include script content + all RenderOptions properties. Changing any option creates new cache entry.

**Telemetry:** Activity spans automatically created when `EnableTelemetry = true`. Tags include script length, layout engine, theme, cache hits, diagnostic ID.

## CI/CD Pipeline

**Build Workflow (.github/workflows/build-and-package.yml):**
1. Matrix build across Windows/Linux/macOS
2. Build Go wrapper for each platform
3. Run tests with coverage collection
4. Upload coverage to Codecov
5. Package NuGet with platform-specific native libraries
6. Auto-release on version change in main branch

**Environment Variable:** Set `CI=true` to skip local Go wrapper builds (used in CI)

## Important Notes

- **Script Length Limit:** 10MB maximum (10,000,000 characters)
- **Timeout Bounds:** 100ms minimum, 10 minutes maximum
- **Thread Safety:** D2Wrapper is thread-safe, multiple instances or concurrent calls are safe
- **Disposal:** Always dispose D2Wrapper when done, especially if caching/concurrency features are enabled
- **NuGet Package:** Native libraries bundled in runtimes/ folder with RID-specific paths (win-x64, linux-x64, osx-x64)
- **Regex Performance:** Uses GeneratedRegex attribute for zero-allocation error parsing
