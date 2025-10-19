using D2Sharp;
using D2Sharp.Builders;
using D2Sharp.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add D2Sharp rendering services with fluent configuration API
// Demonstrates process pool with 15 workers and caching enabled
builder.Services.AddD2Sharp(d2 => d2
    .UseProcessPool(pool => pool.WithWorkerCount(15))
    .ConfigureCaching(cache => cache.Enabled = true));

// Add health checks
builder.Services.AddHealthChecks();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "D2Sharp API",
        Version = "v1",
        Description = "API for rendering D2 diagrams as SVG",
        Contact = new OpenApiContact
        {
            Name = "D2Sharp Project",
            Url = new Uri("https://github.com/AlrikOlson/D2Sharp")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://github.com/AlrikOlson/D2Sharp/blob/main/LICENSE.txt")
        }
    });
});

// Configure rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 100),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimiting:WindowMinutes", 1))
            }));

    options.AddPolicy("RenderEndpoint", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:RenderEndpoint:PermitLimit", 10),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimiting:RenderEndpoint:WindowMinutes", 1))
            }));
});

// Configure CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow all origins
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // In production, use configured origins
            if (corsOrigins.Length > 0)
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // If no origins configured, allow none
                policy.WithOrigins()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        }
    });
});

var app = builder.Build();

// Enable Swagger in all environments (can be restricted to Development if needed)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "D2Sharp API v1");
    options.RoutePrefix = "api-docs"; // Access at /api-docs instead of root
});

// Add security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }

    await next();
});

// Use rate limiting - DISABLED for stress testing
//app.UseRateLimiter();

// Use CORS
app.UseCors("DefaultCorsPolicy");

// Enable serving static files and set default file
app.UseDefaultFiles();
app.UseStaticFiles();

// Map health check endpoints
app.MapHealthChecks("/health").WithTags("Health");
app.MapHealthChecks("/health/ready").WithTags("Health");
app.MapHealthChecks("/health/live").WithTags("Health");

app.MapPost("/render", async (HttpContext context, [FromBody] DiagramRequest request, D2Renderer renderer, CancellationToken cancellationToken) =>
{
    // Validate input
    if (string.IsNullOrEmpty(request.Script))
    {
        return Results.BadRequest("Script is required");
    }

    var config = context.RequestServices.GetRequiredService<IConfiguration>();
    var maxScriptLength = config.GetValue<int>("Validation:MaxScriptLength", 100000);
    var timeoutSeconds = config.GetValue<int>("Rendering:TimeoutSeconds", 30);

    if (request.Script.Length > maxScriptLength)
    {
        return Results.BadRequest($"Script exceeds maximum length of {maxScriptLength} characters");
    }

    try
    {
        // Use async rendering with automatic process isolation
        // Create a timeout cancellation token source
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        // Map request properties to RenderOptions
        RenderOptions? options = null;
        if (request.Layout.HasValue || request.ThemeId.HasValue || request.Sketch.HasValue ||
            request.Pad.HasValue || request.Scale.HasValue || request.Center.HasValue)
        {
            options = new RenderOptions
            {
                Layout = request.Layout.HasValue ? (LayoutEngine)request.Layout.Value : null,
                ThemeId = request.ThemeId,
                Sketch = request.Sketch,
                Pad = request.Pad,
                Scale = request.Scale,
                Center = request.Center
            };
        }

        var result = await renderer.RenderDiagramAsync(
            request.Script,
            options,
            timeoutCts.Token);

        if (result.IsSuccess)
        {
            return Results.Content(result.Svg, "image/svg+xml");
        }
        else
        {
            // Error is guaranteed non-null when IsSuccess is false
            var highlightedParts = result.Error!.GetHighlightedLineParts();
            var errorResponse = new
            {
                message = result.Error.Message,
                lineNumber = result.Error.LineNumber,
                column = result.Error.Column,
                lineContent = result.Error.LineContent,
                highlightedLineParts = new
                {
                    beforeError = highlightedParts.beforeError,
                    errorPart = highlightedParts.errorPart,
                    afterError = highlightedParts.afterError
                }
            };
            return Results.BadRequest(errorResponse);
        }
    }
    catch (TimeoutException ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status408RequestTimeout,
            title: "Rendering Timeout");
    }
    catch (OperationCanceledException)
    {
        return Results.Problem(
            detail: "Rendering was cancelled",
            statusCode: StatusCodes.Status499ClientClosedRequest,
            title: "Request Cancelled");
    }
})
//.RequireRateLimiting("RenderEndpoint")  // Temporarily disabled for testing
.WithName("RenderDiagram")
.WithTags("Diagram")
.WithSummary("Render a D2 diagram")
.WithDescription("Accepts a D2 diagram script and returns the rendered SVG. Rendering has a configurable timeout (default: 30 seconds).");

