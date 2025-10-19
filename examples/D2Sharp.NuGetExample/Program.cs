using D2Sharp;

Console.WriteLine("D2Sharp NuGet Package Example");
Console.WriteLine("==============================\n");

// Example 1: Simple diagram rendering
Console.WriteLine("Example 1: Simple diagram");
using (var renderer = new D2Renderer())
{
    var script = "A -> B -> C";
    var result = await renderer.RenderDiagramAsync(script);

    if (result.IsSuccess)
    {
        await File.WriteAllTextAsync("simple.svg", result.Svg);
        Console.WriteLine("✓ Rendered simple diagram to: simple.svg");
    }
    else
    {
        Console.WriteLine($"✗ Error: {result.Error?.Message}");
    }
}

Console.WriteLine();

// Example 2: Diagram with custom options (dark theme, sketch mode)
Console.WriteLine("Example 2: Diagram with dark theme and sketch mode");
using (var renderer = new D2Renderer())
{
    var script = @"
direction: right
frontend: {shape: hexagon}
backend: {shape: cylinder}
cache: {shape: stored_data}

frontend -> backend: API calls
backend -> cache: queries
";

    var options = new RenderOptions
    {
        ThemeId = 200,  // Dark theme
        Sketch = true,  // Hand-drawn style
        Pad = 20
    };

    var result = await renderer.RenderDiagramAsync(script, options);

    if (result.IsSuccess)
    {
        await File.WriteAllTextAsync("themed.svg", result.Svg);
        Console.WriteLine("✓ Rendered themed diagram to: themed.svg");
    }
    else
    {
        Console.WriteLine($"✗ Error: {result.Error?.Message}");
    }
}

Console.WriteLine();

// Example 3: Complex diagram with ELK layout
Console.WriteLine("Example 3: Complex system diagram with ELK layout");
using (var renderer = new D2Renderer())
{
    var script = @"
system: {
  web: {
    lb: Load Balancer {shape: hexagon}
    app1: App Server 1 {shape: rectangle}
    app2: App Server 2 {shape: rectangle}
  }
  data: {
    primary: Primary DB {shape: cylinder}
    cache: Redis Cache {shape: stored_data}
  }
  queue: Message Queue {shape: queue}
}

user: User {shape: person}

user -> system.web.lb
system.web.lb -> system.web.app1
system.web.lb -> system.web.app2
system.web.app1 -> system.data.primary
system.web.app2 -> system.data.primary
system.web.app1 -> system.data.cache
system.web.app2 -> system.data.cache
system.web.app1 -> system.queue
system.web.app2 -> system.queue
";

    var options = new RenderOptions
    {
        Layout = LayoutEngine.Elk,
        Scale = 1.2,
        Center = true
    };

    var result = await renderer.RenderDiagramAsync(script, options);

    if (result.IsSuccess)
    {
        await File.WriteAllTextAsync("complex.svg", result.Svg);
        Console.WriteLine("✓ Rendered complex diagram to: complex.svg");
    }
    else
    {
        Console.WriteLine($"✗ Error: {result.Error?.Message}");
    }
}

Console.WriteLine();

// Example 4: Error handling
Console.WriteLine("Example 4: Handling D2 compilation errors");
using (var renderer = new D2Renderer())
{
    var script = "A -> B -> C ->";  // Invalid syntax (trailing arrow)
    var result = await renderer.RenderDiagramAsync(script);

    if (result.IsSuccess)
    {
        Console.WriteLine("✓ Unexpectedly succeeded");
    }
    else
    {
        var error = result.Error!;
        Console.WriteLine($"✗ Expected error caught:");
        Console.WriteLine($"   Message: {error.Message}");
        if (error.LineNumber.HasValue)
        {
            Console.WriteLine($"   Location: Line {error.LineNumber}, Column {error.Column}");
            Console.WriteLine($"   Line content: {error.LineContent}");
        }
    }
}

Console.WriteLine();

// Example 5: Custom worker pool configuration
Console.WriteLine("Example 5: Custom worker pool (10 workers for high concurrency)");
using (var renderer = new D2Renderer(workerCount: 10))
{
    var script = "database -> api -> frontend";
    var result = await renderer.RenderDiagramAsync(script);

    if (result.IsSuccess)
    {
        await File.WriteAllTextAsync("pool-example.svg", result.Svg);
        Console.WriteLine("✓ Rendered with custom worker pool to: pool-example.svg");
    }
    else
    {
        Console.WriteLine($"✗ Error: {result.Error?.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("All examples completed! Check the generated SVG files.");
Console.WriteLine("\nTip: Open the .svg files in a web browser to view the diagrams.");
