namespace SpToolkit.Generator.Output;

public sealed class GenerationReportEntry
{
    public required string ProcedureName { get; init; }

    /// <summary>"Success", "Warning", "Excluded", "Error"</summary>
    public required string Status { get; init; }

    public string? Message { get; init; }
    public List<string> FilesGenerated { get; init; } = [];
}
