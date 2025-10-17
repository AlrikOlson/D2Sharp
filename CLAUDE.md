# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.

## Project Overview

D2Sharp is a production-ready .NET wrapper for D2 (d2lang.com), the modern diagram scripting language. It renders D2 diagrams as SVG with full theme and layout support, automatic process isolation, and zero-config API.

**Stack:** .NET 8.0 (C#) + Go 1.22+ (native D2 wrapper) + P/Invoke interop

## Quick Start

```bash
# Build
dotnet build

# Test
dotnet test

# Run web demo
cd examples/D2Sharp.Web && dotnet run
```

## Architecture

### User-Facing API

**D2Renderer (src/D2Sharp/D2Sharp.cs)** - Main entry point
- Zero-config constructor: `new D2Renderer()` creates 3 worker processes
- Factory methods: `CreateWithPool(workerCount)` or `CreateDirect()` for advanced scenarios
- Implements `ID2Renderer` interface for polymorphism
- Thread-safe, disposable, supports sync/async rendering
- Automatically handles process isolation and crash recovery

**Usage:**
```csharp
using var renderer = new D2Renderer();  // Just works!
var result = await renderer.RenderDiagramAsync("A -> B");
if (result.IsSuccess)
    Console.WriteLine(result.Svg);
```

**ASP.NET Core Integration:**
```csharp
builder.Services.AddD2Sharp();  // Register as singleton with 10 workers

// Or customize with fluent API:
builder.Services.AddD2Sharp(d2 => d2
    .UseProcessPool(pool => pool.WithWorkerCount(15))
    .ConfigureCaching(cache => cache.MaxSize = 200));
```

### Core Components

**D2Wrapper (src/D2Sharp/D2Wrapper.cs)** - Direct P/Invoke implementation
- Low-level interface to Go wrapper via P/Invoke
- Used by D2Renderer in direct mode: `D2Renderer.CreateDirect()`
- Thread-safe, minimal overhead, but vulnerable to Go stack overflows on complex diagrams

**D2WrapperProcessPool (src/D2Sharp/D2WrapperProcessPool.cs)** - Process isolation
- Manages pool of worker processes (`D2Sharp.Worker.exe`)
- Circuit breaker pattern for fail-fast behavior
- Background health monitor automatically restarts crashed workers
- Used by default D2Renderer constructor
- Proven reliability: 925/925 concurrent requests in stress tests

**Go Wrapper (src/D2Sharp/d2wrapper/d2wrapper.go)** - Native bridge
- CGo exports: `RenderDiagram` and `FreeDiagram`
- Handles D2 compilation and SVG rendering
- Memory managed via C.CString/C.free

**Worker Process (src/D2Sharp.Worker/Program.cs)** - Isolated renderer
- Standalone executable communicating via stdin/stdout
- JSON protocol for render requests/responses
- Unlimited stack via `ulimit -s unlimited` on Linux/macOS

### Data Flow

1. User calls `renderer.RenderDiagramAsync("A -> B")`
2. **Pool mode (default):** Request queued to available worker process
3. **Direct mode:** Direct P/Invoke to Go wrapper
4. Go wrapper compiles D2 script and renders SVG
5. Result marshaled back as `RenderResult` with SVG or error

### Error Handling

**D2Error Structure:**
- Parse D2 compilation errors: `Compilation error: line:col: message`
- Extract line content from script for context
- `GetHighlightedLineParts()` helper for error display

**Process Pool Errors:**
- Circuit breaker opens when <20% workers healthy
- Fail-fast responses until cooldown period elapses
- Background monitor restarts crashed workers automatically

## Project Structure

```
src/D2Sharp/                    # Main library
├── D2Sharp.cs                  # D2Renderer (main API)
├── D2Wrapper.cs                # Direct P/Invoke implementation
├── D2WrapperProcessPool.cs     # Process pool manager
├── ID2Renderer.cs              # Common interface
├── RenderOptions.cs            # Diagram configuration
├── Extensions/                 # DI integration
│   └── D2SharpServiceExtensions.cs
└── d2wrapper/                  # Go native wrapper
    ├── d2wrapper.go
    ├── build.sh / build.ps1
    └── go.mod

src/D2Sharp.Worker/             # Worker process
└── Program.cs

tests/D2Sharp.Tests/            # Unit tests (106 tests, 60%+ coverage)
├── D2RendererTests.cs          # Main API tests
├── D2WrapperTests.cs           # Direct implementation tests
├── D2WrapperProcessPoolTests.cs # Process pool API tests
├── ID2RendererTests.cs         # Interface conformance tests
└── D2SharpServiceExtensionsTests.cs # DI tests

examples/D2Sharp.Web/           # Web demo with stress tests
benchmarks/D2Sharp.Benchmarks/  # Performance benchmarks
```

## Testing Strategy

**Unit Tests:** Focus on API surface, validation, and D2Wrapper (direct implementation)
- Process pool rendering is proven by stress tests (Web project)
- Avoided flaky process lifecycle tests due to startup timing variability
- Coverage: 60% threshold (excludes process pool infrastructure from coverage calculation)

**Stress Tests:** Run via Web project `/stress-test` endpoint
- 825 concurrent render requests across 4 phases
- Tests warm-up, moderate load, heavy load, and sustained load
- Validates circuit breaker, health monitoring, and crash recovery
- Historical result: 925/925 successful (100% success rate)

**Run Tests:**
```bash
dotnet test  # All unit tests
cd examples/D2Sharp.Web && dotnet run  # Then POST to /stress-test
```

## Common Tasks

### Adding RenderOptions Property

1. Add property to `RenderOptions` class
2. Add field to `RenderOptionsJSON` struct in Go
3. Update `SerializeOptions` method
4. Update Go wrapper to apply option
5. Add test in D2WrapperTests.cs

### Modifying Go Wrapper

1. Edit `src/D2Sharp/d2wrapper/d2wrapper.go`
2. Build: `cd src/D2Sharp/d2wrapper && ./build.sh Debug`
3. Run tests: `dotnet test`
4. CI automatically builds for Windows/Linux/macOS

### Working with Process Pool

**Configuration:**
```csharp
// Default (3 workers)
using var renderer = new D2Renderer();

// Custom worker count
using var renderer = new D2Renderer(workerCount: 10);

// Direct mode (no process isolation)
using var renderer = D2Renderer.CreateDirect();
```

**Logging:** Pass `ILogger<D2Renderer>` to constructor to see worker lifecycle events

## CI/CD Pipeline

**Workflow:** `.github/workflows/build-and-package.yml`
1. Matrix build (Windows/Linux/macOS)
2. Build Go wrapper for each platform
3. Run tests with coverage
4. Package NuGet with platform-specific binaries
5. Auto-release on version bumps in main branch

**Environment Variable:** `CI=true` skips local Go builds (CI builds all platforms)

## Important Notes

- **Thread Safety:** All D2Sharp classes are thread-safe
- **Disposal:** Always dispose renderers (especially with process pools)
- **NuGet Package:** Native libraries in `runtimes/{rid}/native/` (win-x64, linux-x64, osx-x64)
- **Script Limit:** 10MB maximum
- **Worker Files:** Automatically copied to output directory by MSBuild
- **Process Isolation:** Default for reliability (prevents crashes from affecting host)
- **Direct Mode:** Use `CreateDirect()` only if you need lower latency and understand the risks
