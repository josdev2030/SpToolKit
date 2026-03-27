namespace SpToolkit.Abstractions.Exceptions;

public sealed class SpMappingException : SpToolkitException
{
    public string? PropertyName { get; }
    public string? ColumnOrParameterName { get; }

    public SpMappingException(
        string message,
        string? propertyName = null,
        string? columnOrParameterName = null)
        : base(message)
    {
        PropertyName = propertyName;
        ColumnOrParameterName = columnOrParameterName;
    }

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
