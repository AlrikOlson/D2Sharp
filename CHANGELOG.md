# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/AlrikOlson/D2Sharp/compare/v0.2.0-beta.1...HEAD
[0.2.0-beta.1]: https://github.com/AlrikOlson/D2Sharp/compare/v0.1.0-alpha.7...v0.2.0-beta.1
[0.1.0-alpha.7]: https://github.com/AlrikOlson/D2Sharp/releases/tag/v0.1.0-alpha.7
[0.1.0-alpha.6]: https://github.com/AlrikOlson/D2Sharp/releases/tag/v0.1.0-alpha.6
