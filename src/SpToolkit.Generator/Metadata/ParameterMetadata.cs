namespace SpToolkit.Generator.Metadata;

public sealed class ParameterMetadata
{
    /// <summary>SQL parameter name including @ prefix (e.g. "@NOMBRE").</summary>
    public required string Name { get; init; }

    /// <summary>SQL type name in lowercase (e.g. "nvarchar", "int").</summary>
    public required string SqlTypeName { get; init; }

    /// <summary>Max length in bytes from sys.parameters. -1 means MAX.</summary>
    public required int MaxLength { get; init; }

    public required byte Precision { get; init; }
    public required byte Scale { get; init; }
    public required bool IsOutput { get; init; }
}
