using SpToolkit.Generator.Resolved;
using System.Text;

namespace SpToolkit.Generator.Templates;

public sealed class TemplateRenderer
{
    /// <summary>Generates the Request class file content.</summary>
    public string RenderRequest(string ns, ResolvedProcedure proc)
    {
        var sb = new StringBuilder();

        AppendHeader(sb);
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using SpToolkit.Abstractions.Attributes;");
        sb.AppendLine();
        AppendNamespace(sb, ns);
        sb.AppendLine();
        sb.AppendLine($"[SpProcedure(\"{proc.FullName}\")]");
        sb.AppendLine($"public sealed class {proc.RequestClassName}");
        sb.AppendLine("{");

        foreach (var p in proc.InputParameters)
        {
            sb.Append($"    {BuildSpInputAttribute(p)}");
            sb.AppendLine();
            sb.Append($"    public {p.ClrTypeName} {p.PropertyName} {{ get; set; }}");
            if (p.DefaultValueExpression is not null)
                sb.Append($" = {p.DefaultValueExpression};");
            sb.AppendLine();
            sb.AppendLine();
        }

        TrimTrailingBlankLine(sb);
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>Generates the Response class file content (output parameters).</summary>
    public string RenderResponse(string ns, ResolvedProcedure proc)
    {
        var sb = new StringBuilder();

        AppendHeader(sb);
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using SpToolkit.Abstractions.Attributes;");
        sb.AppendLine();
        AppendNamespace(sb, ns);
        sb.AppendLine();
        sb.AppendLine($"public sealed class {proc.ResponseClassName}");
        sb.AppendLine("{");

        foreach (var p in proc.OutputParameters)
        {
            sb.Append($"    {BuildSpOutputAttribute(p)}");
            sb.AppendLine();
            sb.Append($"    public {p.ClrTypeName} {p.PropertyName} {{ get; set; }}");
            if (p.DefaultValueExpression is not null)
                sb.Append($" = {p.DefaultValueExpression};");
            sb.AppendLine();
            sb.AppendLine();
        }

        TrimTrailingBlankLine(sb);
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>Generates the Row class file content (result set columns).</summary>
    public string RenderRow(string ns, ResolvedProcedure proc)
    {
        var sb = new StringBuilder();

        AppendHeader(sb);
        sb.AppendLine("using SpToolkit.Abstractions.Attributes;");
        sb.AppendLine();
        AppendNamespace(sb, ns);
        sb.AppendLine();
        sb.AppendLine($"public sealed class {proc.RowClassName}");
        sb.AppendLine("{");

        foreach (var col in proc.ResultColumns!)
        {
            sb.AppendLine($"    [SpResultColumn(\"{col.ColumnName}\")]");
            sb.Append($"    public {col.ClrTypeName} {col.PropertyName} {{ get; set; }}");
            if (col.DefaultValueExpression is not null)
                sb.Append($" = {col.DefaultValueExpression};");
            sb.AppendLine();
            sb.AppendLine();
        }

        TrimTrailingBlankLine(sb);
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>Generates the Wrapper partial class file content.</summary>
    public string RenderWrapper(string ns, string className, IReadOnlyList<ResolvedProcedure> procedures)
    {
        var sb = new StringBuilder();

        AppendHeader(sb);
        sb.AppendLine("using SpToolkit.Abstractions.Contracts;");
        sb.AppendLine("using SpToolkit.Abstractions.Models;");
        sb.AppendLine();
        AppendNamespace(sb, ns);
        sb.AppendLine();
        sb.AppendLine($"public sealed partial class {className}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IStoredProcedureExecutor _executor;");
        sb.AppendLine();
        sb.AppendLine($"    public {className}(IStoredProcedureExecutor executor)");
        sb.AppendLine("    {");
        sb.AppendLine("        _executor = executor ?? throw new ArgumentNullException(nameof(executor));");
        sb.AppendLine("    }");

        foreach (var proc in procedures)
        {
            sb.AppendLine();
            AppendWrapperMethod(sb, proc);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AppendWrapperMethod(StringBuilder sb, ResolvedProcedure proc)
    {
        var hasInput = proc.HasInputParameters;
        var ctParam  = "CancellationToken cancellationToken = default";

        string returnType;
        string executorCall;

        switch (proc.Pattern)
        {
            case ExecutionPattern.ExecuteOnly:
                if (proc.RequiresResponseType)
                {
                    if (proc.ResponseClassName is null)
                        throw new InvalidOperationException(
                            $"Procedure '{proc.FullName}': ExecuteOnly with outputs requires ResponseClassName.");

                    returnType   = $"Task<{proc.ResponseClassName}>";
                    executorCall = $"_executor.ExecuteAsync<{proc.RequestClassName}, {proc.ResponseClassName}>";
                }
                else
                {
                    returnType   = "Task";
                    executorCall = $"_executor.ExecuteAsync<{proc.RequestClassName}>";
                }

                break;

            case ExecutionPattern.Query:
                returnType   = $"Task<IReadOnlyList<{proc.RowClassName}>>";
                executorCall = $"_executor.QueryAsync<{proc.RequestClassName}, {proc.RowClassName}>";
                break;

            case ExecutionPattern.QuerySingle:
                returnType   = $"Task<{proc.RowClassName}?>";
                executorCall = $"_executor.QuerySingleAsync<{proc.RequestClassName}, {proc.RowClassName}>";
                break;

            case ExecutionPattern.QueryWithOutputs:
                returnType   = $"Task<SpResult<IReadOnlyList<{proc.RowClassName}>, {proc.ResponseClassName}>>";
                executorCall = $"_executor.QueryWithOutputsAsync<{proc.RequestClassName}, {proc.RowClassName}, {proc.ResponseClassName}>";
                break;

            case ExecutionPattern.QuerySingleWithOutputs:
                returnType   = $"Task<SpResult<{proc.RowClassName}?, {proc.ResponseClassName}>>";
                executorCall = $"_executor.QuerySingleWithOutputsAsync<{proc.RequestClassName}, {proc.RowClassName}, {proc.ResponseClassName}>";
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(proc.Pattern), proc.Pattern, "Unhandled ExecutionPattern value.");
        }

        string requestArg;
        string methodSig;

        if (hasInput)
        {
            requestArg = "request";
            methodSig  = $"public {returnType} {proc.MethodName}({proc.RequestClassName} request, {ctParam})";
        }
        else
        {
            requestArg = "EmptyRequest.Instance";
            methodSig  = $"public {returnType} {proc.MethodName}({ctParam})";
        }

        sb.AppendLine($"    {methodSig}");
        sb.AppendLine($"        => {executorCall}(\"{proc.FullName}\", {requestArg}, cancellationToken);");
    }

    private static void AppendHeader(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
    }

    private static void AppendNamespace(StringBuilder sb, string ns)
        => sb.AppendLine($"namespace {ns};");

    private static string BuildSpInputAttribute(ResolvedParameter p)
    {
        var sb = new StringBuilder();
        sb.Append($"[SpInput(\"{p.SqlParameterName}\", SqlDbType.{p.SqlDbTypeName}");
        AppendSizeArgs(sb, p);
        sb.Append(")]");
        return sb.ToString();
    }

    private static string BuildSpOutputAttribute(ResolvedParameter p)
    {
        var sb = new StringBuilder();
        sb.Append($"[SpOutput(\"{p.SqlParameterName}\", SqlDbType.{p.SqlDbTypeName}");
        AppendSizeArgs(sb, p);
        sb.Append(")]");
        return sb.ToString();
    }

    private static void AppendSizeArgs(StringBuilder sb, ResolvedParameter p)
    {
        if (p.Size != 0)
            sb.Append($", Size = {p.Size}");
        if (p.Precision > 0)
            sb.Append($", Precision = {p.Precision}");
        if (p.Scale > 0)
            sb.Append($", Scale = {p.Scale}");
    }

    private static void TrimTrailingBlankLine(StringBuilder sb)
    {
        // Remove the last blank line that was appended after the last property
        var nl = Environment.NewLine;
        if (sb.Length >= nl.Length * 2)
        {
            var tail = sb.ToString(sb.Length - nl.Length, nl.Length);
            if (tail == nl)
            {
                var preTail = sb.ToString(sb.Length - nl.Length * 2, nl.Length);
                if (preTail == nl)
                    sb.Remove(sb.Length - nl.Length, nl.Length);
            }
        }
    }
}
