namespace SpToolkit.Abstractions.Exceptions;

/// <summary>
/// Base class for all exceptions thrown by SpToolKit.
/// Catch this type to handle any SpToolKit-specific error uniformly.
/// </summary>
public class SpToolkitException : Exception
{
    /// <summary>Initializes a new instance with the given message.</summary>
    public SpToolkitException(string message) : base(message) { }

    /// <summary>Initializes a new instance with the given message and inner exception.</summary>
    public SpToolkitException(string message, Exception innerException)
        : base(message, innerException) { }
}
