using SpToolkit.Generator.Configuration;

namespace SpToolkit.Generator.Cli;

/// <summary>
/// Represents the JSON structure of sptoolkit.json.
/// All properties are nullable so the merge logic can distinguish
/// "not specified" from an explicit value.
/// </summary>
public sealed class ConfigFile
{
    public string? ConnectionString { get; set; }
    public string? Namespace { get; set; }
    public string? OutputDirectory { get; set; }
    public string[]? Schemas { get; set; }
    public string[]? PrefixesToRemove { get; set; }
    public string[]? ExcludeProcedures { get; set; }
    public bool? CaseSensitiveColumns { get; set; }
    public string? WrapperClassName { get; set; }
    public ProcedureOverride[]? Overrides { get; set; }
}
