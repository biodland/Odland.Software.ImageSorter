namespace Odland.Software.ImageSorter;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                DisplayUsage();
                return;
            }

            if (ShouldDisplayHelp(args))
            {
                DisplayHelp();
                return;
            }

            var arguments = ParseArguments(args);

            if (!ValidateArguments(arguments))
            {
                DisplayUsage();
                return;
            }

            await RunImageSorter(arguments);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    private static bool ShouldDisplayHelp(string[] args)
    {
        return args.Contains("--help") 
            || args.Contains("-h") 
            || args.Contains("/?") 
            || args.Contains("/help");
    }

    private static void DisplayUsage()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Usage: ImageSorter source=\"path\" target=\"path\" sortby=\"criteria\" [options]");
        Console.ResetColor();
        Console.WriteLine("\nRequired arguments:");
        Console.WriteLine("  source=<path>     The directory containing images to sort");
        Console.WriteLine("  target=<path>     The directory where sorted images will be placed");
        Console.WriteLine("  sortby=<criteria> Sort criteria: date, name, or size");
        Console.WriteLine("\nOptional arguments:");
        Console.WriteLine("  structure=<fmt>   Directory structure (e.g., \"YYYY\\\\MM\\\\\" for date sorting)");
        Console.WriteLine("  rename=<bool>     Rename files based on sorting criteria (true/false, default: false)");
        Console.WriteLine("  overwrite=<bool>  Overwrite existing files (true/false, default: false)");
        Console.WriteLine("  dryrun=<bool>     Perform a dry run without moving files (true/false, default: false)");
        Console.WriteLine("\nExample:");
        Console.WriteLine("  ImageSorter source=\"C:\\Pictures\\Unsorted\" target=\"C:\\Pictures\\Sorted\" sortby=\"date\" structure=\"YYYY\\\\MM\\\\\" rename=\"true\"");
        Console.WriteLine("\nFor more help:");
        Console.WriteLine("  ImageSorter --help");
    }

    private static void DisplayHelp()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Image Sorter Help ===\n");
        Console.ResetColor();

        Console.WriteLine("DESCRIPTION:");
        Console.WriteLine("  Organizes images from a source directory into a target directory based on");
        Console.WriteLine("  specified criteria (date, name, or size).\n");

        Console.WriteLine("SUPPORTED FORMATS:");
        Console.WriteLine("  Standard: JPG, JPEG, PNG, GIF, BMP, TIFF, TIF, WEBP");
        Console.WriteLine("  RAW: NEF (Nikon), CR2 (Canon), ARW (Sony), DNG (Adobe), RAW\n");

        Console.WriteLine("SORT CRITERIA:");
        Console.WriteLine("  date   - Organize by image date taken (requires structure parameter)");
        Console.WriteLine("  name   - Organize by first letter of filename");
        Console.WriteLine("  size   - Organize by file size (Small, Medium, Large)\n");

        Console.WriteLine("STRUCTURE EXAMPLES (for date sorting):");
        Console.WriteLine("  \"YYYY\\\\MM\\\\\"        - Year/Month");
        Console.WriteLine("  \"YYYY\\\\MM\\\\dd\\\\\"    - Year/Month/Day");
        Console.WriteLine("  \"YYYY\\\\\"            - Year only\n");

        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  Sort by date into year/month folders:");
        Console.WriteLine("    ImageSorter source=\"C:\\Photos\" target=\"C:\\Sorted\" sortby=\"date\" structure=\"YYYY\\\\MM\\\\\"");
        Console.WriteLine();
        Console.WriteLine("  Sort by date and rename files:");
        Console.WriteLine("    ImageSorter source=\"C:\\Photos\" target=\"C:\\Sorted\" sortby=\"date\" structure=\"YYYY\\\\MM\\\\\" rename=\"true\"");
        Console.WriteLine();
        Console.WriteLine("  Dry run to preview changes:");
        Console.WriteLine("    ImageSorter source=\"C:\\Photos\" target=\"C:\\Sorted\" sortby=\"date\" dryrun=\"true\"");
        Console.WriteLine();
        Console.WriteLine("  Sort by name with overwrite:");
        Console.WriteLine("    ImageSorter source=\"C:\\Photos\" target=\"C:\\Sorted\" sortby=\"name\" overwrite=\"true\"");
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in args)
        {
            if (!arg.Contains('='))
                continue;

            var parts = arg.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim('"', '\'');
                arguments[key] = value;
            }
        }

        return arguments;
    }

    private static bool ValidateArguments(Dictionary<string, string> arguments)
    {
        if (!arguments.ContainsKey("source"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: 'source' argument is required.");
            Console.ResetColor();
            return false;
        }

        if (!arguments.ContainsKey("target"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: 'target' argument is required.");
            Console.ResetColor();
            return false;
        }

        if (!arguments.ContainsKey("sortby"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: 'sortby' argument is required.");
            Console.ResetColor();
            return false;
        }

        var source = arguments["source"];
        if (!Directory.Exists(source))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Source directory does not exist: {source}");
            Console.ResetColor();
            return false;
        }

        var sortBy = arguments["sortby"].ToLowerInvariant();
        if (sortBy != "date" && sortBy != "name" && sortBy != "size")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Invalid sortby value '{sortBy}'. Must be 'date', 'name', or 'size'.");
            Console.ResetColor();
            return false;
        }

        if (sortBy == "date" && !arguments.ContainsKey("structure"))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: 'structure' parameter is recommended when sorting by date.");
            Console.ResetColor();
        }

        return true;
    }

    private static async Task RunImageSorter(Dictionary<string, string> arguments)
    {
        var source = arguments["source"];
        var target = arguments["target"];
        var sortBy = arguments["sortby"];
        var structure = arguments.TryGetValue("structure", out var s) ? s : string.Empty;
        var rename = arguments.TryGetValue("rename", out var r) && r.Equals("true", StringComparison.OrdinalIgnoreCase);
        var overwrite = arguments.TryGetValue("overwrite", out var o) && o.Equals("true", StringComparison.OrdinalIgnoreCase);
        var dryRun = arguments.TryGetValue("dryrun", out var d) && d.Equals("true", StringComparison.OrdinalIgnoreCase);
        var keepOriginal = !arguments.TryGetValue("keeporiginal", out var k) || k.Equals("true", StringComparison.OrdinalIgnoreCase);

        DisplayConfiguration(source, target, sortBy, structure, rename, overwrite, dryRun, keepOriginal);

        var sorter = new ImageSorter(
            source: source,
            target: target,
            sortBy: sortBy,
            structure: structure,
            rename: rename,
            overwrite: overwrite,
            keepOriginal: keepOriginal
        );

        // Event handlers
        sorter.SortingStarted += (s, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n{e.Message}");
            Console.ResetColor();
        };

        sorter.SortingProgressUpdated += (s, e) =>
        {
            Console.Write($"\r[{new string('█', e.Progress / 5)}{new string('░', 20 - e.Progress / 5)}] {e.Progress}% - {e.Message}");
        };

        sorter.ImageSorted += (s, e) =>
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {e.Message}");
            Console.ResetColor();
        };

        sorter.ErrorOccurred += (s, e) =>
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {e.Message}");
            Console.ResetColor();
        };

        sorter.SortingCompleted += (s, e) =>
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n{e.Message}");
            Console.ResetColor();
        };

        try
        {
            await sorter.StartSortingAsync(dryRun: dryRun);
        }
        catch (InvalidOperationException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nOperation failed: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    private static void DisplayConfiguration(string source, string target, string sortBy, string structure, bool rename, bool overwrite, bool dryRun, bool keepOriginal)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Configuration ===");
        Console.ResetColor();
        Console.WriteLine($"Source:    {source}");
        Console.WriteLine($"Target:    {target}");
        Console.WriteLine($"Sort By:   {sortBy}");
        Console.WriteLine($"Structure: {(string.IsNullOrEmpty(structure) ? "(default)" : structure)}");
        Console.WriteLine($"Rename:    {rename}");
        Console.WriteLine($"Overwrite: {overwrite}");
        Console.WriteLine($"Keep Original: {keepOriginal}");
        Console.WriteLine($"Dry Run:   {dryRun}");
        Console.WriteLine();

        if (dryRun)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ DRY RUN MODE - No files will be moved");
            Console.ResetColor();
        }
    }
}
