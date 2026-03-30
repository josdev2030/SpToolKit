using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using SpToolkit.Abstractions.Attributes;
using SpToolkit.Abstractions.Models;
using SpToolkit.Abstractions.Options;
using SpToolkit.Runtime.Execution;
using Xunit;

namespace SpToolkit.Runtime.Tests;

public sealed class StoredProcedureExecutorExecuteTests
{
    /// <summary>Response shape with one output parameter (for generic ExecuteAsync regression).</summary>
    public sealed class ExecOutResponse
    {
        [SpOutput("@RowCount", SqlDbType.Int)]
        public int RowCount { get; set; }
    }

    [Fact]
    public async Task ExecuteAsync_TInput_only_binds_inputs_and_skips_output_parameters()
    {
        var connection = new RecordingDbConnection();
        var options    = new SpToolkitOptions();
        var executor     = new StoredProcedureExecutor(options, connection);

        await executor.ExecuteAsync("dbo.NoOutputProc", EmptyRequest.Instance);

        var cmd = connection.LastCommand;
        Assert.NotNull(cmd);
        Assert.Equal(1, cmd.ExecuteNonQueryCallCount);
        Assert.Equal(CommandType.StoredProcedure, cmd.CommandType);
        Assert.Equal("dbo.NoOutputProc", cmd.CommandText);

        var outputLike = cmd.Parameters.Cast<DbParameter>()
            .Count(p => p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput);
        Assert.Equal(0, outputLike);
    }

    [Fact]
    public async Task ExecuteAsync_TInput_TOutput_still_adds_output_parameters()
    {
        var connection = new RecordingDbConnection();
        var options    = new SpToolkitOptions();
        var executor     = new StoredProcedureExecutor(options, connection);

        await executor.ExecuteAsync<EmptyRequest, ExecOutResponse>("dbo.WithOutputs", EmptyRequest.Instance);

        var cmd = connection.LastCommand;
        Assert.NotNull(cmd);
        Assert.Equal(1, cmd.ExecuteNonQueryCallCount);

        var outputLike = cmd.Parameters.Cast<DbParameter>()
            .Count(p => p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput);
        Assert.True(outputLike >= 1, "Expected at least one output parameter on the command.");
    }
}
