using D2Sharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<D2Wrapper>();

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
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    await next();
});

// Use rate limiting
app.UseRateLimiter();

// Use CORS
app.UseCors("DefaultCorsPolicy");

// Enable serving static files and set default file
app.UseDefaultFiles();
app.UseStaticFiles();

// Map health check endpoints
app.MapHealthChecks("/health").WithTags("Health");
app.MapHealthChecks("/health/ready").WithTags("Health");
app.MapHealthChecks("/health/live").WithTags("Health");

app.MapPost("/render", async (HttpContext context, [FromBody] DiagramRequest request, D2Wrapper d2Wrapper, CancellationToken cancellationToken) =>
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
        // Use async rendering with timeout
        var result = await d2Wrapper.RenderDiagramAsync(
            request.Script,
            TimeSpan.FromSeconds(timeoutSeconds),
            options: null,
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Content(result.Svg, "image/svg+xml");
        }
        else
        {
            var highlightedParts = result.Error.GetHighlightedLineParts();
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
.RequireRateLimiting("RenderEndpoint")
.WithName("RenderDiagram")
.WithTags("Diagram")
.WithSummary("Render a D2 diagram")
.WithDescription("Accepts a D2 diagram script and returns the rendered SVG. Rendering has a configurable timeout (default: 30 seconds).");

app.Run();

public class DiagramRequest
{
    public string Script { get; set; } = "";
}
