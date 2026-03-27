using System.Reflection;

namespace SpToolkit.Runtime.Mapping;

internal sealed class ResultColumnMap
{
    public required PropertyInfo Property { get; init; }
    public required string ColumnName { get; init; }

    /// <summary>
    /// Zero-based ordinal for direct reader access. -1 means resolve by name.
    /// </summary>
    public required int Order { get; init; }
}
