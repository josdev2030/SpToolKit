using SpToolkit.Generator.Resolved;
using Xunit;

namespace SpToolkit.Generator.Tests;

public sealed class ExecutionPatternRulesTests
{
    [Theory]
    [InlineData(true, true, ExecutionPattern.QueryWithOutputs)]
    [InlineData(true, false, ExecutionPattern.Query)]
    [InlineData(false, true, ExecutionPattern.ExecuteOnly)]
    [InlineData(false, false, ExecutionPattern.ExecuteOnly)]
    public void DetermineDefaultPattern_maps_shape(bool hasResult, bool hasOutputs, ExecutionPattern expected)
    {
        var actual = ExecutionPatternRules.DetermineDefaultPattern(hasResult, hasOutputs);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(ExecutionPattern.Query)]
    [InlineData(ExecutionPattern.QuerySingle)]
    [InlineData(ExecutionPattern.QueryWithOutputs)]
    [InlineData(ExecutionPattern.QuerySingleWithOutputs)]
    public void ValidateExecutionPattern_throws_when_result_required_but_missing(ExecutionPattern pattern)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ExecutionPatternRules.ValidateExecutionPattern("dbo.SP_X", pattern, effectiveHasResultSet: false, hasOutputParameters: true));

        Assert.Contains("dbo.SP_X", ex.Message, StringComparison.Ordinal);
        Assert.Contains("result set", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateExecutionPattern_throws_when_ExecuteOnly_but_has_rows()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ExecutionPatternRules.ValidateExecutionPattern(
                "dbo.SP_X",
                ExecutionPattern.ExecuteOnly,
                effectiveHasResultSet: true,
                hasOutputParameters: false));

        Assert.Contains("dbo.SP_X", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ExecutionPattern.QueryWithOutputs)]
    [InlineData(ExecutionPattern.QuerySingleWithOutputs)]
    public void ValidateExecutionPattern_throws_when_outputs_required_but_missing(ExecutionPattern pattern)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ExecutionPatternRules.ValidateExecutionPattern("dbo.SP_X", pattern, effectiveHasResultSet: true, hasOutputParameters: false));

        Assert.Contains("output parameters", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ExecutionPattern.Query)]
    [InlineData(ExecutionPattern.QuerySingle)]
    public void ValidateExecutionPattern_throws_when_query_has_output_parameters(ExecutionPattern pattern)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ExecutionPatternRules.ValidateExecutionPattern("dbo.SP_X", pattern, effectiveHasResultSet: true, hasOutputParameters: true));

        Assert.Contains("output parameters", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateExecutionPattern_allows_ExecuteOnly_without_outputs()
    {
        ExecutionPatternRules.ValidateExecutionPattern(
            "dbo.SP_X",
            ExecutionPattern.ExecuteOnly,
            effectiveHasResultSet: false,
            hasOutputParameters: false);
    }

    [Theory]
    [InlineData(ExecutionPattern.Query)]
    [InlineData(ExecutionPattern.QuerySingle)]
    [InlineData(ExecutionPattern.QueryWithOutputs)]
    [InlineData(ExecutionPattern.QuerySingleWithOutputs)]
    public void ValidateExecutionPattern_allows_valid_query_shapes(ExecutionPattern pattern)
    {
        bool hasOutputs = pattern is ExecutionPattern.QueryWithOutputs or ExecutionPattern.QuerySingleWithOutputs;
        ExecutionPatternRules.ValidateExecutionPattern("dbo.SP_X", pattern, effectiveHasResultSet: true, hasOutputs);
    }

    [Fact]
    public void ValidateExecutionPattern_allows_ExecuteOnly_with_outputs_only()
    {
        ExecutionPatternRules.ValidateExecutionPattern(
            "dbo.SP_X",
            ExecutionPattern.ExecuteOnly,
            effectiveHasResultSet: false,
            hasOutputParameters: true);
    }
}
