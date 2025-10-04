using D2Sharp;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace D2Sharp.DiagramCLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("D2Sharp Diagram CLI - Real-world Package Test");
        Console.WriteLine("==============================================\n");

        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLower();

        return command switch
        {
            "render" => await RenderCommand(args),
            "batch" => await BatchCommand(args),
            "watch" => await WatchCommand(args),
            "arch" => await ArchitectureCommand(args),
            "analyze" => await AnalyzeSolutionCommand(args),
            "benchmark" => await BenchmarkCommand(args),
            "help" or "--help" or "-h" => ShowHelp(),
            _ => InvalidCommand(command)
        };
    }

    static int ShowHelp()
    {
        Console.WriteLine("Usage: D2Sharp.DiagramCLI <command> [options]");
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  render <file>          Render a single .d2 file to SVG");
        Console.WriteLine("  batch <directory>      Render all .d2 files in a directory");
        Console.WriteLine("  watch <file|dir>       Watch and auto-regenerate on changes");
        Console.WriteLine("  arch <config.json>     Generate architecture diagram from JSON");
        Console.WriteLine("  analyze <solution.sln> Analyze .NET solution and generate dependency graph");
        Console.WriteLine("  benchmark              Run performance benchmarks");
        Console.WriteLine("  help                   Show this help message");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  --theme <id>           Theme ID (default: 0)");
        Console.WriteLine("  --layout <dagre|elk>   Layout engine (default: dagre)");
        Console.WriteLine("  --sketch               Enable sketch mode");
        Console.WriteLine("  --output <path>        Output directory (default: ./output)");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  D2Sharp.DiagramCLI render diagram.d2");
        Console.WriteLine("  D2Sharp.DiagramCLI batch ./diagrams --theme 1 --layout elk");
        Console.WriteLine("  D2Sharp.DiagramCLI watch diagram.d2 --sketch");
        Console.WriteLine("  D2Sharp.DiagramCLI arch system-config.json --output ./diagrams");
        Console.WriteLine("  D2Sharp.DiagramCLI analyze MyProject.sln --layout elk");
        return 0;
    }

    static int InvalidCommand(string command)
    {
        Console.WriteLine($"Error: Unknown command '{command}'");
        Console.WriteLine("Run 'D2Sharp.DiagramCLI help' for usage information.");
        return 1;
    }

    static async Task<int> RenderCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Please specify a .d2 file to render");
            return 1;
        }

        var inputFile = args[1];
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: File not found: {inputFile}");
            return 1;
        }

        var options = ParseRenderOptions(args);
        var outputDir = GetOutputDir(args);
        Directory.CreateDirectory(outputDir);

        Console.WriteLine($"Rendering: {inputFile}");
        Console.WriteLine($"Theme: {options.ThemeId}, Layout: {options.Layout}, Sketch: {options.Sketch}");

        var wrapperOptions = new D2WrapperOptions
        {
            EnableCaching = true,
            EnableDiagnosticIds = true,
            EnableTelemetry = true
        };

        using var wrapper = new D2Wrapper(wrapperOptions);
        var script = await File.ReadAllTextAsync(inputFile);

        var sw = Stopwatch.StartNew();
        var result = await wrapper.RenderDiagramAsync(script, timeout: TimeSpan.FromSeconds(30), options: options);
        sw.Stop();

        if (result.IsSuccess)
        {
            var outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(inputFile) + ".svg");
            await File.WriteAllTextAsync(outputFile, result.Svg);

            Console.WriteLine($"✓ Success! Rendered in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Output: {outputFile}");
            Console.WriteLine($"  Diagnostic ID: {result.DiagnosticId}");
            Console.WriteLine($"  From Cache: {result.FromCache}");
            Console.WriteLine($"  SVG Size: {result.Svg.Length:N0} bytes");
            return 0;
        }
        else
        {
            Console.WriteLine($"✗ Error: {result.Error.Message}");
            if (result.Error.LineNumber > 0)
            {
                Console.WriteLine($"  Line {result.Error.LineNumber}, Column {result.Error.Column}");
                Console.WriteLine($"  {result.Error.LineContent}");
            }
            return 1;
        }
    }

    static async Task<int> BatchCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Please specify a directory containing .d2 files");
            return 1;
        }

        var inputDir = args[1];
        if (!Directory.Exists(inputDir))
        {
            Console.WriteLine($"Error: Directory not found: {inputDir}");
            return 1;
        }

        var options = ParseRenderOptions(args);
        var outputDir = GetOutputDir(args);
        Directory.CreateDirectory(outputDir);

        var d2Files = Directory.GetFiles(inputDir, "*.d2", SearchOption.AllDirectories);
        Console.WriteLine($"Found {d2Files.Length} .d2 files");
        Console.WriteLine($"Output directory: {outputDir}\n");

        var wrapperOptions = new D2WrapperOptions
        {
            EnableCaching = true,
            EnableDiagnosticIds = true,
            MaxConcurrentRenders = 5  // Test concurrency control
        };

        using var wrapper = new D2Wrapper(wrapperOptions);
        var tasks = new List<Task<(string file, bool success, long ms)>>();

        var overallSw = Stopwatch.StartNew();

        foreach (var d2File in d2Files)
        {
            tasks.Add(Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var script = await File.ReadAllTextAsync(d2File);
                    var result = await wrapper.RenderDiagramAsync(script, options: options);

                    if (result.IsSuccess)
                    {
                        var relativePath = Path.GetRelativePath(inputDir, d2File);
                        var outputFile = Path.Combine(outputDir, Path.ChangeExtension(relativePath, ".svg"));
                        var outputSubDir = Path.GetDirectoryName(outputFile);
                        if (!string.IsNullOrEmpty(outputSubDir))
                        {
                            Directory.CreateDirectory(outputSubDir);
                        }
                        await File.WriteAllTextAsync(outputFile, result.Svg);
                        sw.Stop();
                        return (d2File, true, sw.ElapsedMilliseconds);
                    }
                    else
                    {
                        sw.Stop();
                        return (d2File, false, sw.ElapsedMilliseconds);
                    }
                }
                catch
                {
                    sw.Stop();
                    return (d2File, false, sw.ElapsedMilliseconds);
                }
            }));
        }

        var results = await Task.WhenAll(tasks);
        overallSw.Stop();

        var successful = results.Count(r => r.success);
        var failed = results.Length - successful;
        var avgTime = results.Average(r => r.ms);

        Console.WriteLine($"\n✓ Batch rendering complete!");
        Console.WriteLine($"  Total: {results.Length} files");
        Console.WriteLine($"  Success: {successful}");
        Console.WriteLine($"  Failed: {failed}");
        Console.WriteLine($"  Total time: {overallSw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  Average time: {avgTime:F1}ms per file");
        Console.WriteLine($"  Throughput: {(results.Length / overallSw.Elapsed.TotalSeconds):F1} files/sec");

        return failed > 0 ? 1 : 0;
    }

    static async Task<int> WatchCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Please specify a .d2 file or directory to watch");
            return 1;
        }

        var watchPath = args[1];
        var isFile = File.Exists(watchPath);
        var isDirectory = Directory.Exists(watchPath);

        if (!isFile && !isDirectory)
        {
            Console.WriteLine($"Error: Path not found: {watchPath}");
            return 1;
        }

        var options = ParseRenderOptions(args);
        var outputDir = GetOutputDir(args);
        Directory.CreateDirectory(outputDir);

        var wrapperOptions = new D2WrapperOptions
        {
            EnableCaching = true,
            EnableDiagnosticIds = true
        };

        using var wrapper = new D2Wrapper(wrapperOptions);

        Console.WriteLine($"👀 Watching: {watchPath}");
        Console.WriteLine("Press Ctrl+C to stop...\n");

        using var watcher = new FileSystemWatcher
        {
            Path = isFile ? Path.GetDirectoryName(watchPath)! : watchPath,
            Filter = isFile ? Path.GetFileName(watchPath) : "*.d2",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = !isFile
        };

        var renderFile = async (string filePath) =>
        {
            try
            {
                await Task.Delay(100); // Debounce
                var script = await File.ReadAllTextAsync(filePath);
                var result = await wrapper.RenderDiagramAsync(script, options: options);

                if (result.IsSuccess)
                {
                    var outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(filePath) + ".svg");
                    await File.WriteAllTextAsync(outputFile, result.Svg);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✓ Rendered {Path.GetFileName(filePath)} (from cache: {result.FromCache})");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ Error in {Path.GetFileName(filePath)}: {result.Error.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ Exception: {ex.Message}");
            }
        };

        watcher.Changed += async (s, e) => await renderFile(e.FullPath);
        watcher.Created += async (s, e) => await renderFile(e.FullPath);

        watcher.EnableRaisingEvents = true;

        // Initial render
        if (isFile)
        {
            await renderFile(watchPath);
        }
        else
        {
            foreach (var file in Directory.GetFiles(watchPath, "*.d2"))
            {
                await renderFile(file);
            }
        }

        // Keep running until Ctrl+C
        var tcs = new TaskCompletionSource<bool>();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            tcs.SetResult(true);
        };

        await tcs.Task;
        Console.WriteLine("\n👋 Stopping watch mode...");
        return 0;
    }

    static async Task<int> ArchitectureCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Please specify a JSON configuration file");
            return 1;
        }

        var configFile = args[1];
        if (!File.Exists(configFile))
        {
            Console.WriteLine($"Error: File not found: {configFile}");
            return 1;
        }

        var outputDir = GetOutputDir(args);
        Directory.CreateDirectory(outputDir);

        var json = await File.ReadAllTextAsync(configFile);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var config = JsonSerializer.Deserialize<ArchitectureConfig>(json, jsonOptions);

        if (config == null)
        {
            Console.WriteLine("Error: Invalid configuration file");
            return 1;
        }

        Console.WriteLine($"Generating architecture diagram: {config.Name}");

        var d2Script = GenerateArchitectureDiagram(config);

        var options = ParseRenderOptions(args);
        var wrapperOptions = new D2WrapperOptions
        {
            EnableCaching = false,  // Fresh render for architecture
            EnableDiagnosticIds = true
        };

        using var wrapper = new D2Wrapper(wrapperOptions);
        var result = await wrapper.RenderDiagramAsync(d2Script, options: options);

        if (result.IsSuccess)
        {
            var outputFile = Path.Combine(outputDir, $"{config.Name.Replace(" ", "-").ToLower()}.svg");
            await File.WriteAllTextAsync(outputFile, result.Svg);

            var d2OutputFile = Path.Combine(outputDir, $"{config.Name.Replace(" ", "-").ToLower()}.d2");
            await File.WriteAllTextAsync(d2OutputFile, d2Script);

            Console.WriteLine($"✓ Architecture diagram generated!");
            Console.WriteLine($"  SVG: {outputFile}");
            Console.WriteLine($"  D2 Script: {d2OutputFile}");
            return 0;
        }
        else
        {
            Console.WriteLine($"✗ Error: {result.Error.Message}");
            return 1;
        }
    }

    static async Task<int> BenchmarkCommand(string[] args)
    {
        Console.WriteLine("Running D2Sharp performance benchmarks...\n");

        var wrapperOptions = new D2WrapperOptions
        {
            EnableCaching = true,
            EnableDiagnosticIds = true,
            EnableMetrics = true
        };

        using var wrapper = new D2Wrapper(wrapperOptions);

        // Benchmark 1: Simple diagrams
        Console.WriteLine("📊 Benchmark 1: Simple diagrams (A -> B)");
        var simpleTimes = new List<long>();
        for (int i = 0; i < 10; i++)
        {
            var sw = Stopwatch.StartNew();
            var result = await wrapper.RenderDiagramAsync("A -> B -> C");
            sw.Stop();
            simpleTimes.Add(sw.ElapsedMilliseconds);
        }
        Console.WriteLine($"   Average: {simpleTimes.Average():F1}ms, Min: {simpleTimes.Min()}ms, Max: {simpleTimes.Max()}ms");
        Console.WriteLine($"   First render: {simpleTimes[0]}ms, Cache hits: {simpleTimes.Skip(1).Average():F1}ms\n");

        // Benchmark 2: Complex diagrams
        Console.WriteLine("📊 Benchmark 2: Complex diagrams (20 nodes)");
        var complexScript = GenerateComplexDiagram(20);
        var complexTimes = new List<long>();
        for (int i = 0; i < 5; i++)
        {
            var sw = Stopwatch.StartNew();
            var result = await wrapper.RenderDiagramAsync(complexScript);
            sw.Stop();
            complexTimes.Add(sw.ElapsedMilliseconds);
        }
        Console.WriteLine($"   Average: {complexTimes.Average():F1}ms, Min: {complexTimes.Min()}ms, Max: {complexTimes.Max()}ms\n");

        // Benchmark 3: Different themes
        Console.WriteLine("📊 Benchmark 3: Theme rendering (same script, different themes)");
        var themeTimes = new List<long>();
        for (int themeId = 0; themeId < 5; themeId++)
        {
            var sw = Stopwatch.StartNew();
            var result = await wrapper.RenderDiagramAsync(
                "server -> database -> cache",
                options: new RenderOptions { ThemeId = themeId }
            );
            sw.Stop();
            themeTimes.Add(sw.ElapsedMilliseconds);
            Console.WriteLine($"   Theme {themeId}: {sw.ElapsedMilliseconds}ms");
        }
        Console.WriteLine($"   Average: {themeTimes.Average():F1}ms\n");

        // Benchmark 4: Concurrent rendering
        Console.WriteLine("📊 Benchmark 4: Concurrent rendering (10 simultaneous requests)");
        var concurrentSw = Stopwatch.StartNew();
        var concurrentTasks = Enumerable.Range(0, 10)
            .Select(i => wrapper.RenderDiagramAsync($"Task {i} -> Result {i}"))
            .ToArray();
        await Task.WhenAll(concurrentTasks);
        concurrentSw.Stop();
        Console.WriteLine($"   Total time: {concurrentSw.ElapsedMilliseconds}ms for 10 renders");
        Console.WriteLine($"   Throughput: {(10000.0 / concurrentSw.ElapsedMilliseconds):F1} renders/sec\n");

        // Benchmark 5: Cache effectiveness
        Console.WriteLine("📊 Benchmark 5: Cache effectiveness");
        var cacheScript = "api -> service -> db";
        var firstRender = Stopwatch.StartNew();
        await wrapper.RenderDiagramAsync(cacheScript);
        firstRender.Stop();

        var cacheHitTimes = new List<long>();
        for (int i = 0; i < 100; i++)
        {
            var sw = Stopwatch.StartNew();
            var result = await wrapper.RenderDiagramAsync(cacheScript);
            sw.Stop();
            if (result.FromCache)
            {
                cacheHitTimes.Add(sw.ElapsedMilliseconds);
            }
        }
        Console.WriteLine($"   First render: {firstRender.ElapsedMilliseconds}ms");
        Console.WriteLine($"   Cache hit average: {cacheHitTimes.Average():F2}ms");
        Console.WriteLine($"   Speedup: {(firstRender.ElapsedMilliseconds / cacheHitTimes.Average()):F1}x faster\n");

        Console.WriteLine("✓ Benchmarks complete!");
        return 0;
    }

    static async Task<int> AnalyzeSolutionCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Please specify a .sln file to analyze");
            return 1;
        }

        var solutionFile = args[1];
        if (!File.Exists(solutionFile))
        {
            Console.WriteLine($"Error: Solution file not found: {solutionFile}");
            return 1;
        }

        Console.WriteLine($"🔍 Analyzing .NET Solution: {Path.GetFileName(solutionFile)}");
        Console.WriteLine("============================================\n");

        var solutionDir = Path.GetDirectoryName(solutionFile) ?? ".";
        var projects = ParseSolutionFile(solutionFile, solutionDir);

        Console.WriteLine($"Found {projects.Count} projects:");
        foreach (var proj in projects)
        {
            Console.WriteLine($"  • {proj.Name}");
        }
        Console.WriteLine();

        // Analyze each project
        foreach (var project in projects)
        {
            AnalyzeProject(project);
            if (project.ProjectReferences.Any())
            {
                Console.WriteLine($"  {project.Name} references: {string.Join(", ", project.ProjectReferences)}");
            }
        }

        // Generate D2 diagram
        var d2Script = GenerateSolutionDiagram(projects, Path.GetFileNameWithoutExtension(solutionFile));

        // Render diagram
        var options = ParseRenderOptions(args);
        var outputDir = GetOutputDir(args);
        Directory.CreateDirectory(outputDir);

        var wrapperOptions = new D2WrapperOptions
        {
            EnableCaching = false,
            EnableDiagnosticIds = true
        };

        using var wrapper = new D2Wrapper(wrapperOptions);
        Console.WriteLine("📊 Generating dependency graph diagram...\n");

        var sw = Stopwatch.StartNew();
        var result = await wrapper.RenderDiagramAsync(d2Script, options: options);
        sw.Stop();

        if (result.IsSuccess)
        {
            var outputFile = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(solutionFile)}-dependencies.svg");
            await File.WriteAllTextAsync(outputFile, result.Svg);

            var d2OutputFile = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(solutionFile)}-dependencies.d2");
            await File.WriteAllTextAsync(d2OutputFile, d2Script);

            Console.WriteLine($"✓ Analysis complete! Rendered in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  SVG: {outputFile}");
            Console.WriteLine($"  D2 Script: {d2OutputFile}");
            Console.WriteLine($"  Projects: {projects.Count}");
            Console.WriteLine($"  Total Dependencies: {projects.Sum(p => p.ProjectReferences.Count + p.PackageReferences.Count)}");
            return 0;
        }
        else
        {
            Console.WriteLine($"✗ Error: {result.Error.Message}");
            return 1;
        }
    }

    static List<ProjectInfo> ParseSolutionFile(string solutionFile, string solutionDir)
    {
        var projects = new List<ProjectInfo>();
        var lines = File.ReadAllLines(solutionFile);

        foreach (var line in lines)
        {
            // Parse project lines like: Project("{...}") = "ProjectName", "path\to\ProjectName.csproj", "{...}"
            if (line.StartsWith("Project(") && line.Contains(".csproj"))
            {
                // Extract parts between quotes
                var matches = System.Text.RegularExpressions.Regex.Matches(line, "\"([^\"]*)\"");
                if (matches.Count >= 3)
                {
                    var projectName = matches[1].Value.Trim('"');
                    var projectPath = matches[2].Value.Trim('"');

                    if (projectPath.EndsWith(".csproj"))
                    {
                        // Normalize path separators
                        projectPath = projectPath.Replace('\\', Path.DirectorySeparatorChar);
                        var fullPath = Path.Combine(solutionDir, projectPath);

                        if (File.Exists(fullPath))
                        {
                            projects.Add(new ProjectInfo
                            {
                                Name = projectName,
                                Path = fullPath,
                                ProjectReferences = new List<string>(),
                                PackageReferences = new List<(string name, string version)>()
                            });
                        }
                    }
                }
            }
        }

        return projects;
    }

    static void AnalyzeProject(ProjectInfo project)
    {
        try
        {
            var xml = File.ReadAllText(project.Path);
            var doc = System.Xml.Linq.XDocument.Parse(xml);

            // Get project references
            var projectRefs = doc.Descendants("ProjectReference")
                .Select(pr => pr.Attribute("Include")?.Value)
                .Where(include => include != null)
                .Select(include =>
                {
                    // Normalize path separators and get just the filename without extension
                    var normalized = include!.Replace('\\', '/');
                    return Path.GetFileNameWithoutExtension(normalized);
                })
                .ToList();

            project.ProjectReferences.AddRange(projectRefs);

            // Get package references
            var packageRefs = doc.Descendants("PackageReference")
                .Select(pr => new
                {
                    Name = pr.Attribute("Include")?.Value,
                    Version = pr.Attribute("Version")?.Value ?? pr.Element("Version")?.Value ?? "*"
                })
                .Where(pkg => pkg.Name != null)
                .Select(pkg => (pkg.Name!, pkg.Version))
                .ToList();

            project.PackageReferences.AddRange(packageRefs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Warning: Could not analyze {project.Name}: {ex.Message}");
        }
    }

    static string GenerateSolutionDiagram(List<ProjectInfo> projects, string solutionName)
    {
        var lines = new List<string>
        {
            $"# {solutionName} - Project Dependencies",
            "direction: down",
            ""
        };

        // Define projects
        foreach (var project in projects)
        {
            var shape = project.Name.Contains("Test") ? "rectangle" :
                       project.Name.Contains("Web") || project.Name.Contains("API") ? "hexagon" :
                       project.Name.Contains("CLI") ? "rectangle" :
                       "rectangle";

            var color = project.Name.Contains("Test") ? "#FF9800" :
                       project.Name.Contains("Web") || project.Name.Contains("API") ? "#4CAF50" :
                       project.Name.Contains("CLI") ? "#2196F3" :
                       "#9C27B0";

            lines.Add($"{SanitizeId(project.Name)}: {project.Name} {{");
            lines.Add($"  shape: {shape}");
            lines.Add($"  style.fill: \"{color}\"");
            lines.Add("}");
            lines.Add("");
        }

        // Add project dependencies
        foreach (var project in projects)
        {
            foreach (var reference in project.ProjectReferences)
            {
                var targetProject = projects.FirstOrDefault(p => p.Name == reference);
                if (targetProject != null)
                {
                    lines.Add($"{SanitizeId(project.Name)} -> {SanitizeId(reference)}: depends on");
                }
            }
        }

        // Add NuGet packages (top 5 most used)
        var popularPackages = projects
            .SelectMany(p => p.PackageReferences)
            .GroupBy(pkg => pkg.name)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToList();

        if (popularPackages.Any())
        {
            lines.Add("");
            lines.Add("# Key NuGet Packages");
            foreach (var pkg in popularPackages)
            {
                var pkgId = SanitizeId(pkg.Key);
                lines.Add($"{pkgId}: {pkg.Key} {{");
                lines.Add("  shape: stored_data");
                lines.Add("  style.fill: \"#FFC107\"");
                lines.Add("}");

                foreach (var project in projects.Where(p => p.PackageReferences.Any(pr => pr.name == pkg.Key)))
                {
                    lines.Add($"{SanitizeId(project.Name)} -> {pkgId}: uses");
                }
            }
        }

        return string.Join("\n", lines);
    }

    static string SanitizeId(string name)
    {
        // Remove invalid D2 characters and replace with underscores
        return name.Replace(".", "_").Replace("-", "_").Replace(" ", "_");
    }

    static RenderOptions ParseRenderOptions(string[] args)
    {
        int? themeId = null;
        LayoutEngine? layout = null;
        bool sketch = false;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i].ToLower())
            {
                case "--theme":
                    if (int.TryParse(args[i + 1], out var theme))
                        themeId = theme;
                    break;
                case "--layout":
                    if (args[i + 1].ToLower() == "elk")
                        layout = LayoutEngine.Elk;
                    break;
                case "--sketch":
                    sketch = true;
                    break;
            }
        }

        return new RenderOptions
        {
            ThemeId = themeId ?? 0,
            Layout = layout ?? LayoutEngine.Dagre,
            Sketch = sketch
        };
    }

    static string GetOutputDir(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].ToLower() == "--output")
                return args[i + 1];
        }
        return "./output";
    }

    static string GenerateComplexDiagram(int nodeCount)
    {
        var lines = new List<string> { "direction: right" };
        for (int i = 0; i < nodeCount; i++)
        {
            lines.Add($"Node{i}: {{");
            lines.Add($"  shape: rectangle");
            lines.Add($"  style.fill: \"#{Random.Shared.Next(0x1000000):X6}\"");
            lines.Add($"}}");

            if (i > 0)
            {
                var target = Random.Shared.Next(i);
                lines.Add($"Node{i} -> Node{target}: connects");
            }
        }
        return string.Join("\n", lines);
    }

    static string GenerateArchitectureDiagram(ArchitectureConfig config)
    {
        var lines = new List<string>
        {
            $"# {config.Name}",
            "direction: right",
            ""
        };

        foreach (var component in config.Components)
        {
            lines.Add($"{component.Id}: {component.Name} {{");
            lines.Add($"  shape: {component.Shape ?? "rectangle"}");
            if (!string.IsNullOrEmpty(component.Description))
            {
                lines.Add($"  tooltip: \"{component.Description}\"");
            }
            lines.Add("}");
            lines.Add("");
        }

        foreach (var connection in config.Connections)
        {
            var label = string.IsNullOrEmpty(connection.Label) ? "" : $": {connection.Label}";
            lines.Add($"{connection.From} -> {connection.To}{label}");
        }

        return string.Join("\n", lines);
    }
}

public class ArchitectureConfig
{
    public string Name { get; set; } = "";
    public List<Component> Components { get; set; } = new();
    public List<Connection> Connections { get; set; } = new();
}

public class Component
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Shape { get; set; }
    public string? Description { get; set; }
}

public class Connection
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string? Label { get; set; }
}

public class ProjectInfo
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public List<string> ProjectReferences { get; set; } = new();
    public List<(string name, string version)> PackageReferences { get; set; } = new();
}
