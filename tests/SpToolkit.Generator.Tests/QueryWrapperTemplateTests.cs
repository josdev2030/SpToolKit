using SpToolkit.Generator.Resolved;
using SpToolkit.Generator.Templates;
using Xunit;

namespace SpToolkit.Generator.Tests;

public sealed class QueryWrapperTemplateTests
{
    private static ResolvedParameter SampleInputParam() =>
        new()
        {
            PropertyName           = "UserId",
            ClrTypeName            = "int",
            SqlParameterName       = "@UserId",
            SqlDbTypeName          = "Int",
            Size                   = 0,
            Precision              = 0,
            Scale                  = 0,
            DefaultValueExpression = null,
        };

    private static ResolvedParameter SampleOutputParam() =>
        new()
        {
            PropertyName           = "OutId",
            ClrTypeName            = "int",
            SqlParameterName       = "@OutId",
            SqlDbTypeName          = "Int",
            Size                   = 0,
            Precision              = 0,
            Scale                  = 0,
            DefaultValueExpression = null,
        };

    private static ResolvedColumn SampleColumn() =>
        new()
        {
            PropertyName           = "Name",
            ClrTypeName            = "string",
            ColumnName             = "Name",
            DefaultValueExpression = "string.Empty",
        };

    // ── Query ───────────────────────────────────────────────────

    [Fact]
    public void RenderWrapper_Query_without_inputs_uses_EmptyRequest()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_List",
            BaseName           = "List",
            RequestClassName   = "ListRequest",
            ResponseClassName  = null,
            RowClassName       = "ListRow",
            MethodName         = "ListAsync",
            Pattern            = ExecutionPattern.Query,
            InputParameters    = [],
            OutputParameters   = [],
            ResultColumns      = [SampleColumn()],
            HasInputParameters = false,
        };

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("_executor.QueryAsync<EmptyRequest, ListRow>", code, StringComparison.Ordinal);
        Assert.Contains("EmptyRequest.Instance", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ListRequest", code, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWrapper_Query_with_inputs_uses_RequestClassName()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_List",
            BaseName           = "List",
            RequestClassName   = "ListRequest",
            ResponseClassName  = null,
            RowClassName       = "ListRow",
            MethodName         = "ListAsync",
            Pattern            = ExecutionPattern.Query,
            InputParameters    = [SampleInputParam()],
            OutputParameters   = [],
            ResultColumns      = [SampleColumn()],
            HasInputParameters = true,
        };

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("_executor.QueryAsync<ListRequest, ListRow>", code, StringComparison.Ordinal);
        Assert.Contains("ListRequest request", code, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyRequest", code, StringComparison.Ordinal);
    }

    // ── QuerySingle ─────────────────────────────────────────────

    [Fact]
    public void RenderWrapper_QuerySingle_without_inputs_uses_EmptyRequest()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_Get",
            BaseName           = "Get",
            RequestClassName   = "GetRequest",
            ResponseClassName  = null,
            RowClassName       = "GetRow",
            MethodName         = "GetAsync",
            Pattern            = ExecutionPattern.QuerySingle,
            InputParameters    = [],
            OutputParameters   = [],
            ResultColumns      = [SampleColumn()],
            HasInputParameters = false,
        };

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("_executor.QuerySingleAsync<EmptyRequest, GetRow>", code, StringComparison.Ordinal);
        Assert.Contains("EmptyRequest.Instance", code, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequest", code, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWrapper_QuerySingle_with_inputs_uses_RequestClassName()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_Get",
            BaseName           = "Get",
            RequestClassName   = "GetRequest",
            ResponseClassName  = null,
            RowClassName       = "GetRow",
            MethodName         = "GetAsync",
            Pattern            = ExecutionPattern.QuerySingle,
            InputParameters    = [SampleInputParam()],
            OutputParameters   = [],
            ResultColumns      = [SampleColumn()],
            HasInputParameters = true,
        };

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("_executor.QuerySingleAsync<GetRequest, GetRow>", code, StringComparison.Ordinal);
        Assert.Contains("GetRequest request", code, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyRequest", code, StringComparison.Ordinal);
    }

    // ── QueryWithOutputs ────────────────────────────────────────

    [Fact]
    public void RenderWrapper_QueryWithOutputs_without_inputs_uses_EmptyRequest()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_Report",
            BaseName           = "Report",
            RequestClassName   = "ReportRequest",
            ResponseClassName  = "ReportResponse",
            RowClassName       = "ReportRow",
            MethodName         = "ReportAsync",
            Pattern            = ExecutionPattern.QueryWithOutputs,
            InputParameters    = [],
            OutputParameters   = [SampleOutputParam()],
            ResultColumns      = [SampleColumn()],
            HasInputParameters = false,
        };

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("_executor.QueryWithOutputsAsync<EmptyRequest, ReportRow, ReportResponse>", code, StringComparison.Ordinal);
        Assert.Contains("EmptyRequest.Instance", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportRequest", code, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWrapper_QueryWithOutputs_with_inputs_uses_RequestClassName()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_Report",
            BaseName           = "Report",
            RequestClassName   = "ReportRequest",
            ResponseClassName  = "ReportResponse",
            RowClassName       = "ReportRow",
            MethodName         = "ReportAsync",
            Pattern            = ExecutionPattern.QueryWithOutputs,
            InputParameters    = [SampleInputParam()],
            OutputParameters   = [SampleOutputParam()],
            ResultColumns      = [SampleColumn()],
            HasInputParameters = true,
        };

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("_executor.QueryWithOutputsAsync<ReportRequest, ReportRow, ReportResponse>", code, StringComparison.Ordinal);
        Assert.Contains("ReportRequest request", code, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyRequest", code, StringComparison.Ordinal);
    }

    // ── QuerySingleWithOutputs ──────────────────────────────────

    [Fact]
    public void RenderWrapper_QuerySingleWithOutputs_without_inputs_uses_EmptyRequest()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_Detail",
            BaseName           = "Detail",
            RequestClassName   = "DetailRequest",
            ResponseClassName  = "DetailResponse",
            RowClassName       = "DetailRow",
            MethodName         = "DetailAsync",
            Pattern            = ExecutionPattern.QuerySingleWithOutputs,
            InputParameters    = [],
            OutputParameters   = [SampleOutputParam()],
            ResultColumns      = [SampleColumn()],
            HasInputParameters = false,
        };

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("_executor.QuerySingleWithOutputsAsync<EmptyRequest, DetailRow, DetailResponse>", code, StringComparison.Ordinal);
        Assert.Contains("EmptyRequest.Instance", code, StringComparison.Ordinal);
        Assert.DoesNotContain("DetailRequest", code, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWrapper_QuerySingleWithOutputs_with_inputs_uses_RequestClassName()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_Detail",
            BaseName           = "Detail",
            RequestClassName   = "DetailRequest",
            ResponseClassName  = "DetailResponse",
            RowClassName       = "DetailRow",
            MethodName         = "DetailAsync",
            Pattern            = ExecutionPattern.QuerySingleWithOutputs,
            InputParameters    = [SampleInputParam()],
            OutputParameters   = [SampleOutputParam()],
            ResultColumns      = [SampleColumn()],
            HasInputParameters = true,
        };

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("_executor.QuerySingleWithOutputsAsync<DetailRequest, DetailRow, DetailResponse>", code, StringComparison.Ordinal);
        Assert.Contains("DetailRequest request", code, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyRequest", code, StringComparison.Ordinal);
    }
}
