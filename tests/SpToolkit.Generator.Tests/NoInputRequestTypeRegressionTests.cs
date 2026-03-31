using SpToolkit.Generator.Resolved;
using SpToolkit.Generator.Templates;
using Xunit;

namespace SpToolkit.Generator.Tests;

public sealed class NoInputRequestTypeRegressionTests
{
    public static TheoryData<ExecutionPattern, string, string> NoInputCases => new()
    {
        { ExecutionPattern.ExecuteOnly, "AcmeRequest", "_executor.ExecuteAsync<EmptyRequest>(" },
        { ExecutionPattern.Query, "AcmeRequest", "_executor.QueryAsync<EmptyRequest, AcmeRow>" },
        { ExecutionPattern.QuerySingle, "AcmeRequest", "_executor.QuerySingleAsync<EmptyRequest, AcmeRow>" },
        { ExecutionPattern.QueryWithOutputs, "AcmeRequest", "_executor.QueryWithOutputsAsync<EmptyRequest, AcmeRow, AcmeResponse>" },
        { ExecutionPattern.QuerySingleWithOutputs, "AcmeRequest", "_executor.QuerySingleWithOutputsAsync<EmptyRequest, AcmeRow, AcmeResponse>" },
    };

    [Theory]
    [MemberData(nameof(NoInputCases))]
    public void RenderWrapper_no_input_patterns_use_EmptyRequest_and_parameterless_signature(
        ExecutionPattern pattern,
        string requestClassName,
        string expectedExecutorCall)
    {
        var proc = BuildProcedure(pattern, hasInputParameters: false, requestClassName: requestClassName);

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains(expectedExecutorCall, code, StringComparison.Ordinal);
        Assert.Contains("EmptyRequest.Instance", code, StringComparison.Ordinal);
        Assert.Contains($"public {GetExpectedReturnType(pattern)} {proc.MethodName}(CancellationToken cancellationToken = default)", code, StringComparison.Ordinal);
        Assert.DoesNotContain($"{requestClassName} request", code, StringComparison.Ordinal);
        Assert.DoesNotContain($"<{requestClassName}", code, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderWrapper_execute_only_no_input_with_outputs_uses_EmptyRequest_and_no_request_parameter()
    {
        var proc = BuildProcedure(
            ExecutionPattern.ExecuteOnly,
            hasInputParameters: false,
            requestClassName: "AcmeRequest",
            includeOutputs: true);

        var code = new TemplateRenderer().RenderWrapper("TestNs", "AppSp", [proc]);

        Assert.Contains("_executor.ExecuteAsync<EmptyRequest, AcmeResponse>", code, StringComparison.Ordinal);
        Assert.Contains("EmptyRequest.Instance", code, StringComparison.Ordinal);
        Assert.Contains("public Task<AcmeResponse> AcmeAsync(CancellationToken cancellationToken = default)", code, StringComparison.Ordinal);
        Assert.DoesNotContain("AcmeRequest request", code, StringComparison.Ordinal);
    }

    private static ResolvedProcedure BuildProcedure(
        ExecutionPattern pattern,
        bool hasInputParameters,
        string requestClassName,
        bool includeOutputs = false)
    {
        IReadOnlyList<ResolvedParameter> outputParameters =
            (pattern is ExecutionPattern.QueryWithOutputs or ExecutionPattern.QuerySingleWithOutputs) || includeOutputs
                ? new[] { SampleOutputParam() }
                : Array.Empty<ResolvedParameter>();

        var responseClassName = outputParameters.Count > 0 ? "AcmeResponse" : null;
        var hasRows = pattern is not ExecutionPattern.ExecuteOnly;
        var rowClassName = hasRows ? "AcmeRow" : null;
        IReadOnlyList<ResolvedColumn>? resultColumns = hasRows ? new[] { SampleColumn() } : null;

        return new ResolvedProcedure
        {
            FullName           = "dbo.SP_Acme",
            BaseName           = "Acme",
            RequestClassName   = requestClassName,
            ResponseClassName  = responseClassName,
            RowClassName       = rowClassName,
            MethodName         = "AcmeAsync",
            Pattern            = pattern,
            InputParameters    = hasInputParameters ? new[] { SampleInputParam() } : Array.Empty<ResolvedParameter>(),
            OutputParameters   = outputParameters,
            ResultColumns      = resultColumns,
            HasInputParameters = hasInputParameters,
        };
    }

    private static string GetExpectedReturnType(ExecutionPattern pattern)
        => pattern switch
        {
            ExecutionPattern.ExecuteOnly           => "Task",
            ExecutionPattern.Query                 => "Task<IReadOnlyList<AcmeRow>>",
            ExecutionPattern.QuerySingle           => "Task<AcmeRow?>",
            ExecutionPattern.QueryWithOutputs      => "Task<SpResult<IReadOnlyList<AcmeRow>, AcmeResponse>>",
            ExecutionPattern.QuerySingleWithOutputs => "Task<SpResult<AcmeRow?, AcmeResponse>>",
            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unhandled pattern."),
        };

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
}
