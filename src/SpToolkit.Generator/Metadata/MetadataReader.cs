using Microsoft.Data.SqlClient;
using SpToolkit.Generator.Configuration;

namespace SpToolkit.Generator.Metadata;

public sealed class MetadataReader
{
    /// <summary>
    /// Reads metadata for all stored procedures matching the options from SQL Server.
    /// </summary>
    public async Task<IReadOnlyList<StoredProcedureMetadata>> ReadAllAsync(
        GeneratorOptions options,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(ct);

        var procedures = await ListProceduresAsync(connection, options, ct);
        var result = new List<StoredProcedureMetadata>(procedures.Count);

        foreach (var (objectId, schemaName, procedureName) in procedures)
        {
            var parameters = await ReadParametersAsync(connection, objectId, ct);
            var (columns, columnError) = await ReadResultColumnsAsync(
                connection, schemaName, procedureName, ct);

            result.Add(new StoredProcedureMetadata
            {
                SchemaName     = schemaName,
                ProcedureName  = procedureName,
                Parameters     = parameters,
                ResultColumns  = columns,
                ResultSetError = columnError,
            });
        }

        return result;
    }

    private static async Task<List<(int objectId, string schemaName, string procedureName)>>
        ListProceduresAsync(SqlConnection connection, GeneratorOptions options, CancellationToken ct)
    {
        const string sql = """
            SELECT o.object_id, s.name AS schema_name, o.name AS procedure_name
            FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE o.type = 'P' AND o.is_ms_shipped = 0
            ORDER BY s.name, o.name
            """;

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<(int, string, string)>();

        while (await reader.ReadAsync(ct))
        {
            var objectId       = reader.GetInt32(0);
            var schemaName     = reader.GetString(1);
            var procedureName  = reader.GetString(2);

            if (!MatchesSchemas(schemaName, options.Schemas))
                continue;

            if (IsExcluded(procedureName, options.ExcludeProcedures))
                continue;

            result.Add((objectId, schemaName, procedureName));
        }

        return result;
    }

    private static async Task<IReadOnlyList<ParameterMetadata>> ReadParametersAsync(
        SqlConnection connection, int objectId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                p.name,
                TYPE_NAME(p.system_type_id) AS system_type_name,
                p.max_length,
                p.precision,
                p.scale,
                p.is_output
            FROM sys.parameters p
            WHERE p.object_id = @objectId AND p.parameter_id > 0
            ORDER BY p.parameter_id
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@objectId", objectId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<ParameterMetadata>();

        while (await reader.ReadAsync(ct))
        {
            result.Add(new ParameterMetadata
            {
                Name         = reader.GetString(0),
                SqlTypeName  = reader.GetString(1),
                MaxLength    = reader.GetInt16(2),
                Precision    = reader.GetByte(3),
                Scale        = reader.GetByte(4),
                IsOutput     = reader.GetBoolean(5),
            });
        }

        return result;
    }

    private static async Task<(IReadOnlyList<ResultColumnMetadata>? columns, string? error)>
        ReadResultColumnsAsync(
            SqlConnection connection,
            string schemaName,
            string procedureName,
            CancellationToken ct)
    {
        var tsql = $"EXEC [{schemaName}].[{procedureName}]";

        const string sql = """
            EXEC sp_describe_first_result_set @tsql = @tsql
            """;

        try
        {
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@tsql", tsql);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var columns = new List<ResultColumnMetadata>();

            while (await reader.ReadAsync(ct))
            {
                var nameOrd       = reader.GetOrdinal("name");
                var typeNameOrd   = reader.GetOrdinal("system_type_name");
                var nullableOrd   = reader.GetOrdinal("is_nullable");
                var maxLenOrd     = reader.GetOrdinal("max_length");
                var precisionOrd  = reader.GetOrdinal("precision");
                var scaleOrd      = reader.GetOrdinal("scale");

                var colName = reader.IsDBNull(nameOrd) ? $"Column{columns.Count}" : reader.GetString(nameOrd);

                // system_type_name may include length e.g. "nvarchar(100)" -- we keep the full name
                // and let TypeMapper.NormalizeTypeName strip the parens.
                var typeName  = reader.IsDBNull(typeNameOrd) ? "nvarchar" : reader.GetString(typeNameOrd);
                var nullable  = !reader.IsDBNull(nullableOrd) && reader.GetBoolean(nullableOrd);
                var maxLength = reader.IsDBNull(maxLenOrd) ? 0 : (int)reader.GetInt16(maxLenOrd);
                var precision = reader.IsDBNull(precisionOrd) ? (byte)0 : reader.GetByte(precisionOrd);
                var scale     = reader.IsDBNull(scaleOrd) ? (byte)0 : reader.GetByte(scaleOrd);

                columns.Add(new ResultColumnMetadata
                {
                    Name        = colName,
                    SqlTypeName = typeName,
                    IsNullable  = nullable,
                    MaxLength   = maxLength,
                    Precision   = precision,
                    Scale       = scale,
                });
            }

            if (columns.Count == 0)
                return (null, "sp_describe_first_result_set returned no columns (SP may have no result set or uses dynamic SQL).");

            return (columns, null);
        }
        catch (SqlException ex)
        {
            return (null, $"sp_describe_first_result_set failed: {ex.Message}");
        }
    }

    private static bool MatchesSchemas(string schemaName, string[] schemas)
    {
        if (schemas.Length == 0)
            return true;

        foreach (var s in schemas)
        {
            if (string.Equals(s, schemaName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsExcluded(string procedureName, string[] excludePatterns)
    {
        foreach (var pattern in excludePatterns)
        {
            if (pattern.EndsWith('*'))
            {
                var prefix = pattern[..^1];
                if (procedureName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else
            {
                if (string.Equals(procedureName, pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
