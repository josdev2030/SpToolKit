using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpToolkit.Generator.Output;

public sealed class FileWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Writes all generated files to the output directory.
    /// Creates the directory if it does not exist.
    /// </summary>
    public async Task WriteAsync(
        string outputDirectory,
        IReadOnlyList<GeneratedFile> files,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        foreach (var file in files)
        {
            var path = Path.Combine(outputDirectory, file.FileName);
            await File.WriteAllTextAsync(path, file.Content, ct);
        }
    }

    /// <summary>
    /// Serializes and writes generation-report.json to the output directory.
    /// </summary>
    public async Task WriteReportAsync(
        string outputDirectory,
        GenerationReport report,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var path    = Path.Combine(outputDirectory, "generation-report.json");
        var json    = JsonSerializer.Serialize(report, JsonOptions);
        await File.WriteAllTextAsync(path, json, ct);
    }
}
