# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.4.2] - 2025-10-21

### Fixed
- **NuGet packaging**: Fixed Worker files missing from NuGet package (v0.4.1 issue)
  - Changed D2Sharp.csproj packaging condition from launcher check to .dll check (cross-platform compatible)
  - Added Worker build steps to CI workflow before `dotnet pack`
  - Package now includes all 13 Worker files in `build/D2Sharp.Worker/` directory
  - Fixes "Worker executable not found" error when using v0.4.1 package
  - Users of v0.4.1 should upgrade to v0.4.2 for process isolation support

## [0.4.1] - 2025-10-18

### Fixed
- **NuGet packaging**: Fixed broken D2Sharp.Abstractions dependency in v0.4.0
  - Merged D2Sharp.Abstractions project into D2Sharp to eliminate packaging complexity
  - Removed external NuGet dependency that caused NU1101 errors when installing v0.4.0
  - All types previously in D2Sharp.Abstractions are now part of the main D2Sharp package
  - Users of v0.4.0 should upgrade to v0.4.1 immediately

### Added
- **NuGet example project**: Added `examples/D2Sharp.NuGetExample` demonstrating usage from published NuGet package
  - Simple console application with 5 comprehensive examples
  - Shows basic rendering, custom styling, complex layouts, error handling, and worker pool configuration
  - Includes detailed README with quick start guide
  - Note: Project files included but not in solution until v0.4.1 is published

### Changed
- **Project structure**: D2Sharp.Abstractions project merged into D2Sharp
  - No API changes - all types remain in the same namespace
  - Simplifies project structure and eliminates packaging issues

## [0.4.0] - 2025-10-18

### Breaking Changes
- **Main API class renamed**: `D2Wrapper` is now `D2Renderer`
  - Old: `new D2Wrapper()`
  - New: `new D2Renderer()`
  - `D2Wrapper` still exists but moved to `D2Sharp.Internal` namespace for advanced users
- **Dependency injection changed**: Registration pattern updated
  - Old: `services.AddSingleton<D2Wrapper>()`
  - New: `services.AddD2Sharp(d2 => d2.UseProcessPool(pool => pool.WithWorkerCount(15)))`

### Added
- **D2Renderer class**: New zero-config API with smart defaults
  - Uses worker process pool by default (3 workers)
  - Constructor: `new D2Renderer()` or `new D2Renderer(workerCount: 10)`
  - Factory methods: `CreateWithPool()` and `CreateDirect()`
- **Process pool implementation**: Worker processes for crash isolation
  - `D2WrapperProcessPool` manages pool of worker processes
  - Circuit breaker pattern for fail-fast behavior
  - Background health monitoring with automatic worker restart
  - Proven reliability: 925/925 concurrent requests in stress tests
- **Worker process**: `D2Sharp.Worker.exe` for isolated rendering
  - Standalone executable using stdin/stdout
  - JSON protocol for requests/responses
  - Unlimited stack on Linux/macOS to prevent stack overflow
- **D2Sharp.Abstractions project**: Shared types and interfaces
  - `ID2Renderer` interface for polymorphism
  - `RenderResult`, `RenderOptions`, `LayoutEngine`, `D2Error` moved here
- **Dependency injection support**: Full ASP.NET Core integration
  - `AddD2Sharp()` extension method with fluent configuration
  - Builder pattern via `ID2RendererBuilder`
  - Options pattern support with `D2SharpOptions`
- **Integration test project**: New test suite with 100+ additional tests
  - Separate D2Sharp.IntegrationTests project
  - Process pool tests (concurrency, circuit breaker, error handling)
  - Performance tests under load
  - Total tests: 212+ (was 106)
- **Enhanced Web demo**: Professional UI with render options
  - Form controls for layout, theme, sketch mode, padding, scale
  - Stress test endpoint for validating process pool
  - Live rendering with error display
