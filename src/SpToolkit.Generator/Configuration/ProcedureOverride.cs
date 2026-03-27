namespace SpToolkit.Generator.Configuration;

/// <summary>
/// Per-SP override applied during code generation.
/// Matched by exact name (e.g. "dbo.SP_REPORT") or wildcard prefix (e.g. "dbo.SP_INTERNAL_*").
/// </summary>
public sealed class ProcedureOverride
{
    /// <summary>
    /// SP name pattern to match against.
    /// Use exact name ("dbo.SP_REPORT") or a prefix wildcard ("dbo.SP_INTERNAL_*").
    /// </summary>
    public required string Procedure { get; set; }

    /// <summary>
    /// If true, this SP is skipped entirely during generation.
    /// Appears as "Excluded" in the generation report.
    /// </summary>
    public bool Exclude { get; set; }

    /// <summary>
    /// Forces a custom base class name, overriding the NamingService result.
    /// E.g. set "DynamicReport" to produce DynamicReportRequest, DynamicReportRow, etc.
    /// </summary>
    public string? BaseName { get; set; }

    /// <summary>
    /// Forces a custom method name in the generated wrapper class.
    /// E.g. set "GetAllUsersAsync" instead of the auto-generated name.
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Manually defines result columns for SPs where sp_describe_first_result_set fails
    /// (e.g. dynamic SQL). Format: "ColumnName:ClrType" per entry.
    /// Examples: "UserId:int", "Name:string", "Amount:decimal?", "CreatedAt:DateTime?"
    /// </summary>
    public string[]? ResultColumns { get; set; }
}
