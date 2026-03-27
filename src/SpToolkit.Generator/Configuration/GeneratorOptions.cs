namespace SpToolkit.Generator.Configuration;

public sealed class GeneratorOptions
{
    public required string ConnectionString { get; set; }
    public required string Namespace { get; set; }
    public required string OutputDirectory { get; set; }

    public string[] Schemas { get; set; } = ["dbo"];
    public string[] PrefixesToRemove { get; set; } = ["SP_", "USP_"];
    public string[] ExcludeProcedures { get; set; } = [];
    public bool CaseSensitiveColumns { get; set; } = false;
    public string WrapperClassName { get; set; } = "AppStoredProcedures";
    public ProcedureOverride[] Overrides { get; set; } = [];
}
