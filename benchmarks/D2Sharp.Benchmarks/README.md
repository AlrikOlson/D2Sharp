# D2Sharp Benchmarks

Performance benchmarks for D2Sharp using BenchmarkDotNet.

## Running Benchmarks

Run all benchmarks in Release mode:

```bash
cd benchmarks/D2Sharp.Benchmarks
dotnet run -c Release
```

Run specific benchmark:

```bash
dotnet run -c Release --filter *RenderSimpleDiagram*
```

## Benchmark Scenarios

### RenderingBenchmarks

- **RenderSimpleDiagram**: Basic 2-node diagram (`A -> B`)
- **RenderComplexDiagram**: Medium complexity (5 nodes, multiple connections)
- **RenderVeryComplexDiagram**: High complexity (15+ nodes, microservices architecture)
- **RenderSimpleWithTheme**: Simple diagram with theme ID 1
- **RenderSimpleWithSketch**: Simple diagram in sketch mode
- **RenderComplexWithElk**: Complex diagram with ELK layout engine
- **RenderWithAllOptions**: Complex diagram with all render options enabled
- **RenderSimpleDiagramAsync**: Async rendering of simple diagram
- **RenderComplexDiagramAsync**: Async rendering of complex diagram

## Results Interpretation

- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Gen0/Gen1/Gen2**: GC collections per 1000 operations
- **Allocated**: Total memory allocated

## Performance Targets

Based on initial testing, target performance metrics:

- Simple diagrams: < 50ms
- Complex diagrams: < 200ms
- Very complex diagrams: < 500ms
- Memory allocation: Minimize Gen2 collections

## Continuous Performance Monitoring

Results should be documented in CHANGELOG.md with each release to track performance regressions.
