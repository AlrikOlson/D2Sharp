# D2Sharp Diagram CLI - Real-World Package Test

A comprehensive command-line tool that tests the D2Sharp v0.3.0 NuGet package in real-world scenarios.

## Purpose

This tool demonstrates and validates all major features of the published D2Sharp package:
- ✅ Basic diagram rendering
- ✅ Batch processing with concurrency control
- ✅ File watching for auto-regeneration
- ✅ Architecture diagram generation from JSON configs
- ✅ Performance benchmarking
- ✅ Caching validation
- ✅ Async/await patterns
- ✅ Error handling
- ✅ Theme and layout engine support

## Installation

The CLI references D2Sharp v0.3.0 directly from NuGet.org (not a local project reference).

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
1. **Package Installation** - Uses D2Sharp 0.3.0 from NuGet.org
2. **Sync & Async Rendering** - Tests both rendering patterns
3. **Render Options** - Themes, layout engines (Dagre/ELK), sketch mode
4. **Error Handling** - Invalid D2 syntax, file not found, etc.

### v0.3.0 Observability Features
5. **Caching** - Validates cache hits/misses and performance improvements
6. **Diagnostic IDs** - Tracks unique IDs for correlation
7. **Concurrency Control** - MaxConcurrentRenders throttling
8. **Telemetry** - Activity/span tracking
9. **Metrics** - Performance measurement

### Real-World Scenarios
10. **File I/O** - Read .d2 files, write SVG outputs
11. **Batch Processing** - Multiple files with progress reporting
12. **File Watching** - Real-time regeneration on changes
13. **JSON Deserialization** - Architecture configs to D2 scripts
14. **Performance Benchmarking** - Various workload tests

## Expected Output

The CLI outputs to the `./output` directory (or custom via `--output`) and displays:
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

- [x] Package installs from NuGet.org (not local)
- [x] Simple diagram renders successfully
- [x] Complex diagrams with styling work
- [x] Error messages are clear and helpful
- [x] Caching improves performance
- [x] Concurrent rendering respects limits
- [x] Diagnostic IDs are generated
- [x] Watch mode detects file changes
- [x] Batch processing handles multiple files
- [x] Architecture generation from JSON works
- [x] All render options (theme, layout, sketch) function
- [x] Benchmarks show realistic performance

## License

MIT License - see parent project LICENSE for details.
