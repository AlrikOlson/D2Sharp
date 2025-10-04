using D2Sharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<D2Wrapper>();

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

// Use rate limiting
app.UseRateLimiter();

// Use CORS
app.UseCors("DefaultCorsPolicy");

// Enable serving static files and set default file
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/render", async (HttpContext context, [FromBody] DiagramRequest request, D2Wrapper d2Wrapper) =>
{
    // Validate input
    if (string.IsNullOrEmpty(request.Script))
    {
        return Results.BadRequest("Script is required");
    }

    var maxScriptLength = context.RequestServices.GetRequiredService<IConfiguration>()
        .GetValue<int>("Validation:MaxScriptLength", 100000);

    if (request.Script.Length > maxScriptLength)
    {
        return Results.BadRequest($"Script exceeds maximum length of {maxScriptLength} characters");
    }

    var result = d2Wrapper.RenderDiagram(request.Script);

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
}).RequireRateLimiting("RenderEndpoint");

app.Run();

public class DiagramRequest
{
    public string Script { get; set; } = "";
}
