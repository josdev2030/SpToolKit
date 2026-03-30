using SpToolkit.Generator.Resolved;
using SpToolkit.Generator.Templates;
using Xunit;

namespace SpToolkit.Generator.Tests;

public sealed class ExecuteOnlyWrapperTemplateTests
{
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

    [Fact]
    public void RenderWrapper_ExecuteOnly_without_outputs_uses_void_execute_and_Task()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_Acme",
            BaseName           = "Acme",
            RequestClassName   = "AcmeRequest",
            ResponseClassName  = null,
            RowClassName       = null,
            MethodName         = "RunAsync",
            Pattern            = ExecutionPattern.ExecuteOnly,
            InputParameters    = [],
            OutputParameters   = [],
            ResultColumns      = null,
            HasInputParameters = true,
        };

        var renderer = new TemplateRenderer();
        var code     = renderer.RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("public Task RunAsync", code, StringComparison.Ordinal);
        Assert.Contains("_executor.ExecuteAsync<AcmeRequest>(", code, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteAsync<AcmeRequest, ", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Task<>", code, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWrapper_ExecuteOnly_without_inputs_uses_EmptyRequest_and_void_execute()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_Acme",
            BaseName           = "Acme",
            RequestClassName   = "EmptyRequest",
            ResponseClassName  = null,
            RowClassName       = null,
            MethodName         = "RunAsync",
            Pattern            = ExecutionPattern.ExecuteOnly,
            InputParameters    = [],
            OutputParameters   = [],
            ResultColumns      = null,
            HasInputParameters = false,
        };

        var renderer = new TemplateRenderer();
        var code     = renderer.RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("EmptyRequest.Instance", code, StringComparison.Ordinal);
        Assert.Contains("_executor.ExecuteAsync<EmptyRequest>", code, StringComparison.Ordinal);
        Assert.Contains("public Task RunAsync(", code, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWrapper_ExecuteOnly_with_outputs_uses_two_type_execute()
    {
        var proc = new ResolvedProcedure
        {
            FullName           = "dbo.SP_Acme",
            BaseName           = "Acme",
            RequestClassName   = "AcmeRequest",
            ResponseClassName  = "AcmeResponse",
            RowClassName       = null,
            MethodName         = "RunAsync",
            Pattern            = ExecutionPattern.ExecuteOnly,
            InputParameters    = [],
            OutputParameters   = [SampleOutputParam()],
            ResultColumns      = null,
            HasInputParameters = true,
        };

        var renderer = new TemplateRenderer();
        var code     = renderer.RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("Task<AcmeResponse>", code, StringComparison.Ordinal);
        Assert.Contains("_executor.ExecuteAsync<AcmeRequest, AcmeResponse>", code, StringComparison.Ordinal);
        Assert.DoesNotContain("_executor.ExecuteAsync<AcmeRequest>(", code, StringComparison.Ordinal);
    }
}
