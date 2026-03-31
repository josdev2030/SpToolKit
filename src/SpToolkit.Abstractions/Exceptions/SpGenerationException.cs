namespace SpToolkit.Abstractions.Exceptions;

/// <summary>
/// Thrown when the SpToolKit code generator encounters an error while reading metadata
/// or generating wrapper code for a stored procedure.
/// </summary>
public sealed class SpGenerationException : SpToolkitException
{
    /// <summary>
    /// The name of the stored procedure being processed when the error occurred, if available.
    /// </summary>
    public string? ProcedureName { get; }

    /// <summary>Initializes a new instance with the given message and optional procedure name.</summary>
    public SpGenerationException(string message, string? procedureName = null)
        : base(message)
    {
        ProcedureName = procedureName;
    }

    /// <summary>Initializes a new instance with the given message, inner exception, and optional procedure name.</summary>
    public SpGenerationException(string message, Exception innerException, string? procedureName = null)
        : base(message, innerException)
    {
        ProcedureName = procedureName;
    }
}
