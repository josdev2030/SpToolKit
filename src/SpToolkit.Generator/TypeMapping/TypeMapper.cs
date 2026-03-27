namespace SpToolkit.Generator.TypeMapping;

public sealed class TypeMappingResult
{
    public required string ClrTypeName { get; init; }
    public required string SqlDbTypeName { get; init; }
    public required bool NeedsSize { get; init; }
    public required string? DefaultValueExpression { get; init; }
    public bool IsKnownType { get; init; } = true;
}

public sealed class TypeMapper
{
    private sealed record TypeEntry(
        string ClrBase,
        string SqlDbTypeName,
        bool NeedsSize,
        string? NonNullableDefault);

    private static readonly Dictionary<string, TypeEntry> SqlToClr =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["int"]              = new("int",      "Int",              false, null),
            ["bigint"]           = new("long",     "BigInt",           false, null),
            ["smallint"]         = new("short",    "SmallInt",         false, null),
            ["tinyint"]          = new("byte",     "TinyInt",          false, null),
            ["bit"]              = new("bool",     "Bit",              false, null),
            ["decimal"]          = new("decimal",  "Decimal",          false, null),
            ["numeric"]          = new("decimal",  "Decimal",          false, null),
            ["float"]            = new("double",   "Float",            false, null),
            ["real"]             = new("float",    "Real",             false, null),
            ["nvarchar"]         = new("string",   "NVarChar",         true,  "string.Empty"),
            ["varchar"]          = new("string",   "VarChar",          true,  "string.Empty"),
            ["nchar"]            = new("string",   "NChar",            true,  "string.Empty"),
            ["char"]             = new("string",   "Char",             true,  "string.Empty"),
            ["text"]             = new("string",   "Text",             false, "string.Empty"),
            ["ntext"]            = new("string",   "NText",            false, "string.Empty"),
            ["datetime"]         = new("DateTime", "DateTime",         false, null),
            ["datetime2"]        = new("DateTime", "DateTime2",        false, null),
            ["smalldatetime"]    = new("DateTime", "SmallDateTime",    false, null),
            ["date"]             = new("DateOnly", "Date",             false, null),
            ["time"]             = new("TimeOnly", "Time",             false, null),
            ["uniqueidentifier"] = new("Guid",     "UniqueIdentifier", false, null),
            ["varbinary"]        = new("byte[]",   "VarBinary",        true,  "Array.Empty<byte>()"),
            ["binary"]           = new("byte[]",   "Binary",           true,  "Array.Empty<byte>()"),
            ["image"]            = new("byte[]",   "Image",            false, "Array.Empty<byte>()"),
            ["xml"]              = new("string",   "Xml",              false, "string.Empty"),
        };

    private static readonly TypeEntry UnknownEntry =
        new("object", "Variant", false, null);

    /// <summary>
    /// Maps a SQL type name and nullability to the C# type information needed for code generation.
    /// </summary>
    public TypeMappingResult Map(string sqlTypeName, bool isNullable)
    {
        var normalized = NormalizeTypeName(sqlTypeName);

        if (!SqlToClr.TryGetValue(normalized, out var entry))
            entry = UnknownEntry;

        string clrTypeName;
        string? defaultExpr;

        if (isNullable)
        {
            clrTypeName = entry.ClrBase + "?";
            defaultExpr = null;
        }
        else
        {
            clrTypeName = entry.ClrBase;
            defaultExpr = entry.NonNullableDefault;
        }

        return new TypeMappingResult
        {
            ClrTypeName            = clrTypeName,
            SqlDbTypeName          = entry.SqlDbTypeName,
            NeedsSize              = entry.NeedsSize,
            DefaultValueExpression = defaultExpr,
            IsKnownType            = entry != UnknownEntry,
        };
    }

    /// <summary>
    /// Converts max_length in bytes to the character size used in SqlParameter.Size and attributes.
    /// Returns -1 for MAX, 0 when the type does not use a size.
    /// </summary>
    public int ResolveSize(string sqlTypeName, int maxLengthBytes)
    {
        var normalized = NormalizeTypeName(sqlTypeName);

        return normalized switch
        {
            "nvarchar" or "nchar" => maxLengthBytes == -1 ? -1 : maxLengthBytes / 2,
            "varchar"  or "char"  => maxLengthBytes,
            "varbinary"           => maxLengthBytes,
            "binary"              => maxLengthBytes,
            _                     => 0,
        };
    }

    private static string NormalizeTypeName(string sqlTypeName)
    {
        var name = sqlTypeName.Trim();

        // sp_describe_first_result_set may return "nvarchar(100)" or "decimal(18,2)"
        var parenIdx = name.IndexOf('(', StringComparison.Ordinal);
        if (parenIdx >= 0)
            name = name[..parenIdx].TrimEnd();

        return name.ToLowerInvariant();
    }
}
