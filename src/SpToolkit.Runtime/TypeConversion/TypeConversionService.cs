using System.Data;
using SpToolkit.Abstractions.Exceptions;

namespace SpToolkit.Runtime.TypeConversion;

internal sealed class TypeConversionService
{
    private static readonly Dictionary<Type, SqlDbType> ClrToSqlDbType = new()
    {
        [typeof(string)]   = SqlDbType.NVarChar,
        [typeof(int)]      = SqlDbType.Int,
        [typeof(long)]     = SqlDbType.BigInt,
        [typeof(short)]    = SqlDbType.SmallInt,
        [typeof(byte)]     = SqlDbType.TinyInt,
        [typeof(bool)]     = SqlDbType.Bit,
        [typeof(decimal)]  = SqlDbType.Decimal,
        [typeof(double)]   = SqlDbType.Float,
        [typeof(float)]    = SqlDbType.Real,
        [typeof(DateTime)] = SqlDbType.DateTime2,
        [typeof(DateOnly)] = SqlDbType.Date,
        [typeof(TimeOnly)] = SqlDbType.Time,
        [typeof(Guid)]     = SqlDbType.UniqueIdentifier,
        [typeof(byte[])]   = SqlDbType.VarBinary,
    };

    /// <summary>
    /// Converts a .NET value to a value suitable for SqlParameter.Value.
    /// null becomes DBNull.Value; enums become their underlying integer value.
    /// </summary>
    public object ToSqlValue(object? value)
    {
        if (value is null)
            return DBNull.Value;

        var type = value.GetType();

        if (type.IsEnum)
            return Convert.ChangeType(value, Enum.GetUnderlyingType(type));

        return value;
    }

    /// <summary>
    /// Converts a value received from SQL Server to the target .NET type.
    /// Handles DBNull, enums, DateOnly, TimeOnly and nullable value types.
    /// </summary>
    public object? FromSqlValue(object sqlValue, Type targetType)
    {
        if (sqlValue is DBNull || sqlValue is null)
        {
            if (IsNullable(targetType))
                return null;

            throw new SpMappingException(
                $"SQL value is NULL but target type '{targetType.Name}' does not allow null. " +
                "Use a nullable type (e.g. int?) to allow null values from SQL Server.");
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (sqlValue.GetType() == underlyingType)
            return sqlValue;

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, sqlValue);

        if (underlyingType == typeof(DateOnly) && sqlValue is DateTime dt)
            return DateOnly.FromDateTime(dt);

        if (underlyingType == typeof(TimeOnly) && sqlValue is TimeSpan ts)
            return TimeOnly.FromTimeSpan(ts);

        try
        {
            return Convert.ChangeType(sqlValue, underlyingType);
        }
        catch (Exception ex)
        {
            throw new SpMappingException(
                $"Cannot convert SQL value of type '{sqlValue.GetType().Name}' " +
                $"to target type '{targetType.Name}'.",
                ex);
        }
    }

    /// <summary>
    /// Infers the SqlDbType for a CLR type. Used in Convention naming mode when no attribute is present.
    /// </summary>
    public SqlDbType InferSqlDbType(Type clrType)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (ClrToSqlDbType.TryGetValue(underlyingType, out var sqlDbType))
            return sqlDbType;

        if (underlyingType.IsEnum)
            return InferSqlDbType(Enum.GetUnderlyingType(underlyingType));

        throw new SpMappingException(
            $"Cannot infer SqlDbType for CLR type '{clrType.Name}'. " +
            "Use a [SpInput] or [SpOutput] attribute with an explicit SqlDbType.");
    }

    private static bool IsNullable(Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
