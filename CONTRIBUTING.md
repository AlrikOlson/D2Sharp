# Contributing to D2Sharp

Thank you for considering contributing to D2Sharp! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

This project follows a standard code of conduct. Please be respectful and constructive in all interactions.

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue on GitHub with:
- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior
- Actual behavior
- Your environment (OS, .NET version, Go version)
- Code samples or error messages if applicable

### Suggesting Enhancements

Enhancement suggestions are welcome! Please create an issue describing:
- The enhancement you'd like to see
- Why this enhancement would be useful
- Any implementation ideas you have

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Make your changes** following the coding standards below
3. **Add tests** if you've added new functionality
4. **Update documentation** if you've changed APIs
5. **Ensure tests pass** by running `dotnet test`
6. **Commit your changes** with clear, descriptive commit messages
7. **Push to your fork** and submit a pull request

## Development Setup

### Prerequisites

- .NET 8.0 SDK or newer
- Go 1.22.2 or newer
- GCC (for compiling the Go wrapper)

You can check your setup using:
```bash
./depcheck.sh    # Unix-based systems
.\depcheck.ps1   # Windows
```

### Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test
```

### Project Structure

- `src/D2Sharp` - Main library project
- `src/D2Sharp/d2wrapper` - Go wrapper code for D2
- `tests/D2Sharp.Tests` - Unit and integration tests
- `examples/D2Sharp.Web` - Web demo application
- `benchmarks/D2Sharp.Benchmarks` - Performance benchmarks
- `tools/D2Sharp.DiagramCLI` - CLI testing tool

## Coding Standards

### C# Code Style

- Follow standard C# naming conventions
- Use meaningful variable and method names
- Add XML documentation comments to all public APIs
- Keep methods focused and single-purpose
- Prefer explicit types over `var` for complex types

### Go Code Style

- Follow standard Go formatting (`gofmt`)
- Keep functions small and focused
- Use descriptive variable names
- Add comments for exported functions

### Commit Message Guidelines

Write clear, descriptive commit messages:

```
Short summary (50 characters or less)

More detailed explanation if needed. Wrap at 72 characters.
Explain what and why, not how.

- Bullet points are okay
- Use present tense ("Add feature" not "Added feature")
- Reference issues and pull requests
```

## Testing

- Write unit tests for new functionality
- Ensure all tests pass before submitting PR
- Aim for good test coverage of critical paths
- Test edge cases and error conditions

## Documentation

- Update README.md if you change functionality
- Add XML documentation to public C# APIs
- Update example code if APIs change
- Document breaking changes in PR description

## License

By contributing to D2Sharp, you agree that your contributions will be licensed under the MIT License.

## Questions?

Feel free to create an issue if you have questions about contributing!
