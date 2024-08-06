using D2Sharp;
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
});

app.Run();

public class DiagramRequest
{
    public string Script { get; set; } = "";
}