app.MapPost("/stress-test", async (D2Renderer renderer, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var results = new List<PhaseResult>();

    logger.LogInformation("Starting stress test");

    // Test diagrams
    var simpleDiagram = "A -> B -> C";
    var mediumDiagram = @"
frontend: {shape: hexagon}
backend: {shape: cylinder}
cache: {shape: stored_data}
frontend -> backend
backend -> cache
";
    var complexDiagram = @"
direction: right

system: {
  web: {
    lb: {shape: hexagon}
    app1: {shape: rectangle}
    app2: {shape: rectangle}
    app3: {shape: rectangle}
  }
  data: {
    primary: {shape: cylinder}
    replica1: {shape: cylinder}
    replica2: {shape: cylinder}
    cache: {shape: stored_data}
  }
  queue: {shape: queue}
}

user: {shape: person}
admin: {shape: person}

user -> system.web.lb
admin -> system.web.lb
system.web.lb -> system.web.app1
system.web.lb -> system.web.app2
system.web.lb -> system.web.app3
system.web.app1 -> system.data.primary
system.web.app2 -> system.data.primary
system.web.app3 -> system.data.primary
system.web.app1 -> system.data.cache
system.web.app2 -> system.data.cache
system.web.app3 -> system.data.cache
system.data.primary -> system.data.replica1
system.data.primary -> system.data.replica2
system.web.app1 -> system.queue
system.web.app2 -> system.queue
system.web.app3 -> system.queue
";

    // Phase 1: Warm up
    results.Add(await RunTestPhase("Phase 1: Warm up (simple)", simpleDiagram, 25, renderer, logger, cancellationToken));
    await Task.Delay(500, cancellationToken);

    // Phase 2: Moderate load
    results.Add(await RunTestPhase("Phase 2: Moderate load (medium)", mediumDiagram, 100, renderer, logger, cancellationToken));
    await Task.Delay(500, cancellationToken);

    // Phase 3: Heavy concurrent load
    results.Add(await RunTestPhase("Phase 3: Heavy load (complex)", complexDiagram, 200, renderer, logger, cancellationToken));
    await Task.Delay(1000, cancellationToken);

    // Phase 4: Sustained load (10 waves of 50 requests)
    var sustainedStart = System.Diagnostics.Stopwatch.StartNew();
    int sustainedSuccess = 0;
    int sustainedFailed = 0;

    for (int wave = 1; wave <= 10; wave++)
    {
        var waveTasks = new List<Task<RenderResult>>();
        for (int i = 0; i < 50; i++)
        {
            waveTasks.Add(renderer.RenderDiagramAsync(mediumDiagram, null, cancellationToken));
        }

        var waveResults = await Task.WhenAll(waveTasks);
        var waveSuccess = waveResults.Count(r => r.IsSuccess);
        sustainedSuccess += waveSuccess;
        sustainedFailed += waveResults.Length - waveSuccess;

        logger.LogInformation("Wave {Wave}/10: {Success}/50 success", wave, waveSuccess);
        await Task.Delay(200, cancellationToken);
    }
    sustainedStart.Stop();

    results.Add(new PhaseResult
    {
        Name = "Phase 4: Sustained load (500 requests over 10 waves)",
        TotalRequests = 500,
        SuccessCount = sustainedSuccess,
        FailedCount = sustainedFailed,
        DurationMs = sustainedStart.ElapsedMilliseconds
    });

    sw.Stop();

    // Calculate totals
    int totalRequests = results.Sum(r => r.TotalRequests);
    int totalSuccess = results.Sum(r => r.SuccessCount);
    int totalFailed = results.Sum(r => r.FailedCount);
    double totalRate = totalRequests > 0 ? (totalSuccess * 100.0) / totalRequests : 0;

    var summary = new StressTestResult
    {
        TotalRequests = totalRequests,
        TotalSuccess = totalSuccess,
        TotalFailed = totalFailed,
        SuccessRate = totalRate,
        TotalDurationMs = sw.ElapsedMilliseconds,
        Phases = results,
        Rating = totalRate >= 95 ? "EXCELLENT" :
                 totalRate >= 85 ? "GOOD" :
                 totalRate >= 70 ? "ACCEPTABLE" : "POOR"
    };

    logger.LogInformation("Stress test completed: {Success}/{Total} successful ({Rate:F1}%)", totalSuccess, totalRequests, totalRate);

    return Results.Ok(summary);
})
.WithName("StressTest")
.WithTags("Testing")
.WithSummary("Run a comprehensive stress test")
.WithDescription("Runs 825 rendering requests across multiple phases to test system stability and performance.");

app.Run();

async Task<PhaseResult> RunTestPhase(string name, string diagram, int count, D2Renderer renderer, ILogger logger, CancellationToken cancellationToken)
{
    logger.LogInformation("Starting {Phase} ({Count} concurrent requests)", name, count);

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var tasks = new List<Task<RenderResult>>();

    for (int i = 0; i < count; i++)
    {
        tasks.Add(renderer.RenderDiagramAsync(diagram, null, cancellationToken));
    }

    var phaseResults = await Task.WhenAll(tasks);
    sw.Stop();

    var success = phaseResults.Count(r => r.IsSuccess);
    var failed = phaseResults.Count(r => !r.IsSuccess);

    logger.LogInformation("{Phase}: {Success}/{Count} successful ({Rate:F1}%) in {Duration}ms",
        name, success, count, (success * 100.0) / count, sw.ElapsedMilliseconds);

    return new PhaseResult
    {
        Name = name,
        TotalRequests = count,
        SuccessCount = success,
        FailedCount = failed,
        DurationMs = sw.ElapsedMilliseconds
    };
}

public class DiagramRequest
{
    public string Script { get; set; } = "";
    public int? Layout { get; set; }      // 0=Dagre, 1=Elk
    public int? ThemeId { get; set; }
    public bool? Sketch { get; set; }
    public int? Pad { get; set; }
    public double? Scale { get; set; }
    public bool? Center { get; set; }
}

public class PhaseResult
{
    public string Name { get; set; } = "";
    public int TotalRequests { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public long DurationMs { get; set; }
    public double SuccessRate => TotalRequests > 0 ? (SuccessCount * 100.0) / TotalRequests : 0;
    public double AvgTimeMs => TotalRequests > 0 ? DurationMs / (double)TotalRequests : 0;
}

public class StressTestResult
{
    public int TotalRequests { get; set; }
    public int TotalSuccess { get; set; }
    public int TotalFailed { get; set; }
    public double SuccessRate { get; set; }
    public long TotalDurationMs { get; set; }
    public List<PhaseResult> Phases { get; set; } = new();
    public string Rating { get; set; } = "";
}
