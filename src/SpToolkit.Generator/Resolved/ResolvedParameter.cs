namespace SpToolkit.Generator.Resolved;

public sealed class ResolvedParameter
{
    public required string PropertyName { get; init; }
    public required string ClrTypeName { get; init; }

    /// <summary>Original SQL parameter name including @ (e.g. "@NOMBRE").</summary>
    public required string SqlParameterName { get; init; }

    /// <summary>SqlDbType enum member name (e.g. "NVarChar").</summary>
    public required string SqlDbTypeName { get; init; }

    /// <summary>Character size for sized types (-1 = MAX, 0 = not applicable).</summary>
    public required int Size { get; init; }

    public required byte Precision { get; init; }
    public required byte Scale { get; init; }

    /// <summary>C# default value expression, or null for value types.</summary>
    public required string? DefaultValueExpression { get; init; }
}
