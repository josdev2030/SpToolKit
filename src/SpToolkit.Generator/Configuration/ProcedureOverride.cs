using SpToolkit.Generator.Resolved;

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

    /// <summary>
    /// Optional override for the execution pattern used in the generated wrapper method.
    /// When set, the generator validates that the chosen pattern is compatible with the
    /// procedure shape; generation fails with a clear message on mismatch.
    /// <para>Allowed values (JSON strings, PascalCase):</para>
    /// <list type="bullet">
    ///   <item><description><c>ExecuteOnly</c> — no result set; output parameters optional (edge case: neither).</description></item>
    ///   <item><description><c>Query</c> — result set, no output parameters.</description></item>
    ///   <item><description><c>QuerySingle</c> — result set (at most one row), no output parameters.</description></item>
    ///   <item><description><c>QueryWithOutputs</c> — result set + output parameters.</description></item>
    ///   <item><description><c>QuerySingleWithOutputs</c> — result set (at most one row) + output parameters.</description></item>
    /// </list>
    /// </summary>
    public ExecutionPattern? ExecutionPattern { get; set; }
}
