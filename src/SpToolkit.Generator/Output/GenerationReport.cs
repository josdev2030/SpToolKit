namespace SpToolkit.Generator.Output;

public sealed class GenerationReport
{
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public required string Namespace { get; init; }
    public required string OutputDirectory { get; init; }
    public int TotalProcedures { get; init; }
    public int SuccessCount { get; init; }
    public int WarningCount { get; init; }
    public int ExcludedCount { get; init; }
    public int ErrorCount { get; init; }
    public required List<GenerationReportEntry> Entries { get; init; }
}
