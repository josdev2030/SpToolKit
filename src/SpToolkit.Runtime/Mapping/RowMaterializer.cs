using System.Data.Common;
using SpToolkit.Abstractions.Exceptions;
using SpToolkit.Abstractions.Options;
using SpToolkit.Runtime.TypeConversion;

namespace SpToolkit.Runtime.Mapping;

internal sealed class RowMaterializer
{
    private readonly MetadataCache _cache;
    private readonly TypeConversionService _converter;
    private readonly SpToolkitOptions _options;

    public RowMaterializer(MetadataCache cache, TypeConversionService converter, SpToolkitOptions options)
    {
        _cache = cache;
        _converter = converter;
        _options = options;
    }

    public async Task<IReadOnlyList<TResult>> MaterializeListAsync<TResult>(
        DbDataReader reader, CancellationToken ct)
        where TResult : class, new()
    {
        var maps = _cache.GetResultColumnMaps(typeof(TResult));
        var ordinalMap = BuildOrdinalMap<TResult>(reader, maps);
        var list = new List<TResult>();

        while (await reader.ReadAsync(ct))
        {
            list.Add(MaterializeRow<TResult>(reader, ordinalMap));
        }

        return list;
    }

    public async Task<TResult?> MaterializeSingleAsync<TResult>(
        DbDataReader reader, CancellationToken ct)
        where TResult : class, new()
    {
        var maps = _cache.GetResultColumnMaps(typeof(TResult));
        var ordinalMap = BuildOrdinalMap<TResult>(reader, maps);

        if (await reader.ReadAsync(ct))
        {
            return MaterializeRow<TResult>(reader, ordinalMap);
        }

        return null;
    }

    private (ResultColumnMap map, int ordinal)[] BuildOrdinalMap<TResult>(
        DbDataReader reader, ResultColumnMap[] maps)
    {
        var comparison = _options.CaseSensitiveColumnMatching
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var result = new List<(ResultColumnMap, int)>(maps.Length);

        foreach (var map in maps)
        {
            int ordinal;

            if (map.Order >= 0)
            {
                ordinal = map.Order;
            }
            else
            {
                ordinal = FindOrdinalByName(reader, map.ColumnName, comparison);
            }

            if (ordinal < 0)
            {
                if (_options.MissingColumnBehavior == MissingColumnBehavior.Throw)
                    throw new SpMappingException(
                        $"Column '{map.ColumnName}' was not found in the result set " +
                        $"for property '{map.Property.Name}' on type '{typeof(TResult).Name}'.",
                        propertyName: map.Property.Name,
                        columnOrParameterName: map.ColumnName);

                continue;
            }

            result.Add((map, ordinal));
        }

        return result.ToArray();
    }

    private static int FindOrdinalByName(DbDataReader reader, string columnName, StringComparison comparison)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, comparison))
                return i;
        }

        return -1;
    }

    private TResult MaterializeRow<TResult>(
        DbDataReader reader,
        (ResultColumnMap map, int ordinal)[] ordinalMap)
        where TResult : class, new()
    {
        var instance = new TResult();

        foreach (var (map, ordinal) in ordinalMap)
        {
            object rawValue;

            try
            {
                rawValue = reader.GetValue(ordinal);
            }
            catch (Exception ex)
            {
                throw new SpMappingException(
                    $"Failed to read column '{map.ColumnName}' (ordinal {ordinal}) " +
                    $"from the result set for type '{typeof(TResult).Name}'.",
                    ex,
                    propertyName: map.Property.Name,
                    columnOrParameterName: map.ColumnName);
            }

            object? converted;

            try
            {
                converted = _converter.FromSqlValue(rawValue, map.Property.PropertyType);
            }
            catch (SpMappingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SpMappingException(
                    $"Failed to convert column '{map.ColumnName}' value of type '{rawValue.GetType().Name}' " +
                    $"to property '{map.Property.Name}' of type '{map.Property.PropertyType.Name}' " +
                    $"on type '{typeof(TResult).Name}'.",
                    ex,
                    propertyName: map.Property.Name,
                    columnOrParameterName: map.ColumnName);
            }

            try
            {
                map.Property.SetValue(instance, converted);
            }
            catch (Exception ex)
            {
                throw new SpMappingException(
                    $"Failed to assign column '{map.ColumnName}' to property '{map.Property.Name}' " +
                    $"on type '{typeof(TResult).Name}'.",
                    ex,
                    propertyName: map.Property.Name,
                    columnOrParameterName: map.ColumnName);
            }
        }

        return instance;
    }
}
