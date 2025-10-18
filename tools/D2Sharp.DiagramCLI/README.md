# D2Sharp Diagram CLI

Command-line tool for testing the D2Sharp v0.4.0 NuGet package in real-world scenarios.

## Purpose

This tool demonstrates and validates all major features of the published D2Sharp package:

- Basic diagram rendering
- Batch processing with concurrency control
- File watching for auto-regeneration
- Architecture diagram generation from JSON configs
- Performance benchmarking
- Caching validation
- Async/await patterns
- Error handling
- Theme and layout engine support

## Installation

The CLI references D2Sharp v0.4.0 directly from NuGet.org (not a local project reference).

```bash
cd tools/D2Sharp.DiagramCLI
dotnet build
dotnet run
```

## Usage

### Render a single diagram

```bash
dotnet run -- render samples/simple.d2
dotnet run -- render samples/web-architecture.d2 --theme 1 --layout elk
dotnet run -- render samples/microservices.d2 --sketch --output ./diagrams
```

### Batch process multiple diagrams

```bash
dotnet run -- batch samples --theme 1
dotnet run -- batch samples --layout elk --output ./output
```

### Watch mode (auto-regenerate on file changes)

```bash
dotnet run -- watch samples/simple.d2
dotnet run -- watch samples --sketch
```

### Generate architecture diagram from JSON

```bash
dotnet run -- arch samples/ecommerce-config.json
dotnet run -- arch samples/ecommerce-config.json --theme 2 --output ./arch-diagrams
```

### Run performance benchmarks

```bash
dotnet run -- benchmark
```

## Sample Files

- `samples/simple.d2` - Basic diagram for quick testing
- `samples/web-architecture.d2` - Web application architecture
- `samples/microservices.d2` - Microservices architecture with styling
- `samples/ecommerce-config.json` - JSON config for architecture generation

## What Gets Tested

### Core D2Sharp Features
1. Package installation from NuGet.org (v0.4.0)
2. Sync and async rendering
3. Render options (themes, layout engines, sketch mode)
4. Error handling (invalid syntax, missing files)

### v0.4.0 Features
5. D2Renderer unified API with process pool
6. Worker process isolation
7. ASP.NET Core DI integration
8. Caching (cache hits/misses and performance)
9. Diagnostic IDs for correlation
10. Telemetry (Activity/span tracking)
11. Metrics (performance measurement)

### Real-World Scenarios
12. File I/O (read .d2 files, write SVG outputs)
13. Batch processing with progress reporting
14. File watching and real-time regeneration
15. JSON deserialization to D2 scripts
16. Performance benchmarking

## Expected Output

The CLI outputs to `./output` (or custom via `--output`) and displays:
- Render duration
- Diagnostic ID
- Cache hit/miss status
- SVG file size
- Error details (if any)

## Benchmarks

The benchmark command tests:
1. Simple diagrams (A -> B)
2. Complex diagrams (20 nodes)
3. Theme variations
4. Concurrent rendering
5. Cache effectiveness

Typical results:
- First render: 30-100ms
- Cache hits: <5ms
- Speedup: 10-50x faster with caching

## Testing Checklist

- Package installs from NuGet.org (not local)
- Simple diagram renders successfully
- Complex diagrams with styling work
- Error messages are clear and helpful
- Caching improves performance
- Concurrent rendering respects limits
- Diagnostic IDs are generated
- Watch mode detects file changes
- Batch processing handles multiple files
- Architecture generation from JSON works
- All render options (theme, layout, sketch) function
- Benchmarks show realistic performance

## License

MIT License - see parent project LICENSE for details.
