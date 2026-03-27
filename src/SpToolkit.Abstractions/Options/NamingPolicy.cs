namespace SpToolkit.Abstractions.Options;

public enum NamingPolicy
{
    /// <summary>
    /// Only maps properties that have explicit SpInput, SpOutput, or SpResultColumn attributes.
    /// </summary>
    Attribute,

    /// <summary>
    /// Infers parameter/column names from property names when no attribute is present.
    /// </summary>
    Convention
}
