namespace SpToolkit.Abstractions.Exceptions;

public sealed class SpGenerationException : SpToolkitException
{
    public string? ProcedureName { get; }

    public SpGenerationException(string message, string? procedureName = null)
        : base(message)
    {
        ProcedureName = procedureName;
    }

    public SpGenerationException(string message, Exception innerException, string? procedureName = null)
        : base(message, innerException)
    {
        ProcedureName = procedureName;
    }
}
