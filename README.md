# D2Sharp

[![NuGet](https://img.shields.io/nuget/v/D2Sharp.svg)](https://www.nuget.org/packages/D2Sharp/)

D2Sharp wraps the D2 diagramming library for .NET, allowing you to render D2 diagrams with C# in your .NET applications.

## Features

- Render D2 diagrams as SVG
- Integrate with ASP.NET Core for web applications
- Comprehensive error handling with line/column information
- Production-ready web API with:
  - Rate limiting to prevent abuse
  - CORS configuration with environment-based policies
  - Security headers (HSTS, X-Frame-Options, etc.)
  - Input validation and request size limits
  - Swagger/OpenAPI documentation
  - Health check endpoints
- Docker support with multi-stage builds
- Comprehensive test suite
- Full XML API documentation

## Prerequisites for Building

- .NET 8.0 SDK or newer
- Go 1.22.2 or newer
- GCC (for compiling the Go wrapper)

You can check your setup using the provided scripts:

Windows:
```powershell
.\depcheck.ps1
```

Unix-based systems:
```bash
./depcheck.sh
```

## Project Structure

- `src/D2Sharp`: Main library project
- `examples/D2Sharp.Web`: Web demo application
- `src/D2Sharp/d2wrapper`: Go wrapper code

## Setup

1. Clone the repository
2. Build the project: `dotnet build`

## Usage

Basic usage:

```csharp
// Create an instance of D2Wrapper
// You can pass a logger instance if you want to enable logging
var wrapper = new D2Wrapper(logger);

// Define your D2 script as a string
var script = @"direction: right
A -> B -> C";

// Render the diagram
var svg = wrapper.RenderDiagram(script);

// The 'svg' variable now contains the SVG representation of your diagram
// You can save this to a file, display it in a web page, or process it further as needed
```

## Error Handling

The `RenderDiagram` method now returns a `RenderResult` object, which includes both the rendered SVG (if successful) and detailed error information (if rendering failed). Here's how you can use it:

```csharp
var wrapper = new D2Wrapper();
string script = @"
A -> B
B ->  // This line has an error
C -> D
";

var result = wrapper.RenderDiagram(script);

if (result.IsSuccess)
{
    Console.WriteLine("Diagram rendered successfully:");
    Console.WriteLine(result.Svg);
}
else
{
    Console.WriteLine("Error rendering diagram:");
    Console.WriteLine($"Message: {result.Error.Message}");
    if (result.Error.LineNumber.HasValue)
    {
        Console.WriteLine($"Line {result.Error.LineNumber}: {result.Error.LineContent}");
    }
}
```

## Running the Web Demo

### With .NET CLI

```bash
cd examples/D2Sharp.Web
dotnet run
```

Then visit:
- Application: http://localhost:5044
- API Documentation: http://localhost:5044/api-docs
- Health Check: http://localhost:5044/health

### With Docker

```bash
# Build and run with docker-compose
docker-compose up --build

# Or build and run manually
docker build -t d2sharp .
docker run -p 8080:8080 d2sharp
```

Then visit:
- Application: http://localhost:8080
- API Documentation: http://localhost:8080/api-docs
- Health Check: http://localhost:8080/health

## API Documentation

The web API includes Swagger/OpenAPI documentation. When running the application, navigate to `/api-docs` to see the interactive API documentation.

### Endpoints

- `POST /render` - Render a D2 diagram script and return SVG
- `GET /health` - Health check endpoint
- `GET /health/ready` - Readiness check endpoint
- `GET /health/live` - Liveness check endpoint

## Security Features

The web demo includes several security features:

- **Rate Limiting**: Configurable per-endpoint rate limits
- **CORS**: Environment-based CORS policies
- **Security Headers**: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, HSTS
- **Input Validation**: Script length limits and request size restrictions
- **Non-root Docker**: Container runs as non-root user

## Configuration

Configuration is managed through `appsettings.json` with environment-specific overrides. Key settings:

- `RateLimiting:PermitLimit` - Global rate limit
- `RateLimiting:RenderEndpoint:PermitLimit` - Render endpoint rate limit
- `Validation:MaxScriptLength` - Maximum script length in characters
- `Cors:AllowedOrigins` - Allowed CORS origins (Production only)

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## Acknowledgements

This project would not be possible without the following open-source projects:

- [D2](https://github.com/terrastruct/d2): The underlying diagramming engine
- [.NET](https://github.com/dotnet/runtime): The runtime and framework
- [Go](https://github.com/golang/go): Used for the native wrapper
