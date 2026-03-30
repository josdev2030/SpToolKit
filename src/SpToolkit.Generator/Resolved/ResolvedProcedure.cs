namespace SpToolkit.Generator.Resolved;

public sealed class ResolvedProcedure
{
    /// <summary>Fully qualified SP name e.g. "dbo.SP_CREAR_USUARIO".</summary>
    public required string FullName { get; init; }

    /// <summary>PascalCase base name without suffix e.g. "CrearUsuario".</summary>
    public required string BaseName { get; init; }

    public required string RequestClassName { get; init; }

    /// <summary>Null when the SP has no output parameters.</summary>
    public required string? ResponseClassName { get; init; }

    /// <summary>True when a response type is generated (output parameters present).</summary>
    public bool RequiresResponseType => ResponseClassName is not null;

    /// <summary>Null when the SP has no inferrable result set.</summary>
    public required string? RowClassName { get; init; }

    public required string MethodName { get; init; }
    public required ExecutionPattern Pattern { get; init; }
    public required IReadOnlyList<ResolvedParameter> InputParameters { get; init; }
    public required IReadOnlyList<ResolvedParameter> OutputParameters { get; init; }
    public required IReadOnlyList<ResolvedColumn>? ResultColumns { get; init; }

    /// <summary>True when the SP has at least one input parameter.</summary>
    public required bool HasInputParameters { get; init; }

    /// <summary>Optional warning message (e.g. result set could not be inferred).</summary>
    public string? Warning { get; init; }
}
