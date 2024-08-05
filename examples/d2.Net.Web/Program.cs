using d2.Net;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<D2Wrapper>();

var app = builder.Build();

// Enable serving static files and set default file
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/render", async (HttpContext context, [FromBody] DiagramRequest request, D2Wrapper d2Wrapper) =>
{
    if (string.IsNullOrEmpty(request.Script))
    {
        return Results.BadRequest("Script is required");
    }

    try
    {
        var svg = d2Wrapper.RenderDiagram(request.Script);
        return Results.Content(svg, "image/svg+xml");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error rendering diagram: {ex.Message}");
    }
});

app.Run();

public class DiagramRequest
{
    public string Script { get; set; } = "";
}
