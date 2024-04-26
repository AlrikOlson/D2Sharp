using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace d2.Net.ConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger<D2Wrapper>();
        var wrapper = new D2Wrapper(logger);

        var script = D2Scripts.Simple;

        logger.LogInformation("Rendering diagram...");
        var svg = wrapper.RenderDiagram(script, D2Theme.Aubergine);

        if (svg != null)
        {
            var fileName = $"{Guid.NewGuid()}.svg";
            var filePath = Path.Combine(Environment.CurrentDirectory, fileName);

            logger.LogInformation($"Diagram rendered successfully. Saving to file: {fileName}");
            File.WriteAllText(filePath, svg);

            logger.LogInformation($"Opening the diagram in the default viewer...");
            OpenInDefaultViewer(filePath);
        }
        else
        {
            logger.LogError("Failed to render diagram");
        }
    }

    static void OpenInDefaultViewer(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening the file: {ex.Message}");
        }
    }
}
