namespace SpToolkit.Generator.Metadata;

public sealed class StoredProcedureMetadata
{
    public required string SchemaName { get; init; }
    public required string ProcedureName { get; init; }
    public required IReadOnlyList<ParameterMetadata> Parameters { get; init; }

    /// <summary>Null when the result set could not be inferred.</summary>
    public IReadOnlyList<ResultColumnMetadata>? ResultColumns { get; init; }

    /// <summary>Set when sp_describe_first_result_set failed or returned no columns.</summary>
    public string? ResultSetError { get; init; }

    public string FullName => $"{SchemaName}.{ProcedureName}";
    public bool HasResultSet => ResultColumns is { Count: > 0 };
    public bool HasInputParameters => Parameters.Any(p => !p.IsOutput);
    public bool HasOutputParameters => Parameters.Any(p => p.IsOutput);
}
