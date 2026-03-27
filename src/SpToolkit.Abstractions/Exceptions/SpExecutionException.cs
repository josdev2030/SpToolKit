namespace SpToolkit.Abstractions.Exceptions;

public sealed class SpExecutionException : SpToolkitException
{
    public string? ProcedureName { get; }

    public SpExecutionException(string message, string? procedureName = null)
        : base(message)
    {
        ProcedureName = procedureName;
    }

    public SpExecutionException(string message, Exception innerException, string? procedureName = null)
        : base(message, innerException)
    {
        ProcedureName = procedureName;
    }
}
