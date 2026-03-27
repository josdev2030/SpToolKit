namespace SpToolkit.Abstractions.Exceptions;

public class SpToolkitException : Exception
{
    public SpToolkitException(string message) : base(message) { }

    public SpToolkitException(string message, Exception innerException)
        : base(message, innerException) { }
}
