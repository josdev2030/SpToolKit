using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpToolkit.Generator.Cli;
using SpToolkit.Generator.Configuration;
using SpToolkit.Generator.Orchestration;
using SpToolkit.Generator.Output;

// ── Options ────────────────────────────────────────────────────────────────

var configOption = new Option<string>("--config", ["-c"])
{
    Description = "Path to sptoolkit.json config file",
    DefaultValueFactory = _ => "sptoolkit.json"
};

var connectionOption = new Option<string?>("--connection")
{
    Description = "Connection string (overrides config file)"
};

var outputOption = new Option<string?>("--output", ["-o"])
{
    Description = "Output directory for generated files (overrides config file)"
};

var namespaceOption = new Option<string?>("--namespace", ["-n"])
{
    Description = "C# namespace for generated classes (overrides config file)"
};

var schemasOption = new Option<string[]?>("--schemas", ["-s"])
{
    Description = "Schemas to include, space-separated (overrides config file)"
};

var excludeOption = new Option<string[]?>("--exclude", ["-e"])
{
    Description = "Stored procedure names to exclude (overrides config file)"
};

var wrapperOption = new Option<string?>("--wrapper", ["-w"])
{
    Description = "Wrapper class name (overrides config file, default: AppStoredProcedures)"
};

var dryRunOption = new Option<bool>("--dry-run")
{
    Description = "Print resolved configuration and exit without writing files"
};

// ── Root command ───────────────────────────────────────────────────────────

RootCommand rootCommand = new("SpToolkit stored procedure code generator")
{
    configOption,
    connectionOption,
    outputOption,
    namespaceOption,
    schemasOption,
    excludeOption,
    wrapperOption,
    dryRunOption,
};

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
{
    var configPath = parseResult.GetValue(configOption)!;
    var connection = parseResult.GetValue(connectionOption);
    var output     = parseResult.GetValue(outputOption);
    var ns         = parseResult.GetValue(namespaceOption);
    var schemas    = parseResult.GetValue(schemasOption);
    var exclude    = parseResult.GetValue(excludeOption);
    var wrapper    = parseResult.GetValue(wrapperOption);
    var dryRun     = parseResult.GetValue(dryRunOption);
    var schemasFromCli = parseResult.GetResult(schemasOption) is not null;
    var excludeFromCli = parseResult.GetResult(excludeOption) is not null;

    return await RunAsync(
        configPath,
        connection,
        output,
        ns,
        schemas,
        exclude,
        wrapper,
        dryRun,
        schemasFromCli,
        excludeFromCli,
        ct);
});

return await rootCommand.Parse(args).InvokeAsync();

// ── RunAsync ───────────────────────────────────────────────────────────────

