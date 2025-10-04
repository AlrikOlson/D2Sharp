# D2Sharp Benchmarks

Performance benchmarks using BenchmarkDotNet.

## Running Benchmarks

All benchmarks:
```bash
cd benchmarks/D2Sharp.Benchmarks
dotnet run -c Release
```

Specific benchmark:
```bash
dotnet run -c Release --filter *RenderSimpleDiagram*
```

## Benchmark Scenarios

- **RenderSimpleDiagram** - Basic 2-node diagram (A -> B)
- **RenderComplexDiagram** - Medium complexity (5 nodes, multiple connections)
- **RenderVeryComplexDiagram** - High complexity (15+ nodes, microservices architecture)
- **RenderSimpleWithTheme** - Simple diagram with theme
- **RenderSimpleWithSketch** - Simple diagram in sketch mode
- **RenderComplexWithElk** - Complex diagram using ELK layout
- **RenderWithAllOptions** - All render options enabled
- **RenderSimpleDiagramAsync** - Async simple diagram
- **RenderComplexDiagramAsync** - Async complex diagram

## Understanding Results

- **Mean** - Average execution time
- **Error** - Half of 99.9% confidence interval
- **StdDev** - Standard deviation
- **Gen0/Gen1/Gen2** - GC collections per 1000 operations
- **Allocated** - Total memory allocated

## Performance Targets

- Simple diagrams: < 50ms
- Complex diagrams: < 200ms
- Very complex diagrams: < 500ms
- Minimize Gen2 collections

Results are documented in CHANGELOG.md with each release to track performance over time.
