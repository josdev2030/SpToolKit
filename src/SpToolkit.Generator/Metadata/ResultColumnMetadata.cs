namespace SpToolkit.Generator.Metadata;

public sealed class ResultColumnMetadata
{
    /// <summary>Column name from the result set (e.g. "USUARIO_ID").</summary>
    public required string Name { get; init; }

    /// <summary>SQL type name in lowercase (e.g. "int", "nvarchar").</summary>
    public required string SqlTypeName { get; init; }

    public required bool IsNullable { get; init; }

    /// <summary>Max length in bytes. -1 means MAX.</summary>
    public required int MaxLength { get; init; }

    public required byte Precision { get; init; }
    public required byte Scale { get; init; }
}
