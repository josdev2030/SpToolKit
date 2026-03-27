namespace SpToolkit.Abstractions.Attributes;

/// <summary>
/// Associates a request class with a specific stored procedure.
/// When present, callers can omit the procedure name from executor calls.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SpProcedureAttribute : Attribute
{
    /// <summary>
    /// Fully qualified procedure name including schema (e.g. "dbo.SP_CREAR_USUARIO").
    /// </summary>
    public string ProcedureName { get; }

    public SpProcedureAttribute(string procedureName)
    {
        ProcedureName = procedureName;
    }
}
