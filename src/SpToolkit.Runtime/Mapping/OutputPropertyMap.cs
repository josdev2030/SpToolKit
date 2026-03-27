using System.Data;
using System.Reflection;

namespace SpToolkit.Runtime.Mapping;

internal sealed class OutputPropertyMap
{
    public required PropertyInfo Property { get; init; }
    public required string ParameterName { get; init; }
    public required SqlDbType SqlDbType { get; init; }
    public required int Size { get; init; }
    public required byte Precision { get; init; }
    public required byte Scale { get; init; }
}
