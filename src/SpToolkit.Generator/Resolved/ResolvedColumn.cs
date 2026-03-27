namespace SpToolkit.Generator.Resolved;

public sealed class ResolvedColumn
{
    public required string PropertyName { get; init; }
    public required string ClrTypeName { get; init; }

    /// <summary>Original column name from the result set (e.g. "USUARIO_ID").</summary>
    public required string ColumnName { get; init; }

    /// <summary>C# default value expression, or null for nullable/value types.</summary>
    public required string? DefaultValueExpression { get; init; }
}
