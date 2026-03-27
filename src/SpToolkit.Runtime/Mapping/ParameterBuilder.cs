using System.Data;
using Microsoft.Data.SqlClient;
using SpToolkit.Abstractions.Exceptions;
using SpToolkit.Abstractions.Options;
using SpToolkit.Runtime.TypeConversion;

namespace SpToolkit.Runtime.Mapping;

internal sealed class ParameterBuilder
{
    private static readonly HashSet<SqlDbType> SizedTypes = new()
    {
        SqlDbType.NVarChar,
        SqlDbType.VarChar,
        SqlDbType.NChar,
        SqlDbType.Char,
        SqlDbType.VarBinary,
        SqlDbType.Binary,
        SqlDbType.NText,
        SqlDbType.Text,
    };

    private readonly MetadataCache _cache;
    private readonly TypeConversionService _converter;
    private readonly SpToolkitOptions _options;

    public ParameterBuilder(MetadataCache cache, TypeConversionService converter, SpToolkitOptions options)
    {
        _cache = cache;
        _converter = converter;
        _options = options;
    }

    public SqlParameter[] BuildInputParameters<TInput>(TInput input) where TInput : class
    {
        var maps = _cache.GetInputMaps(typeof(TInput));
        var parameters = new SqlParameter[maps.Length];

        for (int i = 0; i < maps.Length; i++)
        {
            var map = maps[i];
            object? rawValue;

            try
            {
                rawValue = map.Property.GetValue(input);
            }
            catch (Exception ex)
            {
                throw new SpMappingException(
                    $"Failed to read property '{map.Property.Name}' from input type '{typeof(TInput).Name}'.",
                    ex,
                    propertyName: map.Property.Name,
                    columnOrParameterName: map.ParameterName);
            }

            var sqlValue = _converter.ToSqlValue(rawValue);

            var param = new SqlParameter
            {
                ParameterName = map.ParameterName,
                SqlDbType = map.SqlDbType,
                Direction = ParameterDirection.Input,
                Value = sqlValue
            };

            ApplySize(param, map.SqlDbType, map.Size, map.Precision, map.Scale);
            parameters[i] = param;
        }

        return parameters;
    }

    public SqlParameter[] BuildOutputParameters(Type outputType)
    {
        var maps = _cache.GetOutputMaps(outputType);
        var parameters = new SqlParameter[maps.Length];

        for (int i = 0; i < maps.Length; i++)
        {
            var map = maps[i];

            var param = new SqlParameter
            {
                ParameterName = map.ParameterName,
                SqlDbType = map.SqlDbType,
                Direction = ParameterDirection.Output
            };

            ApplySize(param, map.SqlDbType, map.Size, map.Precision, map.Scale);
            parameters[i] = param;
        }

        return parameters;
    }

    public void PopulateOutput<TOutput>(TOutput instance, SqlParameter[] outputParams) where TOutput : class
    {
        var maps = _cache.GetOutputMaps(typeof(TOutput));

        foreach (var map in maps)
        {
            var param = FindOutputParam(outputParams, map.ParameterName);

            if (param is null)
            {
                if (_options.MissingColumnBehavior == MissingColumnBehavior.Throw)
                    throw new SpMappingException(
                        $"Output parameter '{map.ParameterName}' was not found in the command after execution.",
                        propertyName: map.Property.Name,
                        columnOrParameterName: map.ParameterName);

                continue;
            }

            object? converted;

            try
            {
                converted = _converter.FromSqlValue(param.Value, map.Property.PropertyType);
            }
            catch (SpMappingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SpMappingException(
                    $"Failed to convert output parameter '{map.ParameterName}' " +
                    $"to property '{map.Property.Name}' of type '{map.Property.PropertyType.Name}'.",
                    ex,
                    propertyName: map.Property.Name,
                    columnOrParameterName: map.ParameterName);
            }

            try
            {
                map.Property.SetValue(instance, converted);
            }
            catch (Exception ex)
            {
                throw new SpMappingException(
                    $"Failed to assign value to property '{map.Property.Name}' " +
                    $"on type '{typeof(TOutput).Name}'.",
                    ex,
                    propertyName: map.Property.Name,
                    columnOrParameterName: map.ParameterName);
            }
        }
    }

    private void ApplySize(SqlParameter param, SqlDbType sqlDbType, int size, byte precision, byte scale)
    {
        if (SizedTypes.Contains(sqlDbType))
        {
            param.Size = size > 0 ? size : _options.DefaultStringSize;
        }

        if (precision > 0)
            param.Precision = precision;

        if (scale > 0)
            param.Scale = scale;
    }

    private static SqlParameter? FindOutputParam(SqlParameter[] outputParams, string parameterName)
    {
        foreach (var p in outputParams)
        {
            if (string.Equals(p.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
                return p;
        }

        return null;
    }
}
