namespace SpToolkit.Abstractions.Options;

public enum MissingColumnBehavior
{
    /// <summary>
    /// If a model property has no matching column in the reader, leave it at its default value.
    /// </summary>
    Ignore,

    /// <summary>
    /// Throw a SpMappingException if a model property has no matching column in the reader.
    /// </summary>
    Throw
}
