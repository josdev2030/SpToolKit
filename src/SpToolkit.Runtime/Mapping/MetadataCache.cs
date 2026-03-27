using System.Collections.Concurrent;
using System.Reflection;
using SpToolkit.Abstractions.Attributes;
using SpToolkit.Abstractions.Options;
using SpToolkit.Runtime.TypeConversion;

namespace SpToolkit.Runtime.Mapping;

internal sealed class MetadataCache
{
    private readonly ConcurrentDictionary<Type, InputPropertyMap[]> _inputCache = new();
    private readonly ConcurrentDictionary<Type, OutputPropertyMap[]> _outputCache = new();
    private readonly ConcurrentDictionary<Type, ResultColumnMap[]> _resultColumnCache = new();
    private readonly SpToolkitOptions _options;
    private readonly TypeConversionService _converter;

    public MetadataCache(SpToolkitOptions options, TypeConversionService converter)
    {
        _options = options;
        _converter = converter;
    }

    public InputPropertyMap[] GetInputMaps(Type type)
        => _inputCache.GetOrAdd(type, BuildInputMaps);

    public OutputPropertyMap[] GetOutputMaps(Type type)
        => _outputCache.GetOrAdd(type, BuildOutputMaps);

    public ResultColumnMap[] GetResultColumnMaps(Type type)
        => _resultColumnCache.GetOrAdd(type, BuildResultColumnMaps);

    private InputPropertyMap[] BuildInputMaps(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var result = new List<InputPropertyMap>(properties.Length);

        foreach (var prop in properties)
        {
            if (!prop.CanRead)
                continue;

            if (prop.GetCustomAttribute<SpIgnoreAttribute>() is not null)
                continue;

            var inputAttr = prop.GetCustomAttribute<SpInputAttribute>();

            if (_options.NamingPolicy == NamingPolicy.Attribute && inputAttr is null)
                continue;

            string paramName;
            System.Data.SqlDbType sqlDbType;
            int size;
            byte precision;
            byte scale;

            if (inputAttr is not null)
            {
                paramName = inputAttr.ParameterName;
                sqlDbType = inputAttr.SqlDbType;
                size = inputAttr.Size;
                precision = inputAttr.Precision;
                scale = inputAttr.Scale;
            }
            else
            {
                // Convention mode: infer from property
                var propName = prop.Name;
                paramName = _options.AutoPrefixAtSign ? "@" + propName : propName;
                sqlDbType = _converter.InferSqlDbType(prop.PropertyType);
                size = 0;
                precision = 0;
                scale = 0;
            }

            result.Add(new InputPropertyMap
            {
                Property = prop,
                ParameterName = paramName,
                SqlDbType = sqlDbType,
                Size = size,
                Precision = precision,
                Scale = scale
            });
        }

        return result.ToArray();
    }

    private OutputPropertyMap[] BuildOutputMaps(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var result = new List<OutputPropertyMap>(properties.Length);

        foreach (var prop in properties)
        {
            if (!prop.CanWrite)
                continue;

            if (prop.GetCustomAttribute<SpIgnoreAttribute>() is not null)
                continue;

            var outputAttr = prop.GetCustomAttribute<SpOutputAttribute>();

            if (_options.NamingPolicy == NamingPolicy.Attribute && outputAttr is null)
                continue;

            string paramName;
            System.Data.SqlDbType sqlDbType;
            int size;
            byte precision;
            byte scale;

            if (outputAttr is not null)
            {
                paramName = outputAttr.ParameterName;
                sqlDbType = outputAttr.SqlDbType;
                size = outputAttr.Size;
                precision = outputAttr.Precision;
                scale = outputAttr.Scale;
            }
            else
            {
                // Convention mode: infer from property
                var propName = prop.Name;
                paramName = _options.AutoPrefixAtSign ? "@" + propName : propName;
                sqlDbType = _converter.InferSqlDbType(prop.PropertyType);
                size = 0;
                precision = 0;
                scale = 0;
            }

            result.Add(new OutputPropertyMap
            {
                Property = prop,
                ParameterName = paramName,
                SqlDbType = sqlDbType,
                Size = size,
                Precision = precision,
                Scale = scale
            });
        }

        return result.ToArray();
    }

    private ResultColumnMap[] BuildResultColumnMaps(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var result = new List<ResultColumnMap>(properties.Length);

        foreach (var prop in properties)
        {
            if (!prop.CanWrite)
                continue;

            if (prop.GetCustomAttribute<SpIgnoreAttribute>() is not null)
                continue;

            var colAttr = prop.GetCustomAttribute<SpResultColumnAttribute>();

            if (_options.NamingPolicy == NamingPolicy.Attribute && colAttr is null)
                continue;

            result.Add(new ResultColumnMap
            {
                Property = prop,
                ColumnName = colAttr?.ColumnName ?? prop.Name,
                Order = colAttr?.Order ?? -1
            });
        }

        return result.ToArray();
    }
}