- **VSCode debug configuration**: Launch configurations for debugging

### Changed
- **Reorganized internal classes**: Better separation of concerns
  - `D2Wrapper`, `D2WrapperOptions`, `RenderCache` moved to `Internal` namespace
  - Telemetry classes moved to `Internal/Telemetry`
- **Test project split**: Separated unit and integration tests
  - Unit tests: D2Sharp.Tests (fast, no process lifecycle)
  - Integration tests: D2Sharp.IntegrationTests (process pool, E2E scenarios)
- **Build scripts**: Added bash build script for WSL/Linux support
- **Documentation**: Simplified README and CLAUDE.md to reflect new API

### Fixed
- **Stack overflow prevention**: Dedicated threads with 8MB stack
  - Prevents crashes on complex diagrams
  - Worker processes use `ulimit -s unlimited` on Linux/macOS
- **Go panic recovery**: Native crash prevention in Go wrapper
  - Catches panics and returns errors instead of crashing
- **D2 v0.7.1 upgrade**: Adds theme 303 support (fixes #40)
  - Updated from D2 v0.6.3 to v0.7.1
  - Go wrapper uses standard library `log/slog` instead of `cdr.dev/slog`
  - Minimum Go version: 1.24

## [0.3.1] - 2025-10-04

### Changed
- **Documentation**: Removed emojis and AI-sounding language across all documentation
  - Removed emojis (checkmarks, hearts, etc.)
  - Simplified marketing language ("production-ready", "comprehensive", "extensive", etc.)
  - Removed redundant subject repetition and vague phrases
  - Made language more direct and natural throughout README, CONTRIBUTING, and other docs

## [0.3.0] - 2025-10-03

### Code Quality
- **Performance**: Cached `JsonSerializerOptions` instance to avoid repeated allocations
- **Modern C#**: Used `ArgumentNullException.ThrowIfNull` for cleaner null checks (3 locations)
- **Modern C#**: Used `ObjectDisposedException.ThrowIf` for cleaner disposal checks
- **Readability**: Simplified substring operations with range operators (`[..]` syntax)
- **Static Analysis**: Marked `GetLineContent` as static (doesn't access instance data)
- **Code Cleanup**: Removed unnecessary variable assignment in error parsing

### Added - Phase 5b: Full Observability Integration
- **Integrated Caching**: Automatic caching of successful renders
  - SHA256-based cache keys from script content and options
  - Configurable cache size (default 100 entries) and expiration (default 1 hour)
  - Caches only successful renders, errors always re-execute
  - Cache hits preserve original SVG but generate new diagnostic IDs
- **Integrated Distributed Tracing**: Activity spans for all render operations
  - Automatic Activity creation when telemetry is enabled
  - Activity tags include: script length, layout engine, theme, sketch mode, diagnostic ID, cache hits, result status
  - Full OpenTelemetry and Application Insights compatibility
  - Traces include both cache lookups and actual renders
- **Integrated Metrics**: EventCounter recording throughout render pipeline
  - Metrics recorded: render started, render completed, cache hits/misses
  - Real-time monitoring with dotnet-counters
  - Metrics include duration, success/failure, cache performance
- **Concurrency Control**: SemaphoreSlim-based limiting of concurrent renders
  - Configurable MaxConcurrentRenders (default 0 = unlimited)
  - Prevents resource exhaustion under load
  - Applied in RenderDiagramAsync method
- **Diagnostic IDs**: Unique correlation IDs for every render operation
  - Generated from Activity.Current?.Id or new Guid
  - Included in all log messages and Activity tags
  - Unique per render, even for cache hits
- **Enhanced RenderResult**: Converted to record type for immutability
  - Supports `with` expressions for clean property updates
  - FromCache and DiagnosticId properties fully integrated
  - Maintains backward compatibility

### Changed
- **D2Wrapper Constructor**: New overload accepting D2WrapperOptions
  - Original parameterless constructor still available
  - Original ILogger constructor chains to new overload
  - Full backward compatibility maintained
- **RenderDiagram Method**: Complete rewrite with observability integration
  - Diagnostic ID generation at method start
  - Activity span creation and tagging
  - Cache lookup before render
  - Metrics recording throughout execution
  - Core logic extracted to RenderDiagramCore for separation of concerns
  - Cache storage after successful render
- **RenderDiagramAsync Method**: Concurrency control integration
  - SemaphoreSlim-based throttling when MaxConcurrentRenders > 0
  - Delegates to synchronous RenderDiagram for actual work
  - Maintains cancellation token support
- **Dispose Method**: Enhanced cleanup
  - Disposes RenderCache if caching is enabled
  - Disposes SemaphoreSlim if concurrency limiting is enabled
  - Proper resource cleanup on wrapper disposal
- **RenderResult Type**: Changed from class to record
  - Enables `with` expressions for immutable updates
  - Better pattern for value-like semantics
  - No breaking changes (init-only properties maintained)

### Testing
- **New Test Suite**: D2WrapperObservabilityTests with 15 comprehensive tests
  - Constructor tests with options
  - Cache hit/miss scenarios
  - Diagnostic ID generation
  - Activity/telemetry integration
  - Concurrency limiting
  - Dispose cleanup verification
  - D2WrapperOptions default values
  - Record type with expressions
- **Total Test Count**: 59 tests (44 existing + 15 new)
- **Coverage**: 85.52% line coverage, 81.61% branch coverage, 100% method coverage

### Documentation
- **README Updates**: Complete observability documentation
  - Quick start guide with D2WrapperOptions
  - Automatic caching examples
  - Concurrency control examples
  - Distributed tracing setup with OpenTelemetry
  - Real-time metrics monitoring guide
  - Diagnostic ID usage patterns
  - Minimal configuration for development
- **Code Documentation**: Full XML documentation on all new members

### Performance
- **Cache Performance**: Instant cache hits (< 1ms) vs ~30-500ms renders
- **Concurrency Benefits**: Prevents thread exhaustion under load
- **Overhead**: Minimal when features disabled (<1% impact)

### Notes
- **Production Ready**: Full integration complete, ready for beta testing
- **Backward Compatible**: Existing code works without changes
- **Opt-In Features**: All observability features can be disabled via D2WrapperOptions
- **Next Phase**: Community feedback and stabilization for v0.3.0 release

## [0.3.0-alpha.1] - 2025-10-03

### Added - Phase 5a: Observability & Performance Infrastructure
- **Telemetry Infrastructure**: Foundation for distributed tracing and metrics
  - `D2SharpActivitySource` for OpenTelemetry/Application Insights integration
  - Activity tags for script length, layout engine, theme, diagnostic ID, cache hits
  - Compatible with .NET distributed tracing ecosystem
- **Performance Metrics**: Real-time monitoring with EventCounters
  - `D2SharpEventCounters` event source
  - Metrics: total renders, active renders, render duration, cache hit rate, error rate
  - Compatible with dotnet-counters, Application Insights, and custom monitoring
- **Render Caching**: In-memory cache with LRU eviction
  - `RenderCache` class with SHA256-based cache keys
  - Configurable size limits and expiration
  - Thread-safe operations
  - Automatic cache key generation from script + options
- **Configuration System**: Centralized options for D2Wrapper behavior
  - `D2WrapperOptions` class for cache, concurrency, and telemetry settings
  - Feature flags for enabling/disabling telemetry, metrics, caching
  - Configurable cache size, expiration, and concurrency limits
- **Enhanced Results**: Additional diagnostic capabilities
  - `DiagnosticId` property on RenderResult for log correlation
  - `FromCache` property to indicate cached results
  - Maintains full backward compatibility

### Infrastructure
- Added `Microsoft.Extensions.Caching.Memory` dependency
- Created `Telemetry/` namespace for observability components
- Created `Caching/` namespace for cache implementations

### Notes
- **Phase 5a** provides the infrastructure foundation
- **Phase 5b** (future) will integrate these components into D2Wrapper
- All new classes are production-ready but not yet wired into the main rendering pipeline

## [0.2.0-beta.2] - 2025-10-03

### Added - Phase 4: Code Quality, Coverage & Performance Baselines
- **Code Coverage**: Comprehensive test coverage reporting with Coverlet
  - Line coverage: 82.6%
  - Branch coverage: 75.5%
  - Method coverage: 97.4%
  - Coverage threshold enforcement (80% minimum)
- **Codecov Integration**: Automatic coverage reporting on all commits
  - Coverage badge in README
  - Per-platform coverage reports
  - codecov.yml configuration with quality gates
- **Performance Benchmarks**: BenchmarkDotNet benchmark suite
  - 9 comprehensive rendering benchmarks
  - Simple, complex, and very complex diagram scenarios
  - Layout engine comparison (Dagre vs ELK)
  - Render options overhead measurement
  - Async rendering benchmarks
  - Memory allocation profiling
- **Documentation**:
  - Coverage metrics in README
  - Performance benchmarks documentation
  - Benchmark running instructions
  - Performance targets and baselines

### Changed
- CI/CD workflow updated to collect and upload coverage
- Test project configured with coverage thresholds
- Excluded examples and benchmarks from coverage calculation

### Infrastructure
- Added benchmarks/D2Sharp.Benchmarks project to solution
- Added codecov.yml for coverage configuration
- Updated .gitignore for coverage artifacts

## [0.2.0-beta.1] - 2025-10-03

### Added - Phase 3: Render Options & NuGet Package Readiness
- **RenderOptions API**: Comprehensive rendering customization options
  - Layout engine selection: Dagre (default, fast) or ELK (sophisticated layouts)
  - Theme support: Access to 300+ built-in D2 themes via ThemeId
  - Dark theme support: DarkThemeId for automatic dark mode rendering
  - Sketch mode: Hand-drawn style diagrams
  - Visual customization: Pad, Scale, and Center options
  - Advanced options: Target, AnimateInterval, ForceAppendix
- **JSON-based options**: Serialization layer for C# to Go interop
- **Backward compatibility**: All RenderOptions parameters are optional
- **8 new unit tests**: Comprehensive test coverage for all render options
- **Production-ready README**: Complete documentation with installation, quick start, API reference, and examples
- **NuGet package metadata**: Enhanced package information including:
  - PackageId, Authors, comprehensive Description
  - PackageReadmeFile and PackageLicenseFile inclusion
  - Repository URL and project URL
  - Rich package tags for discoverability
  - Release notes reference

### Changed
- RenderDiagram methods now accept optional RenderOptions parameter
- Go wrapper updated to parse and apply JSON render options
- Updated all method signatures for RenderOptions support while maintaining backward compatibility

### Added - Phase 2: Critical Reliability & Production Hardening
- **Async/await support**: RenderDiagramAsync methods with CancellationToken support
- **Timeout protection**: Configurable rendering timeout (default: 30s, production: 15s) with validation (100ms min, 10min max)
- **IDisposable pattern**: Proper resource cleanup and disposal support with thread-safe implementation
- **Thread-safe disposal**: Uses Interlocked operations for concurrent Dispose() safety
- **Memory leak prevention**: Try-finally blocks ensure P/Invoke pointer cleanup even on exceptions
- **Null safety**: Added null checks for all Marshal.PtrToStringUTF8 calls
- **Native library error handling**: Catch and wrap DllNotFoundException and EntryPointNotFoundException with helpful messages
- **Input validation**: 10MB script length limit, timeout range validation
- **Immutable data models**: RenderResult and D2Error use init-only setters
- **Performance optimization**: GeneratedRegex for compilation error parsing (removes runtime regex compilation overhead)
- **Comprehensive async unit tests**: 15+ tests for async behavior, cancellation, timeout, and disposal
- **Integration tests**: WebApplicationFactory-based tests for API endpoints
- **Timeout exception handling**: 408 status code for timeouts, 499 for cancellations

### Added - Phase 1: Production Foundations
- Comprehensive XML documentation for all public APIs
- xUnit test project with unit tests for D2Wrapper, D2Error, and RenderResult
- CORS configuration with environment-specific settings
- Rate limiting middleware with configurable limits per environment
- Input validation with configurable max script length
- Request size limits (1MB default)
- Security headers middleware (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, HSTS)
- Swagger/OpenAPI documentation available at /api-docs
- Health check endpoints (/health, /health/ready, /health/live)
- Docker support with multi-stage Dockerfile
- docker-compose.yml for easy deployment
- Environment-specific appsettings files (Development, Production)
- Dependabot configuration for automated dependency updates
- CONTRIBUTING.md with contribution guidelines
- CHANGELOG.md to track project changes

### Changed
- **Web API now uses async rendering**: All /render endpoint calls are async with timeout protection
- **D2Wrapper implements IDisposable**: Better resource management
- Updated GitHub Actions to latest versions (checkout v4, setup-dotnet v4, setup-go v5, cache v4)
- Enhanced Program.cs with production-ready middleware pipeline
- Improved security posture with multiple layers of protection

### Fixed
- **Go wrapper error handling**: Properly handle errors from textmeasure.NewRuler()
- Added ArgumentNullException validation to RenderDiagram method
- Added ObjectDisposedException checks in all public methods

### Security
- Added rate limiting to prevent abuse
- Added security headers to protect against common vulnerabilities
- Configured CORS policies for controlled cross-origin access
- Implemented request size limits to prevent DoS attacks
- Non-root user in Docker container for improved security
- Timeout protection prevents resource exhaustion from long-running renders

### Performance
- Async rendering prevents thread pool starvation under load
- Configurable timeouts allow better resource management
- CancellationToken support enables graceful request cancellation

## [0.1.0-alpha.7]

### Added
- Basic D2 diagram rendering functionality
- Error handling with line and column information
- Web demo application
- Cross-platform support (Windows, macOS, Linux)
- CI/CD pipeline for automated builds

## [0.1.0-alpha.6]

### Added
- Initial release
- D2 diagram rendering as SVG
- Go wrapper for D2 library
- .NET 8.0 library

[Unreleased]: https://github.com/AlrikOlson/D2Sharp/compare/v0.4.2...HEAD
[0.4.2]: https://github.com/AlrikOlson/D2Sharp/compare/v0.4.1...v0.4.2
[0.4.1]: https://github.com/AlrikOlson/D2Sharp/compare/v0.4.0...v0.4.1
[0.4.0]: https://github.com/AlrikOlson/D2Sharp/compare/v0.3.1...v0.4.0
[0.3.1]: https://github.com/AlrikOlson/D2Sharp/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/AlrikOlson/D2Sharp/compare/v0.2.0-beta.2...v0.3.0
[0.3.0-alpha.1]: https://github.com/AlrikOlson/D2Sharp/compare/v0.2.0-beta.2...v0.3.0-alpha.1
[0.2.0-beta.2]: https://github.com/AlrikOlson/D2Sharp/compare/v0.2.0-beta.1...v0.2.0-beta.2
[0.2.0-beta.1]: https://github.com/AlrikOlson/D2Sharp/compare/v0.1.0-alpha.7...v0.2.0-beta.1
[0.1.0-alpha.7]: https://github.com/AlrikOlson/D2Sharp/releases/tag/v0.1.0-alpha.7
[0.1.0-alpha.6]: https://github.com/AlrikOlson/D2Sharp/releases/tag/v0.1.0-alpha.6
