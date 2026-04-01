namespace SpToolkit.Abstractions.Exceptions;

/// <summary>
/// Thrown when SpToolKit fails to execute a stored procedure (e.g. SQL Server connection errors,
/// command execution errors, or invalid procedure names).
/// </summary>
public sealed class SpExecutionException : SpToolkitException
{
    /// <summary>
    /// The name of the stored procedure that was being executed, if available.
    /// </summary>
    public string? ProcedureName { get; }

    /// <summary>Initializes a new instance with the given message and optional procedure name.</summary>
    public SpExecutionException(string message, string? procedureName = null)
        : base(message)
    {
        ProcedureName = procedureName;
    }

    /// <summary>Initializes a new instance with the given message, inner exception, and optional procedure name.</summary>
    public SpExecutionException(string message, Exception innerException, string? procedureName = null)
        : base(message, innerException)
    {
        ProcedureName = procedureName;
    }
}
