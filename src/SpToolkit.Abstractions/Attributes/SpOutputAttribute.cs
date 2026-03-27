using System.Data;

namespace SpToolkit.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SpOutputAttribute : Attribute
{
    /// <summary>
    /// The SQL parameter name including the @ prefix (e.g. "@USUARIO_ID").
    /// </summary>
    public string ParameterName { get; }

    public SqlDbType SqlDbType { get; }

    /// <summary>
    /// Relevant for NVarChar, VarChar, VarBinary. Use 0 to let the runtime apply the default size.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Relevant for Decimal / Numeric.
    /// </summary>
    public byte Precision { get; set; }

    /// <summary>
    /// Relevant for Decimal / Numeric.
    /// </summary>
    public byte Scale { get; set; }

    public SpOutputAttribute(string parameterName, SqlDbType sqlDbType)
    {
        ParameterName = parameterName;
        SqlDbType = sqlDbType;
    }
}
