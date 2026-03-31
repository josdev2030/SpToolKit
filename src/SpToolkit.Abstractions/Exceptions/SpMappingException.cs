namespace SpToolkit.Abstractions.Exceptions;

/// <summary>
/// Thrown when SpToolKit cannot map a SQL column or parameter to a .NET property,
/// or cannot convert a value between SQL and CLR types.
/// </summary>
public sealed class SpMappingException : SpToolkitException
{
    /// <summary>
    /// The .NET property name involved in the failed mapping, if available.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// The SQL column or parameter name involved in the failed mapping, if available.
    /// </summary>
    public string? ColumnOrParameterName { get; }

    /// <summary>Initializes a new instance with the given message and optional context details.</summary>
    public SpMappingException(
        string message,
        string? propertyName = null,
        string? columnOrParameterName = null)
        : base(message)
    {
        PropertyName = propertyName;
        ColumnOrParameterName = columnOrParameterName;
    }

    /// <summary>Initializes a new instance with the given message, inner exception, and optional context details.</summary>
    public SpMappingException(
        string message,
        Exception innerException,
        string? propertyName = null,
        string? columnOrParameterName = null)
        : base(message, innerException)
    {
        PropertyName = propertyName;
        ColumnOrParameterName = columnOrParameterName;
    }
}