static async Task<int> RunAsync(
    string configPath,
    string? cliConnection,
    string? cliOutput,
    string? cliNamespace,
    string[]? cliSchemas,
    string[]? cliExclude,
    string? cliWrapper,
    bool dryRun,
    bool schemasFromCli,
    bool excludeFromCli,
    CancellationToken ct)
{
    PrintBanner();

    // 1. Load config file
    ConfigFile? config = null;
    var configFile = new FileInfo(configPath);

    if (configFile.Exists)
    {
        Console.WriteLine($"Config : {configFile.FullName}");
        await using var stream = configFile.OpenRead();
        config = await JsonSerializer.DeserializeAsync<ConfigFile>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new JsonStringEnumConverter() },
            },
            ct);
    }
    else if (configPath != "sptoolkit.json")
    {
        PrintError($"Config file not found: {configPath}");
        return 1;
    }

    // 2. Merge: CLI args override config file values
    // Array options default to [] when omitted; schemasFromCli / excludeFromCli mean the flag was passed.
    var connectionString = cliConnection ?? config?.ConnectionString;
    var outputDirectory  = cliOutput     ?? config?.OutputDirectory;
    var namespaceName    = cliNamespace  ?? config?.Namespace;
    var schemas          = schemasFromCli
        ? (cliSchemas ?? [])
        : (config?.Schemas ?? ["dbo"]);
    var prefixesToRemove = config?.PrefixesToRemove                  ?? ["SP_", "USP_"];
    var excludeProcs     = excludeFromCli
        ? (cliExclude ?? [])
        : (config?.ExcludeProcedures ?? []);
    var wrapperClass     = cliWrapper    ?? config?.WrapperClassName ?? "AppStoredProcedures";
    var caseSensitive    = config?.CaseSensitiveColumns ?? false;
    var overrides        = config?.Overrides            ?? [];

    // 3. Validate required fields
    var validationErrors = new List<string>();
    if (string.IsNullOrWhiteSpace(connectionString)) validationErrors.Add("connectionString is required");
    if (string.IsNullOrWhiteSpace(outputDirectory))  validationErrors.Add("outputDirectory is required");
    if (string.IsNullOrWhiteSpace(namespaceName))    validationErrors.Add("namespace is required");

    if (validationErrors.Count > 0)
    {
        foreach (var e in validationErrors)
            PrintError(e);
        Console.WriteLine("\nProvide the missing values in sptoolkit.json or via CLI arguments (run --help for details).");
        return 1;
    }

    // 4. Build options
    var options = new GeneratorOptions
    {
        ConnectionString     = connectionString!,
        Namespace            = namespaceName!,
        OutputDirectory      = outputDirectory!,
        Schemas              = schemas,
        PrefixesToRemove     = prefixesToRemove,
        ExcludeProcedures    = excludeProcs,
        WrapperClassName     = wrapperClass,
        CaseSensitiveColumns = caseSensitive,
        Overrides            = overrides,
    };

    // 5. Dry-run: print config and exit without connecting to SQL Server
    if (dryRun)
    {
        Console.WriteLine("\n[Dry Run] Effective configuration:");
        Console.WriteLine($"  Connection : {MaskConnectionString(options.ConnectionString)}");
        Console.WriteLine($"  Namespace  : {options.Namespace}");
        Console.WriteLine($"  Output     : {options.OutputDirectory}");
        Console.WriteLine($"  Schemas    : {string.Join(", ", options.Schemas)}");
        Console.WriteLine($"  Prefixes   : {string.Join(", ", options.PrefixesToRemove)}");
        Console.WriteLine($"  Exclude    : {(options.ExcludeProcedures.Length > 0 ? string.Join(", ", options.ExcludeProcedures) : "(none)")}");
        Console.WriteLine($"  Wrapper    : {options.WrapperClassName}");
        Console.WriteLine($"  CaseSens.  : {options.CaseSensitiveColumns}");
        Console.WriteLine("\nNo files written (--dry-run). Remove the flag to generate.");
        return 0;
    }

    Console.WriteLine($"Output : {options.OutputDirectory}");
    Console.WriteLine("\nGenerating...");

    // 6. Run orchestrator
    GenerationReport report;
    try
    {
        var orchestrator = new GenerationOrchestrator(options);
        report = await orchestrator.GenerateAsync(options, ct);
    }
    catch (Exception ex)
    {
        PrintError($"Generation failed: {ex.Message}");
        return 1;
    }

    // 7. Print per-SP results
    foreach (var entry in report.Entries)
    {
        var fileNames = entry.FilesGenerated.Count > 0
            ? string.Join(", ", entry.FilesGenerated.Select(StripGeneratedSuffix))
            : "(no files)";

        switch (entry.Status)
        {
            case "Success":
                PrintColored($"  [OK]   {entry.ProcedureName} -> {fileNames}", ConsoleColor.Green);
                break;
            case "Warning":
                PrintColored($"  [WARN] {entry.ProcedureName} -> {fileNames}", ConsoleColor.Yellow);
                if (entry.Message is not null)
                    PrintColored($"         {entry.Message}", ConsoleColor.Yellow);
                break;
            case "Error":
                PrintColored($"  [ERR]  {entry.ProcedureName}", ConsoleColor.Red);
                if (entry.Message is not null)
                    PrintColored($"         {entry.Message}", ConsoleColor.Red);
                break;
            case "Excluded":
                PrintColored($"  [SKIP] {entry.ProcedureName}", ConsoleColor.DarkGray);
                break;
        }
    }

    Console.WriteLine();
    var doneColor = report.ErrorCount > 0 ? ConsoleColor.Red
                  : report.WarningCount > 0 ? ConsoleColor.Yellow
                  : ConsoleColor.Green;

    PrintColored(
        $"Done: {report.TotalProcedures} procedures, " +
        $"{report.SuccessCount} success, " +
        $"{report.WarningCount} warnings, " +
        $"{report.ErrorCount} errors",
        doneColor);

    Console.WriteLine($"Files written to: {Path.GetFullPath(options.OutputDirectory)}");

    return report.ErrorCount > 0 ? 1 : 0;
}

// ── Console helpers ────────────────────────────────────────────────────────

static void PrintBanner()
{
    Console.WriteLine("SpToolkit Generator");
    Console.WriteLine(new string('-', 40));
}

static void PrintError(string message) =>
    PrintColored($"[ERROR] {message}", ConsoleColor.Red);

static void PrintColored(string message, ConsoleColor color)
{
    var prev = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ForegroundColor = prev;
}

static string MaskConnectionString(string cs) =>
    System.Text.RegularExpressions.Regex.Replace(
        cs, @"(?i)(password|pwd)\s*=\s*[^;]+", "$1=***");

static string StripGeneratedSuffix(string fileName) =>
    fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
        ? fileName[..^5]
        : Path.GetFileNameWithoutExtension(fileName);
