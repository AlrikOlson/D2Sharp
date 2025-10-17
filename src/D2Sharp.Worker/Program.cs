using D2Sharp.Internal;
using System.Text.Json;
using D2Sharp;

// Worker process that handles D2 rendering in isolation
// Communicates via stdin/stdout using JSON protocol
// If this crashes, parent can spawn a new worker

var wrapper = new D2Wrapper();

try
{
    // Read requests from stdin, one JSON object per line
    while (Console.In.Peek() != -1)
    {
        var line = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(line))
            continue;

        try
        {
            var request = JsonSerializer.Deserialize<RenderRequest>(line);
            if (request == null)
            {
                WriteError("Invalid request format");
                continue;
            }

            // Perform the render
            var result = wrapper.RenderDiagram(request.Script, request.Options);

            // Write response
            var response = new RenderResponse
            {
                Svg = result.Svg,
                Error = result.Error?.Message,
                LineNumber = result.Error?.LineNumber,
                Column = result.Error?.Column,
                LineContent = result.Error?.LineContent
            };

            Console.WriteLine(JsonSerializer.Serialize(response));
            Console.Out.Flush();
        }
        catch (Exception ex)
        {
            WriteError($"Render exception: {ex.Message}");
        }
    }
}
finally
{
    wrapper.Dispose();
}

static void WriteError(string message)
{
    var response = new RenderResponse { Error = message };
    Console.WriteLine(JsonSerializer.Serialize(response));
    Console.Out.Flush();
}

// Request/Response DTOs
record RenderRequest(string Script, RenderOptions? Options);

record RenderResponse
{
    public string? Svg { get; init; }
    public string? Error { get; init; }
    public int? LineNumber { get; init; }
    public int? Column { get; init; }
    public string? LineContent { get; init; }
}
