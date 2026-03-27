namespace SpToolkit.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SpResultColumnAttribute : Attribute
{
    /// <summary>
    /// The exact column name in the result set (e.g. "USUARIO_ID").
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Optional zero-based ordinal. When set, the runtime uses ordinal access instead of name lookup.
    /// Default -1 means resolve by name.
    /// </summary>
    public int Order { get; set; } = -1;

    public SpResultColumnAttribute(string columnName)
    {
        ColumnName = columnName;
    }
}
